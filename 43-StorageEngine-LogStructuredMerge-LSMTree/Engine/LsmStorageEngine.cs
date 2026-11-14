using KiranaHub.LsmStorage.Core;

namespace KiranaHub.LsmStorage.Engine;

/// <summary>
/// Log-Structured Merge-Tree (LSM-Tree) Storage Engine.
/// Provides high-throughput sequential disk writes, crash-resilient WAL,
/// in-memory MemTable, immutable SSTables with Sparse Index & Bloom Filters, and background compaction.
/// </summary>
public sealed class LsmStorageEngine : IDisposable
{
    // [KYA KARTA HAI]: Storage engine ka root data folder path store karta hai.
    // [KYU ZAROORI HAI]: WAL files aur SSTables ko disk par organize karne ke liye.
    // [KAISE CHALTA HAI]: Constructor me provided directory path save karta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable root directory path.
    // [VARIATIONS TO TRY]: Multi-disk striped paths (JBOD).
    private readonly string _dataDirectory;

    // [KYA KARTA HAI]: Flush threshold byte limit define karta hai.
    // [KYU ZAROORI HAI]: MemTable memory limit control karna taaki out-of-memory na ho.
    // [KAISE CHALTA HAI]: Default 256KB ya user-configured byte limit.
    // [OUTPUT / SIDE EFFECT]: Threshold boundary value.
    // [VARIATIONS TO TRY]: Dynamic adaptive threshold based on available system RAM.
    private readonly long _flushThresholdBytes;

    // [KYA KARTA HAI]: Write-Ahead Log instance jo crashes se bachata hai.
    // [KYU ZAROORI HAI]: Durability guarantee (ACID D).
    // [KAISE CHALTA HAI]: Har write pehle WAL me append hoti hai.
    // [OUTPUT / SIDE EFFECT]: Append-only disk log handle.
    // [VARIATIONS TO TRY]: Async non-blocking ring-buffered WAL.
    private readonly WriteAheadLog _wal;

    // [KYA KARTA HAI]: Active mutable in-memory table.
    // [KYU ZAROORI HAI]: New writes aur updates ko O(log N) sorted RAM me absorb karna.
    // [KAISE CHALTA HAI]: MemTable instance.
    // [OUTPUT / SIDE EFFECT]: Fast in-memory state.
    // [VARIATIONS TO TRY]: Double buffering with active and draining MemTables.
    private MemTable _activeMemTable = new();

    // [KYA KARTA HAI]: Disk par maujood SSTables ki ordered list (newest to oldest).
    // [KYU ZAROORI HAI]: Point lookup me sabse taaza SSTable pehle search hona chahiye (shadowing).
    // [KAISE CHALTA HAI]: List of active SsTable instances.
    // [OUTPUT / SIDE EFFECT]: In-memory handles to immutable disk files.
    // [VARIATIONS TO TRY]: Multi-level LSM hierarchy (L0, L1, L2 tiered compaction).
    private readonly List<SsTable> _ssTables = new();

    // [KYA KARTA HAI]: Thread synchronization lock for flushes and compactions.
    // [KYU ZAROORI HAI]: Race conditions aur concurrent table corruption se bachana.
    // [KAISE CHALTA HAI]: lock(_engineLock) pattern.
    // [OUTPUT / SIDE EFFECT]: Thread-safe engine mutations.
    // [VARIATIONS TO TRY]: Async SemaphoreSlim for non-blocking asynchronous flushes.
    private readonly object _engineLock = new();

    // [KYA KARTA HAI]: Counter jo track karta hai kitne disk seeks Bloom filter ne prevent kiye.
    // [KYU ZAROORI HAI]: Bloom filter efficiency telemetry demonstrate karna.
    // [KAISE CHALTA HAI]: Query miss hone par counter atomic increment hota hai.
    // [OUTPUT / SIDE EFFECT]: Diagnostic integer metric.
    // [VARIATIONS TO TRY]: Ratio of bloom hits vs false positives.
    private long _bloomFilterSavedDiskReads;

