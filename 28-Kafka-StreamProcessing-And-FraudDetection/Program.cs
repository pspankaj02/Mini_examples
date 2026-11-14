// ==============================================================================================
// KiranaHub Kafka Streaming: Automated Verification & Real-Time Fraud Test Suite
// [KYA KARTA HAI] Apache Kafka topic partitions, consumer group offset commits, sliding-window analytics, aur real-time fraud rules ko verify karta hai.
// [KYU ZAROORI HAI] Confirm karna ki stream partitions evenly distribute hoti hain, lag calculate hota hai, aur fraudulent transactions inline block hote hain.
// [KAISE CHALTA HAI] Program Main executes 3 deep tests: Kafka Multi-Partition Engine, Sliding Window Surge Multipliers, and Real-Time Streaming Fraud Rules.
// [OUTPUT / SIDE EFFECT] Detailed console metrics and clean exit code 0.
// [VARIATIONS TO TRY] Concurrency testing with 10 parallel consumer groups.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Guid, DateTimeOffset types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Query operations ke liye.
// [KAISE CHALTA HAI] LINQ operators loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators available.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task based async support.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Kafka streaming namespace import karta hai.
// [KYU ZAROORI HAI] KafkaTopic and KafkaRecord access karna.
// [KAISE CHALTA HAI] References local streaming namespace.
// [OUTPUT / SIDE EFFECT] Kafka streaming types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.KafkaStreaming.Streaming;

// [KYA KARTA HAI] Windowing namespace import karta hai.
// [KYU ZAROORI HAI] SlidingWindowAggregator and metrics access karna.
// [KAISE CHALTA HAI] References local windowing namespace.
// [OUTPUT / SIDE EFFECT] Windowing types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.KafkaStreaming.Windowing;

