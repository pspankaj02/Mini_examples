using System.Text;

namespace KiranaHub.LsmStorage.Core;

/// <summary>
/// Immutable Sorted String Table (SSTable) file format.
/// Stores sorted key-value pairs sequentially with an in-memory Sparse Index and Bloom Filter.
/// Disk layout: [Magic] -> [Data Block] -> [Sparse Index Block] -> [Bloom Filter Block] -> [Footer].
/// </summary>
public sealed class SsTable : IDisposable
{
    private const ulong MagicHeader = 0x5453414E4152494B; // "KIRANAST" in little endian hex
    private const ulong MagicFooter = 0x4B4952414E414855; // "KIRANAHU" in little endian hex

    // [KYA KARTA HAI]: SSTable file ka absolute filesystem path store karta hai.
    // [KYU ZAROORI HAI]: Point lookups aur range scans ke liye disk seeks perform karna.
    // [KAISE CHALTA HAI]: FileInfo path string save karta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable path string.
    // [VARIATIONS TO TRY]: Relative paths with working directory base.
    private readonly string _filePath;

    // [KYA KARTA HAI]: In-memory Bloom Filter reference jo write ke waqt compute hua tha.
    // [KYU ZAROORI HAI]: 99% non-existent keys ko instant O(1) me reject karna without single disk seek.
    // [KAISE CHALTA HAI]: MayContain(key) check karta hai before any disk seek.
    // [OUTPUT / SIDE EFFECT]: Memory-resident probabilistic filter.
    // [VARIATIONS TO TRY]: Partitioned block-level bloom filters.
    private readonly BloomFilter _bloomFilter;

    // [KYA KARTA HAI]: In-memory Sparse Index (Sorted List of Key and FileByteOffset pairs).
    // [KYU ZAROORI HAI]: Full file RAM me load kiye bina binary search se exact data block locate karna.
    // [KAISE CHALTA HAI]: Har K-th key (e.g. har 32nd key) ka byte offset memory me store karta hai.
    // [OUTPUT / SIDE EFFECT]: Compact in-memory navigation index.
    // [VARIATIONS TO TRY]: Two-level B-tree index block.
    private readonly List<SparseIndexEntry> _sparseIndex;

    // [KYA KARTA HAI]: Table me total records count store karta hai.
    // [KYU ZAROORI HAI]: Size estimation aur statistics ke liye.
    // [KAISE CHALTA HAI]: Header/Footer read karke assign hota hai.
    // [OUTPUT / SIDE EFFECT]: Total items count integer.
    // [VARIATIONS TO TRY]: MinKey and MaxKey boundary properties.
    private readonly int _entryCount;

    // [KYA KARTA HAI]: Data block ka starting byte offset in file.
    // [KYU ZAROORI HAI]: Header ke baad pehla record kahan se shuru hota hai pata karna.
    // [KAISE CHALTA HAI]: Standard 8-byte magic header offset = 8.
    // [OUTPUT / SIDE EFFECT]: Base data offset.
    // [VARIATIONS TO TRY]: Configurable header size.
    private const long DataBlockStartOffset = 8;

    // [KYA KARTA HAI]: Private constructor jo Open ya Create se instantiate hota hai.
    // [KYU ZAROORI HAI]: Immutable state ko validate aur encapsulate rakhna.
    // [KAISE CHALTA HAI]: Fields ko directly assign karta hai.
    // [OUTPUT / SIDE EFFECT]: Initialized SsTable instance.
    // [VARIATIONS TO TRY]: Factory pattern with async file opening.
    private SsTable(string filePath, BloomFilter bloomFilter, List<SparseIndexEntry> sparseIndex, int entryCount)
    {
        _filePath = filePath;
        _bloomFilter = bloomFilter;
        _sparseIndex = sparseIndex;
        _entryCount = entryCount;
    }

