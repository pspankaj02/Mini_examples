// ==============================================================================================
// KiranaHub Resilience: Polly v8 Unified Reactive & Proactive Resilience Pipelines
// [KYA KARTA HAI] Microservices me Retries, Circuit Breaker, Timeout, Rate Limiter, aur Fallback policies ko compose karta hai.
// [KYU ZAROORI HAI] Downstream services (Payment gateway, inventory database) down ya slow hone par cascade failure se bacha kar graceful degradation ensure karna.
// [KAISE CHALTA HAI] Polly v8 ResiliencePipelineBuilder chain me strategies attach karta hai jo requests ko intercept karti hain.
// [OUTPUT / SIDE EFFECT] Self-healing fault-tolerant microservice execution.
// [VARIATIONS TO TRY] Dynamic reload of circuit breaker thresholds via IOptionsMonitor.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Exception, TimeSpan, Action types provide karna.
// [KAISE CHALTA HAI] Standard library types load hoti hain.
// [OUTPUT / SIDE EFFECT] Core types available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] Thread-safe metrics counters maintain karne ke liye.
// [KAISE CHALTA HAI] Atomic memory access provide karta hai.
// [OUTPUT / SIDE EFFECT] Thread-safe collections available.
// [VARIATIONS TO TRY] Channels.
using System.Collections.Concurrent;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] ValueTask aur Task asynchronous workflows ke liye.
// [KAISE CHALTA HAI] Async/await primitives load hoti hain.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Polly core namespace import karta hai.
// [KYU ZAROORI HAI] ResiliencePipelineBuilder, ResiliencePipeline, aur Polly v8 engine provide karna.
// [KAISE CHALTA HAI] Polly v8 high-performance zero-allocation pipeline load hoti hai.
// [OUTPUT / SIDE EFFECT] Polly v8 available.
// [VARIATIONS TO TRY] Legacy Polly v7 Policy.Wrap.
using Polly;

// [KYA KARTA HAI] Polly Circuit Breaker strategy namespace import karta hai.
// [KYU ZAROORI HAI] Advanced circuit breaker configuration options access karna.
// [KAISE CHALTA HAI] Circuit breaker strategies load hoti hain.
// [OUTPUT / SIDE EFFECT] Circuit breaker ready.
// [VARIATIONS TO TRY] Reactive vs Proactive circuit breakers.
using Polly.CircuitBreaker;

// [KYA KARTA HAI] Polly Retry strategy namespace import karta hai.
// [KYU ZAROORI HAI] RetryStrategyOptions aur backoff math configure karna.
// [KAISE CHALTA HAI] Retry strategies available.
// [OUTPUT / SIDE EFFECT] Retry logic ready.
// [VARIATIONS TO TRY] Exponential backoff with full jitter.
using Polly.Retry;

// [KYA KARTA HAI] Polly Fallback strategy namespace import karta hai.
// [KYU ZAROORI HAI] Degraded default response provide karna failure ke time.
// [KAISE CHALTA HAI] Fallback options available.
// [OUTPUT / SIDE EFFECT] Fallback logic ready.
// [VARIATIONS TO TRY] Static cached fallback vs secondary service fallback.
using Polly.Fallback;

// [KYA KARTA HAI] Polly Timeout strategy namespace import karta hai.
// [KYU ZAROORI HAI] Slow unresponsive third-party calls ko terminate karna.
// [KAISE CHALTA HAI] Timeout options available.
// [OUTPUT / SIDE EFFECT] Timeout strategy ready.
// [VARIATIONS TO TRY] Optimistic vs Pessimistic timeouts.
using Polly.Timeout;

// [KYA KARTA HAI] Namespace declare karta hai resilience policies ke liye.
// [KYU ZAROORI HAI] Code modularity aur organization.
// [KAISE CHALTA HAI] Scopes classes under KiranaHub.ChaosAndLoadTesting.Resilience.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Resilience.Core.
namespace KiranaHub.ChaosAndLoadTesting.Resilience;

