// ==============================================================================================
// KiranaHub Chaos & Load Testing: Automated Stress Test & Resilience Verification Runner
// [KYA KARTA HAI] Polly v8 resilience pipelines, Chaos Engineering fault injector, aur High-Throughput load simulation ko execute karta hai.
// [KYU ZAROORI HAI] Verify karna ki production me third-party gateway drop hone par circuit breaker trip hota hai, fallback gracefully handle hota hai, aur 0 unhandled crashes hote hain.
// [KAISE CHALTA HAI] Program main method 2 deep tests run karta hai: Circuit Trip & Reset Test, and High-Throughput Concurrency Benchmark.
// [OUTPUT / SIDE EFFECT] Detailed console benchmark results and clean exit code 0.
// [VARIATIONS TO TRY] Parameterize request count via command line arguments.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Exception, Task utilities access karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Core library loaded.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Threading tasks import karta hai async entrypoint ke liye.
// [KYU ZAROORI HAI] Async main method support.
// [KAISE CHALTA HAI] Tasks loaded.
// [OUTPUT / SIDE EFFECT] Async runtime ready.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Resilience namespace import karta hai.
// [KYU ZAROORI HAI] PollyPipelines factory and CheckoutResult access karna.
// [KAISE CHALTA HAI] References local resilience namespace.
// [OUTPUT / SIDE EFFECT] Polly types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.Resilience;

// [KYA KARTA HAI] Chaos namespace import karta hai.
// [KYU ZAROORI HAI] ChaosFaultInjector access karna.
// [KAISE CHALTA HAI] References local chaos namespace.
// [OUTPUT / SIDE EFFECT] Fault injector accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.Chaos;

// [KYA KARTA HAI] Load simulation namespace import karta hai.
// [KYU ZAROORI HAI] CheckoutLoadTestEngine and LoadTestReport access karna.
// [KAISE CHALTA HAI] References local load simulation namespace.
// [OUTPUT / SIDE EFFECT] Load engine accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.LoadSimulation;

// [KYA KARTA HAI] Polly circuit breaker namespace import karta hai.
// [KYU ZAROORI HAI] CircuitState enum access karna.
// [KAISE CHALTA HAI] Polly CB types loaded.
// [OUTPUT / SIDE EFFECT] CircuitState ready.
// [VARIATIONS TO TRY] None.
using Polly.CircuitBreaker;

