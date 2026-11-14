namespace KiranaHub.MultiRegionCRDT;

using KiranaHub.MultiRegionCRDT.Causality;
using KiranaHub.MultiRegionCRDT.CRDT;
using KiranaHub.MultiRegionCRDT.Replication;

// [KYA KARTA HAI]: Project 33 ka executable entry point aur automated CRDT verification test suite provide karta hai.
// [KYU ZAROORI HAI]: Multi-region active-active datacenters (Mumbai aur Bengaluru) me PN-Counters, LWW-Element-Sets, OR-Sets, Vector Clocks, aur network partition healing ko end-to-end mathematically prove aur verify karna.
// [KAISE CHALTA HAI]: Mesh setup karta hai, split-brain network partition inject karta hai, dono regions me concurrent conflicting operations run karta hai, partition heal karta hai, aur exact mathematical convergence assert karta hai.
// [OUTPUT / SIDE EFFECT]: Console test reports, assertions verified, zero data loss under CAP theorem network splits.
// [VARIATIONS TO TRY]: 3-datacenter mesh simulation with probabilistic packet drops.
public class Program
{
    // [KYA KARTA HAI]: Main execution method.
    // [KYU ZAROORI HAI]: Program start point.
    // [KAISE CHALTA HAI]: Top-level async task.
    // [OUTPUT / SIDE EFFECT]: Returns 0 on success.
    // [VARIATIONS TO TRY]: Benchmark throughput under high contention.
    public static Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================================================");
        Console.WriteLine("  🌐 KiranaHub Project 33: Multi-Region Active-Active & CRDT Engine 🌐");
        Console.WriteLine("==================================================================================");

        var mesh = new CrossRegionReplicationMesh();

