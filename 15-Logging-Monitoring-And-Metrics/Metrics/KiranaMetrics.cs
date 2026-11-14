using System.Diagnostics.Metrics;

namespace KiranaHub.LoggingAndMetrics.Metrics;

// =========================================================================================
// [TOPIC]: System.Diagnostics.Metrics (OpenTelemetry Metrics Standard)
// [PURPOSE]: Real-time quantitative time-series telemetry (Counters, Histograms, Gauges).
// =========================================================================================

// [KYA KARTA HAI]: KiranaHub metrics instrumentation facade.
public static class KiranaMetrics
{
    // [KYA KARTA HAI]: Meter namespace identifier.
    public const string MeterName = "KiranaHub.Monitoring";

    // [KYA KARTA HAI]: Application Meter instance create karta hai.
    public static readonly Meter Instance = new(MeterName, "1.0.0");

    // [KYA KARTA HAI]: Monotonically increasing counter for total placed orders.
    // [KYU ZAROORI HAI]: Throughput / rate calculation (Orders placed per minute).
    public static readonly Counter<long> OrdersPlacedCounter =
        Instance.CreateCounter<long>(
            name: "kiranahub.orders.placed",
            unit: "{orders}",
            description: "Total number of grocery orders placed across all dark stores."
        );

    // [KYA KARTA HAI]: Statistical distribution histogram for processing duration in milliseconds.
    // [KYU ZAROORI HAI]: Percentile latency calculations (p50, p95, p99 order fulfillment time).
    public static readonly Histogram<double> OrderDurationHistogram =
        Instance.CreateHistogram<double>(
            name: "kiranahub.order.duration_ms",
            unit: "ms",
            description: "Duration taken to process and pack grocery order."
        );

    // [KYA KARTA HAI]: Observable current state variable.
    public static int CurrentActiveRiders { get; set; } = 15;

    // [KYA KARTA HAI]: Observable Gauge jo scraper poll hone par dynamic current rider count return karta hai.
    // [KYU ZAROORI HAI]: Instantaneous snapshot tracking (up and down moving value).
    public static readonly ObservableGauge<int> ActiveRidersGauge =
        Instance.CreateObservableGauge<int>(
            name: "kiranahub.riders.active",
            observeValue: () => CurrentActiveRiders,
            unit: "{riders}",
            description: "Current number of delivery riders actively in transit."
        );

    // [KYA KARTA HAI]: Order placement metric record karne ka helper method.
    public static void RecordOrder(string category, decimal amount, double durationMs)
    {
        // [KYA KARTA HAI]: Counter increment karta hai with dimensional tags (category, currency).
        OrdersPlacedCounter.Add(1, new KeyValuePair<string, object?>("category", category));

        // [KYA KARTA HAI]: Histogram record karta hai duration ms with category tag.
        OrderDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("category", category));
    }
}

