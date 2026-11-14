// ==============================================================================================
// KiranaHub Stream Processing: Sliding Window Real-Time Analytics & Surge Pricing Engine
// [KYA KARTA HAI] Real-time streaming events par sliding time window (e.g. 60s) maintain karke order velocity aur dynamic surge multiplier compute karta hai.
// [KYU ZAROORI HAI] 10-minute delivery dark-store par achanak rush aane par rider delivery fee aur demand automatically balance karna.
// [KAISE CHALTA HAI] Timestamped events ko ring/list buffer me rakhta hai, expired events prune karta hai aur statistical rates compute karta hai.
// [OUTPUT / SIDE EFFECT] Real-time sliding metrics and calculated surge multipliers (1.0x, 1.25x, 1.5x).
// [VARIATIONS TO TRY] Tumbling window or session window aggregation.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] DateTimeOffset, TimeSpan, Math types ke liye.
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

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] ConcurrentDictionary for per-dark-store sliding window isolation.
// [KAISE CHALTA HAI] Multi-threaded dictionary loaded.
// [OUTPUT / SIDE EFFECT] Thread-safe per-store windows.
// [VARIATIONS TO TRY] Partitioned arrays.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Where timestamp filtering queries.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators available.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Namespace declare karta hai windowing stream processing ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.KafkaStreaming.Windowing.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Streaming.Analytics.
namespace KiranaHub.KafkaStreaming.Windowing;

// [KYA KARTA HAI] Windowed event data point record.
// [KYU ZAROORI HAI] Timestamp aur value encapsulate karna.
// [KAISE CHALTA HAI] Immutable record with Timestamp and Value.
// [OUTPUT / SIDE EFFECT] Timestamped point.
// [VARIATIONS TO TRY] Generic payload.
public record WindowEvent(DateTimeOffset Timestamp, decimal Amount);

// [KYA KARTA HAI] Window metric summary result record.
// [KYU ZAROORI HAI] Computed sliding metrics encapsulate karna.
// [KAISE CHALTA HAI] Output DTO with EventCount, VelocityPerMinute, TotalRevenue, SurgeMultiplier.
// [OUTPUT / SIDE EFFECT] Window analysis summary.
// [VARIATIONS TO TRY] Percentile revenue values.
public record SlidingWindowMetrics(
    string EntityKey,
    int EventCount,
    double VelocityPerMinute,
    decimal TotalAmount,
    double SurgeMultiplier
);

// [KYA KARTA HAI] Real-time Sliding Window Aggregator class.
// [KYU ZAROORI HAI] Continuous stream events par sliding duration metrics calculate karna.
// [KAISE CHALTA HAI] Thread-safe buffers per key with automatic eviction of expired events outside windowDuration.
// [OUTPUT / SIDE EFFECT] High performance sliding window processor.
// [VARIATIONS TO TRY] Apache Flink / Kafka Streams RocksDB state store.
public class SlidingWindowAggregator
{
    // [KYA KARTA HAI] Window sliding duration (e.g. 60 seconds).
    // [KYU ZAROORI HAI] Kitni der ka time horizon evaluate karna hai.
    // [KAISE CHALTA HAI] TimeSpan property.
    // [OUTPUT / SIDE EFFECT] Window length defined.
    // [VARIATIONS TO TRY] 5 minutes for macro trends.
    public TimeSpan WindowDuration { get; }

    // [KYA KARTA HAI] Per-entity event timestamp lists: EntityKey (DarkStoreId) -> List of WindowEvent.
    // [KYU ZAROORI HAI] Har dark store ki demand velocity independently calculate karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary maps store key to event list.
    // [OUTPUT / SIDE EFFECT] Isolated per-store buffers.
    // [VARIATIONS TO TRY] Memory-bounded ring buffers.
    private readonly ConcurrentDictionary<string, List<WindowEvent>> _windows = new();

    // [KYA KARTA HAI] Per-entity synchronization locks.
    // [KYU ZAROORI HAI] Thread-safe eviction and append within an entity window.
    // [KAISE CHALTA HAI] Maps key to dedicated lock object.
    // [OUTPUT / SIDE EFFECT] Lock registry.
    // [VARIATIONS TO TRY] ReaderWriterLockSlim.
    private readonly ConcurrentDictionary<string, object> _locks = new();

