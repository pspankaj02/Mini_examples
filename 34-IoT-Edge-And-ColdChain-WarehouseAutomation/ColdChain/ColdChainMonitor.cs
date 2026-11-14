namespace KiranaHub.WarehouseIoT.ColdChain;

using System.Collections.Concurrent;
using System.Text.Json;
using KiranaHub.WarehouseIoT.Mqtt;

// [KYA KARTA HAI]: Dark-Store perishable grocery inventory ke liye real-time Cold-Chain Safety Monitor implement karta hai.
// [KYU ZAROORI HAI]: Doodh, dahi, ice-cream, aur paneer ke freezers ka temperature threshold monitor karna taaki spoilage aur food safety violations prevent hon.
// [KAISE CHALTA HAI]: MQTT broker se sensor packets consume karta hai, sliding window me consecutive anomaly count track karta hai (3 consecutive breaches required to eliminate sensor noise), aur emergency alert raise karta hai.
// [OUTPUT / SIDE EFFECT]: Food safety alerts, automated compressor fault notifications.
// [VARIATIONS TO TRY]: Humidity aur door open/close duration sensors attach karein.
public class ColdChainMonitor
{
    // [KYA KARTA HAI]: Per-zone consecutive violation counters store karta hai.
    // [KYU ZAROORI HAI]: Single sensor electrical spike ko ignore karna aur sirf sustained temperature failure par alert bajana.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, int> (ZoneKey -> ConsecutiveBreachCount).
    // [OUTPUT / SIDE EFFECT]: Noise suppression state.
    // [VARIATIONS TO TRY]: Time-weighted exponentially decaying anomaly score.
    private readonly ConcurrentDictionary<string, int> _consecutiveBreaches = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Raised emergency alerts list store karta hai.
    // [KYU ZAROORI HAI]: Audit log aur automated incident response tickets create karna.
    // [KAISE CHALTA HAI]: ConcurrentBag<ColdChainAlert>.
    // [OUTPUT / SIDE EFFECT]: List of active alerts.
    // [VARIATIONS TO TRY]: PagerDuty API integration.
    private readonly ConcurrentBag<ColdChainAlert> _alerts = new();

    // [KYA KARTA HAI]: Raised alerts count return karta hai.
    // [KYU ZAROORI HAI]: Test assertion aur monitoring dashboard.
    // [KAISE CHALTA HAI]: _alerts.Count property.
    // [OUTPUT / SIDE EFFECT]: Integer count.
    // [VARIATIONS TO TRY]: Active vs Resolved alerts filter.
    public int ActiveAlertCount => _alerts.Count;

    // [KYA KARTA HAI]: Sabhi alerts ka read-only snapshot return karta hai.
    // [KYU ZAROORI HAI]: Inspection.
    // [KAISE CHALTA HAI]: _alerts.ToList().
    // [OUTPUT / SIDE EFFECT]: List of alerts.
    // [VARIATIONS TO TRY]: Filter by severity.
    public IReadOnlyList<ColdChainAlert> GetAlerts() => _alerts.ToList().AsReadOnly();

    // [KYA KARTA HAI]: MQTT broker par cold-chain telemetry topics subscribe karta hai.
    // [KYU ZAROORI HAI]: Dark-store freezers se aane wale telemetry packets process karna.
    // [KAISE CHALTA HAI]: broker.Subscribe("kirana/edge/+/coldchain/+/telemetry", ProcessTelemetryAsync).
    // [OUTPUT / SIDE EFFECT]: Automatic packet pipeline established.
    // [VARIATIONS TO TRY]: Per-store localized topic filters.
    public void AttachToBroker(MqttEdgeBroker broker)
    {
        // [KYA KARTA HAI]: Argument null check.
        // [KYU ZAROORI HAI]: Broker instance required.
        // [KAISE CHALTA HAI]: ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if null.
        // [VARIATIONS TO TRY]: Reconnect retry loop.
        ArgumentNullException.ThrowIfNull(broker);

        // [KYA KARTA HAI]: Wildcard topic subscribe karta hai across all stores and zones.
        // [KYU ZAROORI HAI]: Universal telemetry listener.
        // [KAISE CHALTA HAI]: broker.Subscribe with delegate.
        // [OUTPUT / SIDE EFFECT]: Subscription active.
        // [VARIATIONS TO TRY]: Granular zone subscriptions.
        broker.Subscribe("kirana/edge/+/coldchain/+/telemetry", ProcessTelemetryAsync);
    }

