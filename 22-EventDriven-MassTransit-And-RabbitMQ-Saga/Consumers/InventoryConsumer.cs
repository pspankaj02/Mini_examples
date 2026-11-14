// [KYA KARTA HAI]: Messages namespace import karta hai.
// [KYU ZAROORI HAI]: Event contracts use karne ke liye.
// [KAISE CHALTA HAI]: Using directive.
// [OUTPUT / SIDE EFFECT]: Event symbols available.
// [VARIATIONS TO TRY]: MassTransit.IConsumer<T> interface.
using KiranaHub.EventDrivenSaga.Messages;

// [KYA KARTA HAI]: Namespace define karta hai message consumers ke liye.
// [KYU ZAROORI HAI]: Event driven consumers ko encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Consumers namespace.
// [VARIATIONS TO TRY]: Feature-based consumer folder.
namespace KiranaHub.EventDrivenSaga.Consumers;

// [KYA KARTA HAI]: Inventory Event Consumer jo dark-store stock checks handle karta hai.
// [KYU ZAROORI HAI]: OrderPlacedEvent aane par asynchronously stock reserve karne ya out-of-stock signal emit karne ke liye.
// [KAISE CHALTA HAI]: Product IDs check karta hai. Agar koi item out-of-stock ho toh InventoryExhaustedEvent return karta hai, warna InventoryReservedEvent.
// [OUTPUT / SIDE EFFECT]: Emits next domain event in pipeline.
// [VARIATIONS TO TRY]: Multiple dark-stores inventory lookup.
public class InventoryConsumer
{
    // [KYA KARTA HAI]: OrderPlacedEvent process karke stock reserve karta hai.
    // [KYU ZAROORI HAI]: Stock validation decision execute karne ke liye.
    // [KAISE CHALTA HAI]: Item list check karta hai.
    // [OUTPUT / SIDE EFFECT]: Either InventoryReservedEvent or InventoryExhaustedEvent.
    // [VARIATIONS TO TRY]: Partial reservation logic.
    public object Consume(OrderPlacedEvent message)
    {
        // Simulate out-of-stock scenario if product ID contains 'out-of-stock'
        var missingProduct = message.ProductIds.FirstOrDefault(id => id.Contains("out-of-stock", StringComparison.OrdinalIgnoreCase));

        if (missingProduct != null)
        {
            return new InventoryExhaustedEvent(
                message.OrderId,
                missingProduct,
                "Item temporarily out of stock at nearest dark-store.");
        }

        return new InventoryReservedEvent(
            message.OrderId,
            "DARK-STORE-NOIDA-62",
            DateTime.UtcNow);
    }
}
