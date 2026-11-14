// ==============================================================================================
// KiranaHub Event Sourcing: CQRS Materialized Read Model Projection
// [KYA KARTA HAI] Event store ke raw events ko asynchronous background projection se query-optimized Read Model me transform karta hai.
// [KYU ZAROORI HAI] Heavy event stream replay ke bina customer app par instant (O(1)) order dashboards aur statistics render karna.
// [KAISE CHALTA HAI] Event store subscribers me hook hokar har naye event ke aane par denormalized OrderSummaryView update karta hai.
// [OUTPUT / SIDE EFFECT] Fast, denormalized read-only projections.
// [VARIATIONS TO TRY] Projecting into Elasticsearch or Redis JSON.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Guid, DateTimeOffset types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, IEnumerable collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] ConcurrentDictionary thread-safe read model storage ke liye.
// [KAISE CHALTA HAI] Multi-threaded dictionary loaded.
// [OUTPUT / SIDE EFFECT] Lock-free fast read access.
// [VARIATIONS TO TRY] ConcurrentBag.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Read model filtering queries (Where, ToList).
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators available.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task async projection processing ke liye.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Domain events namespace import karta hai.
// [KYU ZAROORI HAI] IDomainEvent references.
// [KAISE CHALTA HAI] Local event namespace loaded.
// [OUTPUT / SIDE EFFECT] Event contracts accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Events;

// [KYA KARTA HAI] Event store storage namespace import karta hai.
// [KYU ZAROORI HAI] IEventStore reference ke liye.
// [KAISE CHALTA HAI] References local storage namespace.
// [OUTPUT / SIDE EFFECT] Event store types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Storage;

// [KYA KARTA HAI] Namespace declare karta hai read model projections ke liye.
// [KYU ZAROORI HAI] CQRS Query side separation.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.Projections.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.ReadModels.
namespace KiranaHub.EventSourcing.Projections;

// [KYA KARTA HAI] Denormalized query-optimized Order Summary View record.
// [KYU ZAROORI HAI] Single database row/object me customer dashboard ka saara data provide karna.
// [KAISE CHALTA HAI] Mutable/immutable view updated progressively as events arrive.
// [OUTPUT / SIDE EFFECT] Queryable DTO record.
// [VARIATIONS TO TRY] Class with EF Core mapping.
public record OrderSummaryView(
    Guid OrderId,
    string CustomerId,
    string DarkStoreId,
    int ItemCount,
    decimal TotalAmount,
    string Status,
    string? RiderName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastUpdatedUtc,
    DateTimeOffset? DeliveredAtUtc = null
);

// [KYA KARTA HAI] Asynchronous CQRS Read Model Projection class.
// [KYU ZAROORI HAI] Event Store ke events ko consume karke OrderSummaryView cache update karna.
// [KAISE CHALTA HAI] Subscribes to IEventStore and handles each event type via pattern matching.
// [OUTPUT / SIDE EFFECT] Fast in-memory materialized view.
// [VARIATIONS TO TRY] Rebuild projection from scratch by replaying entire event store history.
public class OrderSummaryReadModelProjection
{
    // [KYA KARTA HAI] Materialized views storage map: OrderId -> OrderSummaryView.
    // [KYU ZAROORI HAI] Sub-millisecond read queries serve karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary provides thread-safe access.
    // [OUTPUT / SIDE EFFECT] Materialized view table in memory.
    // [VARIATIONS TO TRY] Redis Hash.
    private readonly ConcurrentDictionary<Guid, OrderSummaryView> _views = new();

    // [KYA KARTA HAI] Projection constructor initialize karta hai.
    // [KYU ZAROORI HAI] Event store ke append notification stream se projection ko subscribe karna.
    // [KAISE CHALTA HAI] Calls eventStore.Subscribe(HandleEventAsync).
    // [OUTPUT / SIDE EFFECT] Real-time event subscription established.
    // [VARIATIONS TO TRY] Polling event checkpoint.
    public OrderSummaryReadModelProjection(IEventStore eventStore)
    {
        eventStore.Subscribe(HandleEventAsync);
    }

