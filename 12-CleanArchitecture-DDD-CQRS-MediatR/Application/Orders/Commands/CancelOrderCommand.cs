using KiranaHub.CleanArchitecture.Application.Common;
using KiranaHub.CleanArchitecture.Domain.Repositories;
using MediatR;

namespace KiranaHub.CleanArchitecture.Application.Orders.Commands;

// =========================================================================================
// [TOPIC]: CQRS Command - CancelOrderCommand
// [PURPOSE]: Business action executing aggregate cancellation with invariant protections.
// =========================================================================================

// [KYA KARTA HAI]: Order cancellation command definition.
public record CancelOrderCommand(string OrderId, string Reason) : IRequest<Result<bool>>;

// [KYA KARTA HAI]: Command handler for CancelOrderCommand.
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    // [KYA KARTA HAI]: Repository reference.
    private readonly IOrderRepository _orderRepository;

    // [KYA KARTA HAI]: Constructor DI.
    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    // [KYA KARTA HAI]: Handler method executing cancellation.
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        // [KYA KARTA HAI]: OrderId validation check.
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return Result<bool>.Failure("Order ID cannot be empty.");
        }

        // [KYA KARTA HAI]: Repository se order aggregate fetch karta hai.
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        // [KYA KARTA HAI]: Check karta hai ki order mila ya nahi.
        if (order == null)
        {
            return Result<bool>.Failure($"Order '{request.OrderId}' not found.");
        }

        // [KYA KARTA HAI]: Domain method invoke karta hai aggregate invariants protect karne ke liye.
        try
        {
            // [KYA KARTA HAI]: Order aggregate ka business method call karta hai.
            order.Cancel(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            // [KYA KARTA HAI]: Domain invariant violation ko Result.Failure me translate karta hai.
            return Result<bool>.Failure(ex.Message);
        }

        // [KYA KARTA HAI]: State change ko repository me save karta hai.
        await _orderRepository.SaveAsync(order, cancellationToken);

        // [KYA KARTA HAI]: Operation success return karta hai.
        return Result<bool>.Success(true);
    }
}

