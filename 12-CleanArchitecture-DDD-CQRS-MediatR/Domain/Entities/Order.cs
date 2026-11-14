using KiranaHub.CleanArchitecture.Domain.Common;
using KiranaHub.CleanArchitecture.Domain.Events;
using KiranaHub.CleanArchitecture.Domain.ValueObjects;

namespace KiranaHub.CleanArchitecture.Domain.Entities;

// =========================================================================================
// [TOPIC]: Order Aggregate Root (Domain-Driven Design)
// [PURPOSE]: Central entity defining transactional consistency boundaries and business invariants.
// =========================================================================================

// [KYA KARTA HAI]: Order aggregate root jo AggregateRoot se inherit karta hai.
// [KYU ZAROORI HAI]: External layers direct OrderItems ko modify nahi kar sakte; sabhi business actions Order class ke zariye hote hain.
public class Order : AggregateRoot
{
    // [KYA KARTA HAI]: Order ka unique string identifier (e.g. "ORD-98721").
    public string Id { get; private set; }

    // [KYA KARTA HAI]: Customer identifier.
    public string CustomerId { get; private set; }

    // [KYA KARTA HAI]: Order creation UTC timestamp.
    public DateTime CreatedAtUtc { get; private set; }

    // [KYA KARTA HAI]: Current lifecycle status.
    public OrderStatus Status { get; private set; }

    // [KYA KARTA HAI]: Internal mutable list of line items.
    private readonly List<OrderItem> _items = new();

    // [KYA KARTA HAI]: Public read-only collection of items.
    // [KYU ZAROORI HAI]: Encapsulation: external callers cannot do `order.Items.Add(...)` bypassing aggregate invariants!
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // [KYA KARTA HAI]: Computed property: Sum of all line totals.
    // [KAISE CHALTA HAI]: LINQ Aggregate se sabhi Money objects ka total sum nikalta hai.
    public Money TotalAmount => _items.Count == 0 
        ? Money.Zero() 
        : _items.Select(x => x.LineTotal).Aggregate((a, b) => a + b);

    // [KYA KARTA HAI]: Private parameterless constructor for ORM reflection.
    private Order()
    {
        Id = string.Empty;
        CustomerId = string.Empty;
    }

    // [KYA KARTA HAI]: Static Factory method to create and initialize a new Order aggregate.
    // [KYU ZAROORI HAI]: DDD Factory pattern ensures clean valid object construction and raises creation event.
    public static Order Create(string customerId, List<OrderItem> items)
    {
        // [KYA KARTA HAI]: CustomerId validation check.
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        }

        // [KYA KARTA HAI]: Items null or empty check.
        // [KYU ZAROORI HAI]: Business rule: Empty order cannot be placed!
        if (items == null || items.Count == 0)
        {
            throw new InvalidOperationException("Cannot create an order with zero items.");
        }

        // [KYA KARTA HAI]: Order aggregate instance instantiate karta hai.
        var order = new Order
        {
            Id = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            CustomerId = customerId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.PendingPayment
        };

        // [KYA KARTA HAI]: Provided items ko internal items list me copy karta hai.
        order._items.AddRange(items);

        // [KYA KARTA HAI]: Domain Event raise karta hai OrderPlacedDomainEvent.
        // [KYU ZAROORI HAI]: Event dispatcher ko batana ki naya order place ho chuka hai.
        order.RaiseDomainEvent(new OrderPlacedDomainEvent(order.Id, order.CustomerId, order.TotalAmount));

        // [KYA KARTA HAI]: Constructed valid Order aggregate return karta hai.
        return order;
    }

    // [KYA KARTA HAI]: Payment complete hone par order status ko Paid me transition karta hai.
    // [KYU ZAROORI HAI]: State machine lifecycle enforcement.
    public void MarkAsPaid()
    {
        // [KYA KARTA HAI]: Invariant check: Order already cancelled toh nahi hai?
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot pay for an already cancelled order.");
        }

        // [KYA KARTA HAI]: Invariant check: Duplicate payment check.
        if (Status == OrderStatus.Paid || Status == OrderStatus.OutForDelivery || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Order is already in '{Status}' state.");
        }

        // [KYA KARTA HAI]: Status update karta hai Paid me.
        Status = OrderStatus.Paid;
    }

    // [KYA KARTA HAI]: Order ko cancel karta hai with reason.
    // [KYU ZAROORI HAI]: Business rule: Delivered order cancel nahi ho sakta!
    public void Cancel(string reason)
    {
        // [KYA KARTA HAI]: Reason validation.
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Cancellation reason must be provided.", nameof(reason));

        // [KYA KARTA HAI]: Invariant check: Delivered orders cannot be cancelled.
        if (Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered orders cannot be cancelled. Initiate return instead.");
        }

        // [KYA KARTA HAI]: Invariant check: Already cancelled check.
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled.");
        }

        // [KYA KARTA HAI]: Status update karta hai Cancelled me.
        Status = OrderStatus.Cancelled;
    }
}