    // [KYA KARTA HAI] Aggregator constructor initialize karta hai.
    // [KYU ZAROORI HAI] Window duration configure karna (default 60s).
    // [KAISE CHALTA HAI] Assigns WindowDuration.
    // [OUTPUT / SIDE EFFECT] Aggregator instance ready.
    // [VARIATIONS TO TRY] Configurable via appsettings.
    public SlidingWindowAggregator(TimeSpan? windowDuration = null)
    {
        WindowDuration = windowDuration ?? TimeSpan.FromSeconds(60);
    }

    // [KYA KARTA HAI] Naya event record karta hai entity ke window me.
    // [KYU ZAROORI HAI] Stream se incoming checkout events ingest karna.
    // [KAISE CHALTA HAI] Appends event and prunes expired events outside WindowDuration.
    // [OUTPUT / SIDE EFFECT] Window updated.
    // [VARIATIONS TO TRY] Bulk ingest.
    public void RecordEvent(string entityKey, decimal amount, DateTimeOffset? timestamp = null)
    {
        var eventTime = timestamp ?? DateTimeOffset.UtcNow;
        var lockObj = _locks.GetOrAdd(entityKey, _ => new object());

        lock (lockObj)
        {
            var list = _windows.GetOrAdd(entityKey, _ => new List<WindowEvent>());
            list.Add(new WindowEvent(eventTime, amount));

            // [KYA KARTA HAI] Expired events prune karta hai.
            // [KYU ZAROORI HAI] WindowDuration se purane events memory se delete karke memory leak prevent karna.
            // [KAISE CHALTA HAI] Cutoff = eventTime - WindowDuration; removes all before cutoff.
            // [OUTPUT / SIDE EFFECT] Memory bounded to sliding window.
            // [VARIATIONS TO TRY] Periodic background cleanup timer.
            var cutoff = eventTime - WindowDuration;
            list.RemoveAll(e => e.Timestamp < cutoff);
        }
    }

    // [KYA KARTA HAI] Current sliding window metrics aur surge multiplier compute karta hai.
    // [KYU ZAROORI HAI] Real-time dynamic pricing aur warehouse workload visibility ke liye.
    // [KAISE CHALTA HAI] Prunes expired events as of now, calculates rate per minute and surge multiplier.
    // [OUTPUT / SIDE EFFECT] SlidingWindowMetrics snapshot.
    // [VARIATIONS TO TRY] Include moving average latency.
    public SlidingWindowMetrics GetMetrics(string entityKey, DateTimeOffset? asOfTime = null)
    {
        var now = asOfTime ?? DateTimeOffset.UtcNow;
        var lockObj = _locks.GetOrAdd(entityKey, _ => new object());

        lock (lockObj)
        {
            var list = _windows.GetOrAdd(entityKey, _ => new List<WindowEvent>());
            var cutoff = now - WindowDuration;
            list.RemoveAll(e => e.Timestamp < cutoff);

            var count = list.Count;
            var totalAmount = list.Sum(e => e.Amount);

            // [KYA KARTA HAI] Rate per minute calculate karta hai.
            // [KYU ZAROORI HAI] Velocity rate normalize karna.
            // [KAISE CHALTA HAI] Count divided by (WindowDuration in minutes).
            // [OUTPUT / SIDE EFFECT] Velocity scalar.
            // [VARIATIONS TO TRY] Exponential moving average (EMA).
            var windowMinutes = Math.Max(0.1, WindowDuration.TotalMinutes);
            var velocityPerMinute = count / windowMinutes;

            // [KYA KARTA HAI] Dynamic Surge Multiplier calculate karta hai.
            // [KYU ZAROORI HAI] Demand surge ke mutabiq pricing adjust karna:
            //   - <= 5 orders/min -> 1.0x (Normal)
            //   - 6 to 15 orders/min -> 1.25x (Moderate Surge)
            //   - > 15 orders/min -> 1.50x (High Peak Surge)
            // [KAISE CHALTA HAI] Threshold conditional evaluation.
            // [OUTPUT / SIDE EFFECT] Multiplier value.
            // [VARIATIONS TO TRY] Continuous logarithmic curve.
            double surgeMultiplier = 1.0;
            if (velocityPerMinute > 15)
            {
                surgeMultiplier = 1.50;
            }
            else if (velocityPerMinute > 5)
            {
                surgeMultiplier = 1.25;
            }

            return new SlidingWindowMetrics(
                EntityKey: entityKey,
                EventCount: count,
                VelocityPerMinute: Math.Round(velocityPerMinute, 1),
                TotalAmount: totalAmount,
                SurgeMultiplier: surgeMultiplier
            );
        }
    }
}
