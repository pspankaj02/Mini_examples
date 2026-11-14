using KiranaHub.StreamingCep.State;
using KiranaHub.StreamingCep.Watermark;

namespace KiranaHub.StreamingCep.Join;

public sealed record CustomerOrderEvent(
    string OrderId,
    string CustomerId,
    long Timestamp,
    string EventType,
    double Amount);

public sealed record RiderTelemetryEvent(
    string RiderId,
    string OrderId,
    long Timestamp,
    double Latitude,
    double Longitude,
    double DistanceToCustomerKm);

public sealed record JoinedOrderRiderEvent(
    string OrderId,
    string CustomerId,
    string RiderId,
    long OrderTimestamp,
    long RiderTimestamp,
    double DistanceKm,
    bool IsSuspiciousDelivery);

/// <summary>
/// Temporal Interval Stream Join Engine (Flink Interval Join standard).
/// Joins two unbounded asynchronous streams (Order Events &amp; Rider Telemetry) over a time window:
/// Order.Timestamp - LowerBound &lt;= Rider.Timestamp &lt;= Order.Timestamp + UpperBound.
/// </summary>
public sealed class IntervalStreamJoiner
{
    public long LowerBoundMs { get; }
    public long UpperBoundMs { get; }

    private readonly TimeSlidingStateStore<string, CustomerOrderEvent> _orderState = new();
    private readonly TimeSlidingStateStore<string, RiderTelemetryEvent> _riderState = new();
    private readonly EventTimeWatermarkGenerator _watermarkGen = new(allowedLatenessMs: 3000);

    public IntervalStreamJoiner(long lowerBoundMs = 120_000, long upperBoundMs = 120_000)
    {
        LowerBoundMs = lowerBoundMs;
        UpperBoundMs = upperBoundMs;
    }

    // [KYA KARTA HAI]: Customer order event ingest karta hai, rider store probe karta hai, aur joins emit karta hai.
    // [KYU ZAROORI HAI]: Stream A se Stream B ke temporal matches find karna.
    // [KAISE CHALTA HAI]: Order store karta hai; rider state se [t - lower, t + upper] matching telemetry nikalta hai.
    // [OUTPUT / SIDE EFFECT]: Emits JoinedOrderRiderEvent list.
    // [VARIATIONS TO TRY]: Dynamic interval bounds per order priority.
    public List<JoinedOrderRiderEvent> IngestOrder(CustomerOrderEvent order)
    {
        var joins = new List<JoinedOrderRiderEvent>();

        // Advance watermark
        _watermarkGen.IngestEvent(order.Timestamp, out long currentWatermark);

        // Store order in sliding state
        _orderState.Put(order.OrderId, order.Timestamp, order);

        // Probe rider state for existing telemetry within temporal interval
        long minRiderTime = order.Timestamp - LowerBoundMs;
        long maxRiderTime = order.Timestamp + UpperBoundMs;

        var matchingRiders = _riderState.Get(order.OrderId);
        foreach (var (rTime, rider) in matchingRiders)
        {
            if (rTime >= minRiderTime && rTime <= maxRiderTime)
            {
                bool isSuspicious = order.EventType == "QrScanDelivered" && rider.DistanceToCustomerKm > 1.0;
                joins.Add(new JoinedOrderRiderEvent(
                    order.OrderId, order.CustomerId, rider.RiderId,
                    order.Timestamp, rTime, rider.DistanceToCustomerKm, isSuspicious));
            }
        }

        // Periodic state eviction past trailing watermark
        if (currentWatermark != long.MinValue)
        {
            _orderState.EvictOlderThan(currentWatermark - UpperBoundMs);
            _riderState.EvictOlderThan(currentWatermark - LowerBoundMs);
        }

        return joins;
    }

    // [KYA KARTA HAI]: Rider telemetry event ingest karta hai, order store probe karta hai, aur joins emit karta hai.
    // [KYU ZAROORI HAI]: Stream B se Stream A ke temporal matches find karna.
    // [KAISE CHALTA HAI]: Rider store karta hai; order state se [t - upper, t + lower] matching orders nikalta hai.
    // [OUTPUT / SIDE EFFECT]: Emits JoinedOrderRiderEvent list.
    // [VARIATIONS TO TRY]: Spatial geofence filtering before temporal join.
    public List<JoinedOrderRiderEvent> IngestRider(RiderTelemetryEvent rider)
    {
        var joins = new List<JoinedOrderRiderEvent>();

        _watermarkGen.IngestEvent(rider.Timestamp, out long currentWatermark);
        _riderState.Put(rider.OrderId, rider.Timestamp, rider);

        long minOrderTime = rider.Timestamp - UpperBoundMs;
        long maxOrderTime = rider.Timestamp + LowerBoundMs;

        var matchingOrders = _orderState.Get(rider.OrderId);
        foreach (var (oTime, order) in matchingOrders)
        {
            if (oTime >= minOrderTime && oTime <= maxOrderTime)
            {
                bool isSuspicious = order.EventType == "QrScanDelivered" && rider.DistanceToCustomerKm > 1.0;
                joins.Add(new JoinedOrderRiderEvent(
                    order.OrderId, order.CustomerId, rider.RiderId,
                    oTime, rider.Timestamp, rider.DistanceToCustomerKm, isSuspicious));
            }
        }

        if (currentWatermark != long.MinValue)
        {
            _orderState.EvictOlderThan(currentWatermark - UpperBoundMs);
            _riderState.EvictOlderThan(currentWatermark - LowerBoundMs);
        }

        return joins;
    }

    public int OrderStateCount => _orderState.TotalEventsCount;
    public int RiderStateCount => _riderState.TotalEventsCount;
    public long CurrentWatermark => _watermarkGen.CurrentWatermark;
}
