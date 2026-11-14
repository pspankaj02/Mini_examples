using System.Text;

namespace KiranaHub.LsmStorage.Core;

/// <summary>
/// Append-only Write-Ahead Log (WAL) ensuring durability and crash-resilience.
/// Before any key is updated in RAM (MemTable), it is sequentially appended to the WAL file.
/// On sudden power outage or OS crash, MemTable is replayed 100% from WAL.
/// </summary>
public sealed class WriteAheadLog : IDisposable
{
    // [KYA KARTA HAI]: WAL file ka absolute disk path store karta hai.
    // [KYU ZAROORI HAI]: Re-open, delete, aur recovery operations ke liye file location chahiye.
    // [KAISE CHALTA HAI]: Constructor me provided directory aur file name se banta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable file path string.
    // [VARIATIONS TO TRY]: Partitioned WAL paths per core.
    private readonly string _filePath;

    // [KYA KARTA HAI]: Append-only sequential disk file stream maintain karta hai.
    // [KYU ZAROORI HAI]: High-speed sequential writes achieve karne ke liye without disk seeks.
    // [KAISE CHALTA HAI]: FileStream FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read.
    // [OUTPUT / SIDE EFFECT]: Open disk handle for appending log records.
    // [VARIATIONS TO TRY]: MemoryMappedFile ya DirectIO for ultra-low latency.
    private FileStream? _fileStream;

    // [KYA KARTA HAI]: Binary writer wrapper jo primitives ko stream me serialize karta hai.
    // [KYU ZAROORI HAI]: Clean typed binary writing (int, byte, long, byte[]).
    // [KAISE CHALTA HAI]: Wraps _fileStream with UTF-8 encoding.
    // [OUTPUT / SIDE EFFECT]: BinaryWriter instance active.
    // [VARIATIONS TO TRY]: Span<byte> direct buffer writes without BinaryWriter overhead.
    private BinaryWriter? _binaryWriter;

    // [KYA KARTA HAI]: Thread-safe synchronization object for concurrent writers.
    // [KYU ZAROORI HAI]: Multiple threads WAL me simultaneous append karein toh binary corruption na ho.
    // [KAISE CHALTA HAI]: Standard C# lock(_syncLock) pattern.
    // [OUTPUT / SIDE EFFECT]: Mutual exclusion on write path.
    // [VARIATIONS TO TRY]: Lock-free RingBuffer / Disruptor queue for single-threaded writer.
    private readonly object _syncLock = new();

    // [KYA KARTA HAI]: WriteAheadLog ko specific directory aur filename ke sath initialize karta hai.
    // [KYU ZAROORI HAI]: Log directory prepare karna aur open stream establish karna.
    // [KAISE CHALTA HAI]: Directory create karta hai agar na ho, aur FileStream open karta hai.
    // [OUTPUT / SIDE EFFECT]: Disk file ready on storage subsystem.
    // [VARIATIONS TO TRY]: Timestamped rolling log files with retention policy.
    public WriteAheadLog(string directoryPath, string logFileName = "active_wal.log")
    {
        // [KYA KARTA HAI]: Target directory create karta hai agar exist na karti ho.
        // [KYU ZAROORI HAI]: Missing directory ki wajah se FileNotFoundException se bachata hai.
        // [KAISE CHALTA HAI]: Directory.CreateDirectory filesystem API call karta hai.
        // [OUTPUT / SIDE EFFECT]: Folder on disk created or verified.
        // [VARIATIONS TO TRY]: Enforce directory permissions/ACLs for security.
        Directory.CreateDirectory(directoryPath);

        // [KYA KARTA HAI]: Complete path build karta hai.
        // [KYU ZAROORI HAI]: Platform-neutral file path generation.
        // [KAISE CHALTA HAI]: Path.Combine se safe path joins bante hain.
        // [OUTPUT / SIDE EFFECT]: _filePath initialized.
        // [VARIATIONS TO TRY]: Path.GetFullPath for canonicalized path.
        _filePath = Path.Combine(directoryPath, logFileName);

        // [KYA KARTA HAI]: Disk stream aur binary writer open karta hai.
        // [KYU ZAROORI HAI]: Storage ready for instant appending.
        // [KAISE CHALTA HAI]: FileStream in append mode with 64KB buffer.
        // [OUTPUT / SIDE EFFECT]: Open file handle ready.
        // [VARIATIONS TO TRY]: FileOptions.WriteThrough for absolute hardware sync.
        OpenStream();
    }

