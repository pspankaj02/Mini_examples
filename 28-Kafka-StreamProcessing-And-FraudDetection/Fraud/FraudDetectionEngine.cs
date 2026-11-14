// ==============================================================================================
// KiranaHub Stream Processing: Real-Time Fraud & Anomaly Detection Engine
// [KYA KARTA HAI] Real-time streaming checkout events ko velocity spikes, impossible geo travel, aur extreme value anomalies ke against evaluate karta hai.
// [KYU ZAROORI HAI] Stolen credit cards, bot checkout attacks, aur account takeovers ko order dispatch hone se pehle block karna.
// [KAISE CHALTA HAI] Historical rolling state aur sliding time windows maintain karke multi-rule heuristics run karta hai.
// [OUTPUT / SIDE EFFECT] FraudAssessment result with RiskLevel (Low, Medium, Critical) and ShouldBlock flag.
// [VARIATIONS TO TRY] ML model inference using ONNX Runtime for gradient-boosted tree scoring.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Math, DateTimeOffset, List types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, IReadOnlyList provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] Per-user / per-device velocity and history state thread-safely maintain karna.
// [KAISE CHALTA HAI] ConcurrentDictionary lock-free operations provide karta hai.
// [OUTPUT / SIDE EFFECT] Multi-threaded fraud state tables.
// [VARIATIONS TO TRY] Redis sliding sorted sets.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sorting and history filtering.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Namespace declare karta hai fraud detection ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.KafkaStreaming.Fraud.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Security.Fraud.
namespace KiranaHub.KafkaStreaming.Fraud;

// [KYA KARTA HAI] Checkout transaction stream event payload record.
// [KYU ZAROORI HAI] Evaluator ko transaction parameters provide karna.
// [KAISE CHALTA HAI] Immutable record with OrderId, CustomerId, CardFingerprint, Amount, Lat, Lon, Timestamp.
// [OUTPUT / SIDE EFFECT] Input transaction event.
// [VARIATIONS TO TRY] Add IP address and user-agent string.
public record CheckoutEvent(
    string OrderId,
    string CustomerId,
    string CardFingerprint,
    decimal Amount,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);

// [KYA KARTA HAI] Fraud assessment evaluation result record.
// [KYU ZAROORI HAI] Fraud detection outcome encapsulate karna.
// [KAISE CHALTA HAI] Output DTO with IsSuspicious, Violations list, RiskLevel, ShouldBlock.
// [OUTPUT / SIDE EFFECT] Fraud decision record.
// [VARIATIONS TO TRY] Include numeric risk score 0..100.
public record FraudAssessment(
    bool IsSuspicious,
    IReadOnlyList<string> Violations,
    string RiskLevel,
    bool ShouldBlock
);

// [KYA KARTA HAI] Real-Time Streaming Fraud Detection Engine class.
// [KYU ZAROORI HAI] Sub-5-millisecond inline fraud verification provide karna.
// [KAISE CHALTA HAI] Card velocity, Haversine geo travel speed, and historical amount checks maintain karta hai.
// [OUTPUT / SIDE EFFECT] Fraud detection engine instance.
// [VARIATIONS TO TRY] External rules engine (Drools).
public class FraudDetectionEngine
{
    // [KYA KARTA HAI] Card velocity tracking map: CardFingerprint -> List of order timestamps.
    // [KYU ZAROORI HAI] Rapid successive checkout attempts detect karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary storing timestamps.
    // [OUTPUT / SIDE EFFECT] Card velocity memory.
    // [VARIATIONS TO TRY] Redis TTL keys.
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _cardVelocityMap = new();

    // [KYA KARTA HAI] User last location map: CustomerId -> (Lat, Lon, Timestamp).
    // [KYU ZAROORI HAI] Impossible geographical travel speed compute karna.
    // [KAISE CHALTA HAI] Stores last known geo point and time.
    // [OUTPUT / SIDE EFFECT] Geo history table.
    // [VARIATIONS TO TRY] GeoIP lookup.
    private readonly ConcurrentDictionary<string, (double Lat, double Lon, DateTimeOffset Time)> _userGeoMap = new();

    // [KYA KARTA HAI] User historical order amounts: CustomerId -> List of past amounts.
    // [KYU ZAROORI HAI] Extreme order value anomalies (> 10x median) detect karna.
    // [KAISE CHALTA HAI] Stores historical order values.
    // [OUTPUT / SIDE EFFECT] Spending baseline memory.
    // [VARIATIONS TO TRY] Rolling exponentially weighted moving average.
    private readonly ConcurrentDictionary<string, List<decimal>> _userAmountHistory = new();

    // [KYA KARTA HAI] Global synchronization lock for state updates.
    // [KYU ZAROORI HAI] Concurrency race conditions guard karna.
    // [KAISE CHALTA HAI] Standard lock object.
    // [OUTPUT / SIDE EFFECT] Synchronization handle.
    // [VARIATIONS TO TRY] Fine-grained per-user lock.
    private readonly object _syncLock = new();

