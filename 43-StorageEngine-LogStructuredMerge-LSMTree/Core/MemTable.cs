namespace KiranaHub.LsmStorage.Core;

/// <summary>
/// In-Memory Sorted Storage Table (MemTable).
/// High-throughput writes accumulate here in RAM in sorted order (O(log N)).
/// Once size exceeds threshold (e.g. 1MB), it becomes immutable and is flushed to disk as an SSTable.
/// </summary>
public sealed class MemTable
{
    // [KYA KARTA HAI]: Sorted dictionary jo in-memory key-value pairs ko lexically sorted order me maintain karta hai.
    // [KYU ZAROORI HAI]: Flush ke waqt O(N) linear sequential write karne ke liye data RAM me already sorted hona chahiye.
    // [KAISE CHALTA HAI]: Red-Black balanced search tree based SortedDictionary.
    // [OUTPUT / SIDE EFFECT]: In-memory indexed tree structure.
    // [VARIATIONS TO TRY]: SkipList implementation for lock-free multi-threaded concurrency.
    private readonly SortedDictionary<string, MemTableEntry> _map = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Reader-Writer lock concurrent readers aur exclusive writers ke liye.
    // [KYU ZAROORI HAI]: Multiple threads can read concurrently without blocking each other.
    // [KAISE CHALTA HAI]: Slim RW lock prevents thread contention.
    // [OUTPUT / SIDE EFFECT]: High-throughput read parallelism.
    // [VARIATIONS TO TRY]: Monitor lock or concurrent skiplist without locks.
    private readonly ReaderWriterLockSlim _rwLock = new();

    // [KYA KARTA HAI]: MemTable ka approximate memory footprint (bytes) track karta hai.
    // [KYU ZAROORI HAI]: Storage engine ko pata chal sake kab MemTable flush threshold (e.g. 1MB) cross karta hai.
    // [KAISE CHALTA HAI]: Har insert par string lengths aur object overhead add karta hai.
    // [OUTPUT / SIDE EFFECT]: _estimatedSizeBytes counter increments.
    // [VARIATIONS TO TRY]: GC.GetAllocatedBytesForCurrentThread() profiling.
    private long _estimatedSizeBytes;

