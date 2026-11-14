namespace KiranaHub.TracingApm.Sampling;

using KiranaHub.TracingApm.Graph;

// [KYA KARTA HAI]: Tail-Based Adaptive Tracing Sampler implement karta hai.
// [KYU ZAROORI HAI]: Head-Based sampling me shuru me hi blind decision lena padta hai jisse important errors ya slow traces drop ho jate hain; Tail-Based sampling trace complete hone ke baad inspect karta hai aur critical telemetry preserve karta hai while saving 95%+ APM storage costs!
// [KAISE CHALTA HAI]: Trace complete hone ke baad errors check karta hai (`HasErrors`), latency threshold compare karta hai (`TotalDurationMs > SlowThresholdMs`), aur normal healthy traces par low sampling rate apply karta hai.
// [OUTPUT / SIDE EFFECT]: SamplingDecision with retention flag and explanatory reason.
// [VARIATIONS TO TRY]: Dynamic adaptive sampling based on current storage queue backpressure.
public class TailBasedSampler
{
    // [KYA KARTA HAI]: Slow trace threshold in milliseconds (default 250ms).
    // [KYU ZAROORI HAI]: P99 latency outliers ko capture karna.
    // [KAISE CHALTA HAI]: Milliseconds property.
    // [OUTPUT / SIDE EFFECT]: Threshold double.
    // [VARIATIONS TO TRY]: Per-endpoint customized thresholds.
    public double SlowThresholdMs { get; set; } = 250.0;

    // [KYA KARTA HAI]: Healthy fast traces ke liye probabilistic sample rate (0.0 to 1.0, default 0.05 = 5%).
    // [KYU ZAROORI HAI]: Baseline traffic patterns observe karna without blowing up storage bills.
    // [KAISE CHALTA HAI]: Double ratio.
    // [OUTPUT / SIDE EFFECT]: Sampling percentage.
    // [VARIATIONS TO TRY]: Dynamic rate based on throughput.
    public double BaselineSampleRate { get; set; } = 0.05;

    // [KYA KARTA HAI]: Random generator for probabilistic sampling.
    // [KYU ZAROORI HAI]: Uniform dice roll.
    // [KAISE CHALTA HAI]: Random instance.
    // [OUTPUT / SIDE EFFECT]: Random number.
    // [VARIATIONS TO TRY]: Deterministic trace ID hash modulo.
    private readonly Random _random = new(42); // Seeded for deterministic testing

    // [KYA KARTA HAI]: TraceGraph ka inspection karke tail-based sampling decision generate karta hai.
    // [KYU ZAROORI HAI]: Decide whether to persist trace in long-term APM store or discard.
    // [KAISE CHALTA HAI]: Priority checks: 1) HasErrors -> Keep; 2) SlowLatency -> Keep; 3) Baseline Dice Roll.
    // [OUTPUT / SIDE EFFECT]: SamplingDecision record.
    // [VARIATIONS TO TRY]: Attribute based rules (e.g. VIP customer cart always sampled).
    public SamplingDecision Evaluate(SpanGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        // RULE 1: Any trace containing an error MUST be 100% retained!
        if (graph.HasErrors)
        {
            return new SamplingDecision(
                IsSampled: true,
                Reason: "Retained [Rule: ErrorDetected] - Trace contains failed span",
                PolicyApplied: "ErrorPolicy");
        }

        // RULE 2: Any trace exceeding slow latency threshold MUST be 100% retained!
        if (graph.TotalDurationMs >= SlowThresholdMs)
        {
            return new SamplingDecision(
                IsSampled: true,
                Reason: $"Retained [Rule: LatencyOutlier] - Duration {graph.TotalDurationMs:F1}ms >= {SlowThresholdMs}ms threshold",
                PolicyApplied: "LatencyPolicy");
        }

        // RULE 3: Probabilistic baseline sampling for healthy fast traces
        bool diceRoll = _random.NextDouble() < BaselineSampleRate;
        if (diceRoll)
        {
            return new SamplingDecision(
                IsSampled: true,
                Reason: $"Retained [Rule: BaselineSampled] - Hit {BaselineSampleRate * 100}% probabilistic quota",
                PolicyApplied: "BaselinePolicy");
        }

        // Discard normal fast trace to save storage
        return new SamplingDecision(
            IsSampled: false,
            Reason: $"Discarded [Rule: HealthyDrop] - Fast duration ({graph.TotalDurationMs:F1}ms) & no errors",
            PolicyApplied: "DropPolicy");
    }
}

// [KYA KARTA HAI]: Tail-based sampling decision record.
// [KYU ZAROORI HAI]: Sampling audit trail and decision transparency.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Decision DTO.
// [VARIATIONS TO TRY]: Storage tier target (e.g. Hot vs Warm vs Cold).
public record SamplingDecision(
    bool IsSampled,
    string Reason,
    string PolicyApplied);