    // [KYA KARTA HAI]: Sorted MemTable entries se naya SSTable file sequentially disk par generate karta hai.
    // [KYU ZAROORI HAI]: Memory overflow se bachne aur data ko durable immutable format me persist karne ke liye.
    // [KAISE CHALTA HAI]: Sequential streaming write of Data -> Index -> Bloom -> Footer.
    // [OUTPUT / SIDE EFFECT]: Disk file created; in-memory SsTable handle returned.
    // [VARIATIONS TO TRY]: Block-level LZ4 compression for data chunks.
    public static SsTable Create(string filePath, IEnumerable<MemTableEntry> sortedEntries, int sparseIndexInterval = 32)
    {
        var entriesList = sortedEntries as IList<MemTableEntry> ?? sortedEntries.ToList();
        var bloomFilter = new BloomFilter(Math.Max(100, entriesList.Count), 0.01);
        var sparseIndex = new List<SparseIndexEntry>();

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536);
        using var writer = new BinaryWriter(fs, Encoding.UTF8);

        // [KYA KARTA HAI]: 8-byte Magic Header write karta hai.
        // [KYU ZAROORI HAI]: File type validation aur corruption detection.
        // [KAISE CHALTA HAI]: Writer.Write(MagicHeader).
        // [OUTPUT / SIDE EFFECT]: 8 bytes written.
        // [VARIATIONS TO TRY]: Versioned magic header (e.g., KIRANAV1).
        writer.Write(MagicHeader);

        int counter = 0;
        foreach (var entry in entriesList)
        {
            // [KYA KARTA HAI]: Har K-th key ka file offset sparse index me record karta hai.
            // [KYU ZAROORI HAI]: Binary search anchor points establish karna.
            // [KAISE CHALTA HAI]: fs.Position inspect karke SparseIndexEntry add karta hai.
            // [OUTPUT / SIDE EFFECT]: Sparse index list grows by 1.
            // [VARIATIONS TO TRY]: Dynamic interval based on byte size instead of key count.
            if (counter % sparseIndexInterval == 0)
            {
                sparseIndex.Add(new SparseIndexEntry(entry.Key, fs.Position));
            }

            // [KYA KARTA HAI]: Key ko Bloom Filter me register karta hai.
            // [KYU ZAROORI HAI]: Fast presence rejection testing.
            // [KAISE CHALTA HAI]: bloomFilter.Add(entry.Key).
            // [OUTPUT / SIDE EFFECT]: Bloom bits updated.
            // [VARIATIONS TO TRY]: Exclude tombstone keys if tombstone is terminal.
            bloomFilter.Add(entry.Key);

            byte[] keyBytes = Encoding.UTF8.GetBytes(entry.Key);
            byte[] valBytes = Encoding.UTF8.GetBytes(entry.Value);

            // [KYA KARTA HAI]: Data record sequentially write karta hai.
            // [KYU ZAROORI HAI]: Core storage persistence format.
            // [KAISE CHALTA HAI]: KeyLen + Key + ValLen + Val + Timestamp + TombstoneByte.
            // [OUTPUT / SIDE EFFECT]: Bytes written to disk file stream.
            // [VARIATIONS TO TRY]: Delta encoding on sorted keys to save prefix bytes.
            writer.Write(keyBytes.Length);
            writer.Write(keyBytes);
            writer.Write(valBytes.Length);
            writer.Write(valBytes);
            writer.Write(entry.Timestamp);
            writer.Write((byte)(entry.IsTombstone ? 1 : 0));

            counter++;
        }

        long indexBlockOffset = fs.Position;

        // [KYA KARTA HAI]: Sparse index block serialize karta hai.
        // [KYU ZAROORI HAI]: Table restart/open ke waqt sparse index instantly load karne ke liye.
        // [KAISE CHALTA HAI]: Count likhta hai followed by key string and offset long.
        // [OUTPUT / SIDE EFFECT]: Index block written to disk.
        // [VARIATIONS TO TRY]: Binary search tree serialization.
        writer.Write(sparseIndex.Count);
        foreach (var item in sparseIndex)
        {
            byte[] kBytes = Encoding.UTF8.GetBytes(item.Key);
            writer.Write(kBytes.Length);
            writer.Write(kBytes);
            writer.Write(item.FileOffset);
        }

        long bloomBlockOffset = fs.Position;

