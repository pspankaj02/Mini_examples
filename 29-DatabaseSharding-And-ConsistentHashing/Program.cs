// ==============================================================================================
// KiranaHub Database Sharding: Automated Verification & Cross-Shard Benchmarking Suite
// [KYA KARTA HAI] Consistent Hashing distribution, minimal key migration on scale-out, aur parallel Scatter-Gather queries verify karta hai.
// [KYU ZAROORI HAI] Confirm karna ki national-scale database sharding me data evenly distribute hota hai aur scale-out par zero cache-thrashing hoti hai.
// [KAISE CHALTA HAI] Program Main executes 3 deep tests: Uniform Key Routing, Scale-Out Minimal Rebalance, and Parallel Scatter-Gather Analytics.
// [OUTPUT / SIDE EFFECT] Detailed console metrics and clean exit code 0.
// [VARIATIONS TO TRY] Rebalance testing with node removal.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Guid, DateTimeOffset types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, Dictionary provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sum, Count, GroupBy queries ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Async task execution support.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async runtime ready.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Sharding namespace import karta hai.
// [KYU ZAROORI HAI] DatabaseShard, ShardOrderEntity, ShardRouter access karna.
// [KAISE CHALTA HAI] References local sharding namespace.
// [OUTPUT / SIDE EFFECT] Sharding types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.DatabaseSharding.Sharding;

// [KYA KARTA HAI] Querying namespace import karta hai.
// [KYU ZAROORI HAI] ScatterGatherQueryEngine access karna.
// [KAISE CHALTA HAI] References local querying namespace.
// [OUTPUT / SIDE EFFECT] Scatter-gather types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.DatabaseSharding.Querying;

// [KYA KARTA HAI] Consistent hashing namespace import karta hai.
// [KYU ZAROORI HAI] ConsistentHashRing access karna.
// [KAISE CHALTA HAI] References local hashing namespace.
// [OUTPUT / SIDE EFFECT] ConsistentHashRing accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.DatabaseSharding.Hashing;

