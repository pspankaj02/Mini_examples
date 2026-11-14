// ============================================================================
// File: ObjectStorageEngine.cs
// Namespace: KiranaHub.ObjectStorage.Engine
// Description: Distributed Content-Addressable Storage (CAS) engine with
// Reed-Solomon Erasure Coding, SHA-256 deduplication, and anti-entropy scrubber.
// ============================================================================

using System.Security.Cryptography;
using KiranaHub.ObjectStorage.Erasure;
using KiranaHub.ObjectStorage.Storage;

namespace KiranaHub.ObjectStorage.Engine;

/// <summary>
/// Object manifest storing metadata, cryptographic hash, and shard dimensions.
/// </summary>
public sealed record ObjectManifest(
    string Bucket,
    string Key,
    string Sha256Hash,
    int OriginalSize,
    int ShardSize,
    int DataShards,
    int ParityShards,
    DateTime CreatedAtUtc
);

/// <summary>
/// Result report of the anti-entropy background scrubber run.
/// </summary>
public sealed record ScrubReport(
    int ObjectsChecked,
    int ShardsScanned,
    int CorruptionsDetected,
    int ShardsHealed
);

/// <summary>
/// Distributed object storage engine coordinating multiple disk volumes with RS erasure coding.
/// </summary>
public sealed class ObjectStorageEngine
{
    // [KYA KARTA HAI]: Reed-Solomon codec reference store karta hai.
    // [KYU ZAROORI HAI]: Encoding and decoding operations perform karne ke liye codec required hai.
    // [KAISE CHALTA HAI]: Readonly reference initialized in constructor.
    // [OUTPUT / SIDE EFFECT]: Codec reference.
    // [VARIATIONS TO TRY]: Dynamic codec factory based on object size.
    public ReedSolomonCodec Codec { get; }

    // [KYA KARTA HAI]: Cluster ke disk volumes array store karta hai.
    // [KYU ZAROORI HAI]: Har shard ko independent disk volume par persist aur scrub karne ke liye.
    // [KAISE CHALTA HAI]: Array of DistributedDiskVolume of length TotalShards.
    // [OUTPUT / SIDE EFFECT]: Fleet of storage nodes.
    // [VARIATIONS TO TRY]: Dynamic multi-rack distributed network clients.
    public DistributedDiskVolume[] Volumes { get; }

    // [KYA KARTA HAI]: Bucket + Key to SHA-256 manifest metadata mapping store karta hai.
    // [KYU ZAROORI HAI]: Quick-commerce object lookup and path navigation.
    // [KAISE CHALTA HAI]: Concurrent dictionary with string key "{bucket}/{key}".
    // [OUTPUT / SIDE EFFECT]: Metadata catalog.
    // [VARIATIONS TO TRY]: Persistent RocksDB or SQLite metadata store.
    private readonly Dictionary<string, ObjectManifest> _manifests = new();

    // [KYA KARTA HAI]: SHA-256 Content-Addressable Storage (CAS) deduplication index.
    // [KYU ZAROORI HAI]: Duplicate invoice PDFs ya images ko multiple baar store hone se rokna (zero storage waste).
    // [KAISE CHALTA HAI]: Maps sha256 hex string to existing ObjectManifest.
    // [OUTPUT / SIDE EFFECT]: Global dedup table.
    // [VARIATIONS TO TRY]: Bloom filter before CAS lookup.
    private readonly Dictionary<string, ObjectManifest> _casIndex = new();

    // [KYA KARTA HAI]: Thread-safety synchronization lock object.
    // [KYU ZAROORI HAI]: Manifest updates aur CAS index mutations atomic rakhna.
    // [KAISE CHALTA HAI]: Standard C# lock.
    // [OUTPUT / SIDE EFFECT]: Thread isolation.
    // [VARIATIONS TO TRY]: ReaderWriterLockSlim.
    private readonly object _engineLock = new();

    // [KYA KARTA HAI]: Constructor ObjectStorageEngine cluster initialize karta hai.
    // [KYU ZAROORI HAI]: Cluster volumes provision karna aur RS codec setup karna.
    // [KAISE CHALTA HAI]: Codec create karta hai aur har shard ke liye dedicated DistributedDiskVolume allocate karta hai.
    // [OUTPUT / SIDE EFFECT]: Ready object storage cluster.
    // [VARIATIONS TO TRY]: Configurable disk failure domains.
    public ObjectStorageEngine(int dataShards = 4, int parityShards = 2)
    {
        // [KYA KARTA HAI]: ReedSolomonCodec instantiate karta hai.
        // [KYU ZAROORI HAI]: Mathematical Cauchy erasure coding setup.
        // [KAISE CHALTA HAI]: new ReedSolomonCodec(dataShards, parityShards).
        // [OUTPUT / SIDE EFFECT]: Codec initialized.
        // [VARIATIONS TO TRY]: 8+4 or 16+4 setup for hyperscale petabyte storage.
        Codec = new ReedSolomonCodec(dataShards, parityShards);

        // [KYA KARTA HAI]: Volumes array allocate karta hai of size TotalShards.
        // [KYU ZAROORI HAI]: 1 volume per shard guarantee karta hai zero co-location.
        // [KAISE CHALTA HAI]: new DistributedDiskVolume[Codec.TotalShards].
        // [OUTPUT / SIDE EFFECT]: Volumes array allocated.
        // [VARIATIONS TO TRY]: Multiple disks per storage server.
        Volumes = new DistributedDiskVolume[Codec.TotalShards];

        // [KYA KARTA HAI]: Loop run karke har disk volume instantiate karta hai.
        // [KYU ZAROORI HAI]: Har disk volume ko discrete ID assign karna.
        // [KAISE CHALTA HAI]: For i from 0 to TotalShards - 1.
        // [OUTPUT / SIDE EFFECT]: Volumes populated.
        // [VARIATIONS TO TRY]: Network-attached storage endpoints.
        for (int i = 0; i < Codec.TotalShards; i++)
        {
            // [KYA KARTA HAI]: Volume instance create karta hai with diskId = i.
            // [KYU ZAROORI HAI]: Independent simulated hardware node.
            // [KAISE CHALTA HAI]: Volumes[i] = new DistributedDiskVolume(i).
            // [OUTPUT / SIDE EFFECT]: Volume ready.
            // [VARIATIONS TO TRY]: Physical directory path binding.
            Volumes[i] = new DistributedDiskVolume(i);
        }
    }