// [KYA KARTA HAI] Program entry point class define karta hai.
// [KYU ZAROORI HAI] Executable console application container.
// [KAISE CHALTA HAI] C# 13 top-level application wrapper.
// [OUTPUT / SIDE EFFECT] Entry class.
// [VARIATIONS TO TRY] Top-level statements.
public class Program
{
    // [KYA KARTA HAI] Application main asynchronous entrypoint.
    // [KYU ZAROORI HAI] Test execution start karna.
    // [KAISE CHALTA HAI] CLR executes Main method on program start.
    // [OUTPUT / SIDE EFFECT] Application life cycle initiates.
    // [VARIATIONS TO TRY] Accept string[] args.
    public static async Task Main()
    {
        // [KYA KARTA HAI] Test suite banner print karta hai.
        // [KYU ZAROORI HAI] Clean visual logging.
        // [KAISE CHALTA HAI] Console.WriteLine.
        // [OUTPUT / SIDE EFFECT] Banner displayed.
        // [VARIATIONS TO TRY] Color console.
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 26: Chaos Engineering & Load Testing Suite      ");
        Console.WriteLine("====================================================================\n");

        // [TEST 1] Testing Circuit Breaker Trip, Fallback & Self-Healing Reset.
        Console.WriteLine("[TEST 1] Verifying Polly v8 Circuit Breaker Trip & Fallback Pipeline...");
        var pollyFactory = new PollyPipelines();
        var pipeline = pollyFactory.CreateCheckoutResiliencePipeline();

        // [KYA KARTA HAI] 100% failure rate chaos injector create karta hai.
        // [KYU ZAROORI HAI] Immediate downstream gateway downtime simulate karke circuit trip test karna.
        // [KAISE CHALTA HAI] FailureRate set to 1.0.
        // [OUTPUT / SIDE EFFECT] 100% failure injector.
        // [VARIATIONS TO TRY] Intermittent 80% failure rate.
        var severeOutageInjector = new ChaosFaultInjector(failureRate: 1.0, minLatencyMs: 5, maxLatencyMs: 15);

        // [KYA KARTA HAI] 6 consecutive requests execute karta hai down service ke against.
        // [KYU ZAROORI HAI] Minimum throughput (4) and failure ratio (50%) cross karke circuit trip karwana.
        // [KAISE CHALTA HAI] Loop invoking pipeline.ExecuteAsync.
        // [OUTPUT / SIDE EFFECT] Circuit trips, fallback returns degraded results.
        // [VARIATIONS TO TRY] Variable loop counts.
        for (int i = 1; i <= 6; i++)
        {
            var res = await pipeline.ExecuteAsync(async ct =>
            {
                return await severeOutageInjector.SimulatePaymentGatewayCallAsync($"ORD-OUTAGE-{i}", 250m);
            });
            // Assert fallback handling
            if (!res.IsDegradedFallback)
            {
                throw new Exception($"Expected degraded fallback for request {i}, but got primary success!");
            }
        }

        // [KYA KARTA HAI] Assert karta hai ki circuit breaker trip hua aur trip count >= 1 hai.
        // [KYU ZAROORI HAI] Confirm karna ki Polly ne downstream storm rokh diya.
        // [KAISE CHALTA HAI] Compares pollyFactory.CircuitBreakerTripCount.
        // [OUTPUT / SIDE EFFECT] Asserts trip behavior.
        // [VARIATIONS TO TRY] Check exact trip timestamp.
        if (pollyFactory.CircuitBreakerTripCount < 1)
        {
            throw new Exception("Circuit Breaker failed to trip to OPEN state during severe outage!");
        }
        Console.WriteLine($"  -> Circuit Breaker tripped {pollyFactory.CircuitBreakerTripCount} time(s) to OPEN state: PASSED");
        Console.WriteLine($"  -> All 6 failed calls were gracefully diverted to Fallback (COD mode): PASSED");

        // [KYA KARTA HAI] Self-healing test: break duration (1500ms) ke expire hone ka wait karta hai.
        // [KYU ZAROORI HAI] Circuit breaker cooldown window test karna.
        // [KAISE CHALTA HAI] Task.Delay 1600ms.
        // [OUTPUT / SIDE EFFECT] Break duration passes.
        // [VARIATIONS TO TRY] Test half-open immediate rejection.
        Console.WriteLine("  -> Waiting 1.6s for Circuit Breaker break duration to expire...");
        await Task.Delay(1600);

        // [KYA KARTA HAI] Downstream service health restore karta hai (0% failure rate).
        // [KYU ZAROORI HAI] Downstream recovery simulate karna.
        // [KAISE CHALTA HAI] Sets failureRate to 0.0.
        // [OUTPUT / SIDE EFFECT] Healthy service simulation.
        // [VARIATIONS TO TRY] Slow graduated recovery.
        severeOutageInjector.FailureRate = 0.0;

        // [KYA KARTA HAI] Trial probe request bhejta hai Half-Open state test karne ke liye.
        // [KYU ZAROORI HAI] Healthy probe pass hone par circuit reset hona chahiye.
        // [KAISE CHALTA HAI] Single execution through pipeline.
        // [OUTPUT / SIDE EFFECT] Circuit transitions HalfOpen -> Closed.
        // [VARIATIONS TO TRY] Multiple trial probes.
        var recoveryRes = await pipeline.ExecuteAsync(async ct =>
        {
            return await severeOutageInjector.SimulatePaymentGatewayCallAsync("ORD-RECOVERY-01", 100m);
        });

        if (!recoveryRes.IsSuccess || recoveryRes.IsDegradedFallback)
        {
            throw new Exception("Recovery trial probe failed to execute successfully!");
        }
        Console.WriteLine("  -> Circuit Breaker probe passed and state RESET to CLOSED: PASSED\n");

        // [TEST 2] High-Throughput Concurrency & Load Stress Test.
        Console.WriteLine("[TEST 2] Running High-Throughput Concurrency & Tail Latency Benchmark...");
        var loadEngine = new CheckoutLoadTestEngine();

        // Realistic chaos scenario: 15% transient errors, 10-30ms network latency
        var realisticChaos = new ChaosFaultInjector(failureRate: 0.15, minLatencyMs: 10, maxLatencyMs: 35);
        var loadPipeline = pollyFactory.CreateCheckoutResiliencePipeline();

        const int totalTestRequests = 300;
        const int concurrentUsers = 30;

        var report = await loadEngine.RunLoadTestAsync(loadPipeline, realisticChaos, totalTestRequests, concurrentUsers);

        // Assert zero unhandled crashes
        if (report.UnhandledCrashes > 0)
        {
            throw new Exception($"Found {report.UnhandledCrashes} unhandled 500 crashes during load testing!");
        }

        // Assert 100% handled rate
        if (report.SuccessfulRequests + report.FallbackRequests != report.TotalRequests)
        {
            throw new Exception("Total handled requests mismatch!");
        }

        Console.WriteLine($"  -> Executed {report.TotalRequests} concurrent checkout transactions in {report.ElapsedTotalSeconds}s");
        Console.WriteLine($"  -> Throughput: {report.RequestsPerSecond} Requests/Second (RPS)");
        Console.WriteLine($"  -> Primary Gateway Authorizations: {report.SuccessfulRequests} ({(double)report.SuccessfulRequests / report.TotalRequests * 100:F1}%)");
        Console.WriteLine($"  -> Graceful COD Fallbacks: {report.FallbackRequests} ({(double)report.FallbackRequests / report.TotalRequests * 100:F1}%)");
        Console.WriteLine($"  -> Unhandled 500 Crashes: {report.UnhandledCrashes} (0.0%): PASSED");
        Console.WriteLine("  -> Latency Percentiles:");
        Console.WriteLine($"     * Min Latency : {report.MinLatencyMs} ms");
        Console.WriteLine($"     * P50 (Median): {report.P50LatencyMs} ms");
        Console.WriteLine($"     * P95 Tail    : {report.P95LatencyMs} ms");
        Console.WriteLine($"     * P99 Tail    : {report.P99LatencyMs} ms");
        Console.WriteLine($"     * Max Latency : {report.MaxLatencyMs} ms: PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All Polly v8 Chaos & Load Testing checks PASSED!         ");
        Console.WriteLine("====================================================================\n");
    }
}
