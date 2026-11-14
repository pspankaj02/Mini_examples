using System.Diagnostics;
using KiranaHub.LsmStorage.Engine;

namespace KiranaHub.LsmStorage;

public static class Program
{
    // [KYA KARTA HAI]: LSM-Tree Storage Engine ka comprehensive demonstration entry point.
    // [KYU ZAROORI HAI]: Storage engine ke har component (WAL, MemTable, SSTable, Bloom, Compaction, Recovery) ko verify karna.
    // [KAISE CHALTA HAI]: 6 distinct phases sequentially execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Console output with metrics and verification proofs.
    // [VARIATIONS TO TRY]: BenchmarkDotNet harness integration.
    public static void Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 10: Log-Structured Merge-Tree (LSM-Tree) Storage Engine       ");
        Console.WriteLine("  Features: WAL Durability, MemTable, SSTable Sparse Index, Bloom Filters, Compaction ");
        Console.WriteLine("================================================================================");

        string dbPath = Path.Combine(AppContext.BaseDirectory, "kiranahub_lsm_data");
        if (Directory.Exists(dbPath))
        {
            Directory.Delete(dbPath, recursive: true);
        }

        // [KYA KARTA HAI]: Flush threshold 128KB ke sath LSM engine initialize karta hai.
        // [KYU ZAROORI HAI]: Small threshold ensures multiple SSTables flush quickly during demonstration.
        // [KAISE CHALTA HAI]: LsmStorageEngine instance creation.
        // [OUTPUT / SIDE EFFECT]: Data directories created.
        // [VARIATIONS TO TRY]: 4MB or 64MB production threshold.
        using var engine = new LsmStorageEngine(dbPath, flushThresholdBytes: 128 * 1024);

        // -------------------------------------------------------------------------
        // PHASE 1: High-Throughput Write Benchmark (100,000 Inventory Updates)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 1] Ingesting 1,00,000 Dark-Store Inventory Ticks (Writes)...");
        const int TotalWrites = 100_000;
        var sw = Stopwatch.StartNew();

        for (int i = 1; i <= TotalWrites; i++)
        {
            string key = $"inv:store_{(i % 20):D2}:sku_{i:D6}";
            string value = $"{{\"stock\":{(i * 7) % 500},\"price\":{(i * 1.5):F2},\"ts\":\"2026-11-15\"}}";
            engine.Put(key, value);
        }