    // [KYA KARTA HAI] Individual domain event ko handle karke materialized view update karta hai.
    // [KYU ZAROORI HAI] Incremental view updates (Event handler).
    // [KAISE CHALTA HAI] Switch statement over IDomainEvent types updating dictionary entries.
    // [OUTPUT / SIDE EFFECT] Updates or inserts view record.
    // [VARIATIONS TO TRY] Idempotency check via event version.
    public Task HandleEventAsync(IDomainEvent evt)
    {
        switch (evt)
        {
            case OrderCreatedEvent created:
                _views[created.AggregateId] = new OrderSummaryView(
                    OrderId: created.AggregateId,
                    CustomerId: created.CustomerId,
                    DarkStoreId: created.DarkStoreId,
                    ItemCount: 0,
                    TotalAmount: 0m,
                    Status: "Created",
                    RiderName: null,
                    CreatedAtUtc: created.OccurredOnUtc,
                    LastUpdatedUtc: created.OccurredOnUtc
                );
                break;

            case ItemAddedEvent itemAdded:
                if (_views.TryGetValue(itemAdded.AggregateId, out var existingView))
                {
                    _views[itemAdded.AggregateId] = existingView with
                    {
                        ItemCount = existingView.ItemCount + itemAdded.Quantity,
                        TotalAmount = existingView.TotalAmount + (itemAdded.Quantity * itemAdded.UnitPrice),
                        LastUpdatedUtc = itemAdded.OccurredOnUtc
                    };
                }
                break;

            case StockAllocatedEvent stock:
                if (_views.TryGetValue(stock.AggregateId, out var stockView))
                {
                    _views[stock.AggregateId] = stockView with
                    {
                        Status = "StockAllocated",
                        LastUpdatedUtc = stock.OccurredOnUtc
                    };
                }
                break;

            case PaymentCapturedEvent payment:
                if (_views.TryGetValue(payment.AggregateId, out var payView))
                {
                    _views[payView.OrderId] = payView with
                    {
                        Status = "Paid",
                        LastUpdatedUtc = payment.OccurredOnUtc
                    };
                }
                break;

            case RiderDispatchedEvent dispatched:
                if (_views.TryGetValue(dispatched.AggregateId, out var dispatchView))
                {
                    _views[dispatchView.OrderId] = dispatchView with
                    {
                        Status = "Dispatched",
                        RiderName = dispatched.RiderName,
                        LastUpdatedUtc = dispatched.OccurredOnUtc
                    };
                }
                break;

            case OrderDeliveredEvent delivered:
                if (_views.TryGetValue(delivered.AggregateId, out var delView))
                {
                    _views[delView.OrderId] = delView with
                    {
                        Status = "Delivered",
                        DeliveredAtUtc = delivered.DeliveredAtUtc,
                        LastUpdatedUtc = delivered.OccurredOnUtc
                    };
                }
                break;

            case OrderCancelledEvent cancelled:
                if (_views.TryGetValue(cancelled.AggregateId, out var cancelView))
                {
                    _views[cancelView.OrderId] = cancelView with
                    {
                        Status = $"Cancelled ({cancelled.Reason})",
                        LastUpdatedUtc = cancelled.OccurredOnUtc
                    };
                }
                break;
        }

        return Task.CompletedTask;
    }

    // [KYA KARTA HAI] Order ID se materialized view query karta hai.
    // [KYU ZAROORI HAI] Single order screen ke liye instant O(1) read.
    // [KAISE CHALTA HAI] Dictionary TryGetValue.
    // [OUTPUT / SIDE EFFECT] OrderSummaryView or null.
    // [VARIATIONS TO TRY] Result<OrderSummaryView>.
    public OrderSummaryView? GetById(Guid orderId)
    {
        _views.TryGetValue(orderId, out var view);
        return view;
    }

    // [KYA KARTA HAI] Customer ID se customer ke saare orders return karta hai.
    // [KYU ZAROORI HAI] Customer order history screen render karna.
    // [KAISE CHALTA HAI] LINQ Where on _views.Values.
    // [OUTPUT / SIDE EFFECT] List of OrderSummaryView.
    // [VARIATIONS TO TRY] Pagination parameters.
    public IReadOnlyList<OrderSummaryView> GetByCustomer(string customerId)
    {
        return _views.Values
            .Where(v => v.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToList();
    }
}
