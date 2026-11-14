namespace KiranaHub.VirtualActors.Grains;

using KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: IRiderGrain ka concrete virtual actor implementation provide karta hai.
// [KYU ZAROORI HAI]: Har rider ka in-memory stateful boundary encapsulate karna taaki concurrent GPS updates safely serialize ho sakein bina locks ke.
// [KAISE CHALTA HAI]: VirtualGrainBase se inherit hokar EnqueueTurnAsync ke zariye messages single-threaded execute karta hai.
// [OUTPUT / SIDE EFFECT]: Thread-safe state mutation, zero race conditions, high-throughput telemetry updates.
// [VARIATIONS TO TRY]: Redis backing store se durable state reload implement kar sakte hain.
public class RiderGrain : VirtualGrainBase, IRiderGrain
{
    // [KYA KARTA HAI]: Rider ki internal mutable state store karta hai.
    // [KYU ZAROORI HAI]: Actor turn ke andar private state mutate hoti hai aur external callers ko snapshot milta hai.
    // [KAISE CHALTA HAI]: Default values se initialize hota hai.
    // [OUTPUT / SIDE EFFECT]: In-memory actor state.
    // [VARIATIONS TO TRY]: Initial state database se load karein.
    private double _latitude;
    private double _longitude;
    private double _speedKmh;
    private int _batteryPct = 100;
    private string? _activeOrderId;
    private bool _isAvailable = true;
    private int _pingsReceived = 0;

    // [KYA KARTA HAI]: Grain activation lifecycle hook execute karta hai.
    // [KYU ZAROORI HAI]: Rider grain activate hone par telemetry metrics aur initial status setup karna.
    // [KAISE CHALTA HAI]: Base activation call karta hai aur log print karta hai.
    // [OUTPUT / SIDE EFFECT]: Grain lifecycle state Active ho jata hai.
    // [VARIATIONS TO TRY]: Dispatch system ko "Rider Online" notification broadcast karein.
    public override async Task OnActivateAsync()
    {
        // [KYA KARTA HAI]: Base class activation call karta hai.
        // [KYU ZAROORI HAI]: Lifecycle state ko Active mark karna.
        // [KAISE CHALTA HAI]: await base.OnActivateAsync().
        // [OUTPUT / SIDE EFFECT]: Base state set to Active.
        // [VARIATIONS TO TRY]: Async telemetry bootstrap logic execute karein.
        await base.OnActivateAsync().ConfigureAwait(false);

        // [KYA KARTA HAI]: Initial default coordinates (e.g. Connaught Place, New Delhi dark-store hub) assign karta hai.
        // [KYU ZAROORI HAI]: Newly activated grain ke paas valid initial coordinates hone chahiye.
        // [KAISE CHALTA HAI]: Field assignment.
        // [OUTPUT / SIDE EFFECT]: Latitude and longitude initialized.
        // [VARIATIONS TO TRY]: Dark-store geofence center point inject karein.
        _latitude = 28.6315;
        _longitude = 77.2167;
    }