    // [KYA KARTA HAI]: Incoming MQTT telemetry packet process karta hai.
    // [KYU ZAROORI HAI]: JSON payload parse karna aur temperature bounds check karna.
    // [KAISE CHALTA HAI]: JSON deserialize karta hai, zone thresholds evaluate karta hai, aur consecutive counter maintain karta hai.
    // [OUTPUT / SIDE EFFECT]: Updates anomaly counter, raises alert if threshold breached 3 times consecutively.
    // [VARIATIONS TO TRY]: Machine learning based predictive failure forecast.
    public Task ProcessTelemetryAsync(MqttMessage message)
    {
        // [KYA KARTA HAI]: JSON payload deserialize karta hai into TelemetryPacket record.
        // [KYU ZAROORI HAI]: Strongly-typed sensor data extract karna.
        // [KAISE CHALTA HAI]: JsonSerializer.Deserialize<TelemetryPacket>.
        // [OUTPUT / SIDE EFFECT]: Packet instance.
        // [VARIATIONS TO TRY]: Fast UTF8 span reader for zero allocation.
        var packet = JsonSerializer.Deserialize<TelemetryPacket>(message.Payload);
        if (packet == null)
        {
            return Task.CompletedTask;
        }

        // [KYA KARTA HAI]: Zone key construct karta hai (StoreId:Zone:SensorId).
        // [KYU ZAROORI HAI]: Unique per-sensor sliding window identification.
        // [KAISE CHALTA HAI]: $"{packet.StoreId}:{packet.Zone}:{packet.SensorId}".
        // [OUTPUT / SIDE EFFECT]: Unique key string.
        // [VARIATIONS TO TRY]: Hash code key.
        var zoneKey = $"{packet.StoreId}:{packet.Zone}:{packet.SensorId}";

        // [KYA KARTA HAI]: Check karta hai kya current temperature zone limits se bahar hai.
        // [KYU ZAROORI HAI]: Zone-specific food safety policy evaluate karna.
        // [KAISE CHALTA HAI]: IsTemperatureBreached(packet.Zone, packet.TemperatureCelsius).
        // [OUTPUT / SIDE EFFECT]: Boolean breach indicator.
        // [VARIATIONS TO TRY]: Configurable threshold database.
        bool isBreached = IsTemperatureBreached(packet.Zone, packet.TemperatureCelsius, out var safeMin, out var safeMax);

        if (isBreached)
        {
            // [KYA KARTA HAI]: Consecutive breaches counter increment karta hai.
            // [KYU ZAROORI HAI]: Track sustained temperature failure.
            // [KAISE CHALTA HAI]: AddOrUpdate(zoneKey, 1, (_, count) => count + 1).
            // [OUTPUT / SIDE EFFECT]: Updated breach count.
            // [VARIATIONS TO TRY]: Exponential sliding window.
            int breachCount = _consecutiveBreaches.AddOrUpdate(zoneKey, 1, (_, count) => count + 1);

            // [KYA KARTA HAI]: Check karta hai kya consecutive breaches threshold (3) reach ho gaya.
            // [KYU ZAROORI HAI]: Sensor noise suppression: 1 ya 2 anomalous spikes ignore karna, lekin 3 par emergency alert raise karna.
            // [KAISE CHALTA HAI]: if (breachCount == 3) raise alert.
            // [OUTPUT / SIDE EFFECT]: Emergency ColdChainAlert created and added to _alerts.
            // [VARIATIONS TO TRY]: SMS / PagerDuty webhook dispatch.
            if (breachCount >= 3)
            {
                var alert = new ColdChainAlert(
                    StoreId: packet.StoreId,
                    Zone: packet.Zone,
                    SensorId: packet.SensorId,
                    CurrentTemperature: packet.TemperatureCelsius,
                    SafeMinTemperature: safeMin,
                    SafeMaxTemperature: safeMax,
                    ConsecutiveBreaches: breachCount,
                    AlertUtc: DateTime.UtcNow,
                    Severity: breachCount >= 5 ? AlertSeverity.Critical : AlertSeverity.Warning);

                _alerts.Add(alert);
            }
        }
        else
        {
            // [KYA KARTA HAI]: Consecutive breach counter ko 0 reset karta hai.
            // [KYU ZAROORI HAI]: Agar temperature wapas safe range me aa gaya, toh purana anomaly sequence cancel ho jata hai.
            // [KAISE CHALTA HAI]: _consecutiveBreaches[zoneKey] = 0.
            // [OUTPUT / SIDE EFFECT]: Counter reset.
            // [VARIATIONS TO TRY]: Record recovery event.
            _consecutiveBreaches[zoneKey] = 0;
        }

        return Task.CompletedTask;
    }

