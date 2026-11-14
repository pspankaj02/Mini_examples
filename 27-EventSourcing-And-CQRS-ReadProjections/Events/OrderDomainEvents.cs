// ==============================================================================================
// KiranaHub Event Sourcing: Immutable Domain Events for Order Aggregate Stream
// [KYA KARTA HAI] Order lifecycle me hone wale har ek fact/change ko immutable domain events me represent karta hai.
// [KYU ZAROORI HAI] State ko overwrite (destructive SQL UPDATE) karne ke bajaye business events ka audit history banana.
// [KAISE CHALTA HAI] C# 13 immutable records implementing IDomainEvent with unique event timestamps and stream versions.
// [OUTPUT / SIDE EFFECT] Append-only event payload ready for Event Store persistence.
// [VARIATIONS TO TRY] Protobuf or Avro binary serialization for event archiving.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, DateTimeOffset types provide karna.
// [KAISE CHALTA HAI] Core CLR types loaded.
// [OUTPUT / SIDE EFFECT] Core types available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Namespace declare karta hai domain events ke liye.
// [KYU ZAROORI HAI] Clean architectural separation of event contracts.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.Events.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Domain.Events.
namespace KiranaHub.EventSourcing.Events;

// [KYA KARTA HAI] Domain events ka base contract interface define karta hai.
// [KYU ZAROORI HAI] Sabhi events ko EventStore aur Read Projections me uniformly handle karne ke liye.
// [KAISE CHALTA HAI] Properties define contract: EventId, AggregateId, OccurredOnUtc, Version.
// [OUTPUT / SIDE EFFECT] Base contract interface.
// [VARIATIONS TO TRY] Add CorrelationId and CausationId properties for tracing.
public interface IDomainEvent
{
    // [KYA KARTA HAI] Event ka globally unique identifier.
    // [KYU ZAROORI HAI] Deduplication aur idempotent message processing ensure karna.
    // [KAISE CHALTA HAI] Guid generated on creation.
    // [OUTPUT / SIDE EFFECT] Unique Guid.
    // [VARIATIONS TO TRY] Ulid for time-sortable IDs.
    Guid EventId { get; }

    // [KYA KARTA HAI] Target aggregate root ka identifier.
    // [KYU ZAROORI HAI] Batata hai ki yeh event kis Order stream me append hoga.
    // [KAISE CHALTA HAI] Matches Order aggregate Guid.
    // [OUTPUT / SIDE EFFECT] Aggregate ID.
    // [VARIATIONS TO TRY] String aggregate ID.
    Guid AggregateId { get; }

    // [KYA KARTA HAI] Event occur hone ka UTC wall-clock timestamp.
    // [KYU ZAROORI HAI] Time-travel debugging aur audit chronological ordering.
    // [KAISE CHALTA HAI] High precision DateTimeOffset.
    // [OUTPUT / SIDE EFFECT] Timestamp offset.
    // [VARIATIONS TO TRY] Unix epoch milliseconds.
    DateTimeOffset OccurredOnUtc { get; }

    // [KYA KARTA HAI] Stream version index number.
    // [KYU ZAROORI HAI] Optimistic concurrency version checking (1, 2, 3...).
    // [KAISE CHALTA HAI] Integer sequence in the stream.
    // [OUTPUT / SIDE EFFECT] Version number.
    // [VARIATIONS TO TRY] Long sequence.
    int Version { get; init; }

    // [KYA KARTA HAI] Version assigned copy create karne ka method.
    // [KYU ZAROORI HAI] Interface reference se immutable versioned record generate karna.
    // [KAISE CHALTA HAI] Record with-expression returns updated instance.
    // [OUTPUT / SIDE EFFECT] Versioned IDomainEvent.
    // [VARIATIONS TO TRY] Set Version property directly if mutable.
    IDomainEvent WithVersion(int version);
}

// [KYA KARTA HAI] Order creation domain event record.
// [KYU ZAROORI HAI] Order stream ka initial event (Genesis event) represent karna.
// [KAISE CHALTA HAI] Captured when customer initiates checkout.
// [OUTPUT / SIDE EFFECT] OrderCreatedEvent instance.
// [VARIATIONS TO TRY] Include customer loyalty tier.
public record OrderCreatedEvent(
    Guid EventId,
    Guid AggregateId,
    string CustomerId,
    string DarkStoreId,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Item addition domain event record.
// [KYU ZAROORI HAI] Cart me grocery product add hone ka audit fact record karna.
// [KAISE CHALTA HAI] Contains SKU, Name, Quantity, and UnitPrice.
// [OUTPUT / SIDE EFFECT] ItemAddedEvent instance.
// [VARIATIONS TO TRY] Add tax rate per item.
public record ItemAddedEvent(
    Guid EventId,
    Guid AggregateId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Stock allocation domain event record.
// [KYU ZAROORI HAI] Dark store inventory physical reservation fact record karna.
// [KAISE CHALTA HAI] Emitted when warehouse picks inventory.
// [OUTPUT / SIDE EFFECT] StockAllocatedEvent instance.
// [VARIATIONS TO TRY] Include bin/aisle location in warehouse.
public record StockAllocatedEvent(
    Guid EventId,
    Guid AggregateId,
    string DarkStoreId,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Payment captured domain event record.
// [KYU ZAROORI HAI] Successful financial transaction fact record karna.
// [KAISE CHALTA HAI] Emitted when payment gateway authorizes and charges amount.
// [OUTPUT / SIDE EFFECT] PaymentCapturedEvent instance.
// [VARIATIONS TO TRY] Include payment method enum (UPI, Card).
public record PaymentCapturedEvent(
    Guid EventId,
    Guid AggregateId,
    string PaymentTransactionId,
    decimal Amount,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Rider dispatched domain event record.
// [KYU ZAROORI HAI] Order delivery partner ke sath dark store se nikalne ka fact record karna.
// [KAISE CHALTA HAI] Emitted when rider picks up bag.
// [OUTPUT / SIDE EFFECT] RiderDispatchedEvent instance.
// [VARIATIONS TO TRY] Include estimated arrival time.
public record RiderDispatchedEvent(
    Guid EventId,
    Guid AggregateId,
    string RiderId,
    string RiderName,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Order delivered domain event record.
// [KYU ZAROORI HAI] Customer doorstep handover fact record karna.
// [KAISE CHALTA HAI] Emitted when rider enters customer OTP.
// [OUTPUT / SIDE EFFECT] OrderDeliveredEvent instance.
// [VARIATIONS TO TRY] Include customer signature image hash.
public record OrderDeliveredEvent(
    Guid EventId,
    Guid AggregateId,
    DateTimeOffset DeliveredAtUtc,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}

// [KYA KARTA HAI] Order cancelled domain event record.
// [KYU ZAROORI HAI] Order cancellation reason and timestamp record karna.
// [KAISE CHALTA HAI] Emitted when customer or system cancels order.
// [OUTPUT / SIDE EFFECT] OrderCancelledEvent instance.
// [VARIATIONS TO TRY] Include refund authorization flag.
public record OrderCancelledEvent(
    Guid EventId,
    Guid AggregateId,
    string Reason,
    DateTimeOffset OccurredOnUtc,
    int Version = 0
) : IDomainEvent
{
    public IDomainEvent WithVersion(int version) => this with { Version = version };
}
