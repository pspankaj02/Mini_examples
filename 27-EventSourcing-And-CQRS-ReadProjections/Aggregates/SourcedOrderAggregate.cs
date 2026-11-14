// ==============================================================================================
// KiranaHub Event Sourcing: Pure Event-Sourced Order Aggregate Root
// [KYA KARTA HAI] Order business domain rules enforce karta hai aur internal state ko strictly events replay se mutate karta hai.
// [KYU ZAROORI HAI] State ko directly change hone se rokna — har state change ek domain event produce karta hai.
// [KAISE CHALTA HAI] Command methods business invariants check karte hain aur events raise karte hain; Apply methods internal state mutate karte hain.
// [OUTPUT / SIDE EFFECT] Self-contained event-sourced aggregate with zero mutable public setters.
// [VARIATIONS TO TRY] Snapshotting aggregate state every 50 events.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Guid, DateTimeOffset, InvalidOperationException types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Core types available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, IReadOnlyList collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections ready.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sum, Any aggregations ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators available.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Domain events namespace import karta hai.
// [KYU ZAROORI HAI] IDomainEvent and concrete event records access karna.
// [KAISE CHALTA HAI] Local event namespace loaded.
// [OUTPUT / SIDE EFFECT] Event contracts accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Events;

// [KYA KARTA HAI] Namespace declare karta hai aggregate root ke liye.
// [KYU ZAROORI HAI] Domain layer separation.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.Aggregates.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Domain.Aggregates.
namespace KiranaHub.EventSourcing.Aggregates;

// [KYA KARTA HAI] Order status lifecycle enumeration.
// [KYU ZAROORI HAI] Allowed state transitions control karna.
// [KAISE CHALTA HAI] Enum values: Created, StockAllocated, Paid, Dispatched, Delivered, Cancelled.
// [OUTPUT / SIDE EFFECT] Status constants.
// [VARIATIONS TO TRY] Add Packing in-progress state.
public enum OrderState
{
    Created,
    StockAllocated,
    Paid,
    Dispatched,
    Delivered,
    Cancelled
}

// [KYA KARTA HAI] Order aggregate internal item entity record.
// [KYU ZAROORI HAI] Cart line items maintain karna.
// [KAISE CHALTA HAI] Immutable record with product attributes and price.
// [OUTPUT / SIDE EFFECT] Line item model.
// [VARIATIONS TO TRY] Include discount percentage.
public record OrderLineItem(string Sku, string ProductName, int Quantity, decimal UnitPrice)
{
    public decimal Subtotal => Quantity * UnitPrice;
}

// [KYA KARTA HAI] Pure Event-Sourced Order Aggregate Root class.
// [KYU ZAROORI HAI] Invariants guard karna aur event-based state transitions manage karna.
// [KAISE CHALTA HAI] Public methods emit events -> Apply() changes state -> uncommitted events saved to store.
// [OUTPUT / SIDE EFFECT] Aggregate root instance.
// [VARIATIONS TO TRY] BaseAggregate<TId> abstract base class.
public class SourcedOrderAggregate
{
    // [KYA KARTA HAI] Uncommitted events list.
    // [KYU ZAROORI HAI] Current transaction me naye raise hue events track karna jo abhi store me commit nahi hue hain.
    // [KAISE CHALTA HAI] Events are added via RaiseEvent() and cleared after AppendEventsAsync.
    // [OUTPUT / SIDE EFFECT] Pending events buffer.
    // [VARIATIONS TO TRY] Queue<IDomainEvent>.
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    // [KYA KARTA HAI] Internal line items list.
    // [KYU ZAROORI HAI] Order ke items track karna.
    // [KAISE CHALTA HAI] Mutated exclusively in Apply(ItemAddedEvent).
    // [OUTPUT / SIDE EFFECT] Private items collection.
    // [VARIATIONS TO TRY] Dictionary<string, OrderLineItem> keyed by SKU.
    private readonly List<OrderLineItem> _items = new();

    // [KYA KARTA HAI] Aggregate Unique Identifier.
    // [KYU ZAROORI HAI] Order stream identification.
    // [KAISE CHALTA HAI] Set in Apply(OrderCreatedEvent).
    // [OUTPUT / SIDE EFFECT] Aggregate ID property.
    // [VARIATIONS TO TRY] Strongly-typed OrderId struct.
    public Guid Id { get; private set; }

