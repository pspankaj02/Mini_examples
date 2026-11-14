using System.Diagnostics;
using KiranaHub.StreamingCep.Cep;
using KiranaHub.StreamingCep.Join;

namespace KiranaHub.StreamingCep;

public static class Program
{
    // [KYA KARTA HAI]: Stateful Stream Join & Complex Event Processing (CEP) Engine ka comprehensive demonstration entry point.
    // [KYU ZAROORI HAI]: Watermarks, out-of-order temporal joins, sliding state memory bounding, aur CEP pattern matching verify karna.
    // [KAISE CHALTA HAI]: Generates 50,000 events, executes interval stream join, flags QR fraud, and triggers cart abandonment alerts.
    // [OUTPUT / SIDE EFFECT]: Console logs with throughput metrics, joined events, fraud alerts, and state eviction counts.
    // [VARIATIONS TO TRY]: Multi-stream 3-way join (Order + Rider + Payment).
    public static void Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 11: Stateful Stream Join & Complex Event Processing (CEP)     ");
        Console.WriteLine("  Features: Event-Time Watermarks, Sliding Interval Joins, Cart Recovery CEP    ");
        Console.WriteLine("================================================================================");

        // -------------------------------------------------------------------------
        // PART 1: Temporal Interval Stream Join (Order Events + Rider Telemetry)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PART 1] Running Two-Stream Temporal Interval Join (50,000 Events)...");
        var joiner = new IntervalStreamJoiner(lowerBoundMs: 60_000, upperBoundMs: 60_000); // +/- 1 minute window

        const int EventPairs = 25_000;
        long baseEpoch = 1773000000000L; // Synthetic 2026 Epoch timestamp

        var sw = Stopwatch.StartNew();
        int totalJoinsEmitted = 0;
        int suspiciousFraudCount = 0;

        for (int i = 1; i <= EventPairs; i++)
        {
            string orderId = $"ord_{i:D6}";
            string customerId = $"cust_{(i % 100):D3}";
            string riderId = $"rider_{(i % 20):D2}";

            long orderTime = baseEpoch + (i * 100);
            // Simulate random network delay / out-of-order arrival for rider (-20s to +30s)
            long riderTime = orderTime + ((i % 50) - 20) * 1000;

            // Introduce 1 fraud case at index 1337: rider is 4.5 km away!
            double distanceKm = (i == 1337) ? 4.5 : 0.05;

            var order = new CustomerOrderEvent(orderId, customerId, orderTime, "QrScanDelivered", Amount: 450.0);
            var rider = new RiderTelemetryEvent(riderId, orderId, riderTime, Latitude: 19.0760, Longitude: 72.8777, DistanceToCustomerKm: distanceKm);

            // Ingest both sides of stream
            var j1 = joiner.IngestOrder(order);
            var j2 = joiner.IngestRider(rider);

            totalJoinsEmitted += (j1.Count + j2.Count);

            foreach (var item in j1.Concat(j2))
            {
                if (item.IsSuspiciousDelivery)
                {
                    suspiciousFraudCount++;
                    Console.WriteLine($"  - 🚨 FRAUD DETECTED on Order '{item.OrderId}': Delivery QR scanned, but Rider '{item.RiderId}' was {item.DistanceKm:F2} km away! (Delta: {Math.Abs(item.OrderTimestamp - item.RiderTimestamp) / 1000}s)");
                }
            }
        }

        sw.Stop();
        double opsPerSec = (EventPairs * 2) / sw.Elapsed.TotalSeconds;

        Console.WriteLine($"  - Total Stream Events    : {EventPairs * 2:N0} events (Orders + Telemetries)");
        Console.WriteLine($"  - Processing Time Taken  : {sw.ElapsedMilliseconds} ms ({opsPerSec:N0} events/sec)");
        Console.WriteLine($"  - Matched Temporal Joins : {totalJoinsEmitted:N0} joined records");
        Console.WriteLine($"  - Suspicious Fraud Traps : {suspiciousFraudCount} alerts generated");
        Console.WriteLine($"  - Bounded State Footprint: {joiner.OrderStateCount} active orders, {joiner.RiderStateCount} active riders (Watermark safely purged {EventPairs * 2 - (joiner.OrderStateCount + joiner.RiderStateCount):N0} historical events!)");

        // -------------------------------------------------------------------------
        // PART 2: Complex Event Processing (CEP) Sequence Pattern Matching
        // Pattern: Search -> AddToCart -> [No Checkout in 5 mins] -> SurgeDiscountTrigger
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PART 2] Running Complex Event Processing (CEP) Cart Abandonment DFA...");
        var cep = new PatternStateMachine(abandonmentTimeoutMs: 300_000); // 5 mins

        long cepTime = baseEpoch;

        // Customer A: Search -> AddToCart -> Checkout (Happy path)
        Console.WriteLine("  - Simulating Customer A (Active Buyer): Search -> AddToCart -> Checkout in 2 mins...");
        cep.IngestUserAction(new UserCartStepEvent("cust_A", "sku_organic_milk_1l", "Search", cepTime));
        cep.IngestUserAction(new UserCartStepEvent("cust_A", "sku_organic_milk_1l", "AddToCart", cepTime + 60_000));
        cep.IngestUserAction(new UserCartStepEvent("cust_A", "sku_organic_milk_1l", "Checkout", cepTime + 120_000));

        // Customer B: Search -> AddToCart -> [Idle for 6 mins] (Abandoned cart!)
        Console.WriteLine("  - Simulating Customer B (Hesitant Buyer): Search -> AddToCart -> Idle for 6 mins...");
        cep.IngestUserAction(new UserCartStepEvent("cust_B", "sku_belgian_dark_chocolate", "Search", cepTime + 10_000));
        cep.IngestUserAction(new UserCartStepEvent("cust_B", "sku_belgian_dark_chocolate", "AddToCart", cepTime + 70_000));

        // Watermark advances to 6 minutes later
        long advancedWatermark = cepTime + 70_000 + 360_000; // 6 minutes after AddToCart
        var alerts = cep.EvaluateTimeouts(advancedWatermark);

        Console.WriteLine($"  - CEP Engine Evaluated Watermark at T + 6 mins:");
        Console.WriteLine($"  - Abandonment Alerts Generated: {alerts.Count}");
        foreach (var alert in alerts)
        {
            Console.WriteLine($"    🎯 [CEP WIN-BACK VOUCHER]: Customer '{alert.CustomerId}' abandoned '{alert.ItemSku}'! Dispatched {alert.RecommendedVoucherDiscountPercent}% Flash Voucher!");
        }

        Console.WriteLine("\n[SUCCESS] Stateful Stream Join & Complex Event Processing Engine completed successfully!");
    }
}
