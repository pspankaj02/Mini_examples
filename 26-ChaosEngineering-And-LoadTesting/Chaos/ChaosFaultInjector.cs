// ==============================================================================================
// KiranaHub Chaos: Chaos Engineering Real-World Fault & Latency Injector
// [KYA KARTA HAI] Microservices calls me synthetic latency, network packet drops, aur third-party crashes inject karta hai.
// [KYU ZAROORI HAI] Production me unexpected outage aane se pehle system ki self-healing aur circuit breaker policies ko verify karna.
// [KAISE CHALTA HAI] Pseudo-random distribution se configured percentage of calls me artificial delay ya custom exceptions throw karta hai.
// [OUTPUT / SIDE EFFECT] Controlled chaos environment testing resilience mechanisms.
// [VARIATIONS TO TRY] Chaos Mesh or Simian Army integrations in Kubernetes cluster.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Random, ArgumentOutOfRangeException types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task.Delay asynchronous latency injection ke liye.
// [KAISE CHALTA HAI] Task library loaded.
// [OUTPUT / SIDE EFFECT] Async delay support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Resilience types import karta hai.
// [KYU ZAROORI HAI] Custom exceptions aur records access karna.
// [KAISE CHALTA HAI] References local resilience namespace.
// [OUTPUT / SIDE EFFECT] Local types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ChaosAndLoadTesting.Resilience;

// [KYA KARTA HAI] Namespace declare karta hai chaos engineering logic ke liye.
// [KYU ZAROORI HAI] Isolation of fault-injection code from core business logic.
// [KAISE CHALTA HAI] Scoped to KiranaHub.ChaosAndLoadTesting.Chaos.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] None.
namespace KiranaHub.ChaosAndLoadTesting.Chaos;

// [KYA KARTA HAI] Chaos fault injector class define karta hai.
// [KYU ZAROORI HAI] Microservices execution ke aage controlled faults trigger karna.
// [KAISE CHALTA HAI] Wraps external API simulation calls with latency and random failures.
// [OUTPUT / SIDE EFFECT] Injected failure behaviors.
// [VARIATIONS TO TRY] Configure failure rates dynamically from configuration file.
public class ChaosFaultInjector
{
    // [KYA KARTA HAI] Thread-safe random number generator.
    // [KYU ZAROORI HAI] High concurrency simulation me non-deterministic fault distribution ensure karna.
    // [KAISE CHALTA HAI] Random.Shared uses per-thread seed for zero lock contention.
    // [OUTPUT / SIDE EFFECT] Thread-safe random values.
    // [VARIATIONS TO TRY] Cryptographic RNG if security required.
    private readonly Random _random = Random.Shared;

    // [KYA KARTA HAI] Injected failure rate probability (0.0 = 0% failures, 1.0 = 100% failures).
    // [KYU ZAROORI HAI] Controlled failure intensity dial karna (e.g. 0.3 = 30% error rate).
    // [KAISE CHALTA HAI] Compares nextDouble() against this threshold.
    // [OUTPUT / SIDE EFFECT] Probability limit set.
    // [VARIATIONS TO TRY] Spike testing with alternating failure rates.
    public double FailureRate { get; set; }

    // [KYA KARTA HAI] Minimum artificial latency in milliseconds.
    // [KYU ZAROORI HAI] Slow network simulation range floor.
    // [KAISE CHALTA HAI] Millisecond integer value.
    // [OUTPUT / SIDE EFFECT] Min delay bounded.
    // [VARIATIONS TO TRY] 0 for instant calls.
    public int MinLatencyMs { get; set; }

    // [KYA KARTA HAI] Maximum artificial latency in milliseconds.
    // [KYU ZAROORI HAI] Slow network simulation range ceiling.
    // [KAISE CHALTA HAI] Millisecond integer value.
    // [OUTPUT / SIDE EFFECT] Max delay bounded.
    // [VARIATIONS TO TRY] Large delay to test timeout triggers.
    public int MaxLatencyMs { get; set; }

    // [KYA KARTA HAI] Total executed requests counter.
    // [KYU ZAROORI HAI] Fault statistics tracking.
    // [KAISE CHALTA HAI] Atomic counter incremented per call.
    // [OUTPUT / SIDE EFFECT] Request count tracked.
    // [VARIATIONS TO TRY] Interlocked increment.
    public int TotalInvocations;

    // [KYA KARTA HAI] Total injected failures counter.
    // [KYU ZAROORI HAI] Verifying that expected number of faults actually occurred.
    // [KAISE CHALTA HAI] Incremented on fault injection.
    // [OUTPUT / SIDE EFFECT] Injected error count.
    // [VARIATIONS TO TRY] Interlocked increment.
    public int InjectedFaultsCount;

