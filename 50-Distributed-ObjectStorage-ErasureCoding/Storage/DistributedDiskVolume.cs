// ============================================================================
// File: DistributedDiskVolume.cs
// Namespace: KiranaHub.ObjectStorage.Storage
// Description: Simulates a physical storage drive / storage node with CRC32
// integrity verification, failure simulation, bit-rot injection, and self-healing.
// ============================================================================

namespace KiranaHub.ObjectStorage.Storage;

/// <summary>
/// Represents a stored shard with header metadata and CRC32 verification.
/// </summary>
public sealed class StoredShard
{
    // [KYA KARTA HAI]: Object unique SHA-256 content address store karta hai.
    // [KYU ZAROORI HAI]: Object identification across distributed volumes.
    // [KAISE CHALTA HAI]: String hash identifier.
    // [OUTPUT / SIDE EFFECT]: Immutable object ID.
    // [VARIATIONS TO TRY]: 128-bit GUID or Spid.
    public string ObjectId { get; }

    // [KYA KARTA HAI]: Shard index number (0 to k+m-1) store karta hai.
    // [KYU ZAROORI HAI]: Distinguishes data shards (0..k-1) from parity shards (k..k+m-1).
    // [KAISE CHALTA HAI]: Integer index property.
    // [OUTPUT / SIDE EFFECT]: Shard position.
    // [VARIATIONS TO TRY]: Enum ShardRole (Data, Parity).
    public int ShardIndex { get; }

    // [KYA KARTA HAI]: Raw payload bytes array store karta hai.
    // [KYU ZAROORI HAI]: Physical shard content on disk.
    // [KAISE CHALTA HAI]: byte[] reference.
    // [OUTPUT / SIDE EFFECT]: Shard data payload.
    // [VARIATIONS TO TRY]: ReadOnlyMemory<byte>.
    public byte[] Payload { get; }

    // [KYA KARTA HAI]: IEEE 802.3 CRC32 checksum store karta hai.
    // [KYU ZAROORI HAI]: Silent bit-rot corruption detect karne ke liye checksum verification mandatory hai.
    // [KAISE CHALTA HAI]: uint 32-bit checksum value.
    // [OUTPUT / SIDE EFFECT]: Integrity checksum.
    // [VARIATIONS TO TRY]: xxHash3 or BLAKE3 for cryptographic speed.
    public uint Checksum { get; set; }

    // [KYA KARTA HAI]: Constructor StoredShard initialize karta hai.
    // [KYU ZAROORI HAI]: Complete shard record create karta hai.
    // [KAISE CHALTA HAI]: Assigns properties and copies payload array.
    // [OUTPUT / SIDE EFFECT]: Initialized StoredShard instance.
    // [VARIATIONS TO TRY]: Shard streaming construct.
    public StoredShard(string objectId, int shardIndex, byte[] payload, uint checksum)
    {
        // [KYA KARTA HAI]: Set ObjectId.
        // [KYU ZAROORI HAI]: Identification.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: ObjectId set.
        // [VARIATIONS TO TRY]: None.
        ObjectId = objectId;

        // [KYA KARTA HAI]: Set ShardIndex.
        // [KYU ZAROORI HAI]: Coordinate in RS matrix.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: ShardIndex set.
        // [VARIATIONS TO TRY]: None.
        ShardIndex = shardIndex;

        // [KYA KARTA HAI]: Set Payload.
        // [KYU ZAROORI HAI]: Stored bytes.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: Payload set.
        // [VARIATIONS TO TRY]: Clone byte array.
        Payload = payload;

        // [KYA KARTA HAI]: Set Checksum.
        // [KYU ZAROORI HAI]: Integrity check.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: Checksum set.
        // [VARIATIONS TO TRY]: Compute automatically.
        Checksum = checksum;
    }
}

/// <summary>
/// Simulates a physical storage disk volume in a distributed object storage cluster.
/// </summary>
public sealed class DistributedDiskVolume
{
    // [KYA KARTA HAI]: Disk volume identification number store karta hai (e.g. 0 to 5).
    // [KYU ZAROORI HAI]: Rack/chassis/disk topology identify karne ke liye disk ID zaroori hai.
    // [KAISE CHALTA HAI]: Readonly integer ID.
    // [OUTPUT / SIDE EFFECT]: Disk unique ID.
    // [VARIATIONS TO TRY]: String volume serial number.
    public int DiskId { get; }

