using KiranaHub.CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KiranaHub.CleanArchitecture.Application.Orders.EventHandlers;

// =========================================================================================
// [TOPIC]: MediatR Notification Handler (Domain Event Listener)
// [PURPOSE]: Asynchronously executes side effects when OrderPlacedDomainEvent is dispatched.
// =========================================================================================

// [KYA KARTA HAI]: MediatR INotificationHandler jo OrderPlacedDomainEvent ko listen karta hai.
// [KYU ZAROORI HAI]: Core domain logic ko external notification / communication side effects se decouple rakhna.
public class OrderPlacedNotificationHandler : INotificationHandler<OrderPlacedDomainEvent>
{
    // [KYA KARTA HAI]: Logger instance.
    private readonly ILogger<OrderPlacedNotificationHandler> _logger;

    // [KYA KARTA HAI]: Dispatched event tracking verification property for test harness.
    public static int DispatchedEventCount { get; private set; }

    // [KYA KARTA HAI]: Constructor DI.
    public OrderPlacedNotificationHandler(ILogger<OrderPlacedNotificationHandler> logger)
    {
        _logger = logger;
    }

    // [KYA KARTA HAI]: Event execution callback method.
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        // [KYA KARTA HAI]: Atomic increment of dispatched count for assertion verification.
        DispatchedEventCount++;

        // [KYA KARTA HAI]: Order placement notification log karta hai.
        _logger.LogInformation("[DOMAIN EVENT RECEIVED] Order {OrderId} placed for Customer {CustomerId} totaling {TotalAmount}",
            notification.OrderId, notification.CustomerId, notification.TotalAmount);

        // [KYA KARTA HAI]: Side effect simulation: send WhatsApp/SMS receipt to customer.
        Console.WriteLine($"   >>> [SIDE EFFECT TRIGGERED] Sent SMS confirmation to customer {notification.CustomerId} for Order {notification.OrderId} ({notification.TotalAmount})");

        // [KYA KARTA HAI]: Completed task return karta hai.
        return Task.CompletedTask;
    }
}