        // [KYA KARTA HAI]: Bloom filter serialized bits write karta hai.
        // [KYU ZAROORI HAI]: Table load par bloom filter reconstruct karna.
        // [KAISE CHALTA HAI]: HashCount + ByteLength + RawByteBuffer.
        // [OUTPUT / SIDE EFFECT]: Bloom filter block written.
        // [VARIATIONS TO TRY]: Bit vector compression.
        byte[] bloomBits = bloomFilter.GetRawBits();
        writer.Write(bloomFilter.HashFunctionCount);
        writer.Write(bloomBits.Length);
        writer.Write(bloomBits);

        // [KYA KARTA HAI]: Fixed 32-byte Footer metadata likhta hai file ke aakhri bytes me.
        // [KYU ZAROORI HAI]: File open karte waqt footer se index aur bloom filter offsets instantly mil sakein.
        // [KAISE CHALTA HAI]: 8 bytes IndexOffset + 8 bytes BloomOffset + 8 bytes EntryCount + 8 bytes MagicFooter.
        // [OUTPUT / SIDE EFFECT]: 32 bytes written at tail.
        // [VARIATIONS TO TRY]: Include checksum of entire file in footer.
        writer.Write(indexBlockOffset);
        writer.Write(bloomBlockOffset);
        writer.Write((long)entriesList.Count);
        writer.Write(MagicFooter);

        writer.Flush();
        fs.Flush(flushToDisk: true);

