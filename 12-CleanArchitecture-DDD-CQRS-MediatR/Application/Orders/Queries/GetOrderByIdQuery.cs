using KiranaHub.CleanArchitecture.Domain.Repositories;
using MediatR;

namespace KiranaHub.CleanArchitecture.Application.Orders.Queries;

// =========================================================================================
// [TOPIC]: CQRS Query - GetOrderByIdQuery
// [PURPOSE]: Segregated read model query returning projection DTOs without domain behavior.
// =========================================================================================

// [KYA KARTA HAI]: Query response line item DTO.
public record OrderItemDto(string Sku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

// [KYA KARTA HAI]: Query response Order DTO.
public record OrderDto(
    string OrderId,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAtUtc,
    List<OrderItemDto> Items
);

// [KYA KARTA HAI]: MediatR Query request definition returning nullable OrderDto.
public record GetOrderByIdQuery(string OrderId) : IRequest<OrderDto?>;

// [KYA KARTA HAI]: Query handler for GetOrderByIdQuery.
public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    // [KYA KARTA HAI]: Repository reference.
    private readonly IOrderRepository _orderRepository;

    // [KYA KARTA HAI]: Constructor DI.
    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    // [KYA KARTA HAI]: Query processing and DTO projection.
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // [KYA KARTA HAI]: Repository se order aggregate load karta hai.
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        // [KYA KARTA HAI]: Check karta hai ki order mila ya nahi.
        if (order == null) return null;

        // [KYA KARTA HAI]: Domain items ko projection DTO list me map karta hai.
        var itemDtos = order.Items.Select(i => new OrderItemDto(
            i.Sku,
            i.ProductName,
            i.UnitPrice.Amount,
            i.Quantity,
            i.LineTotal.Amount
        )).ToList();

        // [KYA KARTA HAI]: Clean OrderDto construct karke return karta hai.
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAtUtc,
            itemDtos
        );
    }
}

