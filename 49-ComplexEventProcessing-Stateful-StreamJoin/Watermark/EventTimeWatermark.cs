namespace KiranaHub.StreamingCep.Watermark;

/// <summary>
/// Event-Time Watermark Generator (Apache Flink / Beam standard).
/// Tracks out-of-order event streams and advances the global event-time watermark:
/// Watermark(t) = MaxEventTimeSeen - AllowedLatenessMs.
/// Signals to downstream window operators that no more events with timestamp &lt; Watermark will arrive.
/// </summary>
public sealed class EventTimeWatermarkGenerator
{
    // [KYA KARTA HAI]: Maximum allowed out-of-order arrival delay (milliseconds) define karta hai.
    // [KYU ZAROORI HAI]: Cellular 4G/5G mobile network jitter aur rider offline buffering ko accommodate karna.
    // [KAISE CHALTA HAI]: Long integer milliseconds (e.g. 3,000 ms = 3 seconds).
    // [OUTPUT / SIDE EFFECT]: Allowed lateness bound.
    // [VARIATIONS TO TRY]: Dynamic adaptive lateness based on P99 network latency.
    public long AllowedLatenessMs { get; }

    // [KYA KARTA HAI]: Ab tak dekha gaya highest event timestamp store karta hai.
    // [KYU ZAROORI HAI]: Watermark generation ka baseline monotonic ceiling.
    // [KAISE CHALTA HAI]: Har event aane par Math.Max(MaxEventTimeSeen, event.Timestamp).
    // [OUTPUT / SIDE EFFECT]: Monotonically advancing timestamp.
    // [VARIATIONS TO TRY]: Partition-aware event time tracking.
    public long MaxEventTimeSeen { get; private set; }

    // [KYA KARTA HAI]: Current emitted watermark timestamp store karta hai.
    // [KYU ZAROORI HAI]: Windows trigger karne aur expired state garbage-collect karne ke liye boundary.
    // [KAISE CHALTA HAI]: MaxEventTimeSeen - AllowedLatenessMs.
    // [OUTPUT / SIDE EFFECT]: Current watermark timestamp.
    // [VARIATIONS TO TRY]: Periodic vs Punctuated watermark emission.
    public long CurrentWatermark { get; private set; }

    // [KYA KARTA HAI]: Total late events count track karta hai (jo watermark ke baad pahuche).
    // [KYU ZAROORI HAI]: Stream quality telemetry aur network health monitoring.
    // [KAISE CHALTA HAI]: Agar event.Timestamp < CurrentWatermark toh counter increment hota hai.
    // [OUTPUT / SIDE EFFECT]: Late events count integer.
    // [VARIATIONS TO TRY]: Side-output channel for late events.
    public long LateEventsDroppedCount { get; private set; }

    public EventTimeWatermarkGenerator(long allowedLatenessMs = 3000)
    {
        AllowedLatenessMs = allowedLatenessMs;
        MaxEventTimeSeen = long.MinValue;
        CurrentWatermark = long.MinValue;
        LateEventsDroppedCount = 0;
    }

    // [KYA KARTA HAI]: Naya incoming event timestamp ingest karta hai aur watermark advance karta hai.
    // [KYU ZAROORI HAI]: Stream time progress karna aur late events filter karna.
    // [KAISE CHALTA HAI]: Checks lateness; updates max timestamp; recomputes watermark.
    // [OUTPUT / SIDE EFFECT]: Returns true if event is timely (valid); false if dropped as late data.
    // [VARIATIONS TO TRY]: Emit WatermarkEvent onto the output stream.
    public bool IngestEvent(long eventTimestamp, out long newWatermark)
    {
        // 1. Check if event is already older than the current watermark
        if (CurrentWatermark != long.MinValue && eventTimestamp < CurrentWatermark)
        {
            LateEventsDroppedCount++;
            newWatermark = CurrentWatermark;
            return false; // Dropped: late event arriving after watermark has passed
        }

        // 2. Advance max event time seen
        if (eventTimestamp > MaxEventTimeSeen)
        {
            MaxEventTimeSeen = eventTimestamp;
        }

        // 3. Compute potential new watermark
        long candidateWatermark = MaxEventTimeSeen - AllowedLatenessMs;
        if (candidateWatermark > CurrentWatermark)
        {
            CurrentWatermark = candidateWatermark;
        }

        newWatermark = CurrentWatermark;
        return true;
    }

    public void Reset()
    {
        MaxEventTimeSeen = long.MinValue;
        CurrentWatermark = long.MinValue;
        LateEventsDroppedCount = 0;
    }
}
