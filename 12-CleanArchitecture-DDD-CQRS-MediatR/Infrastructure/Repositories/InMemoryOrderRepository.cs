using System.Collections.Concurrent;
using KiranaHub.CleanArchitecture.Domain.Entities;
using KiranaHub.CleanArchitecture.Domain.Repositories;
using MediatR;

namespace KiranaHub.CleanArchitecture.Infrastructure.Repositories;

// =========================================================================================
// [TOPIC]: Infrastructure Repository & Domain Event Dispatcher
// [PURPOSE]: Thread-safe in-memory aggregate persistence and automatic domain event publishing.
// =========================================================================================

// [KYA KARTA HAI]: IOrderRepository ka in-memory implementation jo MediatR ke zariye domain events dispatch karta hai.
// [KYU ZAROORI HAI]: Fast testing of Clean Architecture without configuring external database engines.
public class InMemoryOrderRepository : IOrderRepository
{
    // [KYA KARTA HAI]: Thread-safe in-memory storage dictionary (OrderId -> Order Aggregate).
    private static readonly ConcurrentDictionary<string, Order> Storage = new();

    // [KYA KARTA HAI]: Injected MediatR IPublisher interface to dispatch domain events.
    private readonly IPublisher _publisher;

    // [KYA KARTA HAI]: Constructor DI.
    public InMemoryOrderRepository(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    // [KYA KARTA HAI]: Order aggregate ko by ID retrieve karta hai.
    public Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        // [KYA KARTA HAI]: Dictionary se lookup karta hai.
        Storage.TryGetValue(orderId, out var order);

        // [KYA KARTA HAI]: Result task wrap karke return karta hai.
        return Task.FromResult(order);
    }

    // [KYA KARTA HAI]: Order aggregate ko persist karta hai aur uske pending domain events dispatch karta hai.
    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        // [KYA KARTA HAI]: Order aggregate ko dictionary me insert/update karta hai.
        Storage[order.Id] = order;

        // [KYA KARTA HAI]: Aggregate ke uncommitted domain events capture karta hai.
        var pendingEvents = order.DomainEvents.ToList();

        // [KYA KARTA HAI]: Aggregate ke domain events clear karta hai before dispatching (preventing duplicate triggers).
        order.ClearDomainEvents();

        // [KYA KARTA HAI]: Pending events par loop chala kar MediatR par publish karta hai.
        foreach (var domainEvent in pendingEvents)
        {
            // [KYA KARTA HAI]: MediatR ke zariye in-memory notification broadcast karta hai.
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