    // [KYA KARTA HAI]: Total flush events count track karta hai.
    // [KYU ZAROORI HAI]: Memory-to-disk lifecycle statistics.
    // [KAISE CHALTA HAI]: FlushMemTableInternal me increment hota hai.
    // [OUTPUT / SIDE EFFECT]: Flush count metric.
    // [VARIATIONS TO TRY]: Rate of flushes per minute.
    private int _flushCount;

    // [KYA KARTA HAI]: LsmStorageEngine initialize karta hai, existing SSTables load karta hai, aur WAL replay karta hai.
    // [KYU ZAROORI HAI]: Database boot aur crash recovery execution.
    // [KAISE CHALTA HAI]: Directory setup, SSTable discovery, aur WAL recovery pipeline chalata hai.
    // [OUTPUT / SIDE EFFECT]: Fully operational storage engine ready for traffic.
    // [VARIATIONS TO TRY]: Background async warm-up of bloom filters.
    public LsmStorageEngine(string dataDirectory, long flushThresholdBytes = 256 * 1024)
    {
        _dataDirectory = dataDirectory;
        _flushThresholdBytes = flushThresholdBytes;

        Directory.CreateDirectory(_dataDirectory);
        string sstDir = Path.Combine(_dataDirectory, "sstables");
        Directory.CreateDirectory(sstDir);

        // [KYA KARTA HAI]: Existing SSTables discover aur load karta hai.
        // [KYU ZAROORI HAI]: Previous run ka persistent data accessible banana.
        // [KAISE CHALTA HAI]: Disk par maujood .sst files ko sort karke Open karta hai.
        // [OUTPUT / SIDE EFFECT]: _ssTables populated.
        // [VARIATIONS TO TRY]: Parallel SSTable loading via Task.WhenAll.
        LoadExistingSsTables(sstDir);

        // [KYA KARTA HAI]: Active WAL open karta hai.
        // [KYU ZAROORI HAI]: Uncommitted memory updates crash resilience.
        // [KAISE CHALTA HAI]: WriteAheadLog instantiation.
        // [OUTPUT / SIDE EFFECT]: _wal handle active.
        // [VARIATIONS TO TRY]: Dedicated WAL disk volume.
        string walDir = Path.Combine(_dataDirectory, "wal");
        _wal = new WriteAheadLog(walDir, "wal.log");

        // [KYA KARTA HAI]: Crash recovery: WAL ke un-flushed records ko MemTable me replay karta hai.
        // [KYU ZAROORI HAI]: Sudden shutdown me jo data disk par flush nahi hua tha use restore karna.
        // [KAISE CHALTA HAI]: ReplayLog sequentially iterates and inserts into _activeMemTable.
        // [OUTPUT / SIDE EFFECT]: MemTable restored to pre-crash state.
        // [VARIATIONS TO TRY]: Point-in-time recovery using timestamps.
        ReplayWalLog(Path.Combine(walDir, "wal.log"));
    }

