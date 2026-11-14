using KiranaHub.CleanArchitecture.Domain.Common;
using KiranaHub.CleanArchitecture.Domain.ValueObjects;

namespace KiranaHub.CleanArchitecture.Domain.Events;

// =========================================================================================
// [TOPIC]: OrderPlacedDomainEvent (Domain Event)
// [PURPOSE]: Broadcasts that an order was successfully validated and placed within the domain.
// =========================================================================================

// [KYA KARTA HAI]: Order placement event record jo IDomainEvent implement karta hai.
// [KYU ZAROORI HAI]: Application layer ke event handlers (Email sender, Inventory deduction) ko trigger karna.
public record OrderPlacedDomainEvent(
    // [KYA KARTA HAI]: Placed order ka identifier.
    string OrderId,
    // [KYA KARTA HAI]: Customer ka identifier.
    string CustomerId,
    // [KYA KARTA HAI]: Total financial value of the order.
    Money TotalAmount,
    // [KYA KARTA HAI]: Timestamp jab event occur hua (defaults to UTC now).
    DateTime OccurredOnUtc
) : IDomainEvent
{
    // [KYA KARTA HAI]: Convenient secondary constructor with auto-timestamp.
    public OrderPlacedDomainEvent(string orderId, string customerId, Money totalAmount)
        : this(orderId, customerId, totalAmount, DateTime.UtcNow) { }
}

