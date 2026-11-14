namespace KiranaHub.TracingApm.Graph;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Distributed Tracing ke Spans aur DAG (Directed Acyclic Graph) representation implement karta hai.
// [KYU ZAROORI HAI]: Multi-hop distributed transactions me call hierarchy, latency distribution, aur error origins visualize aur analyze karna.
// [KAISE CHALTA HAI]: Spans ko unke `ParentSpanId` ke zariye tree graph me organize karta hai; root span detect karta hai, total duration nikalta hai, aur slowest bottleneck span identify karta hai.
// [OUTPUT / SIDE EFFECT]: Complete trace call graph and critical path metrics.
// [VARIATIONS TO TRY]: Cytoscape / Mermaid diagram JSON exporter.
public class SpanGraph
{
    // [KYA KARTA HAI]: Trace ID jisse ye graph belong karta hai.
    // [KYU ZAROORI HAI]: Single trace correlation.
    // [KAISE CHALTA HAI]: String property.
    // [OUTPUT / SIDE EFFECT]: 32-char hex trace ID.
    // [VARIATIONS TO TRY]: Readonly property.
    public string TraceId { get; }

    // [KYA KARTA HAI]: Spans dictionary (SpanId -> Span).
    // [KYU ZAROORI HAI]: O(1) span lookup by ID.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, Span>.
    // [OUTPUT / SIDE EFFECT]: Spans collection.
    // [VARIATIONS TO TRY]: Presorted array by StartTime.
    private readonly ConcurrentDictionary<string, Span> _spans = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI]: Total spans count in this trace.
    // [KYU ZAROORI HAI]: Complexity metric.
    // [KAISE CHALTA HAI]: _spans.Count.
    // [OUTPUT / SIDE EFFECT]: Integer count.
    // [VARIATIONS TO TRY]: Maximum depth calculation.
    public int SpanCount => _spans.Count;

    // [KYA KARTA HAI]: SpanGraph instance initialize karta hai.
    // [KYU ZAROORI HAI]: Graph setup.
    // [KAISE CHALTA HAI]: Constructor.
    // [OUTPUT / SIDE EFFECT]: Empty trace graph.
    // [VARIATIONS TO TRY]: Pre-allocated capacity.
    public SpanGraph(string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        TraceId = traceId.ToLowerInvariant();
    }

    // [KYA KARTA HAI]: Trace me completed span add karta hai.
    // [KYU ZAROORI HAI]: Ingesting telemetry spans.
    // [KAISE CHALTA HAI]: _spans[span.SpanId] = span.
    // [OUTPUT / SIDE EFFECT]: Graph node added.
    // [VARIATIONS TO TRY]: Validate that span.TraceId matches graph.TraceId.
    public void AddSpan(Span span)
    {
        ArgumentNullException.ThrowIfNull(span);
        if (!string.Equals(span.TraceId, TraceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Span TraceId '{span.TraceId}' does not match graph TraceId '{TraceId}'");
        }
        _spans[span.SpanId] = span;
    }

    // [KYA KARTA HAI]: Trace ka root span (jiska ParentSpanId null ya empty ho) find karta hai.
    // [KYU ZAROORI HAI]: Call tree rendering ka entry point dhundna.
    // [KAISE CHALTA HAI]: LINQ FirstOrDefault where ParentSpanId is null.
    // [OUTPUT / SIDE EFFECT]: Root Span instance.
    // [VARIATIONS TO TRY]: Support multiple roots in disconnected spans.
    public Span? GetRootSpan() => _spans.Values.FirstOrDefault(s => string.IsNullOrEmpty(s.ParentSpanId));

    // [KYA KARTA HAI]: Given parent span ID ke direct child spans return karta hai.
    // [KYU ZAROORI HAI]: Recursive call tree traversal.
    // [KAISE CHALTA HAI]: LINQ Where ParentSpanId == parentId ordered by StartTimeUtc.
    // [OUTPUT / SIDE EFFECT]: List of child spans.
    // [VARIATIONS TO TRY]: Pre-indexed adjacency list.
    public IReadOnlyList<Span> GetChildren(string parentSpanId)
    {
        return _spans.Values
            .Where(s => string.Equals(s.ParentSpanId, parentSpanId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StartTimeUtc)
            .ToList()
            .AsReadOnly();
    }

    // [KYA KARTA HAI]: Trace me koi bhi failed/error span hai ya nahi check karta hai.
    // [KYU ZAROORI HAI]: Tail-based sampling decision making (errors must always be retained).
    // [KAISE CHALTA HAI]: LINQ Any where Status == SpanStatus.Error.
    // [OUTPUT / SIDE EFFECT]: True if any span had an error.
    // [VARIATIONS TO TRY]: Error count metric.
    public bool HasErrors => _spans.Values.Any(s => s.Status == SpanStatus.Error);

    // [KYA KARTA HAI]: Total end-to-end trace latency calculate karta hai in milliseconds.
    // [KYU ZAROORI HAI]: P95/P99 latency monitoring.
    // [KAISE CHALTA HAI]: Root span duration ya max(endTime) - min(startTime).
    // [OUTPUT / SIDE EFFECT]: Total duration double.
    // [VARIATIONS TO TRY]: Microsecond precision.
    public double TotalDurationMs
    {
        get
        {
            var root = GetRootSpan();
            if (root != null) return root.DurationMs;
            if (_spans.Count == 0) return 0;
            var minStart = _spans.Values.Min(s => s.StartTimeUtc);
            var maxEnd = _spans.Values.Max(s => s.StartTimeUtc.AddMilliseconds(s.DurationMs));
            return (maxEnd - minStart).TotalMilliseconds;
        }
    }

    // [KYA KARTA HAI]: Sabse zyada duration lene wala downstream child bottleneck span find karta hai.
    // [KYU ZAROORI HAI]: Root span toh hamesha total wrapper time leta hai; downstream child spans me se asli performance bottleneck identify karna zaroori hai.
    // [KAISE CHALTA HAI]: Non-root spans (where ParentSpanId is not empty) me se MaxBy DurationMs compute karta hai.
    // [OUTPUT / SIDE EFFECT]: Slowest downstream child Span instance.
    // [VARIATIONS TO TRY]: Self-time (duration minus children duration) calculation.
    public Span? FindBottleneckSpan() => _spans.Values.Where(s => !string.IsNullOrEmpty(s.ParentSpanId)).MaxBy(s => s.DurationMs);

    public IEnumerable<Span> GetAllSpans() => _spans.Values;
}

// [KYA KARTA HAI]: Single unit of distributed work represent karne wala model.
// [KYU ZAROORI HAI]: OpenTelemetry compliant span structure.
// [KAISE CHALTA HAI]: Properties hold timing, status, and attributes.
// [OUTPUT / SIDE EFFECT]: Telemetry span model.
// [VARIATIONS TO TRY]: Event logs list attachment.
public record Span(
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    SpanKind Kind,
    DateTime StartTimeUtc,
    double DurationMs,
    SpanStatus Status,
    IReadOnlyDictionary<string, string> Attributes);

// [KYA KARTA HAI]: OpenTelemetry standard SpanKind enum.
// [KYU ZAROORI HAI]: Classifying role of span in network topology.
// [KAISE CHALTA HAI]: Server, Client, Producer, Consumer, Internal.
// [OUTPUT / SIDE EFFECT]: Span kind classification.
// [VARIATIONS TO TRY]: Add RPC specific sub-kinds.
public enum SpanKind
{
    Internal,
    Server,
    Client,
    Producer,
    Consumer
}

// [KYA KARTA HAI]: Span completion status enum.
// [KYU ZAROORI HAI]: Ok vs Error reporting.
// [KAISE CHALTA HAI]: Unset, Ok, Error.
// [OUTPUT / SIDE EFFECT]: Status indicator.
// [VARIATIONS TO TRY]: Add Cancelled status.
public enum SpanStatus
{
    Unset,
    Ok,
    Error
}