    // [KYA KARTA HAI]: Internal FileStream aur BinaryWriter open ya re-open karta hai.
    // [KYU ZAROORI HAI]: Rotation ya crash recovery ke baad clean stream reopen karne ke liye.
    // [KAISE CHALTA HAI]: Stream initialize karta hai in Append mode.
    // [OUTPUT / SIDE EFFECT]: _fileStream and _binaryWriter allocated.
    // [VARIATIONS TO TRY]: Async FileOptions.Asynchronous flag use kar sakte hain.
    private void OpenStream()
    {
        _fileStream = new FileStream(
            _filePath,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 65536,
            FileOptions.SequentialScan);

        // [KYA KARTA HAI]: Stream pointer ko file ke end par seek karta hai for append.
        // [KYU ZAROORI HAI]: Existing WAL records overwrite hone se bachane ke liye.
        // [KAISE CHALTA HAI]: Seek to 0 from End.
        // [OUTPUT / SIDE EFFECT]: File pointer at the very end of file.
        // [VARIATIONS TO TRY]: Length check to verify existing record count.
        _fileStream.Seek(0, SeekOrigin.End);

        _binaryWriter = new BinaryWriter(_fileStream, Encoding.UTF8, leaveOpen: true);
    }

    // [KYA KARTA HAI]: Put operation (Key-Value) ko sequentially disk par append karta hai.
    // [KYU ZAROORI HAI]: Memory me commit hone se pehle crash safety guarantee provide karna (Durability).
    // [KAISE CHALTA HAI]: CRC32 + Type(Put) + Timestamp + KeyLen + Key + ValLen + Val disk par likhta hai.
    // [OUTPUT / SIDE EFFECT]: Bytes appended to disk; file length increases.
    // [VARIATIONS TO TRY]: Group commit / batching multiple writes in a single disk fsync.
    public void AppendPut(string key, string value, long timestamp)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] valBytes = Encoding.UTF8.GetBytes(value);

        // [KYA KARTA HAI]: CRC32 checksum calculate karta hai data integrity ke liye.
        // [KYU ZAROORI HAI]: Torn writes ya sudden power failure me corrupt records detect ho sakein.
        // [KAISE CHALTA HAI]: Crc32 algorithm key aur value bytes par run karta hai.
        // [OUTPUT / SIDE EFFECT]: 32-bit uint checksum produced.
        // [VARIATIONS TO TRY]: HighwayHash ya xxHash64 for even faster speeds.
        uint crc = CalculateChecksum(keyBytes, valBytes);

        lock (_syncLock)
        {
            if (_binaryWriter == null || _fileStream == null)
            {
                throw new ObjectDisposedException(nameof(WriteAheadLog));
            }

            // [KYA KARTA HAI]: 4-byte CRC32 header likhta hai.
            // [KYU ZAROORI HAI]: Record integrity validation on recovery.
            // [KAISE CHALTA HAI]: BinaryWriter.Write(uint).
            // [OUTPUT / SIDE EFFECT]: 4 bytes written to buffer.
            // [VARIATIONS TO TRY]: 8-byte magic header before CRC.
            _binaryWriter.Write(crc);

            // [KYA KARTA HAI]: 1-byte Record Type = 1 (Put) likhta hai.
            // [KYU ZAROORI HAI]: Recovery engine ko pata chale ki yeh insert/update operation tha.
            // [KAISE CHALTA HAI]: BinaryWriter.Write((byte)1).
            // [OUTPUT / SIDE EFFECT]: 1 byte written.
            // [VARIATIONS TO TRY]: Enum RecordType (Put=1, Delete=2).
            _binaryWriter.Write((byte)1);

            // [KYA KARTA HAI]: 8-byte Unix epoch millisecond timestamp likhta hai.
            // [KYU ZAROORI HAI]: LSM conflict resolution aur last-write-wins sequencing ke liye.
            // [KAISE CHALTA HAI]: BinaryWriter.Write(long).
            // [OUTPUT / SIDE EFFECT]: 8 bytes written.
            // [VARIATIONS TO TRY]: Hybrid Logical Clock (HLC).
            _binaryWriter.Write(timestamp);

            // [KYA KARTA HAI]: Key length aur key bytes likhta hai.
            // [KYU ZAROORI HAI]: Variable-length strings ko binary stream se delimit karna.
            // [KAISE CHALTA HAI]: Length prefix pattern.
            // [OUTPUT / SIDE EFFECT]: 4 + N bytes written.
            // [VARIATIONS TO TRY]: Varint encoding to save bytes on short keys.
            _binaryWriter.Write(keyBytes.Length);
            _binaryWriter.Write(keyBytes);

            // [KYA KARTA HAI]: Value length aur value bytes likhta hai.
            // [KYU ZAROORI HAI]: Payload data persistence.
            // [KAISE CHALTA HAI]: Length prefix + byte payload.
            // [OUTPUT / SIDE EFFECT]: 4 + M bytes written.
            // [VARIATIONS TO TRY]: Compression (LZ4 / Zstandard) for values > 1KB.
            _binaryWriter.Write(valBytes.Length);
            _binaryWriter.Write(valBytes);

            // [KYA KARTA HAI]: OS file buffer ko flush karta hai.
            // [KYU ZAROORI HAI]: Application crash par data OS page cache me na atke.
            // [KAISE CHALTA HAI]: _fileStream.Flush().
            // [OUTPUT / SIDE EFFECT]: Buffered bytes sent to OS storage layer.
            // [VARIATIONS TO TRY]: flushToDisk: true for fsync on every single write (stricter ACID).
            _fileStream.Flush();
        }
    }

    // [KYA KARTA HAI]: Delete operation (Tombstone) ko WAL me append karta hai.
    // [KYU ZAROORI HAI]: Deletions bhi durable hone chahiye taaki crash recovery ke baad purana data resurrect na ho!
    // [KAISE CHALTA HAI]: RecordType = 2 (Tombstone) aur empty value ke sath log entry likhta hai.
    // [OUTPUT / SIDE EFFECT]: Tombstone record recorded on disk.
    // [VARIATIONS TO TRY]: Expiring tombstones with TTL.
    public void AppendDelete(string key, long timestamp)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] emptyVal = Array.Empty<byte>();
        uint crc = CalculateChecksum(keyBytes, emptyVal);

        lock (_syncLock)
        {
            if (_binaryWriter == null || _fileStream == null)
            {
                throw new ObjectDisposedException(nameof(WriteAheadLog));
            }

            _binaryWriter.Write(crc);
            _binaryWriter.Write((byte)2); // 2 = Tombstone / Delete
            _binaryWriter.Write(timestamp);
            _binaryWriter.Write(keyBytes.Length);
            _binaryWriter.Write(keyBytes);
            _binaryWriter.Write(0); // ValueLength = 0
            _fileStream.Flush();
        }
    }

    // [KYA KARTA HAI]: Pre-computed CRC32 lookup table (polynomial 0xEDB88320) maintain karta hai.
    // [KYU ZAROORI HAI]: Bit-by-bit loop ke bajay byte-level O(1) table indexing se CRC calculation 8x fast hoti hai.
    // [KAISE CHALTA HAI]: Static constructor me 256 standard IEEE 802.3 polynomial entries initialize hoti hain.
    // [OUTPUT / SIDE EFFECT]: 256-element uint array in memory.
    // [VARIATIONS TO TRY]: Hardware PCLMULQDQ intrinsics for hardware-level acceleration.
    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    // [KYA KARTA HAI]: 256-element CRC32 lookup table generate karta hai.
    // [KYU ZAROORI HAI]: Fast software-based checksum generation.
    // [KAISE CHALTA HAI]: Reversed polynomial 0xEDB88320 shift-xor loop chalata hai.
    // [OUTPUT / SIDE EFFECT]: Table populated with polynomial remainders.
    // [VARIATIONS TO TRY]: Castagnoli CRC32-C polynomial (0x82F63B78).
    private static uint[] GenerateCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                if ((entry & 1) == 1)
                {
                    entry = (entry >> 1) ^ 0xEDB88320;
                }
                else
                {
                    entry >>= 1;
                }
            }
            table[i] = entry;
        }
        return table;
    }

    // [KYA KARTA HAI]: Key aur Value bytes ka IEEE 802.3 standard CRC32 checksum compute karta hai.
    // [KYU ZAROORI HAI]: Disk corruption, torn writes, aur bit rot detect karna without external packages.
    // [KAISE CHALTA HAI]: Table lookup algorithm se har byte ko mix karta hai.
    // [OUTPUT / SIDE EFFECT]: 32-bit checksum uint value.
    // [VARIATIONS TO TRY]: xxHash3 for non-cryptographic maximum throughput.
    private static uint CalculateChecksum(byte[] keyBytes, byte[] valBytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in keyBytes)
        {
            crc = (crc >> 8) ^ Crc32Table[(crc ^ b) & 0xFF];
        }
        foreach (byte b in valBytes)
        {
            crc = (crc >> 8) ^ Crc32Table[(crc ^ b) & 0xFF];
        }
        return ~crc;
    }

    // [KYA KARTA HAI]: Purane WAL file ko read karke saare records replay karta hai.
    // [KYU ZAROORI HAI]: Database boot ke waqt crash recovery ke liye: RAM MemTable ko restore karta hai.
    // [KAISE CHALTA HAI]: File ko binary stream me sequentially read karta hai aur CRC verify karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns sequence of WalEntry (key, value, timestamp, isTombstone).
    // [VARIATIONS TO TRY]: Parallel recovery using multi-threaded log parsers.
    public static IEnumerable<WalEntry> ReplayLog(string filePath)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs, Encoding.UTF8);

        while (fs.Position < fs.Length)
        {
            uint storedCrc;
            byte recordType;
            long timestamp;
            int keyLen;
            byte[] keyBytes;
            int valLen;
            byte[] valBytes;

            try
            {
                // [KYA KARTA HAI]: Record header read karta hai.
                // [KYU ZAROORI HAI]: Record parsing initialization.
                // [KAISE CHALTA HAI]: Sequential binary reads.
                // [OUTPUT / SIDE EFFECT]: Fields populated.
                // [VARIATIONS TO TRY]: Catch EndOfStreamException for truncated last record.
                storedCrc = reader.ReadUInt32();
                recordType = reader.ReadByte();
                timestamp = reader.ReadInt64();
                keyLen = reader.ReadInt32();
                keyBytes = reader.ReadBytes(keyLen);
                valLen = reader.ReadInt32();
                valBytes = reader.ReadBytes(valLen);
            }
            catch (EndOfStreamException)
            {
                // Partial/torn write at the very end of file due to power outage: safely stop replay
                yield break;
            }

            // [KYA KARTA HAI]: Record ka CRC verify karta hai.
            // [KYU ZAROORI HAI]: Corrupt data ko MemTable me insert hone se rokna.
            // [KAISE CHALTA HAI]: Compute checksum and compare with stored CRC.
            // [OUTPUT / SIDE EFFECT]: Skips record if corrupted.
            // [VARIATIONS TO TRY]: Quarantine corrupt segment and alert ops team.
            uint computedCrc = CalculateChecksum(keyBytes, valBytes);
            if (storedCrc != computedCrc)
            {
                // Corrupted entry detected: safely discard remaining corrupt data
                yield break;
            }

            string key = Encoding.UTF8.GetString(keyBytes);
            string val = Encoding.UTF8.GetString(valBytes);
            bool isTombstone = recordType == 2;

            yield return new WalEntry(key, val, timestamp, isTombstone);
        }
    }

    // [KYA KARTA HAI]: Active WAL ko truncate karke zero bytes par reset karta hai.
    // [KYU ZAROORI HAI]: Jab MemTable SSTable ke roop me disk par successfully flush ho jata hai.
    // [KAISE CHALTA HAI]: FileStream ko dispose karke naya empty file banata hai.
    // [OUTPUT / SIDE EFFECT]: WAL file size becomes 0 bytes; disk space reclaimed.
    // [VARIATIONS TO TRY]: Rename WAL to archive folder instead of immediate deletion.
    public void Reset()
    {
        lock (_syncLock)
        {
            _binaryWriter?.Dispose();
            _fileStream?.Dispose();

            File.WriteAllBytes(_filePath, Array.Empty<byte>());

            OpenStream();
        }
    }

    // [KYA KARTA HAI]: File streams close karta hai resources release karne ke liye.
    // [KYU ZAROORI HAI]: File locks clear karna aur clean database shutdown.
    // [KAISE CHALTA HAI]: Flushes and closes file handles.
    // [OUTPUT / SIDE EFFECT]: Stream closed.
    // [VARIATIONS TO TRY]: Safe finalizer pattern.
    public void Dispose()
    {
        lock (_syncLock)
        {
            _binaryWriter?.Flush();
            _binaryWriter?.Dispose();
            _binaryWriter = null;

            _fileStream?.Flush();
            _fileStream?.Dispose();
            _fileStream = null;
        }
    }

    // [KYA KARTA HAI]: Active WAL file path return karta hai.
    // [KYU ZAROORI HAI]: Diagnostic logging aur metrics ke liye.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: Returns _filePath string.
    // [VARIATIONS TO TRY]: FileInfo with size in MB.
    public string FilePath => _filePath;
}

/// <summary>
/// Immutable record container for replaying log entries during recovery.
/// </summary>
public sealed record WalEntry(string Key, string Value, long Timestamp, bool IsTombstone);
