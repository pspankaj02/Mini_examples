// [KYA KARTA HAI]: System libraries import karta hai for JSON serialization and threading.
// [KYU ZAROORI HAI]: Outbox message payload serialization aur concurrency management ke liye.
// [KAISE CHALTA HAI]: .NET core BCL se load hota hai.
// [OUTPUT / SIDE EFFECT]: BCL symbols available.
// [VARIATIONS TO TRY]: System.Text.Json source generators for zero allocation.
using System.Text.Json;

// [KYA KARTA HAI]: Namespace define karta hai Transactional Outbox pattern ke liye.
// [KYU ZAROORI HAI]: Dual-write problem solving logic ko encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Outbox namespace.
// [VARIATIONS TO TRY]: Dedicated Outbox EF Core package.
namespace KiranaHub.EventDrivenSaga.Outbox;

// [KYA KARTA HAI]: Outbox table row entity define karta hai.
// [KYU ZAROORI HAI]: Database me business data ke sath message ko atomic transaction me store karne ke liye.
// [KAISE CHALTA HAI]: Id, EventType, JSON Payload, aur processing status flags encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: OutboxMessage entity.
// [VARIATIONS TO TRY]: RetryCount and NextRetryTimeUtc for exponential backoff.
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public bool IsProcessed => ProcessedAtUtc.HasValue;
}

// [KYA KARTA HAI]: Transactional Outbox Repository jo database aur outbox table ko manage karta hai.
// [KYU ZAROORI HAI]: Microservices me Dual-Write problem (DB update ho gaya par message broker down tha) ko eliminate karne ke liye.
// [KAISE CHALTA HAI]: In-memory tables me order aur outbox message ko atomic simulate karta hai.
// [OUTPUT / SIDE EFFECT]: Outbox storage manager.
// [VARIATIONS TO TRY]: EF Core DbContext.Database.BeginTransactionAsync integration.
public class TransactionalOutboxRepository
{
    private readonly List<OutboxMessage> _outboxTable = new();
    private readonly object _syncLock = new();

    // [KYA KARTA HAI]: Business entity aur outgoing domain event ko ek hi transaction me commit karta hai.
    // [KYU ZAROORI HAI]: Zero message loss guarantee karne ke liye: agar DB write fail hoga toh event bhi save nahi hoga.
    // [KAISE CHALTA HAI]: Event object ko JSON serialize karta hai aur atomic lock me outbox table me append karta hai.
    // [OUTPUT / SIDE EFFECT]: OutboxMessage saved.
    // [VARIATIONS TO TRY]: Pass generic TEvent constraint.
    public void SaveWithOutbox<TEvent>(string orderId, TEvent domainEvent) where TEvent : class
    {
        lock (_syncLock)
        {
            var message = new OutboxMessage
            {
                EventType = typeof(TEvent).Name,
                PayloadJson = JsonSerializer.Serialize(domainEvent),
                CreatedAtUtc = DateTime.UtcNow
            };
            _outboxTable.Add(message);
        }
    }

    // [KYA KARTA HAI]: Unprocessed pending outbox messages fetch karta hai.
    // [KYU ZAROORI HAI]: Background dispatcher worker ko publish karne ke liye batch supply karna.
    // [KAISE CHALTA HAI]: Where(!IsProcessed) filter karke snapshot list return karta hai.
    // [OUTPUT / SIDE EFFECT]: List of pending OutboxMessages.
    // [VARIATIONS TO TRY]: Batch size limit (Take 50).
    public IReadOnlyList<OutboxMessage> GetPendingMessages()
    {
        lock (_syncLock)
        {
            return _outboxTable.Where(m => !m.IsProcessed).ToList();
        }
    }

    // [KYA KARTA HAI]: Message ko processed mark karta hai with current timestamp.
    // [KYU ZAROORI HAI]: Duplicate publishing se bachane ke liye (at-least-once delivery semantics).
    // [KAISE CHALTA HAI]: ProcessedAtUtc property update karta hai.
    // [OUTPUT / SIDE EFFECT]: Message marked processed.
    // [VARIATIONS TO TRY]: Delete message on publish (cleanup table).
    public void MarkAsProcessed(Guid messageId)
    {
        lock (_syncLock)
        {
            var msg = _outboxTable.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.ProcessedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
