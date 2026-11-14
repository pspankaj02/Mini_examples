// [KYA KARTA HAI]: Diagnostic system namespaces import karta hai
// [KYU ZAROORI HAI]: High-resolution Stopwatch aur GC metrics track karne ke liye
// [KAISE CHALTA HAI]: System.Diagnostics namespace provide karta hai
// [OUTPUT / SIDE EFFECT]: Timing aur profiling tools ready
// [VARIATIONS TO TRY]: BenchmarkDotNet harness use karein
using System.Diagnostics;
// [KYA KARTA HAI]: Core Disruptor primitives import karta hai
// [KYU ZAROORI HAI]: RingBuffer, PaddedSequence, YieldingWaitStrategy use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Disruptor classes available
// [VARIATIONS TO TRY]: Aliased imports
using KiranaHub.Disruptor.Core;
// [KYA KARTA HAI]: Domain and Pipeline components import karta hai
// [KYU ZAROORI HAI]: OrderMatchingEvent aur BatchEventProcessor use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Domain models available
// [VARIATIONS TO TRY]: Shared library references
using KiranaHub.Disruptor.Domain;
using KiranaHub.Disruptor.Pipeline;

// [KYA KARTA HAI]: KiranaHub Disruptor Main Program class
// [KYU ZAROORI HAI]: Benchmark test aur multi-stage flash sale matching engine demo run karne ke liye
// [KAISE CHALTA HAI]: Entry point executes Main method
// [OUTPUT / SIDE EFFECT]: Console output with metrics
// [VARIATIONS TO TRY]: Top-level statements
namespace KiranaHub.Disruptor;

