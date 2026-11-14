// ==============================================================================================
// KiranaHub Load Testing: High-Throughput Concurrency Simulation & Latency Profiling Engine
// [KYA KARTA HAI] Thousands of concurrent virtual users simulate karke P50, P95, P99 tail latencies aur RPS throughput measure karta hai.
// [KYU ZAROORI HAI] Flash sales me sudden traffic spike par application ke performance bottlenecks aur latency degradation ko benchmark karna.
// [KAISE CHALTA HAI] Parallel.ForEachAsync or SemaphoreSlim throttled tasks se pipeline execute karta hai, high-resolution Stopwatches se latencies record karta hai aur percentiles compute karta hai.
// [OUTPUT / SIDE EFFECT] Comprehensive statistical performance report.
// [VARIATIONS TO TRY] NBomber or k6 external load testing integrations.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Math, Guid, Action types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] System collections generic import karta hai.
// [KYU ZAROORI HAI] List, IEnumerable collections provide karna.
// [KAISE CHALTA HAI] Generic types loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] ConcurrentBag me multi-threaded latencies safely collect karna.
// [KAISE CHALTA HAI] Lock-free atomic list data structure.
// [OUTPUT / SIDE EFFECT] High-throughput collection.
// [VARIATIONS TO TRY] ConcurrentQueue.
using System.Collections.Concurrent;

// [KYA KARTA HAI] Diagnostics namespace import karta hai.
// [KYU ZAROORI HAI] Stopwatch high-resolution millisecond timer provide karna.
// [KAISE CHALTA HAI] Uses OS performance counters (QueryPerformanceCounter).
// [OUTPUT / SIDE EFFECT] Sub-millisecond timing accuracy.
// [VARIATIONS TO TRY] ValueStopwatch struct for zero-allocation profiling.
using System.Diagnostics;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sorting, OrderBy, and Percentile slicing operations ke liye.
// [KAISE CHALTA HAI] Functional iteration expressions.
// [OUTPUT / SIDE EFFECT] LINQ operators available.
// [VARIATIONS TO TRY] Span-based in-place sorting for zero heap allocations.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task, SemaphoreSlim, Parallel asynchronous execution primitives.
// [KAISE CHALTA HAI] TPL engine loaded.
// [OUTPUT / SIDE EFFECT] Concurrency support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Polly pipelines and results import karta hai.
// [KYU ZAROORI HAI] CheckoutResult and ResiliencePipeline references.
// [KAISE CHALTA HAI] References local resilience namespace.
// [OUTPUT / SIDE EFFECT] Resilience types available.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.Resilience;

// [KYA KARTA HAI] Chaos fault injector import karta hai.
// [KYU ZAROORI HAI] Realistic failure injection simulation.
// [KAISE CHALTA HAI] References local chaos namespace.
// [OUTPUT / SIDE EFFECT] Fault injector available.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.Chaos;

// [KYA KARTA HAI] Namespace declare karta hai load simulation engine ke liye.
// [KYU ZAROORI HAI] Performance testing logic encapsulate karna.
// [KAISE CHALTA HAI] Scoped to KiranaHub.ChaosAndLoadTesting.LoadSimulation.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] None.
namespace KiranaHub.ChaosAndLoadTesting.LoadSimulation;

// [KYA KARTA HAI] Statistical load test metrics report record.
// [KYU ZAROORI HAI] Benchmarking outcomes encapsulate karna (RPS, P50, P95, P99, Success Rate).
// [KAISE CHALTA HAI] Immutable record with properties for all key metrics.
// [OUTPUT / SIDE EFFECT] Performance benchmark report.
// [VARIATIONS TO TRY] Output report to JSON file or CSV.
public record LoadTestReport(
    int TotalRequests,
    int SuccessfulRequests,
    int FallbackRequests,
    int UnhandledCrashes,
    double ElapsedTotalSeconds,
    double RequestsPerSecond,
    double MinLatencyMs,
    double P50LatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    double MaxLatencyMs
);