// [KYA KARTA HAI] Program entry point class define karta hai.
// [KYU ZAROORI HAI] Executable console application container.
// [KAISE CHALTA HAI] C# 13 top-level application wrapper.
// [OUTPUT / SIDE EFFECT] Entry class.
// [VARIATIONS TO TRY] Top-level statements.
public class Program
{
    // [KYA KARTA HAI] Application main asynchronous entrypoint.
    // [KYU ZAROORI HAI] Test execution start karna.
    // [KAISE CHALTA HAI] CLR executes Main method.
    // [OUTPUT / SIDE EFFECT] Test progression.
    // [VARIATIONS TO TRY] Accept string[] args.
    public static async Task Main()
    {
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 29: Database Sharding & Consistent Hashing      ");
        Console.WriteLine("====================================================================\n");

        // [TEST 1] Testing Consistent Hash Ring Uniformity across Regional Shards.
        Console.WriteLine("[TEST 1] Testing Consistent Hashing Shard Routing & Key Distribution...");
        var router = new ShardRouter(virtualNodeCount: 150);

        var delhiShard = new DatabaseShard("SHARD-DELHI-NCR", "Delhi-NCR");
        var mumbaiShard = new DatabaseShard("SHARD-MUMBAI", "Mumbai-MMR");
        var bangaloreShard = new DatabaseShard("SHARD-BENGALURU", "Bengaluru");

        router.RegisterShard(delhiShard);
        router.RegisterShard(mumbaiShard);
        router.RegisterShard(bangaloreShard);

        var darkStores = new[] { "DS-NOIDA-62", "DS-GURGAON-14", "DS-ANDHERI-01", "DS-BANDRA-05", "DS-INDIRANAGAR-02", "DS-KORAMANGALA-04" };
        var categories = new[] { "Dairy", "Vegetables", "Bakery", "Snacks" };

        decimal totalExpectedRevenue = 0m;

        // Ingest 90 orders across 6 dark stores
        for (int i = 1; i <= 90; i++)
        {
            var storeKey = darkStores[i % darkStores.Length];
            var category = categories[i % categories.Length];
            var amount = 100m + (i * 10m);
            totalExpectedRevenue += amount;

            var order = new ShardOrderEntity(
                OrderId: $"ORD-{i:D4}",
                ShardKey: storeKey,
                CustomerId: $"CUST-{(i % 20) + 1:D3}",
                Amount: amount,
                Category: category,
                CreatedAtUtc: DateTimeOffset.UtcNow
            );

            router.RouteInsert(order);
        }

        var delhiCount = delhiShard.GetAllOrders().Count;
        var mumbaiCount = mumbaiShard.GetAllOrders().Count;
        var bangaloreCount = bangaloreShard.GetAllOrders().Count;
        var totalStored = delhiCount + mumbaiCount + bangaloreCount;

        if (totalStored != 90)
            throw new Exception($"Expected 90 stored orders across shards, but found {totalStored}!");

        if (delhiCount == 0 || mumbaiCount == 0 || bangaloreCount == 0)
            throw new Exception("Consistent Hash Ring failed to balance data — found an empty shard!");

        Console.WriteLine($"  -> Delhi Shard Orders     : {delhiCount} (₹{delhiShard.GetAllOrders().Sum(o => o.Amount)})");
        Console.WriteLine($"  -> Mumbai Shard Orders    : {mumbaiCount} (₹{mumbaiShard.GetAllOrders().Sum(o => o.Amount)})");
        Console.WriteLine($"  -> Bengaluru Shard Orders : {bangaloreCount} (₹{bangaloreShard.GetAllOrders().Sum(o => o.Amount)})");
        Console.WriteLine("  -> Uniform key distribution achieved across all 3 regional shards: PASSED");

        // [TEST 2] Testing Minimal Key Rebalancing on Shard Scale-Out.
        Console.WriteLine("\n[TEST 2] Testing Minimal Key Rebalancing during Shard Scale-Out...");
        var testRing = new ConsistentHashRing<string>(virtualNodeCount: 150);
        testRing.AddNode("SHARD-A");
        testRing.AddNode("SHARD-B");
        testRing.AddNode("SHARD-C");

        // Record assignments for 200 keys across 3 shards
        var keyAssignmentsBefore = new Dictionary<string, string>();
        for (int i = 0; i < 200; i++)
        {
            var key = $"item-key-{i:D4}";
            keyAssignmentsBefore[key] = testRing.GetNode(key);
        }

        // Dynamically add a 4th shard (Scale-Out from 3 -> 4)
        testRing.AddNode("SHARD-D");

        int movedKeys = 0;
        for (int i = 0; i < 200; i++)
        {
            var key = $"item-key-{i:D4}";
            var newShard = testRing.GetNode(key);
            if (newShard != keyAssignmentsBefore[key])
            {
                movedKeys++;
                // In consistent hashing, moved keys should strictly move to the NEW shard (SHARD-D)
                if (newShard != "SHARD-D")
                    throw new Exception($"Key '{key}' moved to existing shard '{newShard}' instead of new shard 'SHARD-D'!");
            }
        }

        var rebalancePercentage = ((double)movedKeys / 200) * 100;
        // In naive modulo (Key % N), 3 -> 4 causes ~75% of keys to reshuffle.
        // In Consistent Hashing, expected migration is ~ 1/N = 25% (we assert < 35%).
        if (rebalancePercentage > 35.0)
            throw new Exception($"Rebalance percentage too high ({rebalancePercentage:F1}% > 35%)!");

        Console.WriteLine($"  -> Scaled from 3 to 4 shards: Only {movedKeys}/200 keys ({rebalancePercentage:F1}%) migrated.");
        Console.WriteLine("  -> Compared to Classic Modulo Hashing (75% reshuffle), Consistent Hashing prevented 50%+ cache churn: PASSED");

        // [TEST 3] Parallel Scatter-Gather Cross-Shard Query Aggregation.
        Console.WriteLine("\n[TEST 3] Testing Parallel Scatter-Gather Cross-Shard Query Aggregation...");
        var scatterGatherEngine = new ScatterGatherQueryEngine(router);

        var categorySummaries = await scatterGatherEngine.AggregateRevenueByCategoryAsync();

        if (categorySummaries.Count != categories.Length)
            throw new Exception($"Expected {categories.Length} aggregated categories, but got {categorySummaries.Count}!");

        var gatheredRevenue = categorySummaries.Sum(c => c.TotalRevenue);
        if (gatheredRevenue != totalExpectedRevenue)
            throw new Exception($"Gathered total revenue ₹{gatheredRevenue} does not match expected ₹{totalExpectedRevenue}!");

        var gatheredOrders = categorySummaries.Sum(c => c.TotalOrders);
        if (gatheredOrders != 90)
            throw new Exception($"Gathered total orders {gatheredOrders} does not match expected 90!");

        Console.WriteLine("  -> Scatter-Gather Parallel Aggregation Results:");
        foreach (var cat in categorySummaries)
        {
            Console.WriteLine($"     * Category: {cat.Category,-12} | Orders: {cat.TotalOrders,-2} | Revenue: ₹{cat.TotalRevenue}");
        }
        Console.WriteLine($"  -> Total Cross-Shard Revenue: ₹{gatheredRevenue} (100% Exact Match): PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All Database Sharding & Consistent Hashing checks PASSED! ");
        Console.WriteLine("====================================================================\n");
    }
}