        // -----------------------------------------------------------------------------------------------------
        // STAGE 1: PN-COUNTER (INVENTORY REPLENISHMENT & DEDUCTION)
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 1] Testing PN-Counter (Positive-Negative Counter) for Global Stock...");

        // [KYA KARTA HAI]: Mumbai datacenter me 500 units stock replenish karta hai.
        // [KYU ZAROORI HAI]: Warehouse inward shipment record karna.
        // [KAISE CHALTA HAI]: Increment(500) on MumbaiStock.
        // [OUTPUT / SIDE EFFECT]: Mumbai P-slot becomes 500.
        // [VARIATIONS TO TRY]: Incremental additions.
        mesh.MumbaiStock.Increment(500);

        // [KYA KARTA HAI]: Bengaluru datacenter me 300 units stock replenish karta hai.
        // [KYU ZAROORI HAI]: Southern dark-store inventory inward.
        // [KAISE CHALTA HAI]: Increment(300) on BengaluruStock.
        // [OUTPUT / SIDE EFFECT]: Bengaluru P-slot becomes 300.
        // [VARIATIONS TO TRY]: Variable shipments.
        mesh.BengaluruStock.Increment(300);

        // [KYA KARTA HAI]: Mumbai me 120 units orders bechta hai (deduct karta hai).
        // [KYU ZAROORI HAI]: Customer sales deductions.
        // [KAISE CHALTA HAI]: Decrement(120).
        // [OUTPUT / SIDE EFFECT]: Mumbai N-slot becomes 120.
        // [VARIATIONS TO TRY]: Bulk batch deduction.
        mesh.MumbaiStock.Decrement(120);

        // [KYA KARTA HAI]: Bengaluru me 80 units orders bechta hai.
        // [KYU ZAROORI HAI]: Local sales deduction.
        // [KAISE CHALTA HAI]: Decrement(80).
        // [OUTPUT / SIDE EFFECT]: Bengaluru N-slot becomes 80.
        // [VARIATIONS TO TRY]: Concurrent sales threads.
        mesh.BengaluruStock.Decrement(80);

        Console.WriteLine($" -> Before Sync: Mumbai Stock = {mesh.MumbaiStock.Value} units | Bengaluru Stock = {mesh.BengaluruStock.Value} units");

        // [KYA KARTA HAI]: Cross-region CRDT state merge karta hai.
        // [KYU ZAROORI HAI]: Positive aur Negative vectors element-wise max merge.
        // [KAISE CHALTA HAI]: Merge calls bidirectionally.
        // [OUTPUT / SIDE EFFECT]: Both datacenters share knowledge of all sales and shipments.
        // [VARIATIONS TO TRY]: Unidirectional sync.
        mesh.MumbaiStock.Merge(mesh.BengaluruStock);
        mesh.BengaluruStock.Merge(mesh.MumbaiStock);

        // Math check:
        // Total P = 500 (Mumbai) + 300 (Bengaluru) = 800
        // Total N = 120 (Mumbai) + 80 (Bengaluru) = 200
        // Net Value = 800 - 200 = 600 units
        Console.WriteLine($" -> After Sync: Mumbai Stock = {mesh.MumbaiStock.Value} units | Bengaluru Stock = {mesh.BengaluruStock.Value} units");

        if (mesh.MumbaiStock.Value != 600 || mesh.BengaluruStock.Value != 600)
        {
            throw new InvalidOperationException($"PN-Counter stock mismatch! Expected 600, got Mumbai={mesh.MumbaiStock.Value}, Bengaluru={mesh.BengaluruStock.Value}");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 2: LWW-ELEMENT-SET & OR-SET SHOPPING CART RECONCILIATION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 2] Testing LWW-Element-Set & OR-Set Cart Synchronization...");

        // Base timestamp for reproducible microsecond ordering
        var t0 = DateTime.UtcNow;

        // Mumbai: Customer adds Milk and Bread
        mesh.MumbaiCartLww.Add("PROD-MILK", t0.AddMilliseconds(10));
        mesh.MumbaiCartLww.Add("PROD-BREAD", t0.AddMilliseconds(20));

        // Bengaluru: Customer adds Eggs concurrently
        mesh.BengaluruCartLww.Add("PROD-EGGS", t0.AddMilliseconds(25));

        // Mumbai: Customer removes Milk at t0 + 40ms
        mesh.MumbaiCartLww.Remove("PROD-MILK", t0.AddMilliseconds(40));

        // Bengaluru: Customer re-adds Milk at t0 + 50ms (Last-Write-Wins should keep Milk!)
        mesh.BengaluruCartLww.Add("PROD-MILK", t0.AddMilliseconds(50));

        // Merge LWW carts
        mesh.MumbaiCartLww.Merge(mesh.BengaluruCartLww);
        mesh.BengaluruCartLww.Merge(mesh.MumbaiCartLww);

        var mumbaiItems = mesh.MumbaiCartLww.ToList();
        var bengaluruItems = mesh.BengaluruCartLww.ToList();

        Console.WriteLine($" -> LWW Merged Items in Mumbai: [{string.Join(", ", mumbaiItems)}]");
        Console.WriteLine($" -> LWW Merged Items in Bengaluru: [{string.Join(", ", bengaluruItems)}]");

        // PROD-MILK (re-added at 50ms > 40ms removal) -> Present
        // PROD-BREAD -> Present
        // PROD-EGGS -> Present
        if (!mesh.MumbaiCartLww.Contains("PROD-MILK") || !mesh.MumbaiCartLww.Contains("PROD-BREAD") || !mesh.MumbaiCartLww.Contains("PROD-EGGS"))
        {
            throw new InvalidOperationException("LWW-Element-Set failed to resolve Last-Write-Wins membership!");
        }

        // Test OR-Set (Observed-Remove Set)
        var butterTag = mesh.MumbaiCartOrSet.Add("PROD-BUTTER");
        mesh.MumbaiCartOrSet.Add("PROD-CHEESE");

        // Sync to Bengaluru
        mesh.BengaluruCartOrSet.Merge(mesh.MumbaiCartOrSet);

        // Mumbai removes BUTTER (tombstones butterTag)
        mesh.MumbaiCartOrSet.Remove("PROD-BUTTER");

        // Concurrent action in Bengaluru: Customer adds BUTTER again (gets a new distinct tag!)
        var newButterTag = mesh.BengaluruCartOrSet.Add("PROD-BUTTER");

        // Merge OR-Sets: The newButterTag is NOT in Mumbai's tombstones, so BUTTER must be preserved!
        mesh.MumbaiCartOrSet.Merge(mesh.BengaluruCartOrSet);
        mesh.BengaluruCartOrSet.Merge(mesh.MumbaiCartOrSet);

        Console.WriteLine($" -> OR-Set Merged Items: [{string.Join(", ", mesh.MumbaiCartOrSet.ToList())}]");
        if (!mesh.MumbaiCartOrSet.Contains("PROD-BUTTER") || !mesh.MumbaiCartOrSet.Contains("PROD-CHEESE"))
        {
            throw new InvalidOperationException("OR-Set failed to retain concurrently added item after deletion!");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 3: VECTOR CLOCK CAUSALITY TRACKING & CONFLICT DETECTION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 3] Testing Distributed Vector Clocks & Causal Orderings...");

        var clockA = new VectorClock("node-mumbai");
        var clockB = new VectorClock("node-bengaluru");

        // Event 1 in Mumbai
        clockA.Tick(); // A: <mumbai:1>
        Console.WriteLine($" -> Clock A after Tick: {clockA}");

        // Mumbai sends message to Bengaluru -> Bengaluru receives and merges
        clockB.Merge(clockA); // B: <mumbai:1, bengaluru:1>
        Console.WriteLine($" -> Clock B after Receive & Merge from A: {clockB}");

        // Clock A happened BEFORE Clock B (A -> B)
        var order1 = clockA.Compare(clockB);
        Console.WriteLine($" -> Causality (Clock A vs Clock B): {order1} (Expected: Before)");
        if (order1 != CausalOrder.Before)
        {
            throw new InvalidOperationException($"Expected CausalOrder.Before, got {order1}");
        }

        // Now: Network disconnects! Independent concurrent events occur on both nodes
        clockA.Tick(); // A: <mumbai:2, bengaluru:0>
        clockB.Tick(); // B: <mumbai:1, bengaluru:2>
        Console.WriteLine($" -> Clock A after independent Tick: {clockA}");
        Console.WriteLine($" -> Clock B after independent Tick: {clockB}");

        // Both clocks have components greater than the other -> CONCURRENT CONFLICT! (A || B)
        var conflictOrder = clockA.Compare(clockB);
        Console.WriteLine($" -> Causality During Disconnected Phase: {conflictOrder} (Expected: ConcurrentConflict)");
        if (conflictOrder != CausalOrder.ConcurrentConflict)
        {
            throw new InvalidOperationException($"Expected CausalOrder.ConcurrentConflict, got {conflictOrder}");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 4: SIMULATED NETWORK PARTITION (SPLIT-BRAIN) & RECONCILIATION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 4] Testing Simulated Network Split-Brain & Partition Healing (CAP Theorem)...");

        mesh.SimulateNetworkPartition();
        Console.WriteLine($" -> Network Partition INJECTED! IsPartitioned={mesh.IsPartitioned}");

        // While partitioned, Mumbai receives local traffic
        mesh.MumbaiStock.Increment(150); // Mumbai restocks 150
        mesh.MumbaiStock.Decrement(30);  // Mumbai sells 30
        mesh.MumbaiCartLww.Add("PROD-APPLES", DateTime.UtcNow);

        // Simultaneously, Bengaluru receives independent local traffic
        mesh.BengaluruStock.Increment(200); // Bengaluru restocks 200
        mesh.BengaluruStock.Decrement(50);  // Bengaluru sells 50
        mesh.BengaluruCartLww.Add("PROD-ORANGES", DateTime.UtcNow);

        Console.WriteLine($" -> Partitioned State: Mumbai Stock = {mesh.MumbaiStock.Value} | Bengaluru Stock = {mesh.BengaluruStock.Value}");

        // Now heal partition!
        mesh.HealPartitionAndSynchronize();
        Console.WriteLine($" -> Network Partition HEALED! IsPartitioned={mesh.IsPartitioned}");
        Console.WriteLine($" -> Reconciled Mumbai Stock = {mesh.MumbaiStock.Value} | Reconciled Bengaluru Stock = {mesh.BengaluruStock.Value}");

        // Math:
        // Prior Net = 600
        // Mumbai delta: +150 - 30 = +120
        // Bengaluru delta: +200 - 50 = +150
        // Total expected = 600 + 120 + 150 = 870 units
        if (mesh.MumbaiStock.Value != 870 || mesh.BengaluruStock.Value != 870)
        {
            throw new InvalidOperationException($"Post-partition reconciliation mismatch! Expected 870, got Mumbai={mesh.MumbaiStock.Value}, Bengaluru={mesh.BengaluruStock.Value}");
        }

        if (!mesh.MumbaiCartLww.Contains("PROD-APPLES") || !mesh.MumbaiCartLww.Contains("PROD-ORANGES"))
        {
            throw new InvalidOperationException("Cart items added during partition must be preserved post-healing!");
        }

        Console.WriteLine("\n==================================================================================");
        Console.WriteLine("  ✅ [SUCCESS] ALL MULTI-REGION CRDT & CAUSALITY TESTS PASSED WITH 0 ERRORS! ✅");
        Console.WriteLine("==================================================================================");

        return Task.FromResult(0);
    }
}