    // [KYA KARTA HAI]: Volume health status track karta hai (online ya failed/offline).
    // [KYU ZAROORI HAI]: Hardware crash simulation aur node outage handling ke liye status check hota hai.
    // [KAISE CHALTA HAI]: Boolean flag IsOnline.
    // [OUTPUT / SIDE EFFECT]: Status flag.
    // [VARIATIONS TO TRY]: VolumeState enum (Healthy, Degraded, Failed, Scrubbing).
    public bool IsOnline { get; private set; } = true;

    // [KYA KARTA HAI]: In-memory storage table shards persist karne ke liye.
    // [KYU ZAROORI HAI]: Fast, deterministic, and isolated disk volume behavior simulate karta hai.
    // [KAISE CHALTA HAI]: Dictionary mapping objectId string to StoredShard.
    // [OUTPUT / SIDE EFFECT]: Internal disk sectors map.
    // [VARIATIONS TO TRY]: Physical NVMe file-backed storage directory.
    private readonly Dictionary<string, StoredShard> _shards = new();

    // [KYA KARTA HAI]: Lock object thread-safety ensure karne ke liye.
    // [KYU ZAROORI HAI]: Concurrent writes aur background scrubber parallel access safely handle ho sakein.
    // [KAISE CHALTA HAI]: Standard C# object lock.
    // [OUTPUT / SIDE EFFECT]: Synchronization barrier.
    // [VARIATIONS TO TRY]: ReaderWriterLockSlim.
    private readonly object _syncLock = new();

    // [KYA KARTA HAI]: Precomputed CRC32 polynomial lookup table (0xEDB88320).
    // [KYU ZAROORI HAI]: Fast O(1) per-byte CRC calculation without external packages.
    // [KAISE CHALTA HAI]: Static uint[256] table populated in static constructor.
    // [OUTPUT / SIDE EFFECT]: Fast CRC lookup table.
    // [VARIATIONS TO TRY]: Hardware CRC32 intrinsic (System.Runtime.Intrinsics.X86.Sse42.Crc32).
    private static readonly uint[] CrcTable = new uint[256];

    // [KYA KARTA HAI]: Static constructor CRC32 lookup table generate karta hai.
    // [KYU ZAROORI HAI]: Standard IEEE 802.3 CRC32 polynomial initialization.
    // [KAISE CHALTA HAI]: Bitwise shifts aur XOR with 0xEDB88320.
    // [OUTPUT / SIDE EFFECT]: CrcTable fully precomputed.
    // [VARIATIONS TO TRY]: CRC64-ECMA table.
    static DistributedDiskVolume()
    {
        // [KYA KARTA HAI]: Loop through all 256 byte values.
        // [KYU ZAROORI HAI]: Every byte entry requires precomputed CRC residue.
        // [KAISE CHALTA HAI]: For i from 0 to 255.
        // [OUTPUT / SIDE EFFECT]: Generates 256 entries.
        // [VARIATIONS TO TRY]: None.
        for (uint i = 0; i < 256; i++)
        {
            // [KYA KARTA HAI]: Initialize c with byte value.
            // [KYU ZAROORI HAI]: Polynomial division starting point.
            // [KAISE CHALTA HAI]: c = i.
            // [OUTPUT / SIDE EFFECT]: Local polynomial accumulator.
            // [VARIATIONS TO TRY]: None.
            uint c = i;

            // [KYA KARTA HAI]: 8 bit cycles run karta hai.
            // [KYU ZAROORI HAI]: Shift every bit through polynomial.
            // [KAISE CHALTA HAI]: For j in [0..7].
            // [OUTPUT / SIDE EFFECT]: 8-bit reduction.
            // [VARIATIONS TO TRY]: Unrolled loop.
            for (int j = 0; j < 8; j++)
            {
                // [KYA KARTA HAI]: If lowest bit is 1, XOR with 0xEDB88320, else shift right.
                // [KYU ZAROORI HAI]: Standard CRC polynomial arithmetic.
                // [KAISE CHALTA HAI]: c = (c & 1) != 0 ? (0xEDB88320 ^ (c >> 1)) : (c >> 1).
                // [OUTPUT / SIDE EFFECT]: Polynomial shifted.
                // [VARIATIONS TO TRY]: Hardware instructions.
                c = (c & 1) != 0 ? (0xEDB88320 ^ (c >> 1)) : (c >> 1);
            }

            // [KYA KARTA HAI]: Save precomputed CRC in table.
            // [KYU ZAROORI HAI]: Runtime lookup speed.
            // [KAISE CHALTA HAI]: CrcTable[i] = c.
            // [OUTPUT / SIDE EFFECT]: Table slot populated.
            // [VARIATIONS TO TRY]: None.
            CrcTable[i] = c;
        }
    }

