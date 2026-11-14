// [KYA KARTA HAI]: Messages namespace import karta hai event records access karne ke liye.
// [KYU ZAROORI HAI]: Saga state machine ko events bind karne ke liye.
// [KAISE CHALTA HAI]: Using directive.
// [OUTPUT / SIDE EFFECT]: Event symbols available.
// [VARIATIONS TO TRY]: External Contracts library.
using KiranaHub.EventDrivenSaga.Messages;

// [KYA KARTA HAI]: Namespace define karta hai Saga Orchestration engine ke liye.
// [KYU ZAROORI HAI]: Distributed Saga state management encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Saga namespace.
// [VARIATIONS TO TRY]: MassTransit.SagaStateMachine base class.
namespace KiranaHub.EventDrivenSaga.Saga;

// [KYA KARTA HAI]: Saga lifecycle states enum define karta hai.
// [KYU ZAROORI HAI]: Distributed order ke exact state ko track karne ke liye across microservices.
// [KAISE CHALTA HAI]: Enum values.
// [OUTPUT / SIDE EFFECT]: SagaState enum.
// [VARIATIONS TO TRY]: String state names for database readability.
public enum SagaState
{
    Submitted,
    AwaitingInventory,
    AwaitingPayment,
    Completed,
    Compensating,
    Cancelled
}

// [KYA KARTA HAI]: Saga instance state data container class.
// [KYU ZAROORI HAI]: Distributed transaction ke intermediate state aur correlation ID ko persist karne ke liye.
// [KAISE CHALTA HAI]: Properties holding order metadata and current state.
// [OUTPUT / SIDE EFFECT]: OrderSagaInstance class.
// [VARIATIONS TO TRY]: Version field for optimistic concurrency.
public class OrderSagaInstance
{
    public string CorrelationId { get; set; } = string.Empty; // Matches OrderId
    public SagaState CurrentState { get; set; } = SagaState.Submitted;
    public decimal TotalAmount { get; set; }
    public string? DarkStoreId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

// [KYA KARTA HAI]: Distributed Saga State Machine orchestrator.
// [KYU ZAROORI HAI]: Multi-step asynchronous operations coordinate karna aur failure aane par automatic compensating rollback execute karna.
// [KAISE CHALTA HAI]: In-memory dictionary me saga instances maintain karta hai aur events receive karke state advance karta hai.
// [OUTPUT / SIDE EFFECT]: OrderSagaStateMachine class.
// [VARIATIONS TO TRY]: MassTransit Automatonymous state machine implementation.
public class OrderSagaStateMachine
{
    private readonly Dictionary<string, OrderSagaInstance> _sagas = new();
    private readonly object _lock = new();

    // [KYA KARTA HAI]: OrderPlacedEvent handle karta hai aur saga initiate karta hai.
    // [KYU ZAROORI HAI]: Saga instance create karke state ko AwaitingInventory transition karne ke liye.
    // [KAISE CHALTA HAI]: Naya OrderSagaInstance create karke dictionary me save karta hai.
    // [OUTPUT / SIDE EFFECT]: Saga created in AwaitingInventory state.
    // [VARIATIONS TO TRY]: Emit ReserveInventoryCommand to message queue.
    public OrderSagaInstance Handle(OrderPlacedEvent @event)
    {
        lock (_lock)
        {
            var saga = new OrderSagaInstance
            {
                CorrelationId = @event.OrderId,
                TotalAmount = @event.TotalAmount,
                CurrentState = SagaState.AwaitingInventory,
                CreatedAtUtc = @event.PlacedAtUtc
            };
            _sagas[@event.OrderId] = saga;
            return saga;
        }
    }

    // [KYA KARTA HAI]: InventoryReservedEvent handle karta hai.
    // [KYU ZAROORI HAI]: Stock confirm hone ke baad state ko AwaitingPayment me move karne ke liye.
    // [KAISE CHALTA HAI]: Saga lookup karke DarkStoreId assign karta hai aur state update karta hai.
    // [OUTPUT / SIDE EFFECT]: State becomes AwaitingPayment.
    // [VARIATIONS TO TRY]: Emit ProcessPaymentCommand.
    public OrderSagaInstance Handle(InventoryReservedEvent @event)
    {
        lock (_lock)
        {
            if (!_sagas.TryGetValue(@event.OrderId, out var saga))
                throw new InvalidOperationException($"Saga instance not found for order: {@event.OrderId}");

            saga.DarkStoreId = @event.DarkStoreId;
            saga.CurrentState = SagaState.AwaitingPayment;
            return saga;
        }
    }

    // [KYA KARTA HAI]: PaymentCompletedEvent handle karta hai (Happy Path completion).
    // [KYU ZAROORI HAI]: Money capture hone par order lifecycle ko successfully complete karne ke liye.
    // [KAISE CHALTA HAI]: CurrentState ko Completed mark karta hai with completion timestamp.
    // [OUTPUT / SIDE EFFECT]: Saga becomes Completed.
    // [VARIATIONS TO TRY]: Trigger rider dispatch notification event.
    public OrderSagaInstance Handle(PaymentCompletedEvent @event)
    {
        lock (_lock)
        {
            if (!_sagas.TryGetValue(@event.OrderId, out var saga))
                throw new InvalidOperationException($"Saga instance not found for order: {@event.OrderId}");

            saga.CurrentState = SagaState.Completed;
            saga.CompletedAtUtc = DateTime.UtcNow;
            return saga;
        }
    }

    // [KYA KARTA HAI]: InventoryExhaustedEvent handle karta hai aur COMPENSATING TRANSACTION execute karta hai.
    // [KYU ZAROORI HAI]: Out-of-stock hone par automatic rollback (auto-refund & cancellation) coordinate karne ke liye.
    // [KAISE CHALTA HAI]: State ko Compensating me transition karta hai, cancellation reason attach karta hai, aur Cancelled finalize karta hai.
    // [OUTPUT / SIDE EFFECT]: Saga rolls back and transitions to Cancelled.
    // [VARIATIONS TO TRY]: Emit RefundPaymentCommand to payment gateway.
    public OrderSagaInstance Handle(InventoryExhaustedEvent @event)
    {
        lock (_lock)
        {
            if (!_sagas.TryGetValue(@event.OrderId, out var saga))
                throw new InvalidOperationException($"Saga instance not found for order: {@event.OrderId}");

            // Step 1: Enter Compensating state
            saga.CurrentState = SagaState.Compensating;
            saga.FailureReason = $"Inventory exhausted for item: {@event.UnavailableProductId}. Reason: {@event.Reason}";

            // Step 2: Compensate & finalize Cancelled state
            saga.CurrentState = SagaState.Cancelled;
            saga.CompletedAtUtc = DateTime.UtcNow;
            return saga;
        }
    }

    // [KYA KARTA HAI]: Saga instance retrieve karta hai by OrderId.
    // [KYU ZAROORI HAI]: Verification aur status inspection ke liye.
    // [KAISE CHALTA HAI]: Dictionary lookup.
    // [OUTPUT / SIDE EFFECT]: OrderSagaInstance or null.
    // [VARIATIONS TO TRY]: Readonly copy return karna.
    public OrderSagaInstance? GetSaga(string orderId)
    {
        lock (_lock)
        {
            return _sagas.TryGetValue(orderId, out var saga) ? saga : null;
        }
    }
}