    // [KYA KARTA HAI] Incoming checkout event ko real-time rules ke against evaluate karta hai.
    // [KYU ZAROORI HAI] Fraud detection decisions produce karna before payment authorization.
    // [KAISE CHALTA HAI] Runs Rule 1 (Velocity), Rule 2 (Geo Speed), and Rule 3 (Value Spike).
    // [OUTPUT / SIDE EFFECT] FraudAssessment decision.
    // [VARIATIONS TO TRY] Parallel rule execution with Task.WhenAll.
    public FraudAssessment Evaluate(CheckoutEvent evt)
    {
        var violations = new List<string>();

        lock (_syncLock)
        {
            // -------------------------------------------------------------
            // Rule 1: Card/Device Velocity Anomaly
            // >= 3 order attempts within 10 seconds from the same card/device.
            // -------------------------------------------------------------
            var cardTimes = _cardVelocityMap.GetOrAdd(evt.CardFingerprint, _ => new List<DateTimeOffset>());
            cardTimes.Add(evt.Timestamp);
            var velocityCutoff = evt.Timestamp.AddSeconds(-10);
            cardTimes.RemoveAll(t => t < velocityCutoff);

            if (cardTimes.Count >= 3)
            {
                violations.Add($"[FRAUD_VELOCITY_SPIKE] Card/Device '{evt.CardFingerprint}' triggered {cardTimes.Count} checkouts within 10 seconds!");
            }

            // -------------------------------------------------------------
            // Rule 2: Impossible Geographical Travel Speed
            // Speed > 800 km/h between two successive orders of the same user.
            // -------------------------------------------------------------
            if (_userGeoMap.TryGetValue(evt.CustomerId, out var lastGeo))
            {
                var distanceKm = CalculateHaversineDistance(lastGeo.Lat, lastGeo.Lon, evt.Latitude, evt.Longitude);
                var timeDiffHours = (evt.Timestamp - lastGeo.Time).TotalHours;

                if (timeDiffHours > 0 && distanceKm > 50)
                {
                    var speedKmh = distanceKm / timeDiffHours;
                    // Commercial airplane max speed is ~900 km/h; impossible speed threshold 800 km/h
                    if (speedKmh > 800)
                    {
                        violations.Add($"[FRAUD_IMPOSSIBLE_TRAVEL] User '{evt.CustomerId}' moved {distanceKm:F1} km in {(timeDiffHours * 60):F1} minutes (Implied Speed: {speedKmh:F0} km/h > 800 km/h limit)!");
                    }
                }
            }
            // Update last known location
            _userGeoMap[evt.CustomerId] = (evt.Latitude, evt.Longitude, evt.Timestamp);

            // -------------------------------------------------------------
            // Rule 3: Extreme Value Anomaly
            // Order amount > 10x of user's historical rolling median.
            // -------------------------------------------------------------
            var history = _userAmountHistory.GetOrAdd(evt.CustomerId, _ => new List<decimal>());
            if (history.Count >= 3)
            {
                var sorted = history.OrderBy(x => x).ToList();
                var median = sorted[sorted.Count / 2];

                if (median > 0 && evt.Amount > (median * 10))
                {
                    violations.Add($"[FRAUD_VALUE_ANOMALY] Order amount ₹{evt.Amount} is > 10x user's historical median of ₹{median}!");
                }
            }
            history.Add(evt.Amount);
        }

        // Determine Risk Level & Block Decision
        var isSuspicious = violations.Count > 0;
        var shouldBlock = violations.Any(v => v.Contains("FRAUD_VELOCITY_SPIKE") || v.Contains("FRAUD_IMPOSSIBLE_TRAVEL"));
        var riskLevel = violations.Count >= 2 || shouldBlock ? "CRITICAL" : (isSuspicious ? "HIGH" : "LOW");

        return new FraudAssessment(
            IsSuspicious: isSuspicious,
            Violations: violations.AsReadOnly(),
            RiskLevel: riskLevel,
            ShouldBlock: shouldBlock
        );
    }

    // [KYA KARTA HAI] Do geo coordinates ke beech Haversine surface distance calculate karta hai.
    // [KYU ZAROORI HAI] Great-circle distance accurately measure karna in kilometers.
    // [KAISE CHALTA HAI] Spherical trigonometry math: 2 * R * asin(sqrt(...)).
    // [OUTPUT / SIDE EFFECT] Distance in kilometers.
    // [VARIATIONS TO TRY] Fast Euclidean approximation for nearby points.
    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    // [KYA KARTA HAI] Degree angles ko Radians me convert karta hai.
    // [KYU ZAROORI HAI] Math.Sin and Math.Cos radians accept karte hain.
    // [KAISE CHALTA HAI] degrees * (PI / 180).
    // [OUTPUT / SIDE EFFECT] Radians value.
    // [VARIATIONS TO TRY] None.
    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}