    // [KYA KARTA HAI]: DistributedDiskVolume constructor volume ID initialize karta hai.
    // [KYU ZAROORI HAI]: Discrete disk drive instance configuration.
    // [KAISE CHALTA HAI]: DiskId = diskId.
    // [OUTPUT / SIDE EFFECT]: Ready volume instance.
    // [VARIATIONS TO TRY]: Mount point path string.
    public DistributedDiskVolume(int diskId)
    {
        // [KYA KARTA HAI]: Set DiskId.
        // [KYU ZAROORI HAI]: Identification.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: DiskId initialized.
        // [VARIATIONS TO TRY]: None.
        DiskId = diskId;
    }

    // [KYA KARTA HAI]: Shard data ka IEEE 802.3 CRC32 checksum calculate karta hai.
    // [KYU ZAROORI HAI]: Bit-rot detection aur data integrity verification.
    // [KAISE CHALTA HAI]: Byte stream over CrcTable lookup, initial 0xFFFFFFFF, final inverted.
    // [OUTPUT / SIDE EFFECT]: 32-bit CRC unsigned integer.
    // [VARIATIONS TO TRY]: Vectorized CRC32.
    public static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        // [KYA KARTA HAI]: Initialize crc accumulator with 0xFFFFFFFF.
        // [KYU ZAROORI HAI]: Standard IEEE CRC seed value.
        // [KAISE CHALTA HAI]: crc = 0xFFFFFFFF.
        // [OUTPUT / SIDE EFFECT]: Seed initialized.
        // [VARIATIONS TO TRY]: Custom non-zero seed.
        uint crc = 0xFFFFFFFF;

        // [KYA KARTA HAI]: Har byte par table lookup XOR karta hai.
        // [KYU ZAROORI HAI]: Fast polynomial reduction.
        // [KAISE CHALTA HAI]: crc = (crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF].
        // [OUTPUT / SIDE EFFECT]: CRC accumulated.
        // [VARIATIONS TO TRY]: SIMD PCLMULQDQ.
        foreach (byte b in data)
        {
            // [KYA KARTA HAI]: Lookup CRC table with lowest byte.
            // [KYU ZAROORI HAI]: Standard byte processing step.
            // [KAISE CHALTA HAI]: crc = (crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF].
            // [OUTPUT / SIDE EFFECT]: Acc updated.
            // [VARIATIONS TO TRY]: 4-byte unrolled loop.
            crc = (crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF];
        }

