// [KYA KARTA HAI]: C# namespace define karta hai event messages ke liye.
// [KYU ZAROORI HAI]: Distributed messaging contract ko project me organize karne ke liye.
// [KAISE CHALTA HAI]: Logical namespace boundary create karta hai.
// [OUTPUT / SIDE EFFECT]: Namespace scope available.
// [VARIATIONS TO TRY]: Shared Contracts class library.
namespace KiranaHub.EventDrivenSaga.Messages;

// [KYA KARTA HAI]: Order placement event record define karta hai.
// [KYU ZAROORI HAI]: Customer ke checkout click karne par saga pipeline trigger karne ke liye.
// [KAISE CHALTA HAI]: Immutable record holding orderId, customerId, totalAmount, aur item IDs.
// [OUTPUT / SIDE EFFECT]: OrderPlacedEvent contract.
// [VARIATIONS TO TRY]: Currency property add kar sakte hain.
public record OrderPlacedEvent(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    IReadOnlyList<string> ProductIds,
    DateTime PlacedAtUtc);

// [KYA KARTA HAI]: Inventory reservation success event define karta hai.
// [KYU ZAROORI HAI]: Dark-store me stock lock hone par payment step trigger karne ke liye.
// [KAISE CHALTA HAI]: Record holding orderId, darkStoreId, aur reservation timestamp.
// [OUTPUT / SIDE EFFECT]: InventoryReservedEvent contract.
// [VARIATIONS TO TRY]: Aisle / shelf rack numbers attach karna.
public record InventoryReservedEvent(
    string OrderId,
    string DarkStoreId,
    DateTime ReservedAtUtc);

// [KYA KARTA HAI]: Inventory out-of-stock failure event define karta hai.
// [KYU ZAROORI HAI]: Saga me compensating rollback (auto-refund) trigger karne ke liye.
// [KAISE CHALTA HAI]: Record holding orderId, unavailableProductId, aur reason string.
// [OUTPUT / SIDE EFFECT]: InventoryExhaustedEvent contract.
// [VARIATIONS TO TRY]: Alternative replacement product suggestion ID.
public record InventoryExhaustedEvent(
    string OrderId,
    string UnavailableProductId,
    string Reason);

// [KYA KARTA HAI]: Payment capture success event define karta hai.
// [KYU ZAROORI HAI]: Money deduct hone par order fulfillment complete karne ke liye.
// [KAISE CHALTA HAI]: Record holding orderId, transactionRef, aur amount.
// [OUTPUT / SIDE EFFECT]: PaymentCompletedEvent contract.
// [VARIATIONS TO TRY]: Payment gateway provider name (Razorpay, UPI).
public record PaymentCompletedEvent(
    string OrderId,
    string TransactionRef,
    decimal AmountCaptured);

// [KYA KARTA HAI]: Compensating transaction / Order cancellation event define karta hai.
// [KYU ZAROORI HAI]: Saga failure par refund initiate karne aur customer ko apology notify karne ke liye.
// [KAISE CHALTA HAI]: Record holding orderId, reason, aur refundStatus.
// [OUTPUT / SIDE EFFECT]: OrderCancelledEvent contract.
// [VARIATIONS TO TRY]: RefundTransactionReference property.
public record OrderCancelledEvent(
    string OrderId,
    string CancellationReason,
    bool RefundInitiated);