// [KYA KARTA HAI] Fraud detection namespace import karta hai.
// [KYU ZAROORI HAI] FraudDetectionEngine and CheckoutEvent access karna.
// [KAISE CHALTA HAI] References local fraud namespace.
// [OUTPUT / SIDE EFFECT] Fraud types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.KafkaStreaming.Fraud;

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
    public static void Main()
    {
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 28: Kafka Streaming & Fraud Detection Suite     ");
        Console.WriteLine("====================================================================\n");

        // [TEST 1] Testing Kafka Multi-Partitioning & Consumer Group Lag.
        Console.WriteLine("[TEST 1] Testing Kafka Multi-Partition Broker & Consumer Group Offsets...");
        var topic = new KafkaTopic<string, string>("kirana.orders.stream", partitionCount: 3);

        // Produce 60 events across customers
        for (int i = 1; i <= 60; i++)
        {
            var customerId = $"CUST-{(i % 6) + 1:D3}";
            var orderJson = $"{{\"orderId\":\"ORD-{i:D4}\",\"amount\":{150 + i * 5}}}";
            topic.Produce(customerId, orderJson);
        }

        // Verify partitions distribution
        var p0Count = topic.Partitions[0].Count;
        var p1Count = topic.Partitions[1].Count;
        var p2Count = topic.Partitions[2].Count;
        var totalProduced = p0Count + p1Count + p2Count;

        if (totalProduced != 60)
            throw new Exception($"Expected 60 total produced messages, but found {totalProduced}!");

        if (p0Count == 0 || p1Count == 0 || p2Count == 0)
            throw new Exception("Key routing failed to distribute messages across all 3 partitions!");

        Console.WriteLine($"  -> Produced 60 messages across 3 partitions: P0={p0Count}, P1={p1Count}, P2={p2Count}: PASSED");

        // Consumer Group Consumption & Offset Commit
        const string consumerGroup = "analytics-worker-group";
        long totalConsumed = 0;

        for (int p = 0; p < 3; p++)
        {
            var lagBefore = topic.GetLag(consumerGroup, p);
            var batch = topic.Consume(consumerGroup, p, maxBatchSize: 100);
            totalConsumed += batch.Count;

            if (batch.Count > 0)
            {
                var lastOffset = batch.Last().Offset;
                topic.Commit(consumerGroup, p, lastOffset);
            }

            var lagAfter = topic.GetLag(consumerGroup, p);
            if (lagAfter != 0)
                throw new Exception($"Partition {p} consumer lag should be 0 after full commit, but was {lagAfter}!");
        }

        if (totalConsumed != 60)
            throw new Exception($"Expected 60 consumed messages, but got {totalConsumed}!");

        Console.WriteLine($"  -> Consumer Group '{consumerGroup}' consumed all 60 messages. Remaining Lag = 0: PASSED");

        // [TEST 2] Real-Time Sliding Window Demand Velocity & Dynamic Surge Multipliers.
        Console.WriteLine("\n[TEST 2] Testing Sliding-Window Velocity & Dynamic Surge Multipliers...");
        var windowAggregator = new SlidingWindowAggregator(TimeSpan.FromSeconds(60));
        const string darkStore = "DS-NOIDA-62";

        var baseTime = DateTimeOffset.UtcNow;

        // Step 1: Normal demand (3 orders in 60 seconds)
        for (int i = 0; i < 3; i++)
        {
            windowAggregator.RecordEvent(darkStore, 250m, baseTime.AddSeconds(i * 10));
        }

        var normalMetrics = windowAggregator.GetMetrics(darkStore, baseTime.AddSeconds(30));
        if (normalMetrics.SurgeMultiplier != 1.0)
            throw new Exception($"Expected 1.0x normal surge multiplier, but got {normalMetrics.SurgeMultiplier}x!");

        Console.WriteLine($"  -> Normal Velocity: {normalMetrics.VelocityPerMinute} orders/min -> Surge Multiplier: {normalMetrics.SurgeMultiplier}x: PASSED");

        // Step 2: High Surge rush (inject 18 orders in 60s)
        for (int i = 0; i < 18; i++)
        {
            windowAggregator.RecordEvent(darkStore, 300m, baseTime.AddSeconds(35 + i));
        }

        var peakMetrics = windowAggregator.GetMetrics(darkStore, baseTime.AddSeconds(55));
        if (peakMetrics.SurgeMultiplier != 1.50)
            throw new Exception($"Expected 1.50x peak surge multiplier, but got {peakMetrics.SurgeMultiplier}x!");

        Console.WriteLine($"  -> Peak Velocity: {peakMetrics.VelocityPerMinute} orders/min -> Surge Multiplier: {peakMetrics.SurgeMultiplier}x: PASSED");

        // [TEST 3] Real-Time Streaming Fraud Detection Rules.
        Console.WriteLine("\n[TEST 3] Testing Real-Time Streaming Fraud Detection Engine...");
        var fraudEngine = new FraudDetectionEngine();

        // Baseline: Establish normal spending history for user (median ₹300)
        fraudEngine.Evaluate(new CheckoutEvent("ORD-BASE-1", "CUST-001", "CARD-VISA-111", 280m, 28.628, 77.364, baseTime.AddMinutes(-30)));
        fraudEngine.Evaluate(new CheckoutEvent("ORD-BASE-2", "CUST-001", "CARD-VISA-111", 310m, 28.628, 77.364, baseTime.AddMinutes(-20)));
        fraudEngine.Evaluate(new CheckoutEvent("ORD-BASE-3", "CUST-001", "CARD-VISA-111", 320m, 28.628, 77.364, baseTime.AddMinutes(-10)));

        // Fraud Scenario A: Rapid Velocity Spike (3 orders within 2 seconds with same card)
        fraudEngine.Evaluate(new CheckoutEvent("ORD-RAPID-1", "CUST-001", "CARD-VISA-111", 250m, 28.628, 77.364, baseTime));
        fraudEngine.Evaluate(new CheckoutEvent("ORD-RAPID-2", "CUST-001", "CARD-VISA-111", 250m, 28.628, 77.364, baseTime.AddSeconds(1)));
        var velocityFraud = fraudEngine.Evaluate(new CheckoutEvent("ORD-RAPID-3", "CUST-001", "CARD-VISA-111", 250m, 28.628, 77.364, baseTime.AddSeconds(2)));

        if (!velocityFraud.ShouldBlock || !velocityFraud.Violations.Any(v => v.Contains("FRAUD_VELOCITY_SPIKE")))
            throw new Exception("Fraud engine failed to detect rapid card velocity spike!");

        Console.WriteLine($"  -> Velocity Anomaly Detected: Risk='{velocityFraud.RiskLevel}', Blocked={velocityFraud.ShouldBlock}: PASSED");

        // Fraud Scenario B: Impossible Geographical Travel Speed (Noida to Bengaluru in 3 minutes)
        // Noida (28.628, 77.364) to Bengaluru (12.971, 77.594) = ~1740 km
        var geoFraud = fraudEngine.Evaluate(new CheckoutEvent("ORD-GEO-1", "CUST-001", "CARD-VISA-999", 250m, 12.971, 77.594, baseTime.AddSeconds(180)));

        if (!geoFraud.ShouldBlock || !geoFraud.Violations.Any(v => v.Contains("FRAUD_IMPOSSIBLE_TRAVEL")))
            throw new Exception("Fraud engine failed to detect impossible geo travel speed!");

        Console.WriteLine($"  -> Impossible Travel Speed Detected: {geoFraud.Violations.First(v => v.Contains("FRAUD_IMPOSSIBLE_TRAVEL"))}: PASSED");

        // Fraud Scenario C: Extreme Value Anomaly (₹15,000 order when median is ₹310)
        var valueFraud = fraudEngine.Evaluate(new CheckoutEvent("ORD-VAL-1", "CUST-001", "CARD-VISA-999", 15000m, 12.971, 77.594, baseTime.AddSeconds(190)));

        if (!valueFraud.Violations.Any(v => v.Contains("FRAUD_VALUE_ANOMALY")))
            throw new Exception("Fraud engine failed to detect extreme value anomaly!");

        Console.WriteLine($"  -> Value Anomaly Detected: {valueFraud.Violations.First(v => v.Contains("FRAUD_VALUE_ANOMALY"))}: PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All Kafka Streaming, Windowing & Fraud checks PASSED!    ");
        Console.WriteLine("====================================================================\n");
    }
}