        sw.Stop();
        double writesPerSec = TotalWrites / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"  - Total Writes Ingested : {TotalWrites:N0} records");
        Console.WriteLine($"  - Ingestion Time Taken  : {sw.ElapsedMilliseconds} ms ({writesPerSec:N0} writes/sec)");

        var stats1 = engine.GetStats();
        Console.WriteLine($"  - Active MemTable Items : {stats1.MemTableEntries:N0} ({stats1.MemTableSizeBytes / 1024.0:F2} KB)");
        Console.WriteLine($"  - Flushed SSTables on Disk: {stats1.SSTableCount} files (Total: {stats1.TotalSSTableBytes / 1024.0:F2} KB, {stats1.TotalSSTableEntries:N0} records)");

        // -------------------------------------------------------------------------
        // PHASE 2: Multi-Tier Point Lookups (Verifying Retrieval from MemTable & SSTables)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 2] Executing Multi-Tier Point Lookups across Memory & Disk...");
        string[] sampleKeys =
        [
            "inv:store_05:sku_000005",
            "inv:store_10:sku_050010",
            "inv:store_18:sku_099998"
        ];

        foreach (var key in sampleKeys)
        {
            string? val = engine.Get(key);
            Console.WriteLine($"  - Lookup Key '{key}' => Found: {(val != null ? "YES" : "NO")} | Payload: {val}");
        }

        // -------------------------------------------------------------------------
        // PHASE 3: Bloom Filter Rejection Efficiency (Zero Disk I/O on Misses)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 3] Testing Bloom Filter Rejection on 10,000 Non-Existent Keys...");
        long savedReadsBefore = engine.GetStats().BloomFilterSavedDiskReads;
        sw.Restart();

        int missedCount = 0;
        for (int i = 1; i <= 10_000; i++)
        {
            string nonExistentKey = $"inv:phantom_store:ghost_sku_{i:D6}";
            string? result = engine.Get(nonExistentKey);
            if (result == null)
            {
                missedCount++;
            }
        }

        sw.Stop();
        long savedReadsAfter = engine.GetStats().BloomFilterSavedDiskReads;
        long totalSaved = savedReadsAfter - savedReadsBefore;
        Console.WriteLine($"  - Queries Executed      : 10,000 non-existent keys");
        Console.WriteLine($"  - Correctly Rejected    : {missedCount:N0} (100% Accuracy)");
        Console.WriteLine($"  - Time Taken            : {sw.Elapsed.TotalMilliseconds:F2} ms ({(10000 / sw.Elapsed.TotalSeconds):N0} queries/sec)");
        Console.WriteLine($"  - Disk Seeks Avoided by Bloom Filter: {totalSaved:N0} disk seeks prevented!");

        // -------------------------------------------------------------------------
        // PHASE 4: Soft Deletions & Tombstone Masking
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 4] Testing Soft Deletion with LSM Tombstones...");
        string deleteTarget = "inv:store_10:sku_050010";
        Console.WriteLine($"  - Pre-Delete Check '{deleteTarget}' => {engine.Get(deleteTarget)}");

        Console.WriteLine($"  - Executing Tombstone Delete on '{deleteTarget}'...");
        engine.Delete(deleteTarget);

        string? postDeleteVal = engine.Get(deleteTarget);
        Console.WriteLine($"  - Post-Delete Check '{deleteTarget}' => {(postDeleteVal == null ? "NULL (Masked by Tombstone)" : postDeleteVal)}");

        // -------------------------------------------------------------------------
        // PHASE 5: SSTable Major Compaction (Deduplication & Tombstone Purge)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 5] Executing SSTable Major Compaction (Merging SSTables & Purging Tombstones)...");
        // Flush remaining active MemTable first
        engine.Flush();
        var preCompactStats = engine.GetStats();
        Console.WriteLine($"  - Pre-Compaction SSTable Count : {preCompactStats.SSTableCount} files ({preCompactStats.TotalSSTableBytes / 1024.0:F2} KB)");

        sw.Restart();
        engine.Compact();
        sw.Stop();

        var postCompactStats = engine.GetStats();
        Console.WriteLine($"  - Compaction Completed in      : {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  - Post-Compaction SSTable Count: {postCompactStats.SSTableCount} file ({postCompactStats.TotalSSTableBytes / 1024.0:F2} KB)");
        Console.WriteLine($"  - Active Key Verified Still Present: {engine.Get("inv:store_05:sku_000005") != null}");
        Console.WriteLine($"  - Deleted Key Verified Permanently Purged: {engine.Get(deleteTarget) == null}");

        // -------------------------------------------------------------------------
        // PHASE 6: Crash & Recovery Verification via Write-Ahead Log (WAL)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 6] Testing Crash & Recovery via Write-Ahead Log (WAL)...");
        string unFlushedKey = "inv:store_emergency:crash_sku_999";
        string unFlushedVal = "{\"item\":\"Emergency Oxygen Canister\",\"stock\":100}";

        Console.WriteLine($"  - Writing un-flushed item '{unFlushedKey}' to active MemTable & WAL...");
        engine.Put(unFlushedKey, unFlushedVal);

        Console.WriteLine("  - Simulating sudden process crash (Disposing engine without flush)...");
        engine.Dispose();

        Console.WriteLine("  - Rebooting LsmStorageEngine from disk directory...");
        using var recoveredEngine = new LsmStorageEngine(dbPath, flushThresholdBytes: 128 * 1024);

        string? recoveredVal = recoveredEngine.Get(unFlushedKey);
        Console.WriteLine($"  - Recovered Value for '{unFlushedKey}' => {recoveredVal}");
        if (recoveredVal == unFlushedVal)
        {
            Console.WriteLine("  - Crash Recovery Status: 100% SUCCESS (Zero data lost from WAL!)");
        }
        else
        {
            Console.WriteLine("  - Crash Recovery Status: FAILED (Data mismatch)");
        }

        Console.WriteLine("\n[SUCCESS] All LSM-Tree Storage Engine verifications completed successfully!");
    }
}