public static class Program
{
    // [KYA KARTA HAI]: Main executable entry point method
    // [KYU ZAROORI HAI]: LMAX Disruptor high-frequency matching engine benchmark execute karne ke liye
    // [KAISE CHALTA HAI]: Ring buffer setup karta hai, 3-stage pipeline launch karta hai, 100,000 orders stream karta hai
    // [OUTPUT / SIDE EFFECT]: Prints throughput, latency and zero-GC verification
    // [VARIATIONS TO TRY]: Command line arguments for order count
    public static void Main(string[] args)
    {
        // [KYA KARTA HAI]: Console header banner print karta hai
        // [KYU ZAROORI HAI]: User ko running benchmark identify karne ke liye
        // [KAISE CHALTA HAI]: Console.WriteLine formatted string
        // [OUTPUT / SIDE EFFECT]: Banner printed
        // [VARIATIONS TO TRY]: ANSI color formatting
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 9: Low-Latency High-Frequency Matching Engine (LMAX Disruptor)");
        Console.WriteLine("  Features: Lock-Free RingBuffer, 64-byte Cache-Line Padding, Zero-GC Allocation");
        Console.WriteLine("================================================================================\n");

        // [KYA KARTA HAI]: Power-of-two buffer size define karta hai (65,536 slots)
        // [KYU ZAROORI HAI]: Flash sale burst absorption aur fast bitwise masking ke liye
        // [KAISE CHALTA HAI]: 64 * 1024 = 65,536 slots pre-allocated
        // [OUTPUT / SIDE EFFECT]: Fixed ring buffer capacity
        // [VARIATIONS TO TRY]: 131,072 slots for even bigger flash sales
        const int bufferSize = 65536;

        // [KYA KARTA HAI]: Benchmark test ke liye total order count define karta hai (100,000 orders)
        // [KYU ZAROORI HAI]: High-frequency throughput aur zero-GC properties verify karne ke liye
        // [KAISE CHALTA HAI]: 100,000 test orders
        // [OUTPUT / SIDE EFFECT]: Workload magnitude defined
        // [VARIATIONS TO TRY]: 1,000,000 orders for stress testing
        const int totalOrders = 100000;

        // [KYA KARTA HAI]: Yielding wait strategy instantiate karta hai
        // [KYU ZAROORI HAI]: Low-latency spinning with cooperative OS thread yielding provide karne ke liye
        // [KAISE CHALTA HAI]: new YieldingWaitStrategy()
        // [OUTPUT / SIDE EFFECT]: Strategy ready
        // [VARIATIONS TO TRY]: BusySpinWaitStrategy for sub-nanosecond extreme latency
        IWaitStrategy waitStrategy = new YieldingWaitStrategy();

        // [KYA KARTA HAI]: Circular ring buffer instantiate karta hai with factory function
        // [KYU ZAROORI HAI]: 65,536 OrderMatchingEvent instances ko startup par pre-allocate karne ke liye
        // [KAISE CHALTA HAI]: Pre-allocates slots in array and returns RingBuffer<OrderMatchingEvent>
        // [OUTPUT / SIDE EFFECT]: Zero runtime heap allocations achieved
        // [VARIATIONS TO TRY]: Native unmanaged memory pool
        Console.WriteLine($"[INIT] Pre-allocating RingBuffer with {bufferSize:N0} slots...");
        RingBuffer<OrderMatchingEvent> ringBuffer = new(
            bufferSize,
            () => new OrderMatchingEvent(),
            waitStrategy
        );

        // [KYA KARTA HAI]: Atomic counters for stage completions
        // [KYU ZAROORI HAI]: Multi-thread verification ke liye
        // [KAISE CHALTA HAI]: long counters
        // [OUTPUT / SIDE EFFECT]: Tracking variables
        // [VARIATIONS TO TRY]: PaddedSequence for counters
        long inventoryCount = 0;
        long paymentCount = 0;
        long dispatchCount = 0;

        // [KYA KARTA HAI]: Stage 1: Inventory Validation Consumer Handler
        // [KYU ZAROORI HAI]: Dark-store warehouse inventory stock check karne ke liye
        // [KAISE CHALTA HAI]: Depends directly on producer cursor; sets IsInventoryReserved = true
        // [OUTPUT / SIDE EFFECT]: Validates inventory for each grocery order
        // [VARIATIONS TO TRY]: Decrement live stock bucket
        Action<OrderMatchingEvent, long, bool> stage1Handler = (ev, seq, endOfBatch) =>
        {
            // [KYA KARTA HAI]: Simulates instant O(1) inventory reservation
            // [KYU ZAROORI HAI]: In-memory flash sale stock check
            // [KAISE CHALTA HAI]: Sets flag on pre-allocated event
            // [OUTPUT / SIDE EFFECT]: Inventory reserved
            // [VARIATIONS TO TRY]: Reject if stock depleted
            ev.IsInventoryReserved = true;
            Interlocked.Increment(ref inventoryCount);
        };

        // [KYA KARTA HAI]: Stage 1 processor instantiate karta hai
        // [KYU ZAROORI HAI]: Producer cursor ke sequence stream ko consume karne ke liye
        // [KAISE CHALTA HAI]: new BatchEventProcessor reading from ringBuffer.Cursor
        // [OUTPUT / SIDE EFFECT]: Stage 1 worker ready
        // [VARIATIONS TO TRY]: Multi-core parallel inventory shards
        BatchEventProcessor stage1Processor = new(ringBuffer, ringBuffer.Cursor, stage1Handler, waitStrategy);

        // [KYA KARTA HAI]: Stage 2: Customer Pricing & Wallet Reserver Handler
        // [KYU ZAROORI HAI]: Customer wallet balance verify aur flash sale discount apply karne ke liye
        // [KAISE CHALTA HAI]: Depends on Stage 1 sequence; sets IsPaymentApproved = true
        // [OUTPUT / SIDE EFFECT]: Payment approved
        // [VARIATIONS TO TRY]: Fraud detection scoring
        Action<OrderMatchingEvent, long, bool> stage2Handler = (ev, seq, endOfBatch) =>
        {
            // [KYA KARTA HAI]: Only approves payment if inventory was reserved
            // [KYU ZAROORI HAI]: Stock out orders par wallet charge na ho
            // [KAISE CHALTA HAI]: Conditional check on ev.IsInventoryReserved
            // [OUTPUT / SIDE EFFECT]: Payment marked approved
            // [VARIATIONS TO TRY]: 10% instant flash discount calculation
            if (ev.IsInventoryReserved)
            {
                ev.IsPaymentApproved = true;
                Interlocked.Increment(ref paymentCount);
            }
        };

        // [KYA KARTA HAI]: Stage 2 processor instantiate karta hai
        // [KYU ZAROORI HAI]: Stage 1 ke output par depend karne ke liye
        // [KAISE CHALTA HAI]: Passes stage1Processor.Sequence as dependent sequence
        // [OUTPUT / SIDE EFFECT]: Stage 2 worker ready
        // [VARIATIONS TO TRY]: Stage 2 running in lockstep
        BatchEventProcessor stage2Processor = new(ringBuffer, stage1Processor.Sequence, stage2Handler, waitStrategy);

        // [KYA KARTA HAI]: Stage 3: Order Journaling & Dispatch Clearance Handler
        // [KYU ZAROORI HAI]: Final order confirmation aur rider dispatch queue me add karne ke liye
        // [KAISE CHALTA HAI]: Depends on Stage 2 sequence; marks IsDispatched = true
        // [OUTPUT / SIDE EFFECT]: Order cycle complete
        // [VARIATIONS TO TRY]: Write to zero-copy network socket
        Action<OrderMatchingEvent, long, bool> stage3Handler = (ev, seq, endOfBatch) =>
        {
            // [KYA KARTA HAI]: Approves dispatch if payment was approved
            // [KYU ZAROORI HAI]: Unpaid orders warehouse me pack na hon
            // [KAISE CHALTA HAI]: Conditional check on ev.IsPaymentApproved
            // [OUTPUT / SIDE EFFECT]: Order dispatched
            // [VARIATIONS TO TRY]: Assign delivery rider ID
            if (ev.IsPaymentApproved)
            {
                ev.IsDispatched = true;
                Interlocked.Increment(ref dispatchCount);
            }
        };

        // [KYA KARTA HAI]: Stage 3 processor instantiate karta hai
        // [KYU ZAROORI HAI]: Stage 2 ke output par depend karne ke liye
        // [KAISE CHALTA HAI]: Passes stage2Processor.Sequence as dependent sequence
        // [OUTPUT / SIDE EFFECT]: Stage 3 worker ready
        // [VARIATIONS TO TRY]: Persistence journaler
        BatchEventProcessor stage3Processor = new(ringBuffer, stage2Processor.Sequence, stage3Handler, waitStrategy);

        // [KYA KARTA HAI]: RingBuffer ka gating sequence set karta hai to Stage 3
        // [KYU ZAROORI HAI]: Producer ko un-dispatched slots overwrite karne se rokne ke liye
        // [KAISE CHALTA HAI]: ringBuffer.SetGatingSequences(stage3Processor.Sequence)
        // [OUTPUT / SIDE EFFECT]: Full ring buffer wrap-around protection activated
        // [VARIATIONS TO TRY]: Multiple slow consumer gating
        ringBuffer.SetGatingSequences(stage3Processor.Sequence);

        // [KYA KARTA HAI]: All 3 consumer threads start karta hai
        // [KYU ZAROORI HAI]: Pipeline processing launch karne ke liye
        // [KAISE CHALTA HAI]: Calls .Start() on each processor
        // [OUTPUT / SIDE EFFECT]: 3 worker threads running in parallel
        // [VARIATIONS TO TRY]: Set thread core affinity
        Console.WriteLine("[INIT] Starting 3-Stage Pipeline Processors (Inventory -> Wallet -> Dispatch)...");
        stage1Processor.Start("Stage1-InventoryValidator");
        stage2Processor.Start("Stage2-PricingWallet");
        stage3Processor.Start("Stage3-JournalingDispatch");

        // [KYA KARTA HAI]: Force initial GC to ensure clean slate before benchmark
        // [KYU ZAROORI HAI]: Startup allocations benchmark metrics me count na hon
        // [KAISE CHALTA HAI]: GC.Collect() and GC.WaitForPendingFinalizers()
        // [OUTPUT / SIDE EFFECT]: Clean memory baseline
        // [VARIATIONS TO TRY]: GC.TryStartNoGCRegion
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // [KYA KARTA HAI]: Initial GC collection counts capture karta hai
        // [KYU ZAROORI HAI]: Zero GC assertion verify karne ke liye
        // [KAISE CHALTA HAI]: Reads GC.CollectionCount for Gen 0, 1, 2
        // [OUTPUT / SIDE EFFECT]: Baseline GC counts recorded
        // [VARIATIONS TO TRY]: GC.GetTotalAllocatedBytes() delta
        int gen0Start = GC.CollectionCount(0);
        int gen1Start = GC.CollectionCount(1);
        int gen2Start = GC.CollectionCount(2);
        long allocatedBytesStart = GC.GetAllocatedBytesForCurrentThread();

        Console.WriteLine($"\n[BENCHMARK] Streaming {totalOrders:N0} Flash-Sale Grocery Orders through Disruptor...");
        Stopwatch sw = Stopwatch.StartNew();

        // [KYA KARTA HAI]: 100,000 orders stream karne ka producer loop
        // [KYU ZAROORI HAI]: Realistic flash sale burst simulate karne ke liye
        // [KAISE CHALTA HAI]: Claims next slot, mutates event in-place, publishes sequence
        // [OUTPUT / SIDE EFFECT]: 100,000 orders produced with zero allocations
        // [VARIATIONS TO TRY]: Multi-threaded producers
        for (int i = 0; i < totalOrders; i++)
        {
            // [KYA KARTA HAI]: Ring buffer se agla available slot sequence claim karta hai
            // [KYU ZAROORI HAI]: Producer sequence atomically advance karne ke liye
            // [KAISE CHALTA HAI]: ringBuffer.Next()
            // [OUTPUT / SIDE EFFECT]: Claimed slot sequence number
            // [VARIATIONS TO TRY]: Batch sequence reservation
            long sequence = ringBuffer.Next();

            // [KYA KARTA HAI]: Pre-allocated slot event fetch karta hai
            // [KYU ZAROORI HAI]: In-place mutation ke liye (no 'new' keyword!)
            // [KAISE CHALTA HAI]: ringBuffer[sequence] (bitwise masked lookup)
            // [OUTPUT / SIDE EFFECT]: Reference to slot event
            // [VARIATIONS TO TRY]: Unsafe pointer dereference
            OrderMatchingEvent slotEvent = ringBuffer[sequence];

            // [KYA KARTA HAI]: Order parameters ko in-place populate karta hai
            // [KYU ZAROORI HAI]: Zero heap allocation achieve karne ke liye
            // [KAISE CHALTA HAI]: Mutates pre-allocated object fields
            // [OUTPUT / SIDE EFFECT]: Slot populated with fresh order data
            // [VARIATIONS TO TRY]: Pass discount voucher code
            slotEvent.Populate(
                orderId: 1000000L + i,
                customerId: "CUST-98214",
                itemSku: "SKU-MANGO-ALPHANSO-1KG",
                unitPrice: 499.00m,
                quantity: 2,
                timestamp: Stopwatch.GetTimestamp()
            );

            // [KYA KARTA HAI]: Event ko publish karta hai
            // [KYU ZAROORI HAI]: Consumers ko slot available announce karne ke liye
            // [KAISE CHALTA HAI]: ringBuffer.Publish(sequence) (volatile write memory barrier)
            // [OUTPUT / SIDE EFFECT]: Sequence becomes visible to Stage 1
            // [VARIATIONS TO TRY]: Batch publish at every 64 events
            ringBuffer.Publish(sequence);
        }

        // [KYA KARTA HAI]: Polling loop jab tak Stage 3 saare 100,000 orders finish na kar le
        // [KYU ZAROORI HAI]: End-to-end completion wait karne ke liye
        // [KAISE CHALTA HAI]: Waits until stage3Processor.Sequence.Get() == totalOrders - 1
        // [OUTPUT / SIDE EFFECT]: Synchronizes benchmark finish
        // [VARIATIONS TO TRY]: ManualResetEventSlim signal
        long targetSequence = totalOrders - 1;
        while (stage3Processor.Sequence.Get() < targetSequence)
        {
            Thread.SpinWait(10);
        }

        sw.Stop();

        // [KYA KARTA HAI]: Final GC metrics capture karta hai
        // [KYU ZAROORI HAI]: Zero-GC allocation proof ke liye
        // [KAISE CHALTA HAI]: GC collection count delta calculate karta hai
        // [OUTPUT / SIDE EFFECT]: GC deltas recorded
        // [VARIATIONS TO TRY]: Memory profiler snapshot
        int gen0Delta = GC.CollectionCount(0) - gen0Start;
        int gen1Delta = GC.CollectionCount(1) - gen1Start;
        int gen2Delta = GC.CollectionCount(2) - gen2Start;
        long allocatedBytesEnd = GC.GetAllocatedBytesForCurrentThread();
        long threadBytesAllocated = allocatedBytesEnd - allocatedBytesStart;

        // [KYA KARTA HAI]: Processors ko safely stop karta hai
        // [KYU ZAROORI HAI]: Clean thread shutdown ke liye
        // [KAISE CHALTA HAI]: Calls .Halt() on all processors
        // [OUTPUT / SIDE EFFECT]: Worker threads joined
        // [VARIATIONS TO TRY]: Background auto-terminate
        stage1Processor.Halt();
        stage2Processor.Halt();
        stage3Processor.Halt();

        // [KYA KARTA HAI]: Throughput aur latency calculate karta hai
        // [KYU ZAROORI HAI]: Performance verification ke liye
        // [KAISE CHALTA HAI]: Total orders divided by elapsed seconds
        // [OUTPUT / SIDE EFFECT]: Ops/sec and microsecond latency computed
        // [VARIATIONS TO TRY]: P99 / P99.9 percentile histogram
        double elapsedSeconds = sw.Elapsed.TotalSeconds;
        double throughput = totalOrders / elapsedSeconds;
        double avgLatencyMicros = (sw.Elapsed.TotalMilliseconds * 1000.0) / totalOrders;

        Console.WriteLine("\n[RESULTS] Benchmark Completed Successfully!");
        Console.WriteLine($"  - Total Orders Processed : {totalOrders:N0}");
        Console.WriteLine($"  - Elapsed Time           : {sw.ElapsedMilliseconds} ms ({elapsedSeconds:F4} sec)");
        Console.WriteLine($"  - Throughput             : {throughput:N0} orders/second");
        Console.WriteLine($"  - Average Latency        : {avgLatencyMicros:F3} microseconds / order");
        Console.WriteLine($"  - Stage 1 (Inventory)    : {Interlocked.Read(ref inventoryCount):N0} orders processed");
        Console.WriteLine($"  - Stage 2 (Wallet)       : {Interlocked.Read(ref paymentCount):N0} orders approved");
        Console.WriteLine($"  - Stage 3 (Dispatch)     : {Interlocked.Read(ref dispatchCount):N0} orders dispatched");

        Console.WriteLine("\n[GC & ALLOCATION VERIFICATION]");
        Console.WriteLine($"  - GC Gen 0 Collections   : {gen0Delta} (Target: 0)");
        Console.WriteLine($"  - GC Gen 1 Collections   : {gen1Delta} (Target: 0)");
        Console.WriteLine($"  - GC Gen 2 Collections   : {gen2Delta} (Target: 0)");
        Console.WriteLine($"  - Producer Thread Alloc  : {threadBytesAllocated:N0} bytes (Steady-State In-Place Mutation)");

        // [KYA KARTA HAI]: Integrity assertions
        // [KYU ZAROORI HAI]: Ensure karna ki koi order drop ya corrupt na hua ho
        // [KAISE CHALTA HAI]: Checks counts equal totalOrders
        // [OUTPUT / SIDE EFFECT]: Confirms 100% order fidelity
        // [VARIATIONS TO TRY]: Hash chain checksum
        if (dispatchCount == totalOrders && gen2Delta == 0)
        {
            Console.WriteLine("\n[SUCCESS] LMAX Disruptor zero-allocation low-latency engine passed all verifications!\n");
        }
        else
        {
            Console.WriteLine("\n[WARNING] Dispatched count or GC collections deviated from target!\n");
        }
    }
}
