using Microsoft.AspNetCore.SignalR;

namespace KiranaHub.RealTimeAndJobs.Hubs;

// =========================================================================================
// [TOPIC]: Strongly-Typed SignalR Hub with Dynamic Groups
// [PURPOSE]: Real-time full-duplex bi-directional communication between server and connected clients.
// =========================================================================================

// [KYA KARTA HAI]: Microsoft.AspNetCore.SignalR.Hub<IOrderTrackingClient> se inherit karta hai.
// [KYU ZAROORI HAI]: Real-time order tracking: Long polling ke badle efficient persistent WebSocket connection use karna.
public class OrderTrackingHub : Hub<IOrderTrackingClient>
{
    // [KYA KARTA HAI]: Client connection ko specific order ke pub/sub group me add karta hai.
    // [KYU ZAROORI HAI]: Targeted broadcasting: Rider coordinates sirf us customer ko dikhne chahiye jisne wo order place kiya hai.
    // [KAISE CHALTA HAI]: SignalR internal group manager connection ID ko string group name se associate karta hai.
    public async Task JoinOrderGroup(string orderId)
    {
        // [KYA KARTA HAI]: Input validation: orderId blank toh nahi.
        if (string.IsNullOrWhiteSpace(orderId)) return;

        // [KYA KARTA HAI]: Current connection ID ko order group me enroll karta hai.
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

        // [KYA KARTA HAI]: Console par join event log karta hai.
        Console.WriteLine($"[SIGNALR HUB] Connection {Context.ConnectionId} joined group: 'order-{orderId}'");
    }

    // [KYA KARTA HAI]: Client connection ko order group se remove karta hai.
    // [KYU ZAROORI HAI]: Jab user order tracking screen band karde toh unhe messages stream karna band karna.
    public async Task LeaveOrderGroup(string orderId)
    {
        // [KYA KARTA HAI]: OrderId null check.
        if (string.IsNullOrWhiteSpace(orderId)) return;

        // [KYA KARTA HAI]: Connection ko group se unsubscribe karta hai.
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");

        // [KYA KARTA HAI]: Log print karta hai.
        Console.WriteLine($"[SIGNALR HUB] Connection {Context.ConnectionId} left group: 'order-{orderId}'");
    }

    // [KYA KARTA HAI]: Order status change ko group ke subscribers ko broadcast karta hai.
    public async Task SendStatusUpdate(string orderId, string status)
    {
        // [KYA KARTA HAI]: Strongly-typed client contract call: group ke sabhi connected clients par method invoke karta hai.
        // [OUTPUT / EVENT]: Clients receive ReceiveOrderStatusUpdate.
        await Clients.Group($"order-{orderId}").ReceiveOrderStatusUpdate(orderId, status);
    }

    // [KYA KARTA HAI]: Delivery rider ke live GPS coordinates ko tracking group me stream karta hai.
    public async Task UpdateRiderLocation(string orderId, double latitude, double longitude)
    {
        // [KYA KARTA HAI]: Group clients ko rider location push karta hai.
        await Clients.Group($"order-{orderId}").ReceiveRiderLocation(orderId, latitude, longitude);
    }

    // [KYA KARTA HAI]: Global flash deal announcement sabhi active connected users ko broadcast karta hai.
    public async Task BroadcastGlobalAlert(string sku, int remainingStock)
    {
        // [KYA KARTA HAI]: Sabhi connected clients ko alert push karta hai (Clients.All).
        await Clients.All.ReceiveInventoryAlert(sku, remainingStock);
    }
}

