namespace KiranaHub.VirtualActors.Grains;

using KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: Quick-Commerce delivery rider ke virtual actor interface ko define karta hai.
// [KYU ZAROORI HAI]: Har live rider (50,000+ active riders) ke liye ek in-memory isolated actor grain hota hai jo GPS, battery aur assignments track karta hai.
// [KAISE CHALTA HAI]: Silo client proxy is interface ke methods invoke karti hai, jo background me grain instance tak route hote hain.
// [OUTPUT / SIDE EFFECT]: Strongly-typed actor contract for delivery rider operations.
// [VARIATIONS TO TRY]: Speed violation alerts ya geofencing breach callbacks add kar sakte hain.
public interface IRiderGrain : IVirtualGrain
{
    // [KYA KARTA HAI]: Rider ki GPS coordinates, speed, aur battery percentage update karta hai.
    // [KYU ZAROORI HAI]: Real-time telemetry tracking ke liye har second incoming telemetry packets process karna hota hai.
    // [KAISE CHALTA HAI]: Actor turn queue me serialize hokar in-memory state update karta hai.
    // [OUTPUT / SIDE EFFECT]: Rider state updated, telemetry history appended.
    // [VARIATIONS TO TRY]: Accelerometer crash detection telemetry shamil karein.
    Task UpdateLocationAsync(double latitude, double longitude, double speedKmh, int batteryPct);

    // [KYA KARTA HAI]: Rider ko specific order delivery assign karta hai.
    // [KYU ZAROORI HAI]: Rider availability mark karna aur double-dispatch prevention ensure karna zaroori hai.
    // [KAISE CHALTA HAI]: In-memory active order ID set karta hai aur IsAvailable ko false karta hai.
    // [OUTPUT / SIDE EFFECT]: Rider status busy ho jata hai.
    // [VARIATIONS TO TRY]: Batch delivery assignments (multiple orders in one route) support karein.
    Task AssignOrderAsync(string orderId);

    // [KYA KARTA HAI]: Active order delivery complete mark karta hai.
    // [KYU ZAROORI HAI]: Rider ko wapas available pool me dalna aur dark-store return status update karna zaroori hai.
    // [KAISE CHALTA HAI]: ActiveOrderId clear karta hai aur IsAvailable ko true set karta hai.
    // [OUTPUT / SIDE EFFECT]: Rider available for new orders.
    // [VARIATIONS TO TRY]: Delivery rating aur cash-on-delivery reconciliation step add karein.
    Task CompleteOrderAsync();

    // [KYA KARTA HAI]: Current rider telemetry aur state snapshot return karta hai.
    // [KYU ZAROORI HAI]: Dispatcher UI ya customer order tracking screen ko latest state dikhane ke liye.
    // [KAISE CHALTA HAI]: Immutable snapshot record return karta hai.
    // [OUTPUT / SIDE EFFECT]: RiderState snapshot.
    // [VARIATIONS TO TRY]: Cached read projection se return kar sakte hain.
    Task<RiderState> GetStateAsync();
}

// [KYA KARTA HAI]: Rider ki complete telemetry aur status snapshot represent karne wala immutable record.
// [KYU ZAROORI HAI]: Actor boundary se bahar state leak na ho aur caller immutable data receive kare.
// [KAISE CHALTA HAI]: Record semantics ke zariye value equality aur thread-safe read guarantee milti hai.
// [OUTPUT / SIDE EFFECT]: Thread-safe immutable snapshot.
// [VARIATIONS TO TRY]: Rider rating aur shift timings shamil karein.
public record RiderState(
    string RiderId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    int BatteryPct,
    string? ActiveOrderId,
    bool IsAvailable,
    int TelemetryPingsReceived,
    DateTime LastHeartbeatUtc);