    // [KYA KARTA HAI] Current committed stream version.
    // [KYU ZAROORI HAI] Optimistic concurrency control (ExpectedVersion).
    // [KAISE CHALTA HAI] Incremented on event replay.
    // [OUTPUT / SIDE EFFECT] Integer version.
    // [VARIATIONS TO TRY] None.
    public int Version { get; private set; }

    // [KYA KARTA HAI] Customer identifier.
    // [KYU ZAROORI HAI] Order ownership track karna.
    // [KAISE CHALTA HAI] Set in Apply(OrderCreatedEvent).
    // [OUTPUT / SIDE EFFECT] Customer ID string.
    // [VARIATIONS TO TRY] Customer Guid.
    public string CustomerId { get; private set; } = string.Empty;

    // [KYA KARTA HAI] Assigned dark-store identifier.
    // [KYU ZAROORI HAI] Warehouse fulfillment routing.
    // [KAISE CHALTA HAI] Set in Apply(OrderCreatedEvent).
    // [OUTPUT / SIDE EFFECT] DarkStore ID string.
    // [VARIATIONS TO TRY] None.
    public string DarkStoreId { get; private set; } = string.Empty;

    // [KYA KARTA HAI] Current order lifecycle state.
    // [KYU ZAROORI HAI] Invalid state transitions reject karna (e.g. deliver before dispatch).
    // [KAISE CHALTA HAI] Updated across Apply methods.
    // [OUTPUT / SIDE EFFECT] OrderState enum value.
    // [VARIATIONS TO TRY] State pattern classes.
    public OrderState Status { get; private set; }

    // [KYA KARTA HAI] Assigned delivery rider identifier.
    // [KYU ZAROORI HAI] Rider accountability track karna.
    // [KAISE CHALTA HAI] Set in Apply(RiderDispatchedEvent).
    // [OUTPUT / SIDE EFFECT] Rider ID string.
    // [VARIATIONS TO TRY] None.
    public string? RiderId { get; private set; }

    // [KYA KARTA HAI] Assigned delivery rider name.
    // [KYU ZAROORI HAI] Customer app display.
    // [KAISE CHALTA HAI] Set in Apply(RiderDispatchedEvent).
    // [OUTPUT / SIDE EFFECT] Rider name string.
    // [VARIATIONS TO TRY] None.
    public string? RiderName { get; private set; }

    // [KYA KARTA HAI] Actual delivery timestamp.
    // [KYU ZAROORI HAI] 10-minute SLA verification.
    // [KAISE CHALTA HAI] Set in Apply(OrderDeliveredEvent).
    // [OUTPUT / SIDE EFFECT] Nullable DateTimeOffset.
    // [VARIATIONS TO TRY] None.
    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    // [KYA KARTA HAI] Read-only view of line items.
    // [KYU ZAROORI HAI] External callers items inspect kar sakein bina collection corrupt kiye.
    // [KAISE CHALTA HAI] AsReadOnly wrapper.
    // [OUTPUT / SIDE EFFECT] Immutable collection view.
    // [VARIATIONS TO TRY] IReadOnlyCollection.
    public IReadOnlyList<OrderLineItem> Items => _items.AsReadOnly();

    // [KYA KARTA HAI] Computed total order price.
    // [KYU ZAROORI HAI] Payment amount validation.
    // [KAISE CHALTA HAI] LINQ Sum of item subtotals.
    // [OUTPUT / SIDE EFFECT] Decimal total amount.
    // [VARIATIONS TO TRY] Money value object.
    public decimal TotalAmount => _items.Sum(i => i.Subtotal);