    // [KYA KARTA HAI]: Zone type ke mutabiq safe temperature bounds determine karta hai.
    // [KYU ZAROORI HAI]: Dairy ke liye +2C se +6C safe hai, jabki Ice-Cream ke liye <= -18C safe hai.
    // [KAISE CHALTA HAI]: Switch expression on zone name.
    // [OUTPUT / SIDE EFFECT]: True if breached, along with min and max bounds.
    // [VARIATIONS TO TRY]: Dynamic seasonal thresholds.
    public static bool IsTemperatureBreached(string zone, double temp, out double safeMin, out double safeMax)
    {
        switch (zone.ToUpperInvariant())
        {
            case "DAIRY":
            case "CHILLED":
            case "VEGETABLES":
                // Dairy & fresh produce must stay between +2°C and +6°C
                safeMin = 2.0;
                safeMax = 6.0;
                return temp < safeMin || temp > safeMax;

            case "DEEP_FREEZE":
            case "ICE_CREAM":
            case "MEAT":
                // Frozen items must stay at or below -18°C
                safeMin = -30.0;
                safeMax = -18.0;
                return temp > safeMax;

            default:
                // Ambient storage: +15°C to +25°C
                safeMin = 15.0;
                safeMax = 25.0;
                return temp < safeMin || temp > safeMax;
        }
    }
}

// [KYA KARTA HAI]: Sensor telemetry JSON packet record.
// [KYU ZAROORI HAI]: Serialization contract.
// [KAISE CHALTA HAI]: Record definition.
// [OUTPUT / SIDE EFFECT]: DTO.
// [VARIATIONS TO TRY]: Battery level field.
public record TelemetryPacket(
    string StoreId,
    string Zone,
    string SensorId,
    double TemperatureCelsius,
    double HumidityPct,
    DateTime TimestampUtc);

// [KYA KARTA HAI]: Cold chain threshold breach alert record.
// [KYU ZAROORI HAI]: Incident notification encapsulation.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Alert representation.
// [VARIATIONS TO TRY]: Actionable recommendations enum.
public record ColdChainAlert(
    string StoreId,
    string Zone,
    string SensorId,
    double CurrentTemperature,
    double SafeMinTemperature,
    double SafeMaxTemperature,
    int ConsecutiveBreaches,
    DateTime AlertUtc,
    AlertSeverity Severity);

// [KYA KARTA HAI]: Alert severity classification enum.
// [KYU ZAROORI HAI]: Pager priority routing.
// [KAISE CHALTA HAI]: Warning vs Critical.
// [OUTPUT / SIDE EFFECT]: Severity indicator.
// [VARIATIONS TO TRY]: Info, Warning, Error, Fatal.
public enum AlertSeverity
{
    Warning,
    Critical
}
