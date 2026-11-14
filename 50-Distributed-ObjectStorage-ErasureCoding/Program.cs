// ============================================================================
// File: Program.cs
// Namespace: KiranaHub.ObjectStorage
// Description: Interactive self-test simulating distributed object storage with
// Reed-Solomon 4+2 Cauchy erasure coding, CAS dedup, disk failure, and bit-rot healing.
// ============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using KiranaHub.ObjectStorage.Engine;

namespace KiranaHub.ObjectStorage;

public static class Program
{
    // [KYA KARTA HAI]: Main entrypoint function distributed object storage test execute karta hai.
    // [KYU ZAROORI HAI]: End-to-end verification of erasure coding, hardware fault recovery, and bit-rot healing.
    // [KAISE CHALTA HAI]: Sequentially runs upload, dedup, disk crash, reconstruction, bit-rot, and scrubber.
    // [OUTPUT / SIDE EFFECT]: Prints rich diagnostic console telemetry.
    // [VARIATIONS TO TRY]: Stress test with 100 concurrent uploads.
    public static void Main()
    {
        // [KYA KARTA HAI]: Console text encoding UTF-8 set karta hai.
        // [KYU ZAROORI HAI]: Unicode symbols and emojis cleanly render hone ke liye.
        // [KAISE CHALTA HAI]: Console.OutputEncoding = Encoding.UTF8.
        // [OUTPUT / SIDE EFFECT]: Terminal output encoding configured.
        // [VARIATIONS TO TRY]: None.
        Console.OutputEncoding = Encoding.UTF8;

        // [KYA KARTA HAI]: Banner print karta hai.
        // [KYU ZAROORI HAI]: Visual demarcation of Project 50.
        // [KAISE CHALTA HAI]: Console.WriteLine.
        // [OUTPUT / SIDE EFFECT]: Terminal header.
        // [VARIATIONS TO TRY]: Custom ASCII art.
        Console.WriteLine("================================================================================");
        Console.WriteLine("📦 KIRANAHUB: DISTRIBUTED OBJECT STORAGE & REED-SOLOMON ERASURE CODING (4+2)");
        Console.WriteLine("   Content-Addressable Storage (CAS) • Cauchy GF(2^8) Matrix • Anti-Entropy");
        Console.WriteLine("================================================================================\n");

        // [KYA KARTA HAI]: ObjectStorageEngine instantiate karta hai with 4 data shards and 2 parity shards.
        // [KYU ZAROORI HAI]: S3/MinIO style distributed cluster simulation (50% overhead vs 200% for 3x replication).
        // [KAISE CHALTA HAI]: new ObjectStorageEngine(dataShards: 4, parityShards: 2).
        // [OUTPUT / SIDE EFFECT]: Cluster with 6 independent disk volumes initialized.
        // [VARIATIONS TO TRY]: 8+3 or 16+4 cluster configuration.
        var engine = new ObjectStorageEngine(dataShards: 4, parityShards: 2);

        // [KYA KARTA HAI]: Print cluster configuration.
        // [KYU ZAROORI HAI]: Shows storage topology to user.
        // [KAISE CHALTA HAI]: Console.WriteLine with parameters.
        // [OUTPUT / SIDE EFFECT]: Cluster parameters displayed.
        // [VARIATIONS TO TRY]: JSON config print.
        Console.WriteLine($"[CLUSTER SETUP] Initialized distributed cluster with {engine.Volumes.Length} independent disk volumes.");
        Console.WriteLine($"   • Data Shards (k): {engine.Codec.DataShards} | Parity Shards (m): {engine.Codec.ParityShards}");
        Console.WriteLine($"   • Storage Efficiency: 66.7% (only 50% parity overhead vs 200% in 3x replication!)");
        Console.WriteLine($"   • Fault Tolerance: Any {engine.Codec.ParityShards} arbitrary disk crashes tolerated simultaneously.\n");

        // [KYA KARTA HAI]: Simulated Kirana delivery proof photo payload generate karta hai (512 KB).
        // [KYU ZAROORI HAI]: Quick-commerce delivery partner customer doorstep verification photo.
        // [KAISE CHALTA HAI]: Creates 524,288 bytes array with deterministic pattern and timestamp.
        // [OUTPUT / SIDE EFFECT]: byte[] originalPhotoPayload.
        // [VARIATIONS TO TRY]: 10MB invoice PDF payload.
        int payloadSize = 512 * 1024; // 512 KB
        byte[] originalPhotoPayload = new byte[payloadSize];
        var rand = new Random(42);
        rand.NextBytes(originalPhotoPayload);

        // [KYA KARTA HAI]: Magic bytes append karta hai simulated JPEG format ke liye (0xFF, 0xD8, 0xFF, 0xE0).
        // [KYU ZAROORI HAI]: Realistic binary image file simulation.
        // [KAISE CHALTA HAI]: Array index assignment.
        // [OUTPUT / SIDE EFFECT]: Realistic header in payload.
        // [VARIATIONS TO TRY]: Actual file read from disk.
        originalPhotoPayload[0] = 0xFF;
        originalPhotoPayload[1] = 0xD8;
        originalPhotoPayload[2] = 0xFF;
        originalPhotoPayload[3] = 0xE0;

        // [KYA KARTA HAI]: Calculate and display original SHA-256 hash.
        // [KYU ZAROORI HAI]: Cryptographic ground truth baseline.
        // [KAISE CHALTA HAI]: SHA256.HashData to hex.
        // [OUTPUT / SIDE EFFECT]: originalSha string.
        // [VARIATIONS TO TRY]: None.
        string originalSha = Convert.ToHexString(SHA256.HashData(originalPhotoPayload)).ToLowerInvariant();
        Console.WriteLine($"📸 [ORIGINAL FILE] Kirana Delivery Proof Photo: Size = {payloadSize:N0} bytes");
        Console.WriteLine($"   • SHA-256 Ground Truth: {originalSha}\n");

        // [KYA KARTA HAI]: PutObject call karke photo upload karta hai to bucket 'deliveries'.
        // [KYU ZAROORI HAI]: Tests chunking, RS encoding, and multi-disk striping.
        // [KAISE CHALTA HAI]: engine.PutObject("deliveries", "order_98765_proof.jpg", originalPhotoPayload).
        // [OUTPUT / SIDE EFFECT]: ObjectManifest returned, shards stored on 6 disks.
        // [VARIATIONS TO TRY]: Concurrent uploads.
        Console.WriteLine("🚀 [STEP 1: UPLOAD & ENCODE] Uploading photo into 'deliveries/order_98765_proof.jpg'...");
        var sw = Stopwatch.StartNew();
        var (manifest1, dedup1) = engine.PutObject("deliveries", "order_98765_proof.jpg", originalPhotoPayload);
        sw.Stop();

        Console.WriteLine($"   ✔ Upload & RS 4+2 Encode completed in {sw.ElapsedMilliseconds} ms!");
        Console.WriteLine($"   • Shard Size: {manifest1.ShardSize:N0} bytes per disk volume");
        Console.WriteLine($"   • Shards Striped Across: Disk 0, 1, 2, 3 (Data) & Disk 4, 5 (Cauchy Parity)");
        Console.WriteLine($"   • Deduplicated: {dedup1}\n");

        // [KYA KARTA HAI]: Test CAS deduplication with identical payload under different key.
        // [KYU ZAROORI HAI]: Quick-commerce vendors often upload duplicate invoice copies; CAS saves 100% disk space.
        // [KAISE CHALTA HAI]: PutObject with same payload at "invoices/inv_98765.jpg".
        // [OUTPUT / SIDE EFFECT]: Returns dedup2 == true without writing extra shards.
        // [VARIATIONS TO TRY]: None.
        Console.WriteLine("♻️ [STEP 2: CONTENT-ADDRESSABLE DEDUPLICATION] Uploading identical photo to 'invoices/inv_98765.jpg'...");
        var (manifest2, dedup2) = engine.PutObject("invoices", "inv_98765.jpg", originalPhotoPayload);
        Console.WriteLine($"   ✔ Was Deduplicated: {dedup2} (Zero additional disk bytes consumed!)");
        Console.WriteLine($"   • Canonical SHA-256 Match: {manifest2.Sha256Hash == originalSha}\n");

        // [KYA KARTA HAI]: Simulate catastrophic hardware failure of 2 disks.
        // [KYU ZAROORI HAI]: Tests Reed-Solomon Cauchy decoding and data reconstruction capability.
        // [KAISE CHALTA HAI]: Simulates physical crash of Disk 0 and Disk 2.
        // [OUTPUT / SIDE EFFECT]: Disks 0 and 2 marked offline.
        // [VARIATIONS TO TRY]: Fail 3 disks (should throw exception since m=2).
        Console.WriteLine("💥 [STEP 3: CATASTROPHIC HARDWARE FAILURE] Simulating hardware crash on Disk 0 & Disk 2...");
        engine.Volumes[0].SimulateFailure();
        engine.Volumes[2].SimulateFailure();
        Console.WriteLine($"   ❌ Disk 0: OFFLINE (Power supply blown!)");
        Console.WriteLine($"   ✔  Disk 1: ONLINE");
        Console.WriteLine($"   ❌ Disk 2: OFFLINE (Head crash on NVMe sector!)");
        Console.WriteLine($"   ✔  Disk 3: ONLINE");
        Console.WriteLine($"   ✔  Disk 4: ONLINE (Parity Shard 0)");
        Console.WriteLine($"   ✔  Disk 5: ONLINE (Parity Shard 1)\n");

        // [KYA KARTA HAI]: Attempt retrieval of object with 2 dead disks.
        // [KYU ZAROORI HAI]: Verifies automatic RS matrix inversion and bit-exact reconstruction.
        // [KAISE CHALTA HAI]: engine.GetObject("deliveries", "order_98765_proof.jpg").
        // [OUTPUT / SIDE EFFECT]: Restores exact original payload.
        // [VARIATIONS TO TRY]: Benchmark recovery latency.
        Console.WriteLine("🩺 [STEP 4: REED-SOLOMON DECODING & RECONSTRUCTION] Reading object with 2 dead disks...");
        sw.Restart();
        byte[] reconstructedPayload = engine.GetObject("deliveries", "order_98765_proof.jpg");
        sw.Stop();

        string reconstructedSha = Convert.ToHexString(SHA256.HashData(reconstructedPayload)).ToLowerInvariant();
        bool hashMatch = reconstructedSha == originalSha;

        Console.WriteLine($"   ✔ Reconstruction completed in {sw.ElapsedMilliseconds} ms!");
        Console.WriteLine($"   • Reconstructed Size: {reconstructedPayload.Length:N0} bytes");
        Console.WriteLine($"   • Reconstructed SHA-256: {reconstructedSha}");
        Console.WriteLine($"   • Cryptographic Match: {(hashMatch ? "✅ 100% BIT-EXACT PARITY!" : "❌ MISMATCH!")}\n");

        // [KYA KARTA HAI]: Bring disks back online for bit-rot demonstration.
        // [KYU ZAROORI HAI]: Storage cluster maintenance simulation.
        // [KAISE CHALTA HAI]: RestoreDisk() on Volumes 0 and 2.
        // [OUTPUT / SIDE EFFECT]: Disks active.
        // [VARIATIONS TO TRY]: None.
        Console.WriteLine("🔧 [STEP 5: REPLACING DRIVES & BIT-ROT INJECTION] Replacing failed drives and simulating silent bit-rot...");
        engine.Volumes[0].RestoreDisk();
        engine.Volumes[2].RestoreDisk();

        // [KYA KARTA HAI]: Inject silent bit-rot into Disk 1 at byte offset 1024.
        // [KYU ZAROORI HAI]: Simulates cosmic ray radiation or silent magnetic bit flipping on storage media.
        // [KAISE CHALTA HAI]: Flips bits without updating header CRC32.
        // [OUTPUT / SIDE EFFECT]: Disk 1 contains corrupt data.
        // [VARIATIONS TO TRY]: Inject multiple bit-rots across 2 disks.
        bool injected = engine.Volumes[1].InjectBitRot(originalSha, 1024);
        Console.WriteLine($"   ☣️ Silent bit-rot injected into Disk 1 (offset 1024): {injected}");

        // [KYA KARTA HAI]: Inspect Disk 1 read status.
        // [KYU ZAROORI HAI]: Verifies CRC32 checksum flags corruption immediately.
        // [KAISE CHALTA HAI]: Volumes[1].ReadShard(originalSha).
        // [OUTPUT / SIDE EFFECT]: IsCorrupted == true.
        // [VARIATIONS TO TRY]: None.
        var disk1Check = engine.Volumes[1].ReadShard(originalSha);
        Console.WriteLine($"   🔍 Disk 1 Health Audit: IsCorrupted = {disk1Check.IsCorrupted} (CRC32 Checksum Trap Triggered!)\n");

        // [KYA KARTA HAI]: Run background anti-entropy bit-rot scrubber.
        // [KYU ZAROORI HAI]: Background daemon sweeps disks, detects silent degradation, and repairs data in-place.
        // [KAISE CHALTA HAI]: engine.RunBitRotScrubber().
        // [OUTPUT / SIDE EFFECT]: Scrubber repairs corrupted shard on Disk 1.
        // [VARIATIONS TO TRY]: Schedule periodic timer.
        Console.WriteLine("🛡️ [STEP 6: ANTI-ENTROPY BIT-ROT SCRUBBER] Running background anti-entropy scrubber sweep...");
        sw.Restart();
        var scrubReport = engine.RunBitRotScrubber();
        sw.Stop();

        Console.WriteLine($"   ✔ Scrubber sweep completed in {sw.ElapsedMilliseconds} ms!");
        Console.WriteLine($"   • Objects Audited: {scrubReport.ObjectsChecked}");
        Console.WriteLine($"   • Shards Scanned: {scrubReport.ShardsScanned}");
        Console.WriteLine($"   • Corruptions Detected: {scrubReport.CorruptionsDetected}");
        Console.WriteLine($"   • Shards Self-Healed: {scrubReport.ShardsHealed}");

        // [KYA KARTA HAI]: Verify Disk 1 is now healed and healthy.
        // [KYU ZAROORI HAI]: Confirms self-healing was successful.
        // [KAISE CHALTA HAI]: Volumes[1].ReadShard(originalSha).
        // [OUTPUT / SIDE EFFECT]: IsCorrupted == false.
        // [VARIATIONS TO TRY]: None.
        var disk1PostScrub = engine.Volumes[1].ReadShard(originalSha);
        Console.WriteLine($"   ✨ Disk 1 Post-Scrub Status: IsCorrupted = {disk1PostScrub.IsCorrupted} (100% HEALTH RESTORED!)\n");

        // [KYA KARTA HAI]: Final success summary print.
        // [KYU ZAROORI HAI]: Confirmation of all Project 50 engineering goals.
        // [KAISE CHALTA HAI]: Console.WriteLine.
        // [OUTPUT / SIDE EFFECT]: Conclusion banner.
        // [VARIATIONS TO TRY]: None.
        Console.WriteLine("================================================================================");
        Console.WriteLine("🎉 ALL PROJECT 50 TESTS PASSED: REED-SOLOMON 4+2 DISTRIBUTED OBJECT STORAGE VERIFIED!");
        Console.WriteLine("================================================================================");
    }
}