    // [KYA KARTA HAI]: Key-Value pair insert ya update karta hai MemTable me.
    // [KYU ZAROORI HAI]: Write path ka in-memory commit point (O(log N) fast insertion).
    // [KAISE CHALTA HAI]: Exclusive write lock acquire karta hai aur map update karta hai.
    // [OUTPUT / SIDE EFFECT]: Map size increments, estimated bytes incremented.
    // [VARIATIONS TO TRY]: Upsert batching.
    public void Put(string key, string value, long timestamp)
    {
        _rwLock.EnterWriteLock();
        try
        {
            var entry = new MemTableEntry(key, value, timestamp, IsTombstone: false);

            if (_map.TryGetValue(key, out var oldEntry))
            {
                // [KYA KARTA HAI]: Purane value ka byte delta subtract karta hai.
                // [KYU ZAROORI HAI]: Accurate memory estimation maintain rakhne ke liye.
                // [KAISE CHALTA HAI]: Subtract old value length.
                // [OUTPUT / SIDE EFFECT]: Memory accounting accuracy.
                // [VARIATIONS TO TRY]: Precise object size calculation with unsafe sizeof.
                _estimatedSizeBytes -= (oldEntry.Value.Length * 2);
            }
            else
            {
                // [KYA KARTA HAI]: New key insertion overhead add karta hai.
                // [KYU ZAROORI HAI]: Tree node pointer overhead aur key string memory record karna.
                // [KAISE CHALTA HAI]: Key length * 2 (UTF-16 char size) + 48 bytes node overhead.
                // [OUTPUT / SIDE EFFECT]: Size counter updated.
                // [VARIATIONS TO TRY]: UTF-8 byte length estimation.
                _estimatedSizeBytes += (key.Length * 2) + 48;
            }

            _map[key] = entry;
            _estimatedSizeBytes += (value.Length * 2);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    // [KYA KARTA HAI]: Key par tombstone (soft delete marker) place karta hai.
    // [KYU ZAROORI HAI]: LSM tree me direct in-place delete nahi hota; tombstone record likha jata hai jo reads par key mask kare.
    // [KAISE CHALTA HAI]: IsTombstone = true ke sath empty value store karta hai.
    // [OUTPUT / SIDE EFFECT]: Key marked as deleted in memory.
    // [VARIATIONS TO TRY]: TTL-based automatic eviction of expired items.
    public void Delete(string key, long timestamp)
    {
        _rwLock.EnterWriteLock();
        try
        {
            var entry = new MemTableEntry(key, string.Empty, timestamp, IsTombstone: true);

            if (!_map.ContainsKey(key))
            {
                _estimatedSizeBytes += (key.Length * 2) + 48;
            }

            _map[key] = entry;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    // [KYA KARTA HAI]: In-memory key lookup karta hai.
    // [KYU ZAROORI HAI]: Read path ka sabse pehla stop: most recent updates yahin se serve hoti hain.
    // [KAISE CHALTA HAI]: Shared read lock ke under O(log N) tree search karta hai.
    // [OUTPUT / SIDE EFFECT]: True agar key mili (whether active or tombstone).
    // [VARIATIONS TO TRY]: Bloom filter before in-memory search for extremely large tables.
    public bool TryGet(string key, out MemTableEntry? entry)
    {
        _rwLock.EnterReadLock();
        try
        {
            return _map.TryGetValue(key, out entry);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    // [KYA KARTA HAI]: MemTable ke saare sorted entries snapshot ke taur par return karta hai.
    // [KYU ZAROORI HAI]: Disk par SSTable write karte waqt sequential sorted array chahiye hota hai.
    // [KAISE CHALTA HAI]: Read lock ke under sorted values array copy banata hai.
    // [OUTPUT / SIDE EFFECT]: Array of sorted MemTableEntry objects.
    // [VARIATIONS TO TRY]: Yield return with read lock held if streaming directly to disk.
    public MemTableEntry[] GetSortedEntries()
    {
        _rwLock.EnterReadLock();
        try
        {
            var result = new MemTableEntry[_map.Count];
            _map.Values.CopyTo(result, 0);
            return result;
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    // [KYA KARTA HAI]: MemTable ko clear karke memory reset karta hai.
    // [KYU ZAROORI HAI]: SSTable flush successful hone ke baad active table ko clean karne ke liye.
    // [KAISE CHALTA HAI]: Map clear and byte counter set to 0.
    // [OUTPUT / SIDE EFFECT]: Empty MemTable ready for fresh writes.
    // [VARIATIONS TO TRY]: Create fresh MemTable instance and let old one GC.
    public void Clear()
    {
        _rwLock.EnterWriteLock();
        try
        {
            _map.Clear();
            _estimatedSizeBytes = 0;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    // [KYA KARTA HAI]: Current estimated memory footprint expose karta hai.
    // [KYU ZAROORI HAI]: Auto-flush threshold trigger evaluate karne ke liye.
    // [KAISE CHALTA HAI]: Interlocked or volatile read of _estimatedSizeBytes.
    // [OUTPUT / SIDE EFFECT]: Size in bytes.
    // [VARIATIONS TO TRY]: Expose MB representation.
    public long EstimatedSizeBytes => Volatile.Read(ref _estimatedSizeBytes);

    // [KYA KARTA HAI]: Total items count return karta hai.
    // [KYU ZAROORI HAI]: Telemetry aur health checks.
    // [KAISE CHALTA HAI]: Map count under read lock.
    // [OUTPUT / SIDE EFFECT]: Total keys count.
    // [VARIATIONS TO TRY]: Atomic count field.
    public int Count
    {
        get
        {
            _rwLock.EnterReadLock();
            try { return _map.Count; }
            finally { _rwLock.ExitReadLock(); }
        }
    }
}

/// <summary>
/// In-memory entry holding payload, metadata, and deletion state.
/// </summary>
public sealed record MemTableEntry(string Key, string Value, long Timestamp, bool IsTombstone);
