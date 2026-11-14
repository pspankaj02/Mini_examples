using KiranaHub.CleanArchitecture.Domain.Entities;

namespace KiranaHub.CleanArchitecture.Domain.Repositories;

// =========================================================================================
// [TOPIC]: DDD Order Repository Contract
// [PURPOSE]: Persistence abstraction decoupled from specific database technology.
// =========================================================================================

// [KYA KARTA HAI]: Order aggregate root ke persistence operations define karta hai.
// [KYU ZAROORI HAI]: Dependency Inversion Principle: Domain layer defines contract; Infrastructure implements it.
public interface IOrderRepository
{
    // [KYA KARTA HAI]: Order aggregate ko by ID retrieve karta hai.
    Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default);

    // [KYA KARTA HAI]: Order aggregate aur uske domain events ko persist/dispatch karta hai.
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}