    // [KYA KARTA HAI]: Rider location aur telemetry update karta hai.
    // [KYU ZAROORI HAI]: Live tracking, ETA calculations, aur nearby dispatch matching ke liye latest location zaroori hai.
    // [KAISE CHALTA HAI]: EnqueueTurnAsync ke andar serialize hokar fields mutate karta hai.
    // [OUTPUT / SIDE EFFECT]: Updated rider coordinates and battery levels.
    // [VARIATIONS TO TRY]: High-speed threshold alert (> 60 km/h) trigger karein.
    public Task UpdateLocationAsync(double latitude, double longitude, double speedKmh, int batteryPct)
    {
        // [KYA KARTA HAI]: Single-threaded turn execute karta hai.
        // [KYU ZAROORI HAI]: Multiple parallel HTTP requests se aane wale GPS packets me race condition na ho.
        // [KAISE CHALTA HAI]: Lambda turn queue me schedule hoti hai.
        // [OUTPUT / SIDE EFFECT]: Atomic update of all telemetry fields.
        // [VARIATIONS TO TRY]: Dead reckoning trajectory filter lagayein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Latitude field update karta hai.
            // [KYU ZAROORI HAI]: Latest GPS position record karna.
            // [KAISE CHALTA HAI]: Field mutation.
            // [OUTPUT / SIDE EFFECT]: _latitude updated.
            // [VARIATIONS TO TRY]: Coordinate validation (-90 to +90) check karein.
            _latitude = latitude;

            // [KYA KARTA HAI]: Longitude field update karta hai.
            // [KYU ZAROORI HAI]: Accurate geo mapping.
            // [KAISE CHALTA HAI]: Field mutation.
            // [OUTPUT / SIDE EFFECT]: _longitude updated.
            // [VARIATIONS TO TRY]: Coordinate validation (-180 to +180) check karein.
            _longitude = longitude;

            // [KYA KARTA HAI]: Speed in km/h record karta hai.
            // [KYU ZAROORI HAI]: Delivery speed aur safety monitoring ke liye.
            // [KAISE CHALTA HAI]: Field mutation.
            // [OUTPUT / SIDE EFFECT]: _speedKmh updated.
            // [VARIATIONS TO TRY]: Moving average speed calculate karein.
            _speedKmh = speedKmh;

            // [KYA KARTA HAI]: Battery level percentage set karta hai.
            // [KYU ZAROORI HAI]: Low battery (< 15%) par order dispatch restrict karne ke liye.
            // [KAISE CHALTA HAI]: Math.Clamp se 0 se 100 ke beech constrain karta hai.
            // [OUTPUT / SIDE EFFECT]: _batteryPct updated.
            // [VARIATIONS TO TRY]: Low battery push alert send karein.
            _batteryPct = Math.Clamp(batteryPct, 0, 100);

            // [KYA KARTA HAI]: Processed telemetry packet counter increment karta hai.
            // [KYU ZAROORI HAI]: Telemetry throughput aur message loss audit karne ke liye.
            // [KAISE CHALTA HAI]: Integer increment.
            // [OUTPUT / SIDE EFFECT]: _pingsReceived += 1.
            // [VARIATIONS TO TRY]: Interlocked increment (halanki turn single-threaded hai).
            _pingsReceived++;

            // [KYA KARTA HAI]: CompletedTask return karta hai.
            // [KYU ZAROORI HAI]: Synchronous work ko async turn signature me adapt karna.
            // [KAISE CHALTA HAI]: Task.CompletedTask.
            // [OUTPUT / SIDE EFFECT]: Completed async promise.
            // [VARIATIONS TO TRY]: Stream broadcast event emit karein.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Rider ko order assign karta hai.
    // [KYU ZAROORI HAI]: Active delivery state lock karna taaki doosra order na mil jaye.
    // [KAISE CHALTA HAI]: OrderId set karta hai aur availability false karta hai.
    // [OUTPUT / SIDE EFFECT]: IsAvailable = false, ActiveOrderId = orderId.
    // [VARIATIONS TO TRY]: Agar rider pehle se busy ho toh InvalidOperationException throw karein.
    public Task AssignOrderAsync(string orderId)
    {
        // [KYA KARTA HAI]: Turn queue me assignment serialize karta hai.
        // [KYU ZAROORI HAI]: Race condition free order dispatch.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync lambda.
        // [OUTPUT / SIDE EFFECT]: Rider state becomes busy.
        // [VARIATIONS TO TRY]: Order rejection timeout timer schedule karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Argument null check karta hai.
            // [KYU ZAROORI HAI]: Valid order ID zaroori hai.
            // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
            // [OUTPUT / SIDE EFFECT]: Exception if invalid.
            // [VARIATIONS TO TRY]: Guid parser use karein.
            ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

            // [KYA KARTA HAI]: ActiveOrderId set karta hai.
            // [KYU ZAROORI HAI]: Current assignment track karna.
            // [KAISE CHALTA HAI]: Field mutation.
            // [OUTPUT / SIDE EFFECT]: _activeOrderId set.
            // [VARIATIONS TO TRY]: Assignment timestamp record karein.
            _activeOrderId = orderId;

            // [KYA KARTA HAI]: Availability ko false karta hai.
            // [KYU ZAROORI HAI]: Rider pool me se temporary remove karna.
            // [KAISE CHALTA HAI]: Boolean set.
            // [OUTPUT / SIDE EFFECT]: _isAvailable = false.
            // [VARIATIONS TO TRY]: Status enum (e.g. EnRouteToStore, PickedUp, EnRouteToCustomer) use karein.
            _isAvailable = false;

            // [KYA KARTA HAI]: CompletedTask return.
            // [KYU ZAROORI HAI]: Async signature fulfillment.
            // [KAISE CHALTA HAI]: Return Task.CompletedTask.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Event stream emit karein.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Active order complete karta hai.
    // [KYU ZAROORI HAI]: Order delivered hone ke baad rider ko next order lene ke liye available karna.
    // [KAISE CHALTA HAI]: ActiveOrderId null karke IsAvailable true karta hai.
    // [OUTPUT / SIDE EFFECT]: Rider becomes available.
    // [VARIATIONS TO TRY]: Total completed orders counter increment karein.
    public Task CompleteOrderAsync()
    {
        // [KYA KARTA HAI]: Single-threaded turn execute karta hai.
        // [KYU ZAROORI HAI]: State cleanup serialized tareeqe se ho.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync.
        // [OUTPUT / SIDE EFFECT]: Status reset.
        // [VARIATIONS TO TRY]: Earnings bonus ledger entry trigger karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: ActiveOrderId clear karta hai.
            // [KYU ZAROORI HAI]: No active delivery assignment remaining.
            // [KAISE CHALTA HAI]: Field null assignment.
            // [OUTPUT / SIDE EFFECT]: _activeOrderId = null.
            // [VARIATIONS TO TRY]: Last delivered order ID audit history me save karein.
            _activeOrderId = null;

            // [KYA KARTA HAI]: IsAvailable true set karta hai.
            // [KYU ZAROORI HAI]: Rider free for next assignment.
            // [KAISE CHALTA HAI]: Boolean assignment.
            // [OUTPUT / SIDE EFFECT]: _isAvailable = true.
            // [VARIATIONS TO TRY]: Cooling period timer lagayein before next assignment.
            _isAvailable = true;

            // [KYA KARTA HAI]: Return completed task.
            // [KYU ZAROORI HAI]: Async compliance.
            // [KAISE CHALTA HAI]: Return.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Dispatcher ping trigger karein.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Current state ka immutable snapshot return karta hai.
    // [KYU ZAROORI HAI]: Caller ko consistent, thread-safe, non-leaking data dena.
    // [KAISE CHALTA HAI]: Turn queue me se current fields read karke RiderState record construct karta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable RiderState record.
    // [VARIATIONS TO TRY]: Read-only projection cache use karein.
    public Task<RiderState> GetStateAsync()
    {
        // [KYA KARTA HAI]: Turn queue me safe read execute karta hai.
        // [KYU ZAROORI HAI]: Read aur write operations me race condition prevent karna.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync with return value.
        // [OUTPUT / SIDE EFFECT]: RiderState instance.
        // [VARIATIONS TO TRY]: Stale read allowing fast-path bypass explore karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Immutable snapshot record construct karta hai.
            // [KYU ZAROORI HAI]: Actor boundary se bahar safe data sharing.
            // [KAISE CHALTA HAI]: Record instantiation with current fields.
            // [OUTPUT / SIDE EFFECT]: Snapshot created.
            // [VARIATIONS TO TRY]: Distance from darkstore calculate karke attach karein.
            var state = new RiderState(
                RiderId: GrainId,
                Latitude: _latitude,
                Longitude: _longitude,
                SpeedKmh: _speedKmh,
                BatteryPct: _batteryPct,
                ActiveOrderId: _activeOrderId,
                IsAvailable: _isAvailable,
                TelemetryPingsReceived: _pingsReceived,
                LastHeartbeatUtc: LastAccessedUtc);

            // [KYA KARTA HAI]: State result return karta hai.
            // [KYU ZAROORI HAI]: Async method fulfillment.
            // [KAISE CHALTA HAI]: Task.FromResult(state).
            // [OUTPUT / SIDE EFFECT]: Task holding RiderState.
            // [VARIATIONS TO TRY]: ValueTask use karein.
            return Task.FromResult(state);
        });
    }
}