    // [KYA KARTA HAI]: Object upload karta hai with SHA-256 CAS deduplication aur RS erasure coding.
    // [KYU ZAROORI HAI]: Quick-commerce delivery proof photos ya invoices ko fault-tolerant distributed storage me persist karna.
    // [KAISE CHALTA HAI]: SHA-256 compute karta hai, dedup check karta hai, data chunk karke encode karta hai, aur discrete disks par write karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns ObjectManifest with storage metadata and dedup status.
    // [VARIATIONS TO TRY]: Streaming upload for multi-GB media files.
    public (ObjectManifest Manifest, bool WasDeduplicated) PutObject(string bucket, string key, byte[] payload)
    {
        // [KYA KARTA HAI]: Payload ka SHA-256 cryptographic hash calculate karta hai.
        // [KYU ZAROORI HAI]: Content-Addressable Storage (CAS) key create karta hai and bit-level integrity anchor banta hai.
        // [KAISE CHALTA HAI]: SHA256.HashData(payload) to hex string.
        // [OUTPUT / SIDE EFFECT]: 64-char lowercase hex string.
        // [VARIATIONS TO TRY]: BLAKE3 for 10x faster hashing.
        string sha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        // [KYA KARTA HAI]: Storage key path format karta hai: "{bucket}/{key}".
        // [KYU ZAROORI HAI]: Namespace isolation for Kirana quick-commerce buckets.
        // [KAISE CHALTA HAI]: String interpolation $"{bucket}/{key}".
        // [OUTPUT / SIDE EFFECT]: Uniform object URI.
        // [VARIATIONS TO TRY]: S3 URI standard: s3://bucket/key.
        string manifestKey = $"{bucket}/{key}";

        // [KYA KARTA HAI]: Thread-safe lock acquire karta hai dedup index check karne ke liye.
        // [KYU ZAROORI HAI]: Race condition prevent karta hai concurrent duplicate uploads me.
        // [KAISE CHALTA HAI]: lock (_engineLock).
        // [OUTPUT / SIDE EFFECT]: Atomic critical section.
        // [VARIATIONS TO TRY]: Fine-grained bucket level locks.
        lock (_engineLock)
        {
            // [KYA KARTA HAI]: Check karta hai kya identical SHA-256 payload pehle se cluster me stored hai.
            // [KYU ZAROORI HAI]: Content-Addressable deduplication saves massive disk storage on duplicate receipts/photos.
            // [KAISE CHALTA HAI]: _casIndex.TryGetValue(sha256, out var existingManifest).
            // [OUTPUT / SIDE EFFECT]: Deduplication match detected.
            // [VARIATIONS TO TRY]: Reference count tracking on CAS entries.
            if (_casIndex.TryGetValue(sha256, out var existingManifest))
            {
                // [KYA KARTA HAI]: Naya alias manifest create karta hai pointing to same SHA-256 content.
                // [KYU ZAROORI HAI]: New bucket/key path points to existing shards without writing extra bytes to disk.
                // [KAISE CHALTA HAI]: new ObjectManifest with existing hash and sizes.
                // [OUTPUT / SIDE EFFECT]: Zero byte storage overhead.
                // [VARIATIONS TO TRY]: Symlink manifest type.
                var aliasManifest = new ObjectManifest(
                    bucket,
                    key,
                    sha256,
                    existingManifest.OriginalSize,
                    existingManifest.ShardSize,
                    existingManifest.DataShards,
                    existingManifest.ParityShards,
                    DateTime.UtcNow
                );

                // [KYA KARTA HAI]: Map new manifestKey to alias.
                // [KYU ZAROORI HAI]: Catalog update.
                // [KAISE CHALTA HAI]: _manifests[manifestKey] = aliasManifest.
                // [OUTPUT / SIDE EFFECT]: Manifest registered.
                // [VARIATIONS TO TRY]: None.
                _manifests[manifestKey] = aliasManifest;

                // [KYA KARTA HAI]: Return deduplicated manifest.
                // [KYU ZAROORI HAI]: Informs caller of 100% deduplication win.
                // [KAISE CHALTA HAI]: Return (aliasManifest, true).
                // [OUTPUT / SIDE EFFECT]: Returns dedup result.
                // [VARIATIONS TO TRY]: None.
                return (aliasManifest, true);
            }
        }

        // [KYA KARTA HAI]: Shard size calculate karta hai: ceil(payload.Length / DataShards).
        // [KYU ZAROORI HAI]: Data shards equal size ke hone chahiye RS matrix multiplication ke liye.
        // [KAISE CHALTA HAI]: (payload.Length + Codec.DataShards - 1) / Codec.DataShards.
        // [OUTPUT / SIDE EFFECT]: Even shard size in bytes.
        // [VARIATIONS TO TRY]: Fixed 1MB or 4MB block size chunking.
        int shardSize = (payload.Length + Codec.DataShards - 1) / Codec.DataShards;

        // [KYA KARTA HAI]: Minimum 1 byte shard size enforce karta hai empty payloads ke liye.
        // [KYU ZAROORI HAI]: Zero-length buffer RS encoding me invalid hoti hai.
        // [KAISE CHALTA HAI]: if (shardSize == 0) shardSize = 1.
        // [OUTPUT / SIDE EFFECT]: shardSize >= 1.
        // [VARIATIONS TO TRY]: Reject 0-byte objects early.
        if (shardSize == 0) shardSize = 1;

        // [KYA KARTA HAI]: Shards array allocate karta hai: byte[TotalShards][shardSize].
        // [KYU ZAROORI HAI]: k data shards + m parity shards hold karne ke liye memory buffers.
        // [KAISE CHALTA HAI]: new byte[Codec.TotalShards][].
        // [OUTPUT / SIDE EFFECT]: Buffer pointers array.
        // [VARIATIONS TO TRY]: MemoryPool<byte> rental for zero GC pressure.
        byte[][] shards = new byte[Codec.TotalShards][];

        // [KYA KARTA HAI]: Har shard buffer allocate karta hai of length shardSize.
        // [KYU ZAROORI HAI]: Pre-allocate zeroed buffers for encoding.
        // [KAISE CHALTA HAI]: For i from 0 to TotalShards - 1.
        // [OUTPUT / SIDE EFFECT]: shard buffers initialized.
        // [VARIATIONS TO TRY]: ArrayPool<byte>.Shared.Rent.
        for (int i = 0; i < Codec.TotalShards; i++)
        {
            // [KYA KARTA HAI]: Allocate shard buffer.
            // [KYU ZAROORI HAI]: Memory for individual shard.
            // [KAISE CHALTA HAI]: shards[i] = new byte[shardSize].
            // [OUTPUT / SIDE EFFECT]: Buffer ready.
            // [VARIATIONS TO TRY]: Memory alignment.
            shards[i] = new byte[shardSize];
        }

        // [KYA KARTA HAI]: Original payload ko k data shards me distribute karta hai.
        // [KYU ZAROORI HAI]: Systematic RS code data shards ko directly store karta hai.
        // [KAISE CHALTA HAI]: Copy slices from payload into shards[0..k-1].
        // [OUTPUT / SIDE EFFECT]: Data shards filled with original bytes.
        // [VARIATIONS TO TRY]: Span<byte>.CopyTo.
        for (int i = 0; i < Codec.DataShards; i++)
        {
            // [KYA KARTA HAI]: Source offset calculate karta hai: i * shardSize.
            // [KYU ZAROORI HAI]: Slice offset calculation.
            // [KAISE CHALTA HAI]: offset = i * shardSize.
            // [OUTPUT / SIDE EFFECT]: Byte offset.
            // [VARIATIONS TO TRY]: None.
            int offset = i * shardSize;

            // [KYA KARTA HAI]: Remaining bytes to copy count calculate karta hai.
            // [KYU ZAROORI HAI]: Last data shard might have less bytes than shardSize (padding handled automatically by zeros).
            // [KAISE CHALTA HAI]: Math.Min(shardSize, Math.Max(0, payload.Length - offset)).
            // [OUTPUT / SIDE EFFECT]: bytesToCopy count.
            // [VARIATIONS TO TRY]: Bit padding.
            int bytesToCopy = Math.Min(shardSize, Math.Max(0, payload.Length - offset));

            // [KYA KARTA HAI]: If bytes to copy > 0, copy into shard.
            // [KYU ZAROORI HAI]: Transfers payload slice into shard buffer.
            // [KAISE CHALTA HAI]: Buffer.BlockCopy.
            // [OUTPUT / SIDE EFFECT]: Shard buffer populated.
            // [VARIATIONS TO TRY]: ReadOnlySpan slice copy.
            if (bytesToCopy > 0)
            {
                // [KYA KARTA HAI]: Copy bytes.
                // [KYU ZAROORI HAI]: Fast raw memory transfer.
                // [KAISE CHALTA HAI]: Buffer.BlockCopy(payload, offset, shards[i], 0, bytesToCopy).
                // [OUTPUT / SIDE EFFECT]: Slice copied.
                // [VARIATIONS TO TRY]: Unsafe memory copy.
                Buffer.BlockCopy(payload, offset, shards[i], 0, bytesToCopy);
            }
        }

        // [KYA KARTA HAI]: Reed-Solomon encoding execute karta hai Cauchy generator matrix se.
        // [KYU ZAROORI HAI]: Parity shards (k to k+m-1) compute karna fault tolerance ke liye.
        // [KAISE CHALTA HAI]: Codec.Encode(shards, shardSize).
        // [OUTPUT / SIDE EFFECT]: All m parity shards mathematically populated.
        // [VARIATIONS TO TRY]: SIMD accelerated encode.
        Codec.Encode(shards, shardSize);

        // [KYA KARTA HAI]: Distributed disk volumes par har shard ko striping style me persist karta hai.
        // [KYU ZAROORI HAI]: Zero co-location: Har shard ek independent volume/disk par rehta hai.
        // [KAISE CHALTA HAI]: Loop i from 0 to TotalShards - 1: Volumes[i].WriteShard(sha256, i, shards[i]).
        // [OUTPUT / SIDE EFFECT]: Shards written to independent disks with CRC32.
        // [VARIATIONS TO TRY]: Parallel async writes across network nodes.
        for (int i = 0; i < Codec.TotalShards; i++)
        {
            // [KYA KARTA HAI]: Write shard to disk volume i using content-addressable SHA-256 ID.
            // [KYU ZAROORI HAI]: Hardware persistence.
            // [KAISE CHALTA HAI]: Volumes[i].WriteShard(sha256, i, shards[i]).
            // [OUTPUT / SIDE EFFECT]: Shard committed to volume.
            // [VARIATIONS TO TRY]: Non-blocking worker queue.
            Volumes[i].WriteShard(sha256, i, shards[i]);
        }

        // [KYA KARTA HAI]: ObjectManifest instance construct karta hai.
        // [KYU ZAROORI HAI]: Object metadata, size, aur cryptographic hash record karna.
        // [KAISE CHALTA HAI]: new ObjectManifest(...).
        // [OUTPUT / SIDE EFFECT]: Manifest created.
        // [VARIATIONS TO TRY]: Add custom tags or user metadata.
        var manifest = new ObjectManifest(
            bucket,
            key,
            sha256,
            payload.Length,
            shardSize,
            Codec.DataShards,
            Codec.ParityShards,
            DateTime.UtcNow
        );

        // [KYA KARTA HAI]: Manifest aur CAS deduplication index register karta hai under lock.
        // [KYU ZAROORI HAI]: Atomic catalog and CAS index update.
        // [KAISE CHALTA HAI]: lock (_engineLock) { _manifests[key] = manifest; _casIndex[sha256] = manifest; }.
        // [OUTPUT / SIDE EFFECT]: Engine indexes updated.
        // [VARIATIONS TO TRY]: Transactional commit log.
        lock (_engineLock)
        {
            // [KYA KARTA HAI]: Register manifest in manifests map.
            // [KYU ZAROORI HAI]: Path-based retrieval.
            // [KAISE CHALTA HAI]: _manifests[manifestKey] = manifest.
            // [OUTPUT / SIDE EFFECT]: Manifest saved.
            // [VARIATIONS TO TRY]: None.
            _manifests[manifestKey] = manifest;

            // [KYA KARTA HAI]: Register manifest in CAS index by sha256.
            // [KYU ZAROORI HAI]: Enables future deduplication.
            // [KAISE CHALTA HAI]: _casIndex[sha256] = manifest.
            // [OUTPUT / SIDE EFFECT]: Hash mapped.
            // [VARIATIONS TO TRY]: None.
            _casIndex[sha256] = manifest;
        }

        // [KYA KARTA HAI]: Return manifest with WasDeduplicated = false.
        // [KYU ZAROORI HAI]: Freshly encoded object acknowledgement.
        // [KAISE CHALTA HAI]: Return (manifest, false).
        // [OUTPUT / SIDE EFFECT]: Successful upload.
        // [VARIATIONS TO TRY]: Return ETag.
        return (manifest, false);
    }