    // [KYA KARTA HAI]: Disk se existing SSTables scan karke memory me attach karta hai.
    // [KYU ZAROORI HAI]: Database restart par purana data restore karna.
    // [KAISE CHALTA HAI]: File creation time ya numerical filename ke hisab se descending order me sort karta hai.
    // [OUTPUT / SIDE EFFECT]: _ssTables list populated from newest to oldest.
    // [VARIATIONS TO TRY]: Manifest file tracking SSTable levels.
    private void LoadExistingSsTables(string sstDir)
    {
        var files = Directory.GetFiles(sstDir, "*.sst")
                             .OrderByDescending(f => f)
                             .ToList();

        foreach (var file in files)
        {
            try
            {
                var sst = SsTable.Open(file);
                _ssTables.Add(sst);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LSM WARNING] Skipping corrupt SSTable file {file}: {ex.Message}");
            }
        }
    }

    // [KYA KARTA HAI]: WAL file replay karke MemTable reconstruct karta hai.
    // [KYU ZAROORI HAI]: Crash resilience guarantee (Zero data loss).
    // [KAISE CHALTA HAI]: ReplayLog se iterate karke Put ya Delete call karta hai.
    // [OUTPUT / SIDE EFFECT]: _activeMemTable hydrated.
    // [VARIATIONS TO TRY]: Log truncation post recovery.
    private void ReplayWalLog(string walFile)
    {
        int replayedCount = 0;
        foreach (var entry in WriteAheadLog.ReplayLog(walFile))
        {
            if (entry.IsTombstone)
            {
                _activeMemTable.Delete(entry.Key, entry.Timestamp);
            }
            else
            {
                _activeMemTable.Put(entry.Key, entry.Value, entry.Timestamp);
            }
            replayedCount++;
        }

        if (replayedCount > 0)
        {
            Console.WriteLine($"[LSM RECOVERY] Replayed {replayedCount} operations from Write-Ahead Log into MemTable!");
        }
    }

    // [KYA KARTA HAI]: Key-Value pair write karta hai (WAL append -> MemTable put -> Flush check).
    // [KYU ZAROORI HAI]: LSM write path ka primary high-throughput entry point.
    // [KAISE CHALTA HAI]: Sequential append in WAL, insertion in RAM, and conditional flush to disk.
    // [OUTPUT / SIDE EFFECT]: Durable write committed; MemTable size grows.
    // [VARIATIONS TO TRY]: Batch Put API for multi-key atomicity.
    public void Put(string key, string value)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        lock (_engineLock)
        {
            // 1. Durability first: append to WAL
            _wal.AppendPut(key, value, timestamp);

            // 2. High-speed RAM indexing: insert into MemTable
            _activeMemTable.Put(key, value, timestamp);

            // 3. Threshold check: flush to disk if MemTable size limit reached
            if (_activeMemTable.EstimatedSizeBytes >= _flushThresholdBytes)
            {
                FlushMemTableInternal();
            }
        }
    }

    // [KYA KARTA HAI]: Key ko delete karta hai tombstone marker place karke.
    // [KYU ZAROORI HAI]: LSM tree me in-place rewrite prohibited hota hai; tombstone sequential append hota hai.
    // [KAISE CHALTA HAI]: WAL delete append + MemTable tombstone placement.
    // [OUTPUT / SIDE EFFECT]: Key soft-deleted; reads will return null.
    // [VARIATIONS TO TRY]: Purge expired tombstones during compaction.
    public void Delete(string key)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        lock (_engineLock)
        {
            _wal.AppendDelete(key, timestamp);
            _activeMemTable.Delete(key, timestamp);

            if (_activeMemTable.EstimatedSizeBytes >= _flushThresholdBytes)
            {
                FlushMemTableInternal();
            }
        }
    }

    // [KYA KARTA HAI]: Key lookup karta hai across Memory, WAL, aur disk SSTables.
    // [KYU ZAROORI HAI]: LSM tree ka multi-tier unified read path.
    // [KAISE CHALTA HAI]: MemTable -> SSTables (newest to oldest using Bloom filter & sparse index).
    // [OUTPUT / SIDE EFFECT]: Returns string value if active, or null if deleted/absent.
    // [VARIATIONS TO TRY]: Multi-get batch point lookup API.
    public string? Get(string key)
    {
        lock (_engineLock)
        {
            // Tier 1: Check active in-memory MemTable (Fastest O(log N) RAM read)
            if (_activeMemTable.TryGet(key, out var memEntry))
            {
                return memEntry!.IsTombstone ? null : memEntry.Value;
            }

            // Tier 2: Check SSTables from newest to oldest
            foreach (var sst in _ssTables)
            {
                if (sst.TryGet(key, out var record))
                {
                    // Found key in this SSTable!
                    // If it is a tombstone, it was deleted after older SSTables were written
                    return record.IsTombstone ? null : record.Value;
                }
                else
                {
                    // Bloom filter saved us a disk seek or binary search!
                    Interlocked.Increment(ref _bloomFilterSavedDiskReads);
                }
            }

            return null; // Key does not exist anywhere
        }
    }

    // [KYA KARTA HAI]: MemTable ko manually ya threshold reach hone par disk par flush karta hai as SSTable.
    // [KYU ZAROORI HAI]: RAM memory release karna aur data ko durable immutable SSTable me convert karna.
    // [KAISE CHALTA HAI]: Sorted entries snapshot lekar SsTable.Create call karta hai aur WAL reset karta hai.
    // [OUTPUT / SIDE EFFECT]: New .sst file written; WAL size reset to 0 bytes.
    // [VARIATIONS TO TRY]: Background flush thread using ConcurrentQueue.
    public void Flush()
    {
        lock (_engineLock)
        {
            FlushMemTableInternal();
        }
    }

    // [KYA KARTA HAI]: Internal flush implementation.
    // [KYU ZAROORI HAI]: Clean separation between public API and locked internal operation.
    // [KAISE CHALTA HAI]: Entries extract karta hai, SSTable generate karta hai, aur WAL clear karta hai.
    // [OUTPUT / SIDE EFFECT]: SSTable added to head of _ssTables list.
    // [VARIATIONS TO TRY]: Asynchronous worker thread flush.
    private void FlushMemTableInternal()
    {
        if (_activeMemTable.Count == 0)
        {
            return;
        }

        var sortedEntries = _activeMemTable.GetSortedEntries();
        _flushCount++;

        string sstDir = Path.Combine(_dataDirectory, "sstables");
        string sstFileName = $"sst_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{_flushCount:D4}.sst";
        string sstFilePath = Path.Combine(sstDir, sstFileName);

        // [KYA KARTA HAI]: Naya immutable SSTable disk par generate karta hai.
        // [KYU ZAROORI HAI]: Sequential zero-fragmentation sorted file banata hai with Bloom filter.
        // [KAISE CHALTA HAI]: SsTable.Create streaming binary writer.
        // [OUTPUT / SIDE EFFECT]: File saved on disk.
        // [VARIATIONS TO TRY]: Parallel compression chunks.
        var newSsTable = SsTable.Create(sstFilePath, sortedEntries, sparseIndexInterval: 16);

        // [KYA KARTA HAI]: Naye SSTable ko index ke sabse aage (newest) insert karta hai.
        // [KYU ZAROORI HAI]: Query routing me taaza SSTables pehle probe honi chahiye.
        // [KAISE CHALTA HAI]: _ssTables.Insert(0, newSsTable).
        // [OUTPUT / SIDE EFFECT]: Newest table at index 0.
        // [VARIATIONS TO TRY]: Level-based bucket assignment.
        _ssTables.Insert(0, newSsTable);

        // [KYA KARTA HAI]: MemTable clear karta hai aur WAL truncate karta hai.
        // [KYU ZAROORI HAI]: RAM reclaim karna aur redundant WAL entries remove karna.
        // [KAISE CHALTA HAI]: MemTable.Clear() + WAL.Reset().
        // [OUTPUT / SIDE EFFECT]: Empty MemTable, 0-byte WAL.
        // [VARIATIONS TO TRY]: WAL segment file deletion.
        _activeMemTable.Clear();
        _wal.Reset();
    }

    // [KYA KARTA HAI]: Multiple SSTables ko merge karke single compacted SSTable banata hai.
    // [KYU ZAROORI HAI]: Obsolete overwritten versions aur dead tombstones ko permanently purge karna (Space reclamation).
    // [KAISE CHALTA HAI]: K-way merge sort karta hai; newest timestamp rakhta hai aur purane files delete karta hai.
    // [OUTPUT / SIDE EFFECT]: Merged single SSTable replaces old fragmented SSTables.
    // [VARIATIONS TO TRY]: Leveled Compaction (LCS) vs Size-Tiered Compaction (STCS).
    public void Compact()
    {
        lock (_engineLock)
        {
            if (_ssTables.Count <= 1)
            {
                return; // Nothing to compact
            }

            // K-way merge using dictionary (sorted by key)
            var merged = new SortedDictionary<string, MemTableEntry>(StringComparer.Ordinal);

            // Read from oldest to newest so newest overwrites oldest!
            for (int i = _ssTables.Count - 1; i >= 0; i--)
            {
                var sst = _ssTables[i];
                foreach (var record in sst.ScanAll())
                {
                    if (merged.TryGetValue(record.Key, out var existing))
                    {
                        if (record.Timestamp >= existing.Timestamp)
                        {
                            merged[record.Key] = new MemTableEntry(record.Key, record.Value, record.Timestamp, record.IsTombstone);
                        }
                    }
                    else
                    {
                        merged[record.Key] = new MemTableEntry(record.Key, record.Value, record.Timestamp, record.IsTombstone);
                    }
                }
            }

            // Purge tombstones during major compaction: deleted keys disappear completely!
            var activeEntries = merged.Values.Where(e => !e.IsTombstone).ToList();

            string sstDir = Path.Combine(_dataDirectory, "sstables");
            string compactedFileName = $"sst_compacted_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sst";
            string compactedFilePath = Path.Combine(sstDir, compactedFileName);

            var compactedTable = SsTable.Create(compactedFilePath, activeEntries, sparseIndexInterval: 16);

            // Dispose and delete old SSTable files
            var oldFiles = _ssTables.Select(s => s.FilePath).ToList();
            foreach (var sst in _ssTables)
            {
                sst.Dispose();
            }
            _ssTables.Clear();

            foreach (var oldFile in oldFiles)
            {
                try { File.Delete(oldFile); } catch { /* best effort */ }
            }

            // Register single compacted table
            _ssTables.Add(compactedTable);
        }
    }

    // [KYA KARTA HAI]: Diagnostic metrics aur storage engine stats return karta hai.
    // [KYU ZAROORI HAI]: Performance monitoring aur dashboard observability.
    // [KAISE CHALTA HAI]: Aggregates internal state fields into EngineStats record.
    // [OUTPUT / SIDE EFFECT]: Immutable EngineStats instance.
    // [VARIATIONS TO TRY]: JSON serialization for Prometheus/OpenTelemetry scraping.
    public EngineStats GetStats()
    {
        lock (_engineLock)
        {
            long totalSstBytes = _ssTables.Sum(s => s.FileSizeBytes);
            int totalSstEntries = _ssTables.Sum(s => s.EntryCount);

            return new EngineStats(
                MemTableEntries: _activeMemTable.Count,
                MemTableSizeBytes: _activeMemTable.EstimatedSizeBytes,
                SSTableCount: _ssTables.Count,
                TotalSSTableBytes: totalSstBytes,
                TotalSSTableEntries: totalSstEntries,
                FlushCount: _flushCount,
                BloomFilterSavedDiskReads: Volatile.Read(ref _bloomFilterSavedDiskReads));
        }
    }

    // [KYA KARTA HAI]: Engine resources cleanly close karta hai.
    // [KYU ZAROORI HAI]: File handles release karna aur crash-safe shutdown ensure karna.
    // [KAISE CHALTA HAI]: Disposes WAL and all open SSTable handles.
    // [OUTPUT / SIDE EFFECT]: Handles closed.
    // [VARIATIONS TO TRY]: Auto-flush on dispose.
    public void Dispose()
    {
        lock (_engineLock)
        {
            _wal.Dispose();
            foreach (var sst in _ssTables)
            {
                sst.Dispose();
            }
            _ssTables.Clear();
        }
    }
}

/// <summary>
/// Telemetry statistics model for the LSM Storage Engine.
/// </summary>
public sealed record EngineStats(
    int MemTableEntries,
    long MemTableSizeBytes,
    int SSTableCount,
    long TotalSSTableBytes,
    int TotalSSTableEntries,
    int FlushCount,
    long BloomFilterSavedDiskReads);
