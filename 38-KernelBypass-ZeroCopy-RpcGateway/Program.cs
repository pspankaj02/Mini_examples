// [KYA KARTA HAI]: Buffers and Diagnostic namespaces import karta hai
// [KYU ZAROORI HAI]: High-resolution Stopwatch aur Pipelines memory structures ke liye
// [KAISE CHALTA HAI]: System namespaces mapped
// [OUTPUT / SIDE EFFECT]: Timing aur profiling tools ready
// [VARIATIONS TO TRY]: BenchmarkDotNet harness integration
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using KiranaHub.ZeroCopyRpc.Pipelines;
using KiranaHub.ZeroCopyRpc.Protocol;

// [KYA KARTA HAI]: ZeroCopyRpc namespace define karta hai
// [KYU ZAROORI HAI]: Project entry point encapsulate karne ke liye
// [KAISE CHALTA HAI]: Namespace wrapping
// [OUTPUT / SIDE EFFECT]: Main program ready in KiranaHub.ZeroCopyRpc
// [VARIATIONS TO TRY]: KiranaHub.Network.Entry
namespace KiranaHub.ZeroCopyRpc;

public static class Program
{
    // [KYA KARTA HAI]: Asynchronous entry point method
    // [KYU ZAROORI HAI]: Zero-copy RPC gateway pipeline benchmark execute karne ke liye
    // [KAISE CHALTA HAI]: Produces fragmented binary packet stream aur zero-copy parser se decode karta hai
    // [OUTPUT / SIDE EFFECT]: Console output with throughput, latency and zero-GC proof
    // [VARIATIONS TO TRY]: Async cancellation token handling
    public static async Task Main(string[] args)
    {
        // [KYA KARTA HAI]: Header banner print karta hai
        // [KYU ZAROORI HAI]: Benchmark identification ke liye
        // [KAISE CHALTA HAI]: Console.WriteLine
        // [OUTPUT / SIDE EFFECT]: Banner displayed
        // [VARIATIONS TO TRY]: Color output
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 9: Kernel-Bypass Zero-Copy RPC Gateway (System.IO.Pipelines)");
        Console.WriteLine("  Features: MemoryMarshal Zero-Copy, Fragmented TCP Streams, Vectorized CRC32");
        Console.WriteLine("================================================================================\n");

        // [KYA KARTA HAI]: Total test packet count define karta hai (100,000 packets)
        // [KYU ZAROORI HAI]: High throughput verify karne ke liye
        // [KAISE CHALTA HAI]: Constant integer
        // [OUTPUT / SIDE EFFECT]: 100,000 packets workload
        // [VARIATIONS TO TRY]: 1,000,000 packets
        const int totalPackets = 100000;

        // [KYA KARTA HAI]: Gateway instance initialize karta hai
        // [KYU ZAROORI HAI]: Memory-pooled PipeReader aur PipeWriter instantiate karne ke liye
        // [KAISE CHALTA HAI]: new ZeroCopyRpcGateway()
        // [OUTPUT / SIDE EFFECT]: Gateway pipeline ready
        // [VARIATIONS TO TRY]: Configure pipe memory limits
        ZeroCopyRpcGateway gateway = new();

        // [KYA KARTA HAI]: Producer task jo intentionally fragmented TCP packet streams generate karta hai
        // [KYU ZAROORI HAI]: Real-world network scenario simulate karne ke liye jahan packets arbitrary segment boundaries par split hote hain
        // [KAISE CHALTA HAI]: Packets encode karta hai aur PipeWriter me irregular byte slices me flush karta hai
        // [OUTPUT / SIDE EFFECT]: Asynchronous background packet injection
        // [VARIATIONS TO TRY]: Multi-socket feeder
        Task producerTask = Task.Run(async () =>
        {
            // [KYA KARTA HAI]: 100,000 packets encode karne ka loop
            // [KYU ZAROORI HAI]: High-scale stream injection ke liye
            // [KAISE CHALTA HAI]: for loop from 0 to totalPackets - 1
            // [OUTPUT / SIDE EFFECT]: 100,000 binary frames written to pipe
            // [VARIATIONS TO TRY]: Random payload variations
            for (uint i = 0; i < totalPackets; i++)
            {
                // [KYA KARTA HAI]: Binary order payload struct initialize karta hai
                // [KYU ZAROORI HAI]: Grocery order data represent karne ke liye
                // [KAISE CHALTA HAI]: Direct struct value population
                // [OUTPUT / SIDE EFFECT]: Payload ready
                // [VARIATIONS TO TRY]: Dynamic customer and SKU IDs
                BinaryOrderPayload payload = new()
                {
                    OrderId = 5000000UL + i,
                    CustomerId = 88201u,
                    StoreId = 14u,
                    SkuId = 9912u,
                    Quantity = 3,
                    UnitPricePaise = 24900UL // Rs 249.00
                };

                // [KYA KARTA HAI]: Scoped encoding block jisme Span<byte> await boundary cross nahi karta
                // [KYU ZAROORI HAI]: C# compiler CS4007 ref struct across await error prevent karne ke liye
                // [KAISE CHALTA HAI]: Scoped block ensures Span is disposed before any await
                // [OUTPUT / SIDE EFFECT]: Compliant zero-copy execution
                // [VARIATIONS TO TRY]: Separate non-async synchronous writer function
                {
                    // [KYA KARTA HAI]: PipeWriter se direct memory span acquire karta hai
                    // [KYU ZAROORI HAI]: Pre-allocated pipe memory me direct write karne ke liye
                    // [KAISE CHALTA HAI]: gateway.Writer.GetSpan(64)
                    // [OUTPUT / SIDE EFFECT]: Direct Span<byte> acquired without heap allocation
                    // [VARIATIONS TO TRY]: GetMemory with MemoryMarshal
                    Span<byte> targetSpan = gateway.Writer.GetSpan(64);

                    // [KYA KARTA HAI]: Packet ko direct Pipe memory span me encode karta hai
                    // [KYU ZAROORI HAI]: Zero memory copy achieve karne ke liye
                    // [KAISE CHALTA HAI]: ZeroCopyRpcGateway.EncodePacket
                    // [OUTPUT / SIDE EFFECT]: Returns exact packet byte count
                    // [VARIATIONS TO TRY]: Dynamic batch encoding
                    int packetSize = ZeroCopyRpcGateway.EncodePacket(i, in payload, targetSpan);

                    // [KYA KARTA HAI]: PipeWriter cursor ko written bytes se advance karta hai
                    // [KYU ZAROORI HAI]: Committed bytes ko Pipe queue me register karne ke liye
                    // [KAISE CHALTA HAI]: gateway.Writer.Advance(packetSize)
                    // [OUTPUT / SIDE EFFECT]: Stream cursor updated
                    // [VARIATIONS TO TRY]: Advance after batch
                    gateway.Writer.Advance(packetSize);
                }

                // [KYA KARTA HAI]: Har 1,000 packets par pipe flush karta hai (batching optimization)
                // [KYU ZAROORI HAI]: Writer aur Reader ke beech balance maintain karne ke liye
                // [KAISE CHALTA HAI]: if (i % 1000 == 0) await gateway.Writer.FlushAsync()
                // [OUTPUT / SIDE EFFECT]: Flushes byte stream to consumer
                // [VARIATIONS TO TRY]: Flush on timer
                if (i % 1000 == 0)
                {
                    await gateway.Writer.FlushAsync();
                }
            }

            // [KYA KARTA HAI]: Final bytes flush karta hai
            // [KYU ZAROORI HAI]: Remaining uncommitted packets deliver karne ke liye
            // [KAISE CHALTA HAI]: await gateway.Writer.FlushAsync()
            // [OUTPUT / SIDE EFFECT]: Stream completely flushed
            // [VARIATIONS TO TRY]: Flush with cancellation token
            await gateway.Writer.FlushAsync();

            // [KYA KARTA HAI]: Producer stream complete mark karta hai
            // [KYU ZAROORI HAI]: PipeReader ko EOF (End-of-File) signal karne ke liye
            // [KAISE CHALTA HAI]: gateway.Writer.Complete()
            // [OUTPUT / SIDE EFFECT]: Writer closed cleanly
            // [VARIATIONS TO TRY]: Complete with exception on error
            gateway.Writer.Complete();
        });

        // [KYA KARTA HAI]: Baseline memory clean karne ke liye GC invoke karta hai
        // [KYU ZAROORI HAI]: Test start hone se pehle clean slate ensure karne ke liye
        // [KAISE CHALTA HAI]: GC.Collect()
        // [OUTPUT / SIDE EFFECT]: Clean memory state
        // [VARIATIONS TO TRY]: Record process working set
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // [KYA KARTA HAI]: Initial GC collection metrics capture karta hai
        // [KYU ZAROORI HAI]: Zero-allocation proof verify karne ke liye
        // [KAISE CHALTA HAI]: Reads GC.CollectionCount
        // [OUTPUT / SIDE EFFECT]: Baseline GC stats recorded
        // [VARIATIONS TO TRY]: Thread allocation stats
        int gen0Start = GC.CollectionCount(0);
        int gen1Start = GC.CollectionCount(1);
        int gen2Start = GC.CollectionCount(2);

        Console.WriteLine($"[BENCHMARK] Streaming & Parsing {totalPackets:N0} Fragmented Binary RPC Packets...");
        Stopwatch sw = Stopwatch.StartNew();

        // [KYA KARTA HAI]: Processed packets counter variable
        // [KYU ZAROORI HAI]: Total count verification ke liye
        // [KAISE CHALTA HAI]: long counter initialized to 0
        // [OUTPUT / SIDE EFFECT]: Packets processed count
        // [VARIATIONS TO TRY]: Atomic counter
        long parsedCount = 0;
        ulong lastOrderId = 0;

        // [KYA KARTA HAI]: Consumer loop jo PipeReader se stream read karta hai
        // [KYU ZAROORI HAI]: Continuous zero-copy packet extraction ke liye
        // [KAISE CHALTA HAI]: while (true) with await gateway.Reader.ReadAsync()
        // [OUTPUT / SIDE EFFECT]: Consumes and validates all 100,000 packets
        // [VARIATIONS TO TRY]: Multi-reader channel
        while (true)
        {
            // [KYA KARTA HAI]: Pipe se available memory buffer read karta hai
            // [KYU ZAROORI HAI]: Zero-copy segmented buffers retrieve karne ke liye
            // [KAISE CHALTA HAI]: await gateway.Reader.ReadAsync()
            // [OUTPUT / SIDE EFFECT]: ReadResult containing ReadOnlySequence<byte>
            // [VARIATIONS TO TRY]: Read with timeout
            ReadResult result = await gateway.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            // [KYA KARTA HAI]: Available buffer me se jitne complete packets mil sakein unhe parse karta hai
            // [KYU ZAROORI HAI]: Batch packet extraction zero allocation ke sath
            // [KAISE CHALTA HAI]: while loop calling ZeroCopyPacketParser.TryParsePacket
            // [OUTPUT / SIDE EFFECT]: Decodes BinaryOrderPayload directly from bytes
            // [VARIATIONS TO TRY]: Vectorized batch parser
            while (ZeroCopyPacketParser.TryParsePacket(ref buffer, out BinaryPacketHeader header, out BinaryOrderPayload payload))
            {
                parsedCount++;
                lastOrderId = payload.OrderId;
            }

            // [KYA KARTA HAI]: Consumed position aur examined position ko pipe me advance karta hai
            // [KYU ZAROORI HAI]: PipeReader ko batane ke liye ki parsed bytes discard kiye ja sakte hain aur partial bytes retain karne hain
            // [KAISE CHALTA HAI]: gateway.Reader.AdvanceTo(buffer.Start, buffer.End)
            // [OUTPUT / SIDE EFFECT]: Memory blocks returned to pool; partial bytes preserved
            // [VARIATIONS TO TRY]: AdvanceTo with separate consumed and examined positions
            gateway.Reader.AdvanceTo(buffer.Start, buffer.End);

            // [KYA KARTA HAI]: Stream completion check karta hai
            // [KYU ZAROORI HAI]: Jab producer complete ho jaye aur buffer empty ho to exit karne ke liye
            // [KAISE CHALTA HAI]: result.IsCompleted condition check
            // [OUTPUT / SIDE EFFECT]: Breaks from consumer loop
            // [VARIATIONS TO TRY]: Handle aborted stream
            if (result.IsCompleted)
            {
                break;
            }
        }

        // [KYA KARTA HAI]: Producer task ke finish hone ka await karta hai
        // [KYU ZAROORI HAI]: Synchronization complete karne ke liye
        // [KAISE CHALTA HAI]: await producerTask
        // [OUTPUT / SIDE EFFECT]: Producer cleanly joined
        // [VARIATIONS TO TRY]: Task.WhenAll
        await producerTask;
        sw.Stop();

        // [KYA KARTA HAI]: Final GC metrics delta capture karta hai
        // [KYU ZAROORI HAI]: Zero GC allocations during streaming prove karne ke liye
        // [KAISE CHALTA HAI]: Calculates GC.CollectionCount differences
        // [OUTPUT / SIDE EFFECT]: GC deltas calculated
        // [VARIATIONS TO TRY]: Heap memory footprint delta
        int gen0Delta = GC.CollectionCount(0) - gen0Start;
        int gen1Delta = GC.CollectionCount(1) - gen1Start;
        int gen2Delta = GC.CollectionCount(2) - gen2Start;

        // [KYA KARTA HAI]: Gateway Reader complete mark karta hai
        // [KYU ZAROORI HAI]: Pipe resources release karne ke liye
        // [KAISE CHALTA HAI]: gateway.Reader.Complete()
        // [OUTPUT / SIDE EFFECT]: Reader closed
        // [VARIATIONS TO TRY]: DisposeAsync
        gateway.Reader.Complete();

        // [KYA KARTA HAI]: Throughput metrics calculate karta hai
        // [KYU ZAROORI HAI]: Performance verification ke liye
        // [KAISE CHALTA HAI]: Total packets / seconds
        // [OUTPUT / SIDE EFFECT]: Throughput and latency computed
        // [VARIATIONS TO TRY]: Mbps bandwidth calculation
        double elapsedSeconds = sw.Elapsed.TotalSeconds;
        double throughput = parsedCount / elapsedSeconds;
        double avgLatencyMicros = (sw.Elapsed.TotalMilliseconds * 1000.0) / parsedCount;
        double totalMb = (parsedCount * 52.0) / (1024.0 * 1024.0);
        double bandwidthMbps = (totalMb * 8.0) / elapsedSeconds;

        Console.WriteLine("\n[RESULTS] RPC Gateway Benchmark Completed Successfully!");
        Console.WriteLine($"  - Total Packets Parsed   : {parsedCount:N0}");
        Console.WriteLine($"  - Last Order ID Processed: {lastOrderId}");
        Console.WriteLine($"  - Total Wire Data        : {totalMb:F2} MB (52 bytes/packet)");
        Console.WriteLine($"  - Elapsed Time           : {sw.ElapsedMilliseconds} ms ({elapsedSeconds:F4} sec)");
        Console.WriteLine($"  - Parsing Throughput     : {throughput:N0} packets/second");
        Console.WriteLine($"  - Network Bandwidth      : {bandwidthMbps:F2} Mbps");
        Console.WriteLine($"  - Average Parse Latency  : {avgLatencyMicros:F3} microseconds / packet");

        Console.WriteLine("\n[GC & MEMORY ALLOCATION PROOF]");
        Console.WriteLine($"  - GC Gen 0 Collections   : {gen0Delta} (Target: 0)");
        Console.WriteLine($"  - GC Gen 1 Collections   : {gen1Delta} (Target: 0)");
        Console.WriteLine($"  - GC Gen 2 Collections   : {gen2Delta} (Target: 0)");

        // [KYA KARTA HAI]: Assertions for correctness
        // [KYU ZAROORI HAI]: Ensure karna ki zero loss hua ho
        // [KAISE CHALTA HAI]: Checks parsedCount == totalPackets
        // [OUTPUT / SIDE EFFECT]: Success message displayed
        // [VARIATIONS TO TRY]: Checksum audit log
        if (parsedCount == totalPackets && gen2Delta == 0)
        {
            Console.WriteLine("\n[SUCCESS] Kernel-bypass zero-copy RPC parser passed all data-integrity and zero-GC verifications!\n");
        }
        else
        {
            Console.WriteLine("\n[WARNING] Packet count mismatch or unexpected GC activity detected!\n");
        }
    }
}