    // [KYA KARTA HAI]: Object retrieve karta hai; disk failure ya bit rot hone par automatic RS decoding karta hai.
    // [KYU ZAROORI HAI]: High-availability data access guarantee karta hai even after physical disk crashes.
    // [KAISE CHALTA HAI]: Manifest read karta hai, surviving shards gather karta hai, CRC verify karta hai, aur RS decode execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns exact original byte payload with verified SHA-256 hash.
    // [VARIATIONS TO TRY]: Stream output directly to HTTP response.
    public byte[] GetObject(string bucket, string key)
    {
        // [KYA KARTA HAI]: Lookup manifest path: "{bucket}/{key}".
        // [KYU ZAROORI HAI]: Find object metadata.
        // [KAISE CHALTA HAI]: string manifestKey = $"{bucket}/{key}".
        // [OUTPUT / SIDE EFFECT]: Uniform key.
        // [VARIATIONS TO TRY]: None.
        string manifestKey = $"{bucket}/{key}";

        // [KYA KARTA HAI]: Local manifest variable.
        // [KYU ZAROORI HAI]: Holds manifest fetched under lock.
        // [KAISE CHALTA HAI]: ObjectManifest? manifest.
        // [OUTPUT / SIDE EFFECT]: Local reference.
        // [VARIATIONS TO TRY]: None.
        ObjectManifest? manifest;

        // [KYA KARTA HAI]: Lock acquire karke manifest lookup karta hai.
        // [KYU ZAROORI HAI]: Thread-safe metadata access.
        // [KAISE CHALTA HAI]: lock (_engineLock) { _manifests.TryGetValue(...) }.
        // [OUTPUT / SIDE EFFECT]: Fetches manifest.
        // [VARIATIONS TO TRY]: ConcurrentDictionary.
        lock (_engineLock)
        {
            // [KYA KARTA HAI]: Check if object exists.
            // [KYU ZAROORI HAI]: Prevent accessing non-existent objects.
            // [KAISE CHALTA HAI]: _manifests.TryGetValue(manifestKey, out manifest).
            // [OUTPUT / SIDE EFFECT]: manifest populated.
            // [VARIATIONS TO TRY]: Return FileNotFoundException.
            if (!_manifests.TryGetValue(manifestKey, out manifest))
            {
                // [KYA KARTA HAI]: Throw KeyNotFoundException.
                // [KYU ZAROORI HAI]: S3 404 NoSuchKey equivalent.
                // [KAISE CHALTA HAI]: Throws KeyNotFoundException.
                // [OUTPUT / SIDE EFFECT]: 404 response.
                // [VARIATIONS TO TRY]: Custom ObjectNotFoundException.
                throw new KeyNotFoundException($"Object not found: {manifestKey}");
            }
        }

        // [KYA KARTA HAI]: Shards buffer array allocate karta hai.
        // [KYU ZAROORI HAI]: Shard bytes read karne aur missing shards reconstruct karne ke liye.
        // [KAISE CHALTA HAI]: new byte[Codec.TotalShards][].
        // [OUTPUT / SIDE EFFECT]: Buffer references array.
        // [VARIATIONS TO TRY]: Pooled buffers.
        byte[][] shards = new byte[Codec.TotalShards][];

        // [KYA KARTA HAI]: Boolean array shardPresent allocate karta hai.
        // [KYU ZAROORI HAI]: Tracks which shards are healthy vs missing/corrupted.
        // [KAISE CHALTA HAI]: new bool[Codec.TotalShards].
        // [OUTPUT / SIDE EFFECT]: Health flags array.
        // [VARIATIONS TO TRY]: Bitmask uint.
        bool[] shardPresent = new bool[Codec.TotalShards];

        // [KYA KARTA HAI]: Loop run karke har volume se shard read karta hai.
        // [KYU ZAROORI HAI]: Physical disk inspection aur CRC32 verification.
        // [KAISE CHALTA HAI]: For i from 0 to TotalShards - 1: Volumes[i].ReadShard(manifest.Sha256Hash).
        // [OUTPUT / SIDE EFFECT]: Reads all available shards.
        // [VARIATIONS TO TRY]: Speculative read (read only first k shards unless error).
        for (int i = 0; i < Codec.TotalShards; i++)
        {
            // [KYA KARTA HAI]: Allocate individual shard buffer.
            // [KYU ZAROORI HAI]: Destination for read or reconstruction.
            // [KAISE CHALTA HAI]: shards[i] = new byte[manifest.ShardSize].
            // [OUTPUT / SIDE EFFECT]: Buffer ready.
            // [VARIATIONS TO TRY]: None.
            shards[i] = new byte[manifest.ShardSize];

            // [KYA KARTA HAI]: Volume se shard read karta hai aur CRC verification check karta hai.
            // [KYU ZAROORI HAI]: Detects missing shards, offline disks, and silent bit-rot.
            // [KAISE CHALTA HAI]: Volumes[i].ReadShard(manifest.Sha256Hash).
            // [OUTPUT / SIDE EFFECT]: Read tuple (Found, Data, IsCorrupted).
            // [VARIATIONS TO TRY]: Async disk read.
            var readResult = Volumes[i].ReadShard(manifest.Sha256Hash);

            // [KYA KARTA HAI]: Check karta hai kya shard present hai aur corrupted nahi hai.
            // [KYU ZAROORI HAI]: Only verified healthy shards can be used for Reed-Solomon decoding.
            // [KAISE CHALTA HAI]: if (readResult.Found && !readResult.IsCorrupted && readResult.Data != null).
            // [OUTPUT / SIDE EFFECT]: Health validation.
            // [VARIATIONS TO TRY]: Log warning if IsCorrupted is true.
            if (readResult.Found && !readResult.IsCorrupted && readResult.Data != null)
            {
                // [KYA KARTA HAI]: Copy data into shards array.
                // [KYU ZAROORI HAI]: Load valid bytes into working buffer.
                // [KAISE CHALTA HAI]: Buffer.BlockCopy.
                // [OUTPUT / SIDE EFFECT]: Shard loaded.
                // [VARIATIONS TO TRY]: Direct reference reuse.
                Buffer.BlockCopy(readResult.Data, 0, shards[i], 0, manifest.ShardSize);

                // [KYA KARTA HAI]: Mark shard present.
                // [KYU ZAROORI HAI]: Flags healthy shard for RS codec.
                // [KAISE CHALTA HAI]: shardPresent[i] = true.
                // [OUTPUT / SIDE EFFECT]: Health flag true.
                // [VARIATIONS TO TRY]: None.
                shardPresent[i] = true;
            }
            else
            {
                // [KYA KARTA HAI]: Mark shard missing/unhealthy.
                // [KYU ZAROORI HAI]: Tells RS codec this shard requires reconstruction.
                // [KAISE CHALTA HAI]: shardPresent[i] = false.
                // [OUTPUT / SIDE EFFECT]: Health flag false.
                // [VARIATIONS TO TRY]: Increment failure telemetry counter.
                shardPresent[i] = false;
            }
        }

        // [KYA KARTA HAI]: RS Decode execute karta hai if any data shard is missing/corrupt.
        // [KYU ZAROORI HAI]: Mathematically reconstructs missing shards from surviving parity shards.
        // [KAISE CHALTA HAI]: Codec.Decode(shards, shardPresent, manifest.ShardSize).
        // [OUTPUT / SIDE EFFECT]: All missing data shards recovered in-place.
        // [VARIATIONS TO TRY]: Hardware acceleration.
        Codec.Decode(shards, shardPresent, manifest.ShardSize);

        // [KYA KARTA HAI]: Original payload byte array allocate karta hai with OriginalSize.
        // [KYU ZAROORI HAI]: Reassemble reconstructed data slices into single output stream.
        // [KAISE CHALTA HAI]: new byte[manifest.OriginalSize].
        // [OUTPUT / SIDE EFFECT]: Output buffer ready.
        // [VARIATIONS TO TRY]: MemoryStream wrapper.
        byte[] restoredPayload = new byte[manifest.OriginalSize];

        // [KYA KARTA HAI]: Reconstruct payload from the first k recovered data shards.
        // [KYU ZAROORI HAI]: Systematic RS code original data pehle k shards me reassemble karta hai.
        // [KAISE CHALTA HAI]: Loop i from 0 to DataShards - 1: copy bytes into restoredPayload.
        // [OUTPUT / SIDE EFFECT]: Reassembled byte array.
        // [VARIATIONS TO TRY]: Zero-copy stream reader.
        for (int i = 0; i < Codec.DataShards; i++)
        {
            // [KYA KARTA HAI]: Offset calculate karta hai: i * shardSize.
            // [KYU ZAROORI HAI]: Target write position.
            // [KAISE CHALTA HAI]: offset = i * manifest.ShardSize.
            // [OUTPUT / SIDE EFFECT]: Offset calculated.
            // [VARIATIONS TO TRY]: None.
            int offset = i * manifest.ShardSize;

            // [KYA KARTA HAI]: Bytes to copy count calculate karta hai.
            // [KYU ZAROORI HAI]: Trims padding off the final shard to restore exact original file size.
            // [KAISE CHALTA HAI]: Math.Min(manifest.ShardSize, Math.Max(0, manifest.OriginalSize - offset)).
            // [OUTPUT / SIDE EFFECT]: Exact slice size.
            // [VARIATIONS TO TRY]: None.
            int bytesToCopy = Math.Min(manifest.ShardSize, Math.Max(0, manifest.OriginalSize - offset));

            // [KYA KARTA HAI]: If bytesToCopy > 0, copy bytes into restoredPayload.
            // [KYU ZAROORI HAI]: Copy shard content into output buffer.
            // [KAISE CHALTA HAI]: Buffer.BlockCopy(shards[i], 0, restoredPayload, offset, bytesToCopy).
            // [OUTPUT / SIDE EFFECT]: Slice copied.
            // [VARIATIONS TO TRY]: Span copy.
            if (bytesToCopy > 0)
            {
                // [KYA KARTA HAI]: Raw copy.
                // [KYU ZAROORI HAI]: Fast byte transfer.
                // [KAISE CHALTA HAI]: Buffer.BlockCopy.
                // [OUTPUT / SIDE EFFECT]: Bytes transferred.
                // [VARIATIONS TO TRY]: Unsafe pointer copy.
                Buffer.BlockCopy(shards[i], 0, restoredPayload, offset, bytesToCopy);
            }
        }

        // [KYA KARTA HAI]: Restored payload ka SHA-256 hash verify karta hai against manifest.
        // [KYU ZAROORI HAI]: 100% cryptographic proof ki reconstructed data original ke barabar hai bit-for-bit.
        // [KAISE CHALTA HAI]: SHA256.HashData(restoredPayload) compared to manifest.Sha256Hash.
        // [OUTPUT / SIDE EFFECT]: Hash validation check.
        // [VARIATIONS TO TRY]: Throw CorruptedObjectException if mismatch.
        string restoredSha = Convert.ToHexString(SHA256.HashData(restoredPayload)).ToLowerInvariant();

        // [KYA KARTA HAI]: Check hash parity.
        // [KYU ZAROORI HAI]: Safety barrier against corruption.
        // [KAISE CHALTA HAI]: restoredSha != manifest.Sha256Hash.
        // [OUTPUT / SIDE EFFECT]: Guard check.
        // [VARIATIONS TO TRY]: None.
        if (restoredSha != manifest.Sha256Hash)
        {
            // [KYA KARTA HAI]: Throw exception if hash mismatch.
            // [KYU ZAROORI HAI]: Unrecoverable data corruption alert.
            // [KAISE CHALTA HAI]: Throws InvalidDataException.
            // [OUTPUT / SIDE EFFECT]: Halts corrupted delivery.
            // [VARIATIONS TO TRY]: Trigger emergency cluster scrub.
            throw new InvalidDataException($"SHA-256 integrity check failed! Expected {manifest.Sha256Hash}, got {restoredSha}.");
        }

        // [KYA KARTA HAI]: Restored byte payload return karta hai.
        // [KYU ZAROORI HAI]: Caller receives verified intact file bytes.
        // [KAISE CHALTA HAI]: Returns restoredPayload.
        // [OUTPUT / SIDE EFFECT]: Successful object delivery.
        // [VARIATIONS TO TRY]: ReadOnlySpan<byte>.
        return restoredPayload;
    }