    // [KYA KARTA HAI] Factory method: Naya Order Aggregate create karta hai.
    // [KYU ZAROORI HAI] Genesis event (OrderCreatedEvent) emit karna.
    // [KAISE CHALTA HAI] Instantiates aggregate and raises OrderCreatedEvent.
    // [OUTPUT / SIDE EFFECT] New uncommitted aggregate instance.
    // [VARIATIONS TO TRY] Constructor injection.
    public static SourcedOrderAggregate Create(Guid orderId, string customerId, string darkStoreId)
    {
        var aggregate = new SourcedOrderAggregate();
        aggregate.RaiseEvent(new OrderCreatedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: orderId,
            CustomerId: customerId,
            DarkStoreId: darkStoreId,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
        return aggregate;
    }

    // [KYA KARTA HAI] Business Command: Cart me product line item add karta hai.
    // [KYU ZAROORI HAI] Quantities aur pricing invariants validate karke ItemAddedEvent raise karna.
    // [KAISE CHALTA HAI] Asserts Status == Created, quantity > 0, raises event.
    // [OUTPUT / SIDE EFFECT] Emits ItemAddedEvent.
    // [VARIATIONS TO TRY] Update existing item quantity instead of appending.
    public void AddItem(string sku, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderState.Created)
            throw new InvalidOperationException($"Cannot add items to an order in '{Status}' state!");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive!", nameof(quantity));
        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be positive!", nameof(unitPrice));

        RaiseEvent(new ItemAddedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Sku: sku,
            ProductName: productName,
            Quantity: quantity,
            UnitPrice: unitPrice,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Business Command: Dark store inventory physical stock allocate karta hai.
    // [KYU ZAROORI HAI] Empty order allocation rokhna aur StockAllocatedEvent raise karna.
    // [KAISE CHALTA HAI] Checks items present, raises StockAllocatedEvent.
    // [OUTPUT / SIDE EFFECT] Emits StockAllocatedEvent.
    // [VARIATIONS TO TRY] Pass warehouse picker ID.
    public void AllocateStock(string darkStoreId)
    {
        if (Status != OrderState.Created)
            throw new InvalidOperationException($"Stock cannot be allocated in '{Status}' state!");
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot allocate stock for an empty order!");

        RaiseEvent(new StockAllocatedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            DarkStoreId: darkStoreId,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Business Command: Customer payment capture confirm karta hai.
    // [KYU ZAROORI HAI] Ensure karna ki stock allocated hai aur correct amount capture hui hai.
    // [KAISE CHALTA HAI] Verifies amount == TotalAmount, raises PaymentCapturedEvent.
    // [OUTPUT / SIDE EFFECT] Emits PaymentCapturedEvent.
    // [VARIATIONS TO TRY] Partial payment support.
    public void CapturePayment(string paymentTransactionId, decimal amount)
    {
        if (Status != OrderState.StockAllocated)
            throw new InvalidOperationException($"Payment cannot be captured in '{Status}' state! Stock must be allocated first.");
        if (amount != TotalAmount)
            throw new InvalidOperationException($"Payment amount mismatch! Expected: ₹{TotalAmount}, Provided: ₹{amount}");

        RaiseEvent(new PaymentCapturedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            PaymentTransactionId: paymentTransactionId,
            Amount: amount,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Business Command: Order delivery partner ko handover karta hai.
    // [KYU ZAROORI HAI] Only paid orders can be dispatched.
    // [KAISE CHALTA HAI] Asserts Status == Paid, raises RiderDispatchedEvent.
    // [OUTPUT / SIDE EFFECT] Emits RiderDispatchedEvent.
    // [VARIATIONS TO TRY] Assign delivery bag barcode.
    public void DispatchRider(string riderId, string riderName)
    {
        if (Status != OrderState.Paid)
            throw new InvalidOperationException($"Cannot dispatch rider for an unpaid order! Current status: '{Status}'");

        RaiseEvent(new RiderDispatchedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            RiderId: riderId,
            RiderName: riderName,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Business Command: Order customer ko deliver mark karta hai.
    // [KYU ZAROORI HAI] Dispatched status verify karke OrderDeliveredEvent emit karna.
    // [KAISE CHALTA HAI] Asserts Status == Dispatched, raises OrderDeliveredEvent.
    // [OUTPUT / SIDE EFFECT] Emits OrderDeliveredEvent.
    // [VARIATIONS TO TRY] Pass OTP verification token.
    public void MarkDelivered(DateTimeOffset deliveredAtUtc)
    {
        if (Status != OrderState.Dispatched)
            throw new InvalidOperationException($"Cannot deliver an order that is not dispatched! Current status: '{Status}'");

        RaiseEvent(new OrderDeliveredEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            DeliveredAtUtc: deliveredAtUtc,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Business Command: Order cancel karta hai.
    // [KYU ZAROORI HAI] Delivered orders ko cancel hone se rokna aur OrderCancelledEvent emit karna.
    // [KAISE CHALTA HAI] Asserts Status != Delivered, raises OrderCancelledEvent.
    // [OUTPUT / SIDE EFFECT] Emits OrderCancelledEvent.
    // [VARIATIONS TO TRY] Calculate cancellation penalty fee.
    public void Cancel(string reason)
    {
        if (Status == OrderState.Delivered)
            throw new InvalidOperationException("Cannot cancel an already delivered order!");

        RaiseEvent(new OrderCancelledEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Reason: reason,
            OccurredOnUtc: DateTimeOffset.UtcNow
        ));
    }

    // [KYA KARTA HAI] Uncommitted events return karta hai.
    // [KYU ZAROORI HAI] EventStore.AppendEventsAsync me pass karne ke liye.
    // [KAISE CHALTA HAI] Returns read-only copy of _uncommittedEvents.
    // [OUTPUT / SIDE EFFECT] Pending events list.
    // [VARIATIONS TO TRY] None.
    public IReadOnlyList<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    // [KYA KARTA HAI] Uncommitted events list clear karta hai.
    // [KYU ZAROORI HAI] EventStore me successful commit hone ke baad buffer clean karna.
    // [KAISE CHALTA HAI] Clears list.
    // [OUTPUT / SIDE EFFECT] Buffer emptied.
    // [VARIATIONS TO TRY] None.
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    // [KYA KARTA HAI] Historical events sequence se aggregate state reconstruct karta hai (Rehydration).
    // [KYU ZAROORI HAI] Database me state store kiye bina stream replay se live object create karna.
    // [KAISE CHALTA HAI] Iterates through past events and calls Apply() without adding to _uncommittedEvents.
    // [OUTPUT / SIDE EFFECT] Fully reconstructed aggregate state.
    // [VARIATIONS TO TRY] Load from snapshot first, then replay recent events.
    public static SourcedOrderAggregate RehydrateFromHistory(IEnumerable<IDomainEvent> history)
    {
        var aggregate = new SourcedOrderAggregate();
        foreach (var evt in history)
        {
            aggregate.Apply(evt);
            aggregate.Version = evt.Version;
        }
        return aggregate;
    }

    // [KYA KARTA HAI] Naya event raise karta hai: Apply() call karta hai aur uncommitted list me add karta hai.
    // [KYU ZAROORI HAI] Local state update aur commit tracking ek sath karna.
    // [KAISE CHALTA HAI] Calls Apply(evt) then _uncommittedEvents.Add(evt).
    // [OUTPUT / SIDE EFFECT] State mutated, event buffered.
    // [VARIATIONS TO TRY] None.
    private void RaiseEvent(IDomainEvent evt)
    {
        Apply(evt);
        _uncommittedEvents.Add(evt);
    }

    // [KYA KARTA HAI] Pattern matching dispatcher for internal state mutations.
    // [KYU ZAROORI HAI] Event type ke mutabiq aggregate ke private fields ko update karna.
    // [KAISE CHALTA HAI] C# switch pattern matching on IDomainEvent types.
    // [OUTPUT / SIDE EFFECT] Aggregate properties mutated.
    // [VARIATIONS TO TRY] Reflection-based When() method routing.
    private void Apply(IDomainEvent evt)
    {
        switch (evt)
        {
            case OrderCreatedEvent created:
                Id = created.AggregateId;
                CustomerId = created.CustomerId;
                DarkStoreId = created.DarkStoreId;
                Status = OrderState.Created;
                break;

            case ItemAddedEvent itemAdded:
                _items.Add(new OrderLineItem(itemAdded.Sku, itemAdded.ProductName, itemAdded.Quantity, itemAdded.UnitPrice));
                break;

            case StockAllocatedEvent:
                Status = OrderState.StockAllocated;
                break;

            case PaymentCapturedEvent:
                Status = OrderState.Paid;
                break;

            case RiderDispatchedEvent dispatched:
                Status = OrderState.Dispatched;
                RiderId = dispatched.RiderId;
                RiderName = dispatched.RiderName;
                break;

            case OrderDeliveredEvent delivered:
                Status = OrderState.Delivered;
                DeliveredAtUtc = delivered.DeliveredAtUtc;
                break;

            case OrderCancelledEvent:
                Status = OrderState.Cancelled;
                break;
        }
    }
}
