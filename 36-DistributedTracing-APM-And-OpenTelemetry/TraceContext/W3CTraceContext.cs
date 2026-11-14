namespace KiranaHub.TracingApm.TraceContext;

using System.Security.Cryptography;
using System.Text.RegularExpressions;

// [KYA KARTA HAI]: W3C Trace Context Specification (traceparent header) standard implement karta hai.
// [KYU ZAROORI HAI]: Distributed microservices (API Gateway -> Order Service -> Inventory -> Payments) ke beech cross-network tracing context propagate karna.
// [KAISE CHALTA HAI]: `traceparent: {version:2hex}-{trace_id:32hex}-{parent_id:16hex}-{trace_flags:2hex}` parse karta hai, validate karta hai, random cryptographic IDs generate karta hai, aur HTTP headers me serialize karta hai.
// [OUTPUT / SIDE EFFECT]: W3C compliant distributed trace context object.
// [VARIATIONS TO TRY]: tracestate vendor list key-value propagation add karein.
public class W3CTraceContext
{
    // [KYA KARTA HAI]: W3C version string (currently "00").
    // [KYU ZAROORI HAI]: Specification version indicator.
    // [KAISE CHALTA HAI]: Fixed string "00".
    // [OUTPUT / SIDE EFFECT]: Version string.
    // [VARIATIONS TO TRY]: Forward-compatibility check for future specs.
    public const string CurrentVersion = "00";

    // [KYA KARTA HAI]: Globally unique 128-bit (32 hex chars) Trace ID store karta hai.
    // [KYU ZAROORI HAI]: Ek hi user request ke sabhi downstream microservice spans ko single trace me bind karna.
    // [KAISE CHALTA HAI]: Hex string property.
    // [OUTPUT / SIDE EFFECT]: 32-character hex string.
    // [VARIATIONS TO TRY]: Time-ordered UUID v7 trace ID.
    public string TraceId { get; }

    // [KYA KARTA HAI]: 64-bit (16 hex chars) Span/Parent ID store karta hai.
    // [KYU ZAROORI HAI]: Immediate caller span ka reference hold karna for DAG parent-child relationship.
    // [KAISE CHALTA HAI]: Hex string property.
    // [OUTPUT / SIDE EFFECT]: 16-character hex string.
    // [VARIATIONS TO TRY]: Span identifier enum.
    public string SpanId { get; }

    // [KYA KARTA HAI]: 8-bit (2 hex chars) Trace Flags store karta hai (e.g. "01" = Sampled).
    // [KYU ZAROORI HAI]: Upstream caller ka sampling decision (Recorded/Sampled) downstream pass karna.
    // [KAISE CHALTA HAI]: Hex string property.
    // [OUTPUT / SIDE EFFECT]: "01" (sampled) ya "00" (not sampled).
    // [VARIATIONS TO TRY]: Bitmask flags for debugging or high priority.
    public string TraceFlags { get; }

    // [KYA KARTA HAI]: W3CTraceContext instance initialize karta hai.
    // [KYU ZAROORI HAI]: Context creation and validation.
    // [KAISE CHALTA HAI]: Fields set karta hai.
    // [OUTPUT / SIDE EFFECT]: Trace context object.
    // [VARIATIONS TO TRY]: Immutable record.
    public W3CTraceContext(string traceId, string spanId, string traceFlags = "01")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spanId);

        TraceId = traceId.ToLowerInvariant();
        SpanId = spanId.ToLowerInvariant();
        TraceFlags = traceFlags.ToLowerInvariant();
    }

    // [KYA KARTA HAI]: Naya fresh root trace context generate karta hai with random cryptographic IDs.
    // [KYU ZAROORI HAI]: Jab user request API Gateway par enter hoti hai tab trace genesis establish karna.
    // [KAISE CHALTA HAI]: RandomNumberGenerator se 16 bytes (TraceId) aur 8 bytes (SpanId) generate karta hai aur hex me encode karta hai.
    // [OUTPUT / SIDE EFFECT]: Fresh W3CTraceContext instance.
    // [VARIATIONS TO TRY]: Machine ID prefix embedding.
    public static W3CTraceContext CreateNew(bool sampled = true)
    {
        byte[] traceBytes = new byte[16];
        byte[] spanBytes = new byte[8];

        RandomNumberGenerator.Fill(traceBytes);
        RandomNumberGenerator.Fill(spanBytes);

        string traceId = Convert.ToHexString(traceBytes).ToLowerInvariant();
        string spanId = Convert.ToHexString(spanBytes).ToLowerInvariant();
        string flags = sampled ? "01" : "00";

        return new W3CTraceContext(traceId, spanId, flags);
    }

    // [KYA KARTA HAI]: Child span ke liye naya context create karta hai preserving TraceId.
    // [KYU ZAROORI HAI]: Downstream microservice ya RPC call invoke karte waqt naya SpanId banana lekin TraceId same rakhna.
    // [KAISE CHALTA HAI]: Naya random 8-byte SpanId generate karta hai aur existing TraceId reuse karta hai.
    // [OUTPUT / SIDE EFFECT]: Child W3CTraceContext instance.
    // [VARIATIONS TO TRY]: Propagate custom baggage items.
    public W3CTraceContext CreateChild()
    {
        byte[] spanBytes = new byte[8];
        RandomNumberGenerator.Fill(spanBytes);
        string newSpanId = Convert.ToHexString(spanBytes).ToLowerInvariant();

        return new W3CTraceContext(TraceId, newSpanId, TraceFlags);
    }

    // [KYA KARTA HAI]: W3C standard `traceparent` header string me format karta hai.
    // [KYU ZAROORI HAI]: HTTP headers (`traceparent: 00-traceid-spanid-flags`) inject karna.
    // [KAISE CHALTA HAI]: $"{CurrentVersion}-{TraceId}-{SpanId}-{TraceFlags}".
    // [OUTPUT / SIDE EFFECT]: 55-character standard header string.
    // [VARIATIONS TO TRY]: Custom prefix format.
    public string ToHeaderString() => $"{CurrentVersion}-{TraceId}-{SpanId}-{TraceFlags}";

    // [KYA KARTA HAI]: Standard W3C `traceparent` HTTP header string ko parse karta hai.
    // [KYU ZAROORI HAI]: Incoming HTTP requests se trace context extract karna.
    // [KAISE CHALTA HAI]: Regex validate karta hai, 4 dash-separated parts split karta hai, aur all-zero invalid IDs check karta hai.
    // [OUTPUT / SIDE EFFECT]: Parsed W3CTraceContext instance.
    // [VARIATIONS TO TRY]: TryParse boolean pattern.
    public static W3CTraceContext Parse(string headerValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerValue);

        var parts = headerValue.Trim().Split('-');
        if (parts.Length != 4)
        {
            throw new FormatException($"Invalid W3C traceparent header format: '{headerValue}'. Expected 4 parts.");
        }

        string version = parts[0];
        string traceId = parts[1];
        string spanId = parts[2];
        string flags = parts[3];

        // Format validations according to W3C spec
        if (version.Length != 2 || traceId.Length != 32 || spanId.Length != 16 || flags.Length != 2)
        {
            throw new FormatException($"Invalid segment length in traceparent: '{headerValue}'");
        }

        // TraceId and SpanId cannot be all zeros
        if (traceId == "00000000000000000000000000000000" || spanId == "0000000000000000")
        {
            throw new FormatException($"TraceId or SpanId cannot be all zeros: '{headerValue}'");
        }

        return new W3CTraceContext(traceId, spanId, flags);
    }

    public override string ToString() => ToHeaderString();
}
