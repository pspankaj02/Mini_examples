// [KYA KARTA HAI]: Messages namespace import karta hai.
// [KYU ZAROORI HAI]: Event contracts use karne ke liye.
// [KAISE CHALTA HAI]: Using directive.
// [OUTPUT / SIDE EFFECT]: Event symbols available.
// [VARIATIONS TO TRY]: Real payment gateway SDK.
using KiranaHub.EventDrivenSaga.Messages;

// [KYA KARTA HAI]: Namespace define karta hai consumers ke liye.
// [KYU ZAROORI HAI]: Consumers organization ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Consumers namespace.
// [VARIATIONS TO TRY]: Payment domain module.
namespace KiranaHub.EventDrivenSaga.Consumers;

// [KYA KARTA HAI]: Payment Event Consumer jo money capture karta hai.
// [KYU ZAROORI HAI]: InventoryReserved hone ke baad payment gateway se funds debit karne ke liye.
// [KAISE CHALTA HAI]: InventoryReservedEvent leta hai aur PaymentCompletedEvent return karta hai.
// [OUTPUT / SIDE EFFECT]: Emits PaymentCompletedEvent.
// [VARIATIONS TO TRY]: Idempotency key verification.
public class PaymentConsumer
{
    // [KYA KARTA HAI]: InventoryReservedEvent consume karke payment process karta hai.
    // [KYU ZAROORI HAI]: Payment step execute karne ke liye.
    // [KAISE CHALTA HAI]: Generates transaction reference and returns event.
    // [OUTPUT / SIDE EFFECT]: PaymentCompletedEvent instance.
    // [VARIATIONS TO TRY]: Simulated card decline exception.
    public PaymentCompletedEvent Consume(InventoryReservedEvent message, decimal amount)
    {
        var txnRef = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        return new PaymentCompletedEvent(
            message.OrderId,
            txnRef,
            amount);
    }
}