        // [KYA KARTA HAI]: Final inversion ~crc return karta hai.
        // [KYU ZAROORI HAI]: Standard CRC32 specification.
        // [KAISE CHALTA HAI]: Bitwise NOT ^ 0xFFFFFFFF.
        // [OUTPUT / SIDE EFFECT]: Final verified CRC32.
        // [VARIATIONS TO TRY]: None.
        return ~crc;
    }

    // [KYA KARTA HAI]: Disk volume par shard write karta hai with calculated CRC32.
    // [KYU ZAROORI HAI]: Distributed storage me shard persistence.
    // [KAISE CHALTA HAI]: Checksum calculate karta hai, payload clone karta hai, aur dictionary me store karta hai.
    // [OUTPUT / SIDE EFFECT]: Shard persisted on volume.
    // [VARIATIONS TO TRY]: Asynchronous async file write.
    public void WriteShard(string objectId, int shardIndex, byte[] shardData)
    {
        // [KYA KARTA HAI]: Check karta hai disk volume online hai ya nahi.
        // [KYU ZAROORI HAI]: Crashed/failed disk par write reject hona chahiye.
        // [KAISE CHALTA HAI]: if (!IsOnline) throw IOException.
        // [OUTPUT / SIDE EFFECT]: Hardware failure simulation guard.
        // [VARIATIONS TO TRY]: Buffer write in offline queue.
        if (!IsOnline)
        {
            // [KYA KARTA HAI]: Throw IOException.
            // [KYU ZAROORI HAI]: Simulate disk drive failure.
            // [KAISE CHALTA HAI]: Throws IOException.
            // [OUTPUT / SIDE EFFECT]: Write failure.
            // [VARIATIONS TO TRY]: Custom DiskOfflineException.
            throw new IOException($"Disk volume {DiskId} is offline or failed.");
        }

        // [KYA KARTA HAI]: Shard bytes ka CRC32 checksum compute karta hai.
        // [KYU ZAROORI HAI]: Shard integrity validation footprint.
        // [KAISE CHALTA HAI]: ComputeCrc32(shardData).
        // [OUTPUT / SIDE EFFECT]: uint crc32.
        // [VARIATIONS TO TRY]: Append crypto signature.
        uint checksum = ComputeCrc32(shardData);

        // [KYA KARTA HAI]: Payload bytes ko clone karta hai taaki caller ke modifications se isolate rahe.
        // [KYU ZAROORI HAI]: Defense in depth memory isolation.
        // [KAISE CHALTA HAI]: shardData.ToArray().
        // [OUTPUT / SIDE EFFECT]: Isolated byte array.
        // [VARIATIONS TO TRY]: Memory-mapped file slice.
        byte[] payloadCopy = (byte[])shardData.Clone();

        // [KYA KARTA HAI]: Thread-safe lock acquire karta hai.
        // [KYU ZAROORI HAI]: Protects internal dictionary state.
        // [KAISE CHALTA HAI]: lock (_syncLock).
        // [OUTPUT / SIDE EFFECT]: Synchronized execution.
        // [VARIATIONS TO TRY]: ConcurrentDictionary.
        lock (_syncLock)
        {
            // [KYA KARTA HAI]: StoredShard create karke dictionary me store karta hai.
            // [KYU ZAROORI HAI]: Object shard mapping persistence.
            // [KAISE CHALTA HAI]: _shards[objectId] = new StoredShard(...).
            // [OUTPUT / SIDE EFFECT]: Shard saved.
            // [VARIATIONS TO TRY]: Overwrite validation.
            _shards[objectId] = new StoredShard(objectId, shardIndex, payloadCopy, checksum);
        }
    }

    // [KYA KARTA HAI]: Disk volume se shard read karta hai aur CRC32 verify karta hai.
    // [KYU ZAROORI HAI]: Object retrieval ke time integrity ensure karta hai; silent bit rot detect karta hai.
    // [KAISE CHALTA HAI]: Shard fetch karta hai, live CRC calculate karta hai, stored CRC se match karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns (success, data, isCorrupt).
    // [VARIATIONS TO TRY]: Stream reading.
    public (bool Found, byte[]? Data, bool IsCorrupted) ReadShard(string objectId)
    {
        // [KYA KARTA HAI]: Check karta hai disk volume online hai ya nahi.
        // [KYU ZAROORI HAI]: Dead disk se read fail hona chahiye (simulated network/hardware outage).
        // [KAISE CHALTA HAI]: if (!IsOnline) return (false, null, false).
        // [OUTPUT / SIDE EFFECT]: Offline disk returns not found.
        // [VARIATIONS TO TRY]: Throw hardware exception.
        if (!IsOnline)
        {
            // [KYA KARTA HAI]: Return failure.
            // [KYU ZAROORI HAI]: Offline disk unreachable.
            // [KAISE CHALTA HAI]: Direct tuple return.
            // [OUTPUT / SIDE EFFECT]: Read bypassed.
            // [VARIATIONS TO TRY]: Retry policy.
            return (false, null, false);
        }

        // [KYA KARTA HAI]: Local variable for found shard.
        // [KYU ZAROORI HAI]: Store fetched shard reference under lock.
        // [KAISE CHALTA HAI]: StoredShard? shard = null.
        // [OUTPUT / SIDE EFFECT]: Local reference.
        // [VARIATIONS TO TRY]: None.
        StoredShard? shard = null;

        // [KYA KARTA HAI]: Lock acquire karke shard lookup karta hai.
        // [KYU ZAROORI HAI]: Thread-safe retrieval.
        // [KAISE CHALTA HAI]: lock (_syncLock) { _shards.TryGetValue(...) }.
        // [OUTPUT / SIDE EFFECT]: Safely retrieved shard.
        // [VARIATIONS TO TRY]: ConcurrentDictionary TryGetValue without lock.
        lock (_syncLock)
        {
            // [KYA KARTA HAI]: Dictionary lookup by objectId.
            // [KYU ZAROORI HAI]: Fetch stored shard metadata.
            // [KAISE CHALTA HAI]: _shards.TryGetValue(objectId, out shard).
            // [OUTPUT / SIDE EFFECT]: shard populated if found.
            // [VARIATIONS TO TRY]: Multi-shard keys.
            if (!_shards.TryGetValue(objectId, out shard))
            {
                // [KYA KARTA HAI]: Not found path.
                // [KYU ZAROORI HAI]: Shard never written to this volume.
                // [KAISE CHALTA HAI]: Return tuple (false, null, false).
                // [OUTPUT / SIDE EFFECT]: Not found returned.
                // [VARIATIONS TO TRY]: None.
                return (false, null, false);
            }
        }

        // [KYA KARTA HAI]: Shard payload ka actual current CRC32 compute karta hai.
        // [KYU ZAROORI HAI]: Compares against stored CRC to detect silent physical bit rot.
        // [KAISE CHALTA HAI]: ComputeCrc32(shard.Payload).
        // [OUTPUT / SIDE EFFECT]: liveChecksum calculated.
        // [VARIATIONS TO TRY]: Periodic background hash checking.
        uint liveChecksum = ComputeCrc32(shard.Payload);

        // [KYA KARTA HAI]: Check karta hai live CRC stored CRC se match karta hai ya nahi.
        // [KYU ZAROORI HAI]: Bit rot detection: agar match nahi hua toh disk sector corrupt ho chuka hai!
        // [KAISE CHALTA HAI]: liveChecksum != shard.Checksum.
        // [OUTPUT / SIDE EFFECT]: Corruption flag trigger.
        // [VARIATIONS TO TRY]: Log alert to monitoring telemetry.
        if (liveChecksum != shard.Checksum)
        {
            // [KYA KARTA HAI]: Return corrupted status.
            // [KYU ZAROORI HAI]: Signifies data payload corrupted on disk.
            // [KAISE CHALTA HAI]: Return (true, shard.Payload, true).
            // [OUTPUT / SIDE EFFECT]: Caller is notified of corruption.
            // [VARIATIONS TO TRY]: Automatically invoke repair.
            return (true, shard.Payload, true);
        }

        // [KYA KARTA HAI]: Healthy shard payload return karta hai.
        // [KYU ZAROORI HAI]: Verified intact data returned.
        // [KAISE CHALTA HAI]: Return (true, shard.Payload, false).
        // [OUTPUT / SIDE EFFECT]: Clean read.
        // [VARIATIONS TO TRY]: Return readonly memory slice.
        return (true, shard.Payload, false);
    }

    // [KYA KARTA HAI]: Silent bit-rot inject karta hai shard payload ke specific offset par bits flip karke.
    // [KYU ZAROORI HAI]: Cosmic ray / bad sector bit-rot detection and self-healing test karne ke liye.
    // [KAISE CHALTA HAI]: Payload[byteOffset] ^= 0xFF (flips all 8 bits) without updating the stored CRC.
    // [OUTPUT / SIDE EFFECT]: Shard data becomes corrupt while header CRC remains unchanged.
    // [VARIATIONS TO TRY]: Flip single bit (e.g. ^ 0x01).
    public bool InjectBitRot(string objectId, int byteOffset)
    {
        // [KYA KARTA HAI]: Thread-safe lock acquire karta hai.
        // [KYU ZAROORI HAI]: Safely mutate payload array.
        // [KAISE CHALTA HAI]: lock (_syncLock).
        // [OUTPUT / SIDE EFFECT]: Synchronized corruption.
        // [VARIATIONS TO TRY]: None.
        lock (_syncLock)
        {
            // [KYA KARTA HAI]: Shard find karta hai dictionary me.
            // [KYU ZAROORI HAI]: Target shard for fault injection.
            // [KAISE CHALTA HAI]: TryGetValue lookup.
            // [OUTPUT / SIDE EFFECT]: shard reference.
            // [VARIATIONS TO TRY]: None.
            if (_shards.TryGetValue(objectId, out var shard))
            {
                // [KYA KARTA HAI]: Validate offset bounds.
                // [KYU ZAROORI HAI]: Prevents IndexOutOfRangeException.
                // [KAISE CHALTA HAI]: byteOffset >= 0 && byteOffset < shard.Payload.Length.
                // [OUTPUT / SIDE EFFECT]: Bounds check.
                // [VARIATIONS TO TRY]: Random offset selection.
                if (byteOffset >= 0 && byteOffset < shard.Payload.Length)
                {
                    // [KYA KARTA HAI]: Flip all 8 bits at target byte offset.
                    // [KYU ZAROORI HAI]: Simulates silent magnetic/transistor bit rot on disk.
                    // [KAISE CHALTA HAI]: shard.Payload[byteOffset] ^= 0xFF.
                    // [OUTPUT / SIDE EFFECT]: Corrupted byte without updating CRC header.
                    // [VARIATIONS TO TRY]: Corrupt multiple bytes.
                    shard.Payload[byteOffset] ^= 0xFF;

                    // [KYA KARTA HAI]: Return true on successful injection.
                    // [KYU ZAROORI HAI]: Acknowledges bit-rot applied.
                    // [KAISE CHALTA HAI]: Return true.
                    // [OUTPUT / SIDE EFFECT]: Injection success.
                    // [VARIATIONS TO TRY]: None.
                    return true;
                }
            }

            // [KYA KARTA HAI]: Return false if shard not found or invalid offset.
            // [KYU ZAROORI HAI]: Guard failure.
            // [KAISE CHALTA HAI]: Return false.
            // [OUTPUT / SIDE EFFECT]: Injection skipped.
            // [VARIATIONS TO TRY]: Throw ArgumentException.
            return false;
        }
    }

    // [KYA KARTA HAI]: Hardware disk failure simulate karta hai (disk goes offline).
    // [KYU ZAROORI HAI]: Multi-disk disaster recovery scenarios test karne ke liye.
    // [KAISE CHALTA HAI]: IsOnline = false.
    // [OUTPUT / SIDE EFFECT]: Volume rejects all reads and writes.
    // [VARIATIONS TO TRY]: Wipe data dictionary completely.
    public void SimulateFailure()
    {
        // [KYA KARTA HAI]: Set IsOnline = false.
        // [KYU ZAROORI HAI]: Simulates dead drive / broken controller.
        // [KAISE CHALTA HAI]: IsOnline = false.
        // [OUTPUT / SIDE EFFECT]: Volume marked offline.
        // [VARIATIONS TO TRY]: Drop network packets.
        IsOnline = false;
    }

    // [KYA KARTA HAI]: Disk volume ko restore / replace karta hai.
    // [KYU ZAROORI HAI]: Replacement drive provision hone ke baad volume online laana.
    // [KAISE CHALTA HAI]: IsOnline = true.
    // [OUTPUT / SIDE EFFECT]: Volume becomes active.
    // [VARIATIONS TO TRY]: Empty new drive provision.
    public void RestoreDisk()
    {
        // [KYA KARTA HAI]: Set IsOnline = true.
        // [KYU ZAROORI HAI]: Drive back online.
        // [KAISE CHALTA HAI]: IsOnline = true.
        // [OUTPUT / SIDE EFFECT]: Active status restored.
        // [VARIATIONS TO TRY]: Auto-scrub trigger.
        IsOnline = true;
    }

    // [KYA KARTA HAI]: Corrupted shard ko fresh repaired data se heal karta hai.
    // [KYU ZAROORI HAI]: Anti-entropy background scrubber bit-rot repair karke disk ko clean banata hai.
    // [KAISE CHALTA HAI]: Re-writes shard payload and updates stored CRC32 to clean state.
    // [OUTPUT / SIDE EFFECT]: Repaired shard persisted.
    // [VARIATIONS TO TRY]: Write to new sector and remap bad sector.
    public void HealShard(string objectId, int shardIndex, byte[] repairedData)
    {
        // [KYA KARTA HAI]: Call WriteShard with clean data.
        // [KYU ZAROORI HAI]: Atomically overwrites corrupt sector and sets valid CRC32.
        // [KAISE CHALTA HAI]: WriteShard(objectId, shardIndex, repairedData).
        // [OUTPUT / SIDE EFFECT]: Volume restored to healthy state.
        // [VARIATIONS TO TRY]: Verify read after heal.
        WriteShard(objectId, shardIndex, repairedData);
    }
}