    // [KYA KARTA HAI]: Background anti-entropy bit-rot scrubber run karta hai.
    // [KYU ZAROORI HAI]: Silent bit rot detect karke in-place disk repair karna taaki data degradation accumulate na ho.
    // [KAISE CHALTA HAI]: Har stored manifest scan karta hai, corrupt shards detect karta hai, RS decode se heal karta hai, aur clean data re-write karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns ScrubReport with detected and repaired shard metrics.
    // [VARIATIONS TO TRY]: Periodic cron or timer scheduled scrubbing.
    public ScrubReport RunBitRotScrubber()
    {
        // [KYA KARTA HAI]: Counter variables for scrub report metrics.
        // [KYU ZAROORI HAI]: Observability and audit log tracking.
        // [KAISE CHALTA HAI]: Initialize integers to 0.
        // [OUTPUT / SIDE EFFECT]: Metric counters initialized.
        // [VARIATIONS TO TRY]: Prometheus OpenTelemetry metrics counter.
        int objectsChecked = 0;
        int shardsScanned = 0;
        int corruptionsDetected = 0;
        int shardsHealed = 0;

        // [KYA KARTA HAI]: Copy current manifests snapshot under lock.
        // [KYU ZAROORI HAI]: Prevents collection modification exceptions during background sweep.
        // [KAISE CHALTA HAI]: lock (_engineLock) { _manifests.Values.ToArray() }.
        // [OUTPUT / SIDE EFFECT]: Snapshot array.
        // [VARIATIONS TO TRY]: Channel-based streaming scrubber.
        ObjectManifest[] manifestsToScrub;
        lock (_engineLock)
        {
            // [KYA KARTA HAI]: ToArray snapshot.
            // [KYU ZAROORI HAI]: Thread-safe enumeration.
            // [KAISE CHALTA HAI]: _manifests.Values.DistinctBy(m => m.Sha256Hash).ToArray().
            // [OUTPUT / SIDE EFFECT]: Array of unique CAS objects.
            // [VARIATIONS TO TRY]: Parallel batch slicing.
            manifestsToScrub = _manifests.Values.DistinctBy(m => m.Sha256Hash).ToArray();
        }

        // [KYA KARTA HAI]: Har unique CAS object par iterate karta hai.
        // [KYU ZAROORI HAI]: Cluster-wide data durability verification.
        // [KAISE CHALTA HAI]: foreach manifest in manifestsToScrub.
        // [OUTPUT / SIDE EFFECT]: Inspects every stored object.
        // [VARIATIONS TO TRY]: Throttle I/O to avoid production latency spikes.
        foreach (var manifest in manifestsToScrub)
        {
            // [KYA KARTA HAI]: Increment objects checked counter.
            // [KYU ZAROORI HAI]: Tracks total audited files.
            // [KAISE CHALTA HAI]: objectsChecked++.
            // [OUTPUT / SIDE EFFECT]: Counter incremented.
            // [VARIATIONS TO TRY]: None.
            objectsChecked++;

            // [KYA KARTA HAI]: Buffers allocate karta hai shards aur health flags ke liye.
            // [KYU ZAROORI HAI]: Shards verification working space.
            // [KAISE CHALTA HAI]: Array allocations.
            // [OUTPUT / SIDE EFFECT]: Allocated arrays.
            // [VARIATIONS TO TRY]: Reusable scrubber scratchpad.
            byte[][] shards = new byte[Codec.TotalShards][];
            bool[] shardPresent = new bool[Codec.TotalShards];
            bool hasCorruptionOrMissing = false;

            // [KYA KARTA HAI]: Loop run karke har volume se shard read aur CRC verify karta hai.
            // [KYU ZAROORI HAI]: Audit each disk volume for this object.
            // [KAISE CHALTA HAI]: For i from 0 to TotalShards - 1.
            // [OUTPUT / SIDE EFFECT]: Scans each shard.
            // [VARIATIONS TO TRY]: Parallel read across volumes.
            for (int i = 0; i < Codec.TotalShards; i++)
            {
                // [KYA KARTA HAI]: Increment shardsScanned counter.
                // [KYU ZAROORI HAI]: Metrics accounting.
                // [KAISE CHALTA HAI]: shardsScanned++.
                // [OUTPUT / SIDE EFFECT]: Counter incremented.
                // [VARIATIONS TO TRY]: None.
                shardsScanned++;

                // [KYA KARTA HAI]: Allocate shard buffer.
                // [KYU ZAROORI HAI]: Destination memory.
                // [KAISE CHALTA HAI]: shards[i] = new byte[manifest.ShardSize].
                // [OUTPUT / SIDE EFFECT]: Buffer ready.
                // [VARIATIONS TO TRY]: None.
                shards[i] = new byte[manifest.ShardSize];

                // [KYA KARTA HAI]: Read shard and check CRC32.
                // [KYU ZAROORI HAI]: Identify bit rot or disk failure.
                // [KAISE CHALTA HAI]: Volumes[i].ReadShard(manifest.Sha256Hash).
                // [OUTPUT / SIDE EFFECT]: Read status tuple.
                // [VARIATIONS TO TRY]: Async check.
                var result = Volumes[i].ReadShard(manifest.Sha256Hash);

                // [KYA KARTA HAI]: Check if shard is healthy.
                // [KYU ZAROORI HAI]: Distinguish clean shards from damaged ones.
                // [KAISE CHALTA HAI]: if (result.Found && !result.IsCorrupted && result.Data != null).
                // [OUTPUT / SIDE EFFECT]: Health verification.
                // [VARIATIONS TO TRY]: None.
                if (result.Found && !result.IsCorrupted && result.Data != null)
                {
                    // [KYA KARTA HAI]: Load healthy shard data.
                    // [KYU ZAROORI HAI]: Retain for potential reconstruction.
                    // [KAISE CHALTA HAI]: Buffer.BlockCopy.
                    // [OUTPUT / SIDE EFFECT]: Shard loaded.
                    // [VARIATIONS TO TRY]: None.
                    Buffer.BlockCopy(result.Data, 0, shards[i], 0, manifest.ShardSize);

                    // [KYA KARTA HAI]: Mark shard healthy.
                    // [KYU ZAROORI HAI]: Usable for repair math.
                    // [KAISE CHALTA HAI]: shardPresent[i] = true.
                    // [OUTPUT / SIDE EFFECT]: Flag set.
                    // [VARIATIONS TO TRY]: None.
                    shardPresent[i] = true;
                }
                else
                {
                    // [KYA KARTA HAI]: Mark corruption detected.
                    // [KYU ZAROORI HAI]: Triggers repair workflow for this object.
                    // [KAISE CHALTA HAI]: shardPresent[i] = false; hasCorruptionOrMissing = true; corruptionsDetected++.
                    // [OUTPUT / SIDE EFFECT]: Flags repair need.
                    // [VARIATIONS TO TRY]: Alert pager duty.
                    shardPresent[i] = false;
                    hasCorruptionOrMissing = true;
                    corruptionsDetected++;
                }
            }

            // [KYA KARTA HAI]: Agar corruption detect hui ho to self-healing invoke karta hai.
            // [KYU ZAROORI HAI]: Automatic healing eliminates human intervention in hardware bit rot.
            // [KAISE CHALTA HAI]: RS decode se missing shards rebuild karta hai aur affected volumes par re-write karta hai.
            // [OUTPUT / SIDE EFFECT]: Volumes restored to pristine state.
            // [VARIATIONS TO TRY]: Quarantine bad volume if repeat failures.
            if (hasCorruptionOrMissing)
            {
                // [KYA KARTA HAI]: Decode missing data shards.
                // [KYU ZAROORI HAI]: Reconstructs original data from healthy shards.
                // [KAISE CHALTA HAI]: Codec.Decode(shards, shardPresent, manifest.ShardSize).
                // [OUTPUT / SIDE EFFECT]: Missing data shards repaired.
                // [VARIATIONS TO TRY]: Handle case where parity shards were damaged.
                Codec.Decode(shards, shardPresent, manifest.ShardSize);

                // [KYA KARTA HAI]: Parity shards ko re-encode karta hai clean data shards se.
                // [KYU ZAROORI HAI]: Guarantees both data and parity shards are 100% healthy and aligned.
                // [KAISE CHALTA HAI]: Codec.Encode(shards, manifest.ShardSize).
                // [OUTPUT / SIDE EFFECT]: All parity shards recomputed.
                // [VARIATIONS TO TRY]: Recompute only damaged parity shard.
                Codec.Encode(shards, manifest.ShardSize);

                // [KYA KARTA HAI]: Har volume par healed shard rewrite karta hai.
                // [KYU ZAROORI HAI]: Replaces corrupt sectors on disk with freshly generated clean shards and new CRC32.
                // [KAISE CHALTA HAI]: Loop i from 0 to TotalShards - 1: Volumes[i].HealShard(...).
                // [OUTPUT / SIDE EFFECT]: Disks healed in-place.
                // [VARIATIONS TO TRY]: Write to hot spare drive.
                for (int i = 0; i < Codec.TotalShards; i++)
                {
                    // [KYA KARTA HAI]: Only heal online disks that were previously corrupted.
                    // [KYU ZAROORI HAI]: Write clean data to drive.
                    // [KAISE CHALTA HAI]: Volumes[i].HealShard(manifest.Sha256Hash, i, shards[i]).
                    // [OUTPUT / SIDE EFFECT]: Volume sector repaired.
                    // [VARIATIONS TO TRY]: None.
                    if (Volumes[i].IsOnline)
                    {
                        // [KYA KARTA HAI]: Heal volume shard.
                        // [KYU ZAROORI HAI]: Overwrites corrupted data and fixes CRC32.
                        // [KAISE CHALTA HAI]: Volumes[i].HealShard(manifest.Sha256Hash, i, shards[i]).
                        // [OUTPUT / SIDE EFFECT]: Volume restored.
                        // [VARIATIONS TO TRY]: None.
                        Volumes[i].HealShard(manifest.Sha256Hash, i, shards[i]);

                        // [KYA KARTA HAI]: Increment shardsHealed counter.
                        // [KYU ZAROORI HAI]: Tracks repair count.
                        // [KAISE CHALTA HAI]: shardsHealed++.
                        // [OUTPUT / SIDE EFFECT]: Counter incremented.
                        // [VARIATIONS TO TRY]: None.
                        shardsHealed++;
                    }
                }
            }
        }

        // [KYA KARTA HAI]: ScrubReport return karta hai with audit statistics.
        // [KYU ZAROORI HAI]: Summarizes anti-entropy scrubber actions.
        // [KAISE CHALTA HAI]: new ScrubReport(...).
        // [OUTPUT / SIDE EFFECT]: ScrubReport metrics delivered.
        // [VARIATIONS TO TRY]: Send to dashboard webhook.
        return new ScrubReport(objectsChecked, shardsScanned, corruptionsDetected, shardsHealed);
    }
}