// [KYA KARTA HAI] Payment checkout response data record.
// [KYU ZAROORI HAI] Structured outcome encapsulate karna (Success, Fallback, TransactionId).
// [KAISE CHALTA HAI] Immutable C# record with init-only properties.
// [OUTPUT / SIDE EFFECT] Type-safe result object.
// [VARIATIONS TO TRY] Class or struct.
public record CheckoutResult(bool IsSuccess, string Message, string TransactionId, bool IsDegradedFallback = false);

// [KYA KARTA HAI] Custom transient exception representing third-party gateway downtime.
// [KYU ZAROORI HAI] Circuit breaker aur retry policies ko specific network errors par trigger karwana.
// [KAISE CHALTA HAI] Inherits from System.Exception.
// [OUTPUT / SIDE EFFECT] Custom exception type.
// [VARIATIONS TO TRY] Add HTTP status code property.
public class PaymentGatewayUnavailableException : Exception
{
    // [KYA KARTA HAI] Exception constructor initialize karta hai.
    // [KYU ZAROORI HAI] Custom error message pass karna base class ko.
    // [KAISE CHALTA HAI] Passes message string to base constructor.
    // [OUTPUT / SIDE EFFECT] Exception instance created.
    // [VARIATIONS TO TRY] Pass innerException.
    public PaymentGatewayUnavailableException(string message) : base(message) { }
}

// [KYA KARTA HAI] Unified Polly v8 resilience pipelines factory class.
// [KYU ZAROORI HAI] Production-grade resilience strategies ko ek central place par configure karna.
// [KAISE CHALTA HAI] Builds and exposes compiled ResiliencePipeline<CheckoutResult> instances.
// [OUTPUT / SIDE EFFECT] Reusable high-performance resilience pipelines.
// [VARIATIONS TO TRY] Register into Microsoft.Extensions.DependencyInjection with AddResiliencePipeline.
public class PollyPipelines
{
    // [KYA KARTA HAI] Circuit breaker state change audit counter.
    // [KYU ZAROORI HAI] Automated tests verify kar sakein ki circuit sach me trip hua ya nahi.
    // [KAISE CHALTA HAI] Incremented on circuit state changes.
    // [OUTPUT / SIDE EFFECT] State change metrics.
    // [VARIATIONS TO TRY] Push to OpenTelemetry Metrics Meter.
    public int CircuitBreakerTripCount { get; private set; }

    // [KYA KARTA HAI] Fallback execution counter.
    // [KYU ZAROORI HAI] Track karna ki kitni requests fallback par divert hui hain.
    // [KAISE CHALTA HAI] Incremented inside fallback handler.
    // [OUTPUT / SIDE EFFECT] Fallback metrics.
    // [VARIATIONS TO TRY] Interlocked.Increment for thread safety.
    public int FallbackExecutionCount;

    // [KYA KARTA HAI] Circuit breaker controller reference.
    // [KYU ZAROORI HAI] Test runner circuit breaker ki live state inspect kar sake.
    // [KAISE CHALTA HAI] Stored via manual manual controller if needed.
    // [OUTPUT / SIDE EFFECT] Manual inspection handle.
    // [VARIATIONS TO TRY] HealthCheck integration.
    public CircuitState CurrentCircuitState { get; private set; } = CircuitState.Closed;