// [KYA KARTA HAI] High-throughput checkout load testing engine class.
// [KYU ZAROORI HAI] Synthetic load generation aur statistical percentile analysis orchestrate karna.
// [KAISE CHALTA HAI] Spawns concurrent tasks, executes through Polly resilience pipeline, and compiles metrics.
// [OUTPUT / SIDE EFFECT] Production-grade load test orchestrator.
// [VARIATIONS TO TRY] Distributed load generation across multiple worker nodes.
public class CheckoutLoadTestEngine
{
    // [KYA KARTA HAI] High-throughput concurrent checkout simulation execute karta hai.
    // [KYU ZAROORI HAI] Concurrency, throughput, aur tail latencies ko under-load measure karna.
    // [KAISE CHALTA HAI] Batches requests, executes through resilience pipeline, and computes percentiles.
    // [OUTPUT / SIDE EFFECT] LoadTestReport containing detailed metrics.
    // [VARIATIONS TO TRY] Ramp-up virtual users over a duration curve.
    public async Task<LoadTestReport> RunLoadTestAsync(
        Polly.ResiliencePipeline<CheckoutResult> pipeline,
        ChaosFaultInjector faultInjector,
        int totalRequests = 500,
        int maxConcurrency = 25)
    {
        // [KYA KARTA HAI] High-resolution overall stopwatch start karta hai.
        // [KYU ZAROORI HAI] Total wall-clock time accurately measure karke RPS calculate karna.
        // [KAISE CHALTA HAI] Starts OS performance counter.
        // [OUTPUT / SIDE EFFECT] Overall timer running.
        // [VARIATIONS TO TRY] None.
        var totalTimer = Stopwatch.StartNew();

        // [KYA KARTA HAI] Individual request latencies collect karne ke liye thread-safe bag initialize karta hai.
        // [KYU ZAROORI HAI] Multi-threaded workers se lock contention ke bina latencies record karna.
        // [KAISE CHALTA HAI] ConcurrentBag with lock-free linked list buffers.
        // [OUTPUT / SIDE EFFECT] Thread-safe storage ready.
        // [VARIATIONS TO TRY] Array with Interlocked.Increment index.
        var latencies = new ConcurrentBag<double>();

        // [KYA KARTA HAI] Metrics counters initialize karta hai.
        // [KYU ZAROORI HAI] Success, Fallback, aur Crash outcomes count karna.
        // [KAISE CHALTA HAI] Local integer variables updated via Interlocked.
        // [OUTPUT / SIDE EFFECT] Counters ready.
        // [VARIATIONS TO TRY] Metric struct.
        int successful = 0;
        int fallbacks = 0;
        int crashes = 0;

        // [KYA KARTA HAI] Concurrency throttling ke liye SemaphoreSlim initialize karta hai.
        // [KYU ZAROORI HAI] Max active concurrent workers limit karna (e.g. 25 simultaneous users).
        // [KAISE CHALTA HAI] Semaphore permits gate concurrent async entries.
        // [OUTPUT / SIDE EFFECT] Concurrency bounded to maxConcurrency.
        // [VARIATIONS TO TRY] Dynamic semaphore resizing.
        using var throttle = new System.Threading.SemaphoreSlim(maxConcurrency);

        // [KYA KARTA HAI] Tasks array initialize karta hai.
        // [KYU ZAROORI HAI] Saari async request tasks track karna for Task.WhenAll.
        // [KAISE CHALTA HAI] List of Task objects.
        // [OUTPUT / SIDE EFFECT] Tasks tracking list.
        // [VARIATIONS TO TRY] Periodic batch awaiting to prevent memory accumulation on millions of requests.
        var tasks = new List<Task>(totalRequests);

        // [KYA KARTA HAI] Loop chala kar totalRequests tasks spawn karta hai.
        // [KYU ZAROORI HAI] High-concurrency traffic generate karna.
        // [KAISE CHALTA HAI] For loop from 0 to totalRequests.
        // [OUTPUT / SIDE EFFECT] Tasks initiated.
        // [VARIATIONS TO TRY] Infinite stream until cancellation token triggers.
        for (int i = 0; i < totalRequests; i++)
        {
            var orderId = $"ORD-{i + 1:D5}";
            var amount = 150m + (i % 50) * 10m;

            // [KYA KARTA HAI] Async work item create karke tasks list me add karta hai.
            // [KYU ZAROORI HAI] Parallel execution pipeline queue karna.
            // [KAISE CHALTA HAI] Task.Run or direct async task invocation.
            // [OUTPUT / SIDE EFFECT] Request queued.
            // [VARIATIONS TO TRY] Thread pool starvation checks.
            tasks.Add(Task.Run(async () =>
            {
                // [KYA KARTA HAI] Semaphore permit acquire karta hai.
                // [KYU ZAROORI HAI] Enforce maxConcurrency active requests.
                // [KAISE CHALTA HAI] Awaits semaphore permit.
                // [OUTPUT / SIDE EFFECT] Throttled entry.
                // [VARIATIONS TO TRY] Timeout on acquire.
                await throttle.WaitAsync();
                var reqTimer = Stopwatch.StartNew();

                try
                {
                    // [KYA KARTA HAI] Request ko Polly resilience pipeline ke through execute karta hai.
                    // [KYU ZAROORI HAI] Automatic retries, circuit breaking, aur fallback protection activate karna.
                    // [KAISE CHALTA HAI] pipeline.ExecuteAsync wraps faultInjector.SimulatePaymentGatewayCallAsync.
                    // [OUTPUT / SIDE EFFECT] Resilient execution outcome.
                    // [VARIATIONS TO TRY] Pass Polly Context with correlation ID.
                    var result = await pipeline.ExecuteAsync(async ct =>
                    {
                        return await faultInjector.SimulatePaymentGatewayCallAsync(orderId, amount);
                    });

                    // [KYA KARTA HAI] Result type inspect karke metrics counters update karta hai.
                    // [KYU ZAROORI HAI] Primary success vs Degraded fallback distinguish karna.
                    // [KAISE CHALTA HAI] Evaluates result.IsDegradedFallback.
                    // [OUTPUT / SIDE EFFECT] Metric updated.
                    // [VARIATIONS TO TRY] Log audit trail.
                    if (result.IsDegradedFallback)
                    {
                        System.Threading.Interlocked.Increment(ref fallbacks);
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref successful);
                    }
                }
                catch (Exception)
                {
                    // [KYA KARTA HAI] Unhandled exceptions catch karta hai.
                    // [KYU ZAROORI HAI] Unhandled crashes count karna (ideal target is 0 crashes due to fallback).
                    // [KAISE CHALTA HAI] Interlocked.Increment on crashes counter.
                    // [OUTPUT / SIDE EFFECT] Crash metric updated.
                    // [VARIATIONS TO TRY] Log stack trace.
                    System.Threading.Interlocked.Increment(ref crashes);
                }
                finally
                {
                    // [KYA KARTA HAI] Request latency record karta hai aur stopwatch stop karta hai.
                    // [KYU ZAROORI HAI] Accurate statistical percentile calculation.
                    // [KAISE CHALTA HAI] Stores reqTimer.Elapsed.TotalMilliseconds in thread-safe bag.
                    // [OUTPUT / SIDE EFFECT] Latency recorded.
                    // [VARIATIONS TO TRY] Exclude outliers.
                    reqTimer.Stop();
                    latencies.Add(reqTimer.Elapsed.TotalMilliseconds);

                    // [KYA KARTA HAI] Semaphore permit release karta hai.
                    // [KYU ZAROORI HAI] Agle waiting virtual user ko execution allow karna.
                    // [KAISE CHALTA HAI] throttle.Release().
                    // [OUTPUT / SIDE EFFECT] Permit returned.
                    // [VARIATIONS TO TRY] None.
                    throttle.Release();
                }
            }));
        }

