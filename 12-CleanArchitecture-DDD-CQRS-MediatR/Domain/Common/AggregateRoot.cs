using MediatR;

namespace KiranaHub.CleanArchitecture.Domain.Common;

// =========================================================================================
// [TOPIC]: Domain Events & Aggregate Root (Domain-Driven Design)
// [PURPOSE]: Encapsulate state changes and decouple domain logic from side effects.
// =========================================================================================

// [KYA KARTA HAI]: Domain Event marker interface jo MediatR ke INotification se inherit karta hai.
// [KYU ZAROORI HAI]: Aggregate ke andar occur hone wale important business events ko notify karna.
// [KAISE CHALTA HAI]: MediatR.Publish() ke zariye event dispatch hota hai multiple in-memory handlers ko.
public interface IDomainEvent : INotification
{
    // [KYA KARTA HAI]: Event generate hone ka exact UTC timestamp capture karta hai.
    // [KYU ZAROORI HAI]: Event auditing, event sourcing, aur sequence tracking ke liye timestamp zaroori hai.
    DateTime OccurredOnUtc { get; }
}

// [KYA KARTA HAI]: DDD Aggregate Root base class jo uncommitted domain events collect karta hai.
// [KYU ZAROORI HAI]: Consistency boundary: Aggregate state change karta hai aur events queue karta hai; repo commit par events publish hote hain.
public abstract class AggregateRoot
{
    // [KYA KARTA HAI]: Uncommitted domain events ki internal list maintain karta hai.
    private readonly List<IDomainEvent> _domainEvents = new();

    // [KYA KARTA HAI]: Read-only view expose karta hai domain events collection ka.
    // [KYU ZAROORI HAI]: External callers (dispatchers/repos) events inspect kar sakein bina collection ko bahar se modify kiye.
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // [KYA KARTA HAI]: Naya domain event aggregate ki internal list me append karta hai.
    // [KYU ZAROORI HAI]: Aggregate methods (e.g. Order.PlaceOrder) business decision lene ke baad event register kar sakein.
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        // [KYA KARTA HAI]: Event ko internal list me add karta hai.
        _domainEvents.Add(domainEvent);
    }

    // [KYA KARTA HAI]: Domain events list ko empty karta hai dispatch hone ke baad.
    // [KYU ZAROORI HAI]: Prevent duplicate event dispatch (idempotency guarantee).
    public void ClearDomainEvents()
    {
        // [KYA KARTA HAI]: Internal list ko clear karta hai.
        _domainEvents.Clear();
    }
}

