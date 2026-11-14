namespace KiranaHub.RealTimeAndJobs.Hubs;

// =========================================================================================
// [TOPIC]: Strongly-Typed SignalR Client Contract
// [PURPOSE]: Eliminates magic string method names in real-time WebSocket communications.
// =========================================================================================

// [KYA KARTA HAI]: Client (React/Blazor/Mobile) par trigger hone wale methods ka strongly-typed contract define karta hai.
// [KYU ZAROORI HAI]: Compile-time safety: agar method rename ho toh hub aur client me compile error aayega instead of silent runtime failure.
public interface IOrderTrackingClient
{
    // [KYA KARTA HAI]: Client ko order status change notify karta hai (e.g. "Out For Delivery", "Delivered").
    // [OUTPUT / EVENT]: Client app par order timeline animation advance hoti hai.
    Task ReceiveOrderStatusUpdate(string orderId, string status);

    // [KYA KARTA HAI]: Client ko delivery rider ke real-time GPS coordinates stream karta hai.
    // [OUTPUT / EVENT]: Map marker live move hota hai UI par.
    Task ReceiveRiderLocation(string orderId, double latitude, double longitude);

    // [KYA KARTA HAI]: Low-stock ya out-of-stock flash notification broadcast karta hai.
    // [OUTPUT / EVENT]: Banner alert UI par pop hota hai.
    Task ReceiveInventoryAlert(string sku, int remainingStock);
}