    // [KYA KARTA HAI] Production-grade Polly v8 Resilience Pipeline build karta hai.
    // [KYU ZAROORI HAI] Multi-layered defense: Fallback -> Circuit Breaker -> Retry with Jitter -> Timeout.
    // [KAISE CHALTA HAI] Chains strategies using ResiliencePipelineBuilder<CheckoutResult>.
    // [OUTPUT / SIDE EFFECT] Compiled, zero-allocation ResiliencePipeline ready for high throughput.
    // [VARIATIONS TO TRY] Add RateLimiter strategy for concurrency capping.
    public ResiliencePipeline<CheckoutResult> CreateCheckoutResiliencePipeline()
    {
        // [KYA KARTA HAI] Generic ResiliencePipelineBuilder instantiate karta hai.
        // [KYU ZAROORI HAI] Type-safe pipeline strategies assemble karna.
        // [KAISE CHALTA HAI] Allocates builder instance.
        // [OUTPUT / SIDE EFFECT] Pipeline builder ready.
        // [VARIATIONS TO TRY] Non-generic ResiliencePipelineBuilder for void actions.
        var builder = new ResiliencePipelineBuilder<CheckoutResult>();

        // [KYA KARTA HAI] Layer 1: Graceful Fallback Strategy add karta hai.
        // [KYU ZAROORI HAI] Agar downstream gateway permanently fail ho jaye ya circuit khula ho, toh customer ko 500 error ke bajaye graceful offline payment response dena.
        // [KAISE CHALTA HAI] Intercepts BrokenCircuitException aur PaymentGatewayUnavailableException and returns fallback CheckoutResult.
        // [OUTPUT / SIDE EFFECT] Zero unhandled crashes returned to user.
        // [VARIATIONS TO TRY] Fallback to secondary payment partner (e.g. PayU -> Razorpay).
        builder.AddFallback(new FallbackStrategyOptions<CheckoutResult>
        {
            // [KYA KARTA HAI] Predicate define karta hai ki kin exceptions/results par fallback trigger hoga.
            // [KYU ZAROORI HAI] BrokenCircuitException aur custom network exceptions handle karna.
            // [KAISE CHALTA HAI] Evaluates args.Outcome.Exception.
            // [OUTPUT / SIDE EFFECT] Filters matching errors.
            // [VARIATIONS TO TRY] Also trigger on HTTP 504 Gateway Timeout.
            ShouldHandle = new PredicateBuilder<CheckoutResult>()
                .Handle<PaymentGatewayUnavailableException>()
                .Handle<BrokenCircuitException>()
                .Handle<TimeoutRejectedException>(),

            // [KYA KARTA HAI] Fallback generator function define karta hai.
            // [KYU ZAROORI HAI] Customer-safe degraded result produce karna.
            // [KAISE CHALTA HAI] Returns pre-calculated successful degraded outcome.
            // [OUTPUT / SIDE EFFECT] Degraded result with transaction ID.
            // [VARIATIONS TO TRY] Cache response or send SMS acknowledgment.
            FallbackAction = args =>
            {
                System.Threading.Interlocked.Increment(ref FallbackExecutionCount);
                var fallbackResult = new CheckoutResult(
                    IsSuccess: true,
                    Message: "Payment Gateway busy. Order accepted in Cash-on-Delivery (COD) fallback mode.",
                    TransactionId: $"COD-FALLBACK-{Guid.NewGuid().ToString("N")[..8]}",
                    IsDegradedFallback: true
                );
                return ValueTask.FromResult(Outcome.FromResult(fallbackResult));
            }
        });

        // [KYA KARTA HAI] Layer 2: Advanced Circuit Breaker Strategy add karta hai.
        // [KYU ZAROORI HAI] Downstream failing service ko continuous requests se spam karne se rokhna aur system self-healing allow karna.
        // [KAISE CHALTA HAI] Sliding window me failure rate measure karta hai; threshold cross hone par circuit trip karta hai.
        // [OUTPUT / SIDE EFFECT] Circuit transitions: Closed -> Open -> HalfOpen -> Closed.
        // [VARIATIONS TO TRY] Consecutive failure count breaker.
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<CheckoutResult>
        {
            // [KYA KARTA HAI] Circuit breaker failure handling filter define karta hai.
            // [KYU ZAROORI HAI] Specific gateway crashes count karna.
            // [KAISE CHALTA HAI] Predicate matching.
            // [OUTPUT / SIDE EFFECT] Tracks transient failures.
            // [VARIATIONS TO TRY] Handle HttpRequestException.
            ShouldHandle = new PredicateBuilder<CheckoutResult>()
                .Handle<PaymentGatewayUnavailableException>(),

            // [KYA KARTA HAI] Failure ratio threshold define karta hai (50%).
            // [KYU ZAROORI HAI] Agar 50% requests fail ho rahi hain, toh service ko down maan lena.
            // [KAISE CHALTA HAI] Compares failures / total requests in sampling duration.
            // [OUTPUT / SIDE EFFECT] Ratio boundary 0.5.
            // [VARIATIONS TO TRY] 0.7 for higher error tolerance.
            FailureRatio = 0.5,

            // [KYA KARTA HAI] Minimum throughput before evaluation begins (minimum 4 requests).
            // [KYU ZAROORI HAI] 1 single early failure par premature circuit trip hone se rokna.
            // [KAISE CHALTA HAI] Ignores ratio until 4 executions occur.
            // [OUTPUT / SIDE EFFECT] Minimum sample size 4.
            // [VARIATIONS TO TRY] 20 in high-volume production systems.
            MinimumThroughput = 4,

            // [KYA KARTA HAI] Sampling analysis duration time window.
            // [KYU ZAROORI HAI] Kitni der ki requests ko analyze karna hai.
            // [KAISE CHALTA HAI] 5 seconds sliding window.
            // [OUTPUT / SIDE EFFECT] Time window established.
            // [VARIATIONS TO TRY] 30 seconds for smoother trends.
            SamplingDuration = TimeSpan.FromSeconds(5),

            // [KYA KARTA HAI] Break duration (circuit open time).
            // [KYU ZAROORI HAI] Service ko heal hone ke liye kitni der tak rest dena hai.
            // [KAISE CHALTA HAI] Circuit remains Open for 1.5 seconds, then enters Half-Open.
            // [OUTPUT / SIDE EFFECT] 1.5s cooldown period.
            // [VARIATIONS TO TRY] 30 seconds in production.
            BreakDuration = TimeSpan.FromMilliseconds(1500),

            // [KYA KARTA HAI] Circuit open event handler.
            // [KYU ZAROORI HAI] Metrics update karna aur alert log karna jab circuit trip ho.
            // [KAISE CHALTA HAI] Invoked by Polly on circuit open transition.
            // [OUTPUT / SIDE EFFECT] Increments trip counter.
            // [VARIATIONS TO TRY] Trigger PagerDuty alert.
            OnOpened = args =>
            {
                CircuitBreakerTripCount++;
                CurrentCircuitState = CircuitState.Open;
                Console.WriteLine($"  [POLLY ALERT] Circuit Breaker TRIPPED to OPEN! Break Duration: {args.BreakDuration.TotalMilliseconds}ms. Reason: {args.Outcome.Exception?.Message}");
                return ValueTask.CompletedTask;
            },

            // [KYA KARTA HAI] Circuit closed event handler.
            // [KYU ZAROORI HAI] Health restore hone par alert emit karna.
            // [KAISE CHALTA HAI] Invoked when trial requests succeed in Half-Open state.
            // [OUTPUT / SIDE EFFECT] State set to Closed.
            // [VARIATIONS TO TRY] Send Slack notification that incident is resolved.
            OnClosed = args =>
            {
                CurrentCircuitState = CircuitState.Closed;
                Console.WriteLine("  [POLLY RECOVERY] Circuit Breaker RESET to CLOSED! Downstream health fully restored.");
                return ValueTask.CompletedTask;
            },

            // [KYA KARTA HAI] Circuit half-open event handler.
            // [KYU ZAROORI HAI] Downstream service test karne ke liye trial probes bhejna.
            // [KAISE CHALTA HAI] Invoked after break duration expires.
            // [OUTPUT / SIDE EFFECT] State set to HalfOpen.
            // [VARIATIONS TO TRY] Log diagnostic probes.
            OnHalfOpened = args =>
            {
                CurrentCircuitState = CircuitState.HalfOpen;
                Console.WriteLine("  [POLLY PROBE] Circuit Breaker entered HALF-OPEN state. Testing trial requests...");
                return ValueTask.CompletedTask;
            }
        });

