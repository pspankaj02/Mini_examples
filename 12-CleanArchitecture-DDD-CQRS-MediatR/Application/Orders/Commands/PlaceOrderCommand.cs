using KiranaHub.CleanArchitecture.Application.Common;
using KiranaHub.CleanArchitecture.Domain.Entities;
using KiranaHub.CleanArchitecture.Domain.Repositories;
using KiranaHub.CleanArchitecture.Domain.ValueObjects;
using MediatR;

namespace KiranaHub.CleanArchitecture.Application.Orders.Commands;

// =========================================================================================
// [TOPIC]: CQRS Command - PlaceOrderCommand
// [PURPOSE]: Write operation creating a new grocery order aggregate.
// =========================================================================================

// [KYA KARTA HAI]: PlaceOrder command ka line item input DTO.
public record PlaceOrderItemDto(string Sku, string ProductName, decimal UnitPrice, int Quantity);

// [KYA KARTA HAI]: MediatR Command request jo Result<string> (OrderId) return karta hai.
public record PlaceOrderCommand(string CustomerId, List<PlaceOrderItemDto> Items) : IRequest<Result<string>>;

// [KYA KARTA HAI]: PlaceOrderCommand ka MediatR request handler.
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<string>>
{
    // [KYA KARTA HAI]: Injected domain repository interface.
    private readonly IOrderRepository _orderRepository;

    // [KYA KARTA HAI]: Constructor DI.
    public PlaceOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    // [KYA KARTA HAI]: Command execution logic.
    public async Task<Result<string>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // ---------------------------------------------------------------------------------
        // 1. INPUT VALIDATION
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Customer ID validation check.
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return Result<string>.Failure("Customer ID cannot be empty.");
        }

        // [KYA KARTA HAI]: Items collection check.
        if (request.Items == null || request.Items.Count == 0)
        {
            return Result<string>.Failure("An order must contain at least one item.");
        }

        // ---------------------------------------------------------------------------------
        // 2. DOMAIN MODEL MAPPING & AGGREGATE CREATION
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: DTOs ko DDD OrderItem entities me convert karta hai using Money Value Objects.
        var domainItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            // [KYA KARTA HAI]: Money Value Object create karta hai for unit price.
            var money = new Money(item.UnitPrice, "INR");
            // [KYA KARTA HAI]: OrderItem entity create karta hai.
            domainItems.Add(new OrderItem(item.Sku, item.ProductName, money, item.Quantity));
        }

        // [KYA KARTA HAI]: Order aggregate root create karta hai via factory method (raises OrderPlacedDomainEvent).
        var order = Order.Create(request.CustomerId, domainItems);

        // ---------------------------------------------------------------------------------
        // 3. PERSISTENCE & DOMAIN EVENT DISPATCH
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Repository me order save karta hai (and dispatches pending domain events).
        await _orderRepository.SaveAsync(order, cancellationToken);

        // [KYA KARTA HAI]: Generated Order ID return karta hai wrapped in Result.Success.
        return Result<string>.Success(order.Id);
    }
}

