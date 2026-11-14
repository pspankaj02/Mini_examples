using System.Collections.Concurrent;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Bus;

/// <summary>
/// Thread-Safe Shared Swarm Blackboard & Pub/Sub Message Bus.
/// Enables decentralized multi-agent communication without tight point-to-point coupling.
/// Implements blackboard pattern where agents post state, inspect ambient context, and react asynchronously.
/// </summary>
public sealed class SwarmBlackboardBus
{
    // [KYA KARTA HAI]: Topic prefix se subscriber callback handlers ka concurrent mapping store karta hai.
    // [KYU ZAROORI HAI]: Decentralized routing: agents specific topics (e.g. "weather/*", "inventory/*") subscribe karte hain.
    // [KAISE CHALTA HAI]: ConcurrentDictionary of topic prefix -> thread-safe list of handlers.
    // [OUTPUT / SIDE EFFECT]: In-memory subscriber registry.
    // [VARIATIONS TO TRY]: Priority-ordered handlers with cancellation tokens.
    private readonly ConcurrentDictionary<string, List<Action<AgentMessage>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI]: Blackboard par post kiye gaye messages ka audit trail maintain karta hai.
    // [KYU ZAROORI HAI]: Agents ambient blackboard state inspect kar sakein (e.g. "kya koi CFP pending hai?").
    // [KAISE CHALTA HAI]: ConcurrentQueue of AgentMessage objects.
    // [OUTPUT / SIDE EFFECT]: Chronological message history.
    // [VARIATIONS TO TRY]: Bounded ring buffer with sliding window expiration.
    private readonly ConcurrentQueue<AgentMessage> _history = new();

    // [KYA KARTA HAI]: Global sync object subscriber registration ke liye.
    // [KYU ZAROORI HAI]: Multiple threads simultaneous subscribe karein toh list mutation thread-safe rahe.
    // [KAISE CHALTA HAI]: lock(_subLock).
    // [OUTPUT / SIDE EFFECT]: Mutual exclusion on subscriber list updates.
    // [VARIATIONS TO TRY]: ReaderWriterLockSlim for read-heavy subscriber lookups.
    private readonly object _subLock = new();

    // [KYA KARTA HAI]: Naye topic prefix par subscriber callback register karta hai.
    // [KYU ZAROORI HAI]: Agents ko relevant domain events (e.g. "cnp/proposals", "weather/alerts") receive karana.
    // [KAISE CHALTA HAI]: Topic list find ya create karke action delegate add karta hai.
    // [OUTPUT / SIDE EFFECT]: Handler registered; future messages on topic will trigger action.
    // [VARIATIONS TO TRY]: Pattern matching or regex wildcard topics.
    public void Subscribe(string topicPrefix, Action<AgentMessage> handler)
    {
        lock (_subLock)
        {
            var list = _subscribers.GetOrAdd(topicPrefix, _ => new List<Action<AgentMessage>>());
            list.Add(handler);
        }
    }

    // [KYA KARTA HAI]: Message ko Blackboard par publish karta hai aur matching subscribers ko dispatch karta hai.
    // [KYU ZAROORI HAI]: Swarm me asynchronous, event-driven agent interaction trigger karna.
    // [KAISE CHALTA HAI]: History me append karta hai aur topic match hone wale handlers ko execute karta hai.
    // [OUTPUT / SIDE EFFECT]: All interested agents receive the message.
    // [VARIATIONS TO TRY]: Asynchronous dispatch via Task.Run or Channel<T>.
    public void Publish(AgentMessage message)
    {
        _history.Enqueue(message);

        List<Action<AgentMessage>> matchingHandlers = new();

        lock (_subLock)
        {
            foreach (var (prefix, handlers) in _subscribers)
            {
                string cleanPrefix = prefix.EndsWith("/*") ? prefix[..^2] : (prefix.EndsWith("*") ? prefix[..^1] : prefix);
                if (cleanPrefix == "" || cleanPrefix == "*" || message.Topic.StartsWith(cleanPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    matchingHandlers.AddRange(handlers);
                }
            }
        }

        // Dispatch outside the lock to prevent deadlocks in agent callbacks
        foreach (var handler in matchingHandlers)
        {
            try
            {
                handler(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLACKBOARD ERROR] Handler failed on message {message.MessageId}: {ex.Message}");
            }
        }
    }

    // [KYA KARTA HAI]: Blackboard history se specific type ya topic ke messages query karta hai.
    // [KYU ZAROORI HAI]: Contract Net Protocol me proposals aggregate karna aur consensus check karna.
    // [KAISE CHALTA HAI]: LINQ filtering over concurrent history snapshot.
    // [OUTPUT / SIDE EFFECT]: List of matched AgentMessage items.
    // [VARIATIONS TO TRY]: Indexed queries using secondary indexes by MessageType.
    public IReadOnlyList<AgentMessage> GetHistory(AgentMessageType? type = null, string? topicPrefix = null)
    {
        IEnumerable<AgentMessage> query = _history;

        if (type.HasValue)
        {
            query = query.Where(m => m.Type == type.Value);
        }

        if (!string.IsNullOrEmpty(topicPrefix))
        {
            query = query.Where(m => m.Topic.StartsWith(topicPrefix, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    // [KYA KARTA HAI]: Blackboard par total published messages count return karta hai.
    // [KYU ZAROORI HAI]: Swarm communication activity aur throughput monitor karna.
    // [KAISE CHALTA HAI]: _history.Count inspect karta hai.
    // [OUTPUT / SIDE EFFECT]: Total messages integer.
    // [VARIATIONS TO TRY]: Metrics counter per message type.
    public int TotalMessagesCount => _history.Count;
}