        // [KYA KARTA HAI] Sabhi spawned load generation tasks ke complete hone ka wait karta hai.
        // [KYU ZAROORI HAI] Benchmark run complete hona guarantee karna.
        // [KAISE CHALTA HAI] Task.WhenAll awaits all tasks in parallel.
        // [OUTPUT / SIDE EFFECT] All requests finished.
        // [VARIATIONS TO TRY] Timeout on Task.WhenAll.
        await Task.WhenAll(tasks);
        totalTimer.Stop();

        // [KYA KARTA HAI] Recorded latencies ko ascending order me sort karta hai percentiles compute karne ke liye.
        // [KYU ZAROORI HAI] P50 (median), P95, and P99 percentiles nikalna.
        // [KAISE CHALTA HAI] Orders list of doubles.
        // [OUTPUT / SIDE EFFECT] Sorted latencies list.
        // [VARIATIONS TO TRY] In-place Array.Sort.
        var sortedLatencies = latencies.OrderBy(x => x).ToList();
        var elapsedSec = totalTimer.Elapsed.TotalSeconds;
        var rps = elapsedSec > 0 ? totalRequests / elapsedSec : 0;

        // [KYA KARTA HAI] Statistical percentiles calculate karta hai.
        // [KYU ZAROORI HAI] Average ke bajaye real tail latencies measure karna (average hides slow outliers).
        // [KAISE CHALTA HAI] Array index calculation: floor(percentile * count).
        // [OUTPUT / SIDE EFFECT] Latency numbers.
        // [VARIATIONS TO TRY] Linear interpolation between percentiles.
        var min = sortedLatencies.Count > 0 ? sortedLatencies.First() : 0;
        var max = sortedLatencies.Count > 0 ? sortedLatencies.Last() : 0;
        var p50 = GetPercentile(sortedLatencies, 0.50);
        var p95 = GetPercentile(sortedLatencies, 0.95);
        var p99 = GetPercentile(sortedLatencies, 0.99);

        // [KYA KARTA HAI] Final compiled load test report return karta hai.
        // [KYU ZAROORI HAI] Benchmark verification aur reporting ke liye.
        // [KAISE CHALTA HAI] Instantiates LoadTestReport record.
        // [OUTPUT / SIDE EFFECT] Comprehensive performance report.
        // [VARIATIONS TO TRY] Save to HTML dashboard.
        return new LoadTestReport(
            TotalRequests: totalRequests,
            SuccessfulRequests: successful,
            FallbackRequests: fallbacks,
            UnhandledCrashes: crashes,
            ElapsedTotalSeconds: Math.Round(elapsedSec, 2),
            RequestsPerSecond: Math.Round(rps, 1),
            MinLatencyMs: Math.Round(min, 2),
            P50LatencyMs: Math.Round(p50, 2),
            P95LatencyMs: Math.Round(p95, 2),
            P99LatencyMs: Math.Round(p99, 2),
            MaxLatencyMs: Math.Round(max, 2)
        );
    }

    // [KYA KARTA HAI] Percentile value extract karne ka mathematical helper function.
    // [KYU ZAROORI HAI] Array index boundary safe percentile extraction.
    // [KAISE CHALTA HAI] Calculates index = clamp(floor(percentile * count), 0, count - 1).
    // [OUTPUT / SIDE EFFECT] Double value at target percentile.
    // [VARIATIONS TO TRY] Nearest rank method vs linear interpolation.
    private static double GetPercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        int index = (int)Math.Floor(percentile * (sorted.Count - 1));
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }
}