        return new SsTable(filePath, bloomFilter, sparseIndex, entriesList.Count);
    }

    // [KYA KARTA HAI]: Disk par maujood SSTable file ko open karta hai aur index RAM me load karta hai.
    // [KYU ZAROORI HAI]: Database boot ya crash recovery ke waqt existing tables attach karna.
    // [KAISE CHALTA HAI]: Footer se offsets read karke sparse index aur bloom filter memory me load karta hai.
    // [OUTPUT / SIDE EFFECT]: Ready-to-query SsTable instance.
    // [VARIATIONS TO TRY]: Lazy loading of sparse index on first query.
    public static SsTable Open(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"SSTable file not found: {filePath}");
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs, Encoding.UTF8);

        // [KYA KARTA HAI]: Magic header check karta hai.
        // [KYU ZAROORI HAI]: Validate valid SSTable binary file.
        // [KAISE CHALTA HAI]: ReadUInt64 comparison.
        // [OUTPUT / SIDE EFFECT]: Throws InvalidDataException if mismatched.
        // [VARIATIONS TO TRY]: Soft warning for backward compatible formats.
        ulong header = reader.ReadUInt64();
        if (header != MagicHeader)
        {
            throw new InvalidDataException($"Corrupted SSTable Header in {filePath}");
        }

        // [KYA KARTA HAI]: Footer seek karta hai (file length - 32 bytes).
        // [KYU ZAROORI HAI]: Footer metadata read karna.
        // [KAISE CHALTA HAI]: fs.Seek(-32, SeekOrigin.End).
        // [OUTPUT / SIDE EFFECT]: File pointer at start of footer.
        // [VARIATIONS TO TRY]: Dynamic length footers.
        fs.Seek(-32, SeekOrigin.End);
        long indexOffset = reader.ReadInt64();
        long bloomOffset = reader.ReadInt64();
        int entryCount = (int)reader.ReadInt64();
        ulong footer = reader.ReadUInt64();

        if (footer != MagicFooter)
        {
            throw new InvalidDataException($"Corrupted SSTable Footer in {filePath}");
        }

        // [KYA KARTA HAI]: Sparse Index block read karke memory me load karta hai.
        // [KYU ZAROORI HAI]: Fast binary search lookup routing.
        // [KAISE CHALTA HAI]: Seek to indexOffset and deserialize entries.
        // [OUTPUT / SIDE EFFECT]: _sparseIndex populated in RAM.
        // [VARIATIONS TO TRY]: Binary format parsing via Span<byte>.
        fs.Seek(indexOffset, SeekOrigin.Begin);
        int indexCount = reader.ReadInt32();
        var sparseIndex = new List<SparseIndexEntry>(indexCount);
        for (int i = 0; i < indexCount; i++)
        {
            int kLen = reader.ReadInt32();
            byte[] kBytes = reader.ReadBytes(kLen);
            long offset = reader.ReadInt64();
            sparseIndex.Add(new SparseIndexEntry(Encoding.UTF8.GetString(kBytes), offset));
        }

        // [KYA KARTA HAI]: Bloom Filter block read karta hai.
        // [KYU ZAROORI HAI]: Zero-disk-seek rejection capability restore karna.
        // [KAISE CHALTA HAI]: Seek to bloomOffset and reconstruct BloomFilter object.
        // [OUTPUT / SIDE EFFECT]: _bloomFilter active in RAM.
        // [VARIATIONS TO TRY]: Share Bloom filters across readers.
        fs.Seek(bloomOffset, SeekOrigin.Begin);
        int hashCount = reader.ReadInt32();
        int bloomByteLen = reader.ReadInt32();
        byte[] bloomBytes = reader.ReadBytes(bloomByteLen);
        var bloomFilter = new BloomFilter(bloomBytes, hashCount);

        return new SsTable(filePath, bloomFilter, sparseIndex, entryCount);
    }

    // [KYA KARTA HAI]: Key SSTable me locate karta hai using Bloom Filter + Sparse Index + Disk Seek.
    // [KYU ZAROORI HAI]: LSM tree ka main disk point lookup mechanism.
    // [KAISE CHALTA HAI]: Bloom filter -> Sparse index binary search -> Local sequential scan on disk.
    // [OUTPUT / SIDE EFFECT]: Returns true and record if found; false if absent.
    // [VARIATIONS TO TRY]: Disk block cache (LRU cache) to avoid repeated disk reads.
    public bool TryGet(string key, out SsTableRecord record)
    {
        record = default;

        // [KYA KARTA HAI]: O(1) Bloom filter check run karta hai.
        // [KYU ZAROORI HAI]: Agar key nahi hai toh disk seek 100% bypass ho jaye!
        // [KAISE CHALTA HAI]: In-memory bit array inspects hash bits.
        // [OUTPUT / SIDE EFFECT]: Immediate false exit on miss (Zero disk I/O).
        // [VARIATIONS TO TRY]: Increment metric counter for bloom filter savings.
        if (!_bloomFilter.MayContain(key))
        {
            return false;
        }

        // [KYA KARTA HAI]: Sparse index me binary search karke candidate seek window locate karta hai.
        // [KYU ZAROORI HAI]: Exact byte offset range identify karna jahan key reside karti hai.
        // [KAISE CHALTA HAI]: Finds entry where IndexKey <= SearchKey.
        // [OUTPUT / SIDE EFFECT]: Seek offset and stop offset determined.
        // [VARIATIONS TO TRY]: BinarySearch with custom IComparer.
        long startOffset = DataBlockStartOffset;
        long endOffset = _sparseIndex.Count > 0 ? _sparseIndex[0].FileOffset : DataBlockStartOffset;

        int low = 0;
        int high = _sparseIndex.Count - 1;
        int targetIdx = -1;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            int cmp = string.CompareOrdinal(_sparseIndex[mid].Key, key);

            if (cmp <= 0)
            {
                targetIdx = mid;
                low = mid + 1; // look for higher lower-bound
            }
            else
            {
                high = mid - 1;
            }
        }

        if (targetIdx >= 0)
        {
            startOffset = _sparseIndex[targetIdx].FileOffset;
            endOffset = (targetIdx + 1 < _sparseIndex.Count)
                ? _sparseIndex[targetIdx + 1].FileOffset
                : _sparseIndex[targetIdx].FileOffset + (1024 * 1024); // safe window
        }

        // [KYA KARTA HAI]: Targeted disk byte window sequentially scan karta hai.
        // [KYU ZAROORI HAI]: Sparse index ke do points ke beech exact key search karna.
        // [KAISE CHALTA HAI]: FileStream seek to startOffset and reads consecutive records.
        // [OUTPUT / SIDE EFFECT]: Record populated if key matches.
        // [VARIATIONS TO TRY]: Memory mapped file view accessor.
        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096);
        using var reader = new BinaryReader(fs, Encoding.UTF8);

        fs.Seek(startOffset, SeekOrigin.Begin);

        while (fs.Position < fs.Length)
        {
            if (fs.Position >= endOffset && targetIdx >= 0 && targetIdx + 1 < _sparseIndex.Count)
            {
                // Reached next sparse index point without finding key -> Key does not exist!
                break;
            }

            int keyLen;
            try
            {
                keyLen = reader.ReadInt32();
            }
            catch (EndOfStreamException) { break; }

            byte[] keyBytes = reader.ReadBytes(keyLen);
            int valLen = reader.ReadInt32();
            byte[] valBytes = reader.ReadBytes(valLen);
            long timestamp = reader.ReadInt64();
            byte tombstoneByte = reader.ReadByte();

            string currentKey = Encoding.UTF8.GetString(keyBytes);
            int cmp = string.CompareOrdinal(currentKey, key);

            if (cmp == 0)
            {
                // Found exact key!
                string val = Encoding.UTF8.GetString(valBytes);
                record = new SsTableRecord(currentKey, val, timestamp, tombstoneByte == 1);
                return true;
            }
            else if (cmp > 0)
            {
                // Passed lexicographical position; key cannot exist in sorted table!
                break;
            }
        }

        return false;
    }

    // [KYA KARTA HAI]: SSTable ke saare records ko stream karta hai sorted order me.
    // [KYU ZAROORI HAI]: Compaction engine me multiple SSTables ko k-way merge karne ke liye.
    // [KAISE CHALTA HAI]: Data block sequential streaming from start to index offset.
    // [OUTPUT / SIDE EFFECT]: Yields SsTableRecord items.
    // [VARIATIONS TO TRY]: Range scan filters with startKey and endKey parameters.
    public IEnumerable<SsTableRecord> ScanAll()
    {
        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536);
        using var reader = new BinaryReader(fs, Encoding.UTF8);

        // Read footer to know where data block ends (indexOffset)
        fs.Seek(-32, SeekOrigin.End);
        long indexOffset = reader.ReadInt64();

        fs.Seek(DataBlockStartOffset, SeekOrigin.Begin);

        while (fs.Position < indexOffset)
        {
            int keyLen = reader.ReadInt32();
            byte[] keyBytes = reader.ReadBytes(keyLen);
            int valLen = reader.ReadInt32();
            byte[] valBytes = reader.ReadBytes(valLen);
            long timestamp = reader.ReadInt64();
            byte tombstoneByte = reader.ReadByte();

            string currentKey = Encoding.UTF8.GetString(keyBytes);
            string val = Encoding.UTF8.GetString(valBytes);

            yield return new SsTableRecord(currentKey, val, timestamp, tombstoneByte == 1);
        }
    }

    // [KYA KARTA HAI]: SSTable file size (bytes) return karta hai.
    // [KYU ZAROORI HAI]: Storage capacity monitoring aur compaction triggers ke liye.
    // [KAISE CHALTA HAI]: FileInfo.Length inspect karta hai.
    // [OUTPUT / SIDE EFFECT]: File size in bytes.
    // [VARIATIONS TO TRY]: Cached size.
    public long FileSizeBytes => new FileInfo(_filePath).Length;

    // [KYA KARTA HAI]: Total records count property.
    // [KYU ZAROORI HAI]: Engine statistics display karne ke liye.
    // [KAISE CHALTA HAI]: Field getter.
    // [OUTPUT / SIDE EFFECT]: _entryCount integer.
    // [VARIATIONS TO TRY]: Active vs Tombstone breakdown.
    public int EntryCount => _entryCount;

    // [KYA KARTA HAI]: File path property.
    // [KYU ZAROORI HAI]: Compaction ke baad purane files delete karne ke liye.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: Returns _filePath.
    // [VARIATIONS TO TRY]: Canonical file URI.
    public string FilePath => _filePath;

    // [KYA KARTA HAI]: IDisposable implementation for clean architecture.
    // [KYU ZAROORI HAI]: SSTable lifetime management.
    // [KAISE CHALTA HAI]: Empty as streams are opened on-demand per query.
    // [OUTPUT / SIDE EFFECT]: Safe disposal.
    // [VARIATIONS TO TRY]: Close open mapped view accessors.
    public void Dispose()
    {
        // On-demand streams disposed per operation
    }
}

/// <summary>
/// Record read from SSTable data block.
/// </summary>
public readonly record struct SsTableRecord(string Key, string Value, long Timestamp, bool IsTombstone);

/// <summary>
/// In-memory entry pointing a key to its byte offset in the SSTable file.
/// </summary>
public readonly record struct SparseIndexEntry(string Key, long FileOffset);