    // [KYA KARTA HAI] Chaos fault injector constructor initialize karta hai.
    // [KYU ZAROORI HAI] Default safe chaos parameters set karna.
    // [KAISE CHALTA HAI] Sets failure rate and latency boundaries.
    // [OUTPUT / SIDE EFFECT] Injector initialized.
    // [VARIATIONS TO TRY] Pass parameters via constructor.
    public ChaosFaultInjector(double failureRate = 0.25, int minLatencyMs = 10, int maxLatencyMs = 80)
    {
        // [KYA KARTA HAI] Failure rate boundary validation.
        // [KYU ZAROORI HAI] Rate 0.0 aur 1.0 ke beech hona mandatory hai.
        // [KAISE CHALTA HAI] Validates range.
        // [OUTPUT / SIDE EFFECT] Valid rate assigned.
        // [VARIATIONS TO TRY] None.
        if (failureRate < 0.0 || failureRate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(failureRate), "Failure rate must be between 0.0 and 1.0");

        FailureRate = failureRate;
        MinLatencyMs = minLatencyMs;
        MaxLatencyMs = maxLatencyMs;
    }

    // [KYA KARTA HAI] Downstream payment gateway call ko chaos faults ke sath simulate karta hai.
    // [KYU ZAROORI HAI] Real third-party API (Paytm / Razorpay) ke real-world intermittent failures simulate karna.
    // [KAISE CHALTA HAI] Random latency sleep karta hai; failure probability match hone par exception throw karta hai, warna success return karta hai.
    // [OUTPUT / SIDE EFFECT] Either CheckoutResult or PaymentGatewayUnavailableException.
    // [VARIATIONS TO TRY] Inject HTTP 429 Too Many Requests.
    public async Task<CheckoutResult> SimulatePaymentGatewayCallAsync(string orderId, decimal amount)
    {
        // [KYA KARTA HAI] Total invocations counter increment karta hai thread-safely.
        // [KYU ZAROORI HAI] Audit ratio calculate karna.
        // [KAISE CHALTA HAI] Interlocked.Increment atomic instruction.
        // [OUTPUT / SIDE EFFECT] Total count updated.
        // [VARIATIONS TO TRY] None.
        System.Threading.Interlocked.Increment(ref TotalInvocations);

        // [KYA KARTA HAI] Random latency inject karta hai network transmission delay simulate karne ke liye.
        // [KYU ZAROORI HAI] Real-world network variance aur database queries ki latency reflect karna.
        // [KAISE CHALTA HAI] Next int between min and max passed to Task.Delay.
        // [OUTPUT / SIDE EFFECT] Non-blocking async delay.
        // [VARIATIONS TO TRY] Normal (Gaussian) latency distribution.
        var latency = _random.Next(MinLatencyMs, MaxLatencyMs + 1);
        if (latency > 0)
        {
            await Task.Delay(latency);
        }

        // [KYA KARTA HAI] Random probability draw karta hai failure injection ke liye.
        // [KYU ZAROORI HAI] Stochastic distribution maintain karna.
        // [KAISE CHALTA HAI] Generates double between 0.0 and 1.0.
        // [OUTPUT / SIDE EFFECT] Roll value evaluated.
        // [VARIATIONS TO TRY] Seeded pseudo-random sequence for reproducible chaos tests.
        var roll = _random.NextDouble();
        if (roll < FailureRate)
        {
            // [KYA KARTA HAI] Injected faults counter increment karta hai.
            // [KYU ZAROORI HAI] Track injected error count.
            // [KAISE CHALTA HAI] Interlocked.Increment atomic operation.
            // [OUTPUT / SIDE EFFECT] Error count updated.
            // [VARIATIONS TO TRY] None.
            System.Threading.Interlocked.Increment(ref InjectedFaultsCount);

            // [KYA KARTA HAI] PaymentGatewayUnavailableException throw karta hai.
            // [KYU ZAROORI HAI] Polly pipeline ke retry aur circuit breaker ko trigger karna.
            // [KAISE CHALTA HAI] Throws custom exception with simulated error message.
            // [OUTPUT / SIDE EFFECT] Simulated gateway crash.
            // [VARIATIONS TO TRY] Throw SocketException or HttpRequestException.
            throw new PaymentGatewayUnavailableException($"[CHAOS FAULT] Downstream Payment Gateway timed out or returned HTTP 503 Service Unavailable (Roll: {roll:F2} < {FailureRate:F2})");
        }

        // [KYA KARTA HAI] Successful checkout transaction return karta hai.
        // [KYU ZAROORI HAI] Normal happy path execution simulate karna.
        // [KAISE CHALTA HAI] Instantiates successful CheckoutResult record.
        // [OUTPUT / SIDE EFFECT] Confirmed checkout result.
        // [VARIATIONS TO TRY] Include mock authorization tokens.
        return new CheckoutResult(
            IsSuccess: true,
            Message: "Payment successfully authorized via Primary Gateway",
            TransactionId: $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            IsDegradedFallback: false
        );
    }
}