        // [KYA KARTA HAI] Layer 3: Exponential Backoff Retry Strategy with Jitter.
        // [KYU ZAROORI HAI] Transient network glitches (1-2 lost packets) ko bina user ko fail kiye turant retry karna.
        // [KAISE CHALTA HAI] Backoff math: baseDelay * 2^attempt + random jitter to prevent thundering herd.
        // [OUTPUT / SIDE EFFECT] Max 2 retries with exponential delay.
        // [VARIATIONS TO TRY] Decorrelated jitter algorithm.
        builder.AddRetry(new RetryStrategyOptions<CheckoutResult>
        {
            // [KYA KARTA HAI] Retryable exceptions filter define karta hai.
            // [KYU ZAROORI HAI] Sirf transient errors retry karna, business validation (e.g. Insufficient Balance) retry na karna.
            // [KAISE CHALTA HAI] Predicate builder checks exception type.
            // [OUTPUT / SIDE EFFECT] Targeted retry.
            // [VARIATIONS TO TRY] Do not retry 4xx client errors.
            ShouldHandle = new PredicateBuilder<CheckoutResult>()
                .Handle<PaymentGatewayUnavailableException>(),

            // [KYA KARTA HAI] Maximum retry attempts count.
            // [KYU ZAROORI HAI] Latency accumulate hone se rokna.
            // [KAISE CHALTA HAI] Tries up to 2 times.
            // [OUTPUT / SIDE EFFECT] Max 2 retries.
            // [VARIATIONS TO TRY] 3 retries in non-interactive background jobs.
            MaxRetryAttempts = 2,

            // [KYA KARTA HAI] Exponential backoff mode select karta hai.
            // [KYU ZAROORI HAI] Har retry me wait time double karna taaki downstream service par overload na ho.
            // [KAISE CHALTA HAI] Delay = Delay * 2.
            // [OUTPUT / SIDE EFFECT] Exponential delays.
            // [VARIATIONS TO TRY] Linear or constant delay.
            BackoffType = DelayBackoffType.Exponential,

            // [KYA KARTA HAI] Initial delay duration set karta hai.
            // [KYU ZAROORI HAI] Fast sub-second retry for 10-minute delivery SLAs.
            // [KAISE CHALTA HAI] Starts at 50 milliseconds.
            // [OUTPUT / SIDE EFFECT] 50ms base delay.
            // [VARIATIONS TO TRY] 200ms in WAN environments.
            Delay = TimeSpan.FromMilliseconds(50),

            // [KYA KARTA HAI] Random jitter addition enable karta hai.
            // [KYU ZAROORI HAI] Thundering herd problem rokhna — 1,000 clients ek hi exact millisecond par retry na karein.
            // [KAISE CHALTA HAI] Injects random millisecond variance into delay.
            // [OUTPUT / SIDE EFFECT] Desynchronized client retries.
            // [VARIATIONS TO TRY] Full jitter vs Equal jitter.
            UseJitter = true
        });

        // [KYA KARTA HAI] Layer 4: Hard Execution Timeout Strategy add karta hai.
        // [KYU ZAROORI HAI] Agar downstream service socket hang kar de, toh customer request indefinitely block na rahe.
        // [KAISE CHALTA HAI] Abort timer trigger karta hai specified timeout ke baad.
        // [OUTPUT / SIDE EFFECT] TimeoutRejectedException thrown after 800ms.
        // [VARIATIONS TO TRY] Dynamic timeout based on operation type.
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(800)
        });

        // [KYA KARTA HAI] Pipeline compile karke immutable instance return karta hai.
        // [KYU ZAROORI HAI] Thread-safe, high-concurrency execution ke liye pipeline compile karna.
        // [KAISE CHALTA HAI] Compiles registered strategies into an optimized execution chain.
        // [OUTPUT / SIDE EFFECT] Compiled pipeline ready.
        // [VARIATIONS TO TRY] None.
        return builder.Build();
    }
}
