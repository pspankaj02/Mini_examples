namespace KiranaHub.WarehouseIoT.Mqtt;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

// [KYA KARTA HAI]: Dark-Store warehouse ke liye lightweight MQTT 5.0 Edge Broker implement karta hai.
// [KYU ZAROORI HAI]: IoT temperature sensors, smart scales, barcode conveyor belts, aur AGV robots ke sath low-overhead pub/sub communication enable karna.
// [KAISE CHALTA HAI]: Hierarchical topics (e.g. "kirana/edge/darkstore-01/freezer/dairy/telemetry") parse karta hai, wildcard matching ('+' for single level, '#' for multi-level) support karta hai, aur subscribers ko asynchronous callbacks dispatch karta hai.
// [OUTPUT / SIDE EFFECT]: High-throughput edge message distribution.
// [VARIATIONS TO TRY]: Persistent session queues aur QoS 2 exactly-once handshake support shamil karein.
public class MqttEdgeBroker
{
    // [KYA KARTA HAI]: Topic pattern se subscriber delegates ki list map karta hai.
    // [KYU ZAROORI HAI]: Fast subscriber dispatching.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, List<Func<MqttMessage, Task>>>.
    // [OUTPUT / SIDE EFFECT]: In-memory topic subscription directory.
    // [VARIATIONS TO TRY]: Radix tree ya trie based topic matching for ultra-high topic scale.
    private readonly ConcurrentDictionary<string, List<Func<MqttMessage, Task>>> _subscriptions = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Processed message counter track karta hai.
    // [KYU ZAROORI HAI]: Throughput monitoring.
    // [KAISE CHALTA HAI]: Interlocked.Increment.
    // [OUTPUT / SIDE EFFECT]: Total messages processed.
    // [VARIATIONS TO TRY]: OpenTelemetry counter.
    private long _messagesProcessed = 0;

    // [KYA KARTA HAI]: Total processed messages count return karta hai.
    // [KYU ZAROORI HAI]: Metric observation.
    // [KAISE CHALTA HAI]: Interlocked.Read.
    // [OUTPUT / SIDE EFFECT]: Long count.
    // [VARIATIONS TO TRY]: Messages per second rate.
    public long MessagesProcessed => Interlocked.Read(ref _messagesProcessed);

    // [KYA KARTA HAI]: Topic pattern par subscriber register karta hai.
    // [KYU ZAROORI HAI]: Specific telemetry streams listen karna (e.g. cold-chain alerts).
    // [KAISE CHALTA HAI]: Subscription list lock karke delegate append karta hai.
    // [OUTPUT / SIDE EFFECT]: Subscription added.
    // [VARIATIONS TO TRY]: Unsubscribe token return karein.
    public void Subscribe(string topicPattern, Func<MqttMessage, Task> handler)
    {
        // [KYA KARTA HAI]: Validation check.
        // [KYU ZAROORI HAI]: Valid pattern zaroori hai.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Regex check.
        ArgumentException.ThrowIfNullOrWhiteSpace(topicPattern);
        ArgumentNullException.ThrowIfNull(handler);

        // [KYA KARTA HAI]: Subscription list fetch ya create karta hai.
        // [KYU ZAROORI HAI]: Thread-safe collection management.
        // [KAISE CHALTA HAI]: GetOrAdd thread-safe factory.
        // [OUTPUT / SIDE EFFECT]: Target list.
        // [VARIATIONS TO TRY]: Immutable list swap.
        var list = _subscriptions.GetOrAdd(topicPattern, _ => new List<Func<MqttMessage, Task>>());

        // [KYA KARTA HAI]: List lock karke handler append karta hai.
        // [KYU ZAROORI HAI]: Concurrency safety during subscription.
        // [KAISE CHALTA HAI]: lock (list) { list.Add(handler); }.
        // [OUTPUT / SIDE EFFECT]: Handler registered.
        // [VARIATIONS TO TRY]: Lock-free reader-writer lock.
        lock (list)
        {
            list.Add(handler);
        }
    }

    // [KYA KARTA HAI]: Topic par message publish karta hai aur matching subscribers ko dispatch karta hai.
    // [KYU ZAROORI HAI]: IoT telemetry packets deliver karna.
    // [KAISE CHALTA HAI]: Sabhi subscriptions scan karta hai, TopicMatches check karta hai, aur async tasks fire karta hai.
    // [OUTPUT / SIDE EFFECT]: Subscribers receive and execute payload callbacks.
    // [VARIATIONS TO TRY]: Parallel dispatch with Task.WhenAll.
    public async Task PublishAsync(string topic, string payload, MqttQosLevel qos = MqttQosLevel.AtLeastOnce)
    {
        // [KYA KARTA HAI]: Argument validation.
        // [KYU ZAROORI HAI]: Valid topic string required.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Exception if empty.
        // [VARIATIONS TO TRY]: Topic level length limit check.
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        // [KYA KARTA HAI]: MqttMessage payload encapsulate karta hai.
        // [KYU ZAROORI HAI]: Structured packet create karna.
        // [KAISE CHALTA HAI]: Record instantiation.
        // [OUTPUT / SIDE EFFECT]: MqttMessage instance.
        // [VARIATIONS TO TRY]: Binary payload byte array.
        var message = new MqttMessage(topic, payload, qos, DateTime.UtcNow);

        // [KYA KARTA HAI]: Processed counter increment karta hai.
        // [KYU ZAROORI HAI]: Metric tracking.
        // [KAISE CHALTA HAI]: Interlocked.Increment.
        // [OUTPUT / SIDE EFFECT]: Counter += 1.
        // [VARIATIONS TO TRY]: Metric gauge.
        Interlocked.Increment(ref _messagesProcessed);

        // [KYA KARTA HAI]: Matching dispatch tasks list.
        // [KYU ZAROORI HAI]: Concurrently await multiple subscribers.
        // [KAISE CHALTA HAI]: List<Task>.
        // [OUTPUT / SIDE EFFECT]: Tasks container.
        // [VARIATIONS TO TRY]: Bounded channel pipeline.
        var dispatchTasks = new List<Task>();

        // [KYA KARTA HAI]: Sabhi subscription patterns iterate karta hai.
        // [KYU ZAROORI HAI]: Pattern matching evaluation.
        // [KAISE CHALTA HAI]: foreach over _subscriptions.
        // [OUTPUT / SIDE EFFECT]: Matches identified.
        // [VARIATIONS TO TRY]: Trie search instead of linear pattern scan.
        foreach (var (pattern, handlers) in _subscriptions)
        {
            // [KYA KARTA HAI]: Check karta hai kya topic pattern match karta hai.
            // [KYU ZAROORI HAI]: Wildcard ('+' aur '#') logic evaluate karna.
            // [KAISE CHALTA HAI]: TopicMatches(pattern, topic).
            // [OUTPUT / SIDE EFFECT]: Boolean match result.
            // [VARIATIONS TO TRY]: Pre-compiled regex cache.
            if (TopicMatches(pattern, topic))
            {
                // [KYA KARTA HAI]: Handlers snapshot copy karta hai.
                // [KYU ZAROORI HAI]: Avoid lock contention during callback execution.
                // [KAISE CHALTA HAI]: lock (handlers) { handlers.ToList(); }.
                // [OUTPUT / SIDE EFFECT]: Snapshot list.
                // [VARIATIONS TO TRY]: Array copy.
                List<Func<MqttMessage, Task>> handlersSnapshot;
                lock (handlers)
                {
                    handlersSnapshot = new List<Func<MqttMessage, Task>>(handlers);
                }

                // [KYA KARTA HAI]: Har subscriber callback trigger karta hai.
                // [KYU ZAROORI HAI]: Event notification.
                // [KAISE CHALTA HAI]: handler(message).
                // [OUTPUT / SIDE EFFECT]: Task added to dispatchTasks.
                // [VARIATIONS TO TRY]: Catch individual subscriber exceptions.
                foreach (var handler in handlersSnapshot)
                {
                    dispatchTasks.Add(handler(message));
                }
            }
        }

        // [KYA KARTA HAI]: Sabhi subscriber callbacks complete hone ka wait karta hai.
        // [KYU ZAROORI HAI]: QoS 1 guarantee (AtLeastOnce completion).
        // [KAISE CHALTA HAI]: Task.WhenAll.
        // [OUTPUT / SIDE EFFECT]: All subscribers finished processing.
        // [VARIATIONS TO TRY]: Fire-and-forget for QoS 0.
        if (dispatchTasks.Count > 0)
        {
            await Task.WhenAll(dispatchTasks).ConfigureAwait(false);
        }
    }

    // [KYA KARTA HAI]: MQTT topic wildcard matching implement karta hai according to MQTT 5.0 specification.
    // [KYU ZAROORI HAI]: '+' (single level wildcard) aur '#' (multi-level trailing wildcard) support karna.
    // [KAISE CHALTA HAI]: Regex pattern build karke topic match karta hai: '+' becomes '[^/]+', '#' becomes '.*'.
    // [OUTPUT / SIDE EFFECT]: True if topic matches pattern.
    // [VARIATIONS TO TRY]: Fast zero-allocation token iterator.
    public static bool TopicMatches(string pattern, string topic)
    {
        // [KYA KARTA HAI]: Exact string match check karta hai.
        // [KYU ZAROORI HAI]: Fastest path for non-wildcard topics.
        // [KAISE CHALTA HAI]: string.Equals.
        // [OUTPUT / SIDE EFFECT]: True if identical.
        // [VARIATIONS TO TRY]: Span comparison.
        if (string.Equals(pattern, topic, StringComparison.Ordinal))
        {
            return true;
        }

        // [KYA KARTA HAI]: Pattern ko regex me convert karta hai.
        // [KYU ZAROORI HAI]: Wildcard replacement.
        // [KAISE CHALTA HAI]: Escape special regex chars, replace '+' with '[^/]+', replace '#' with '.*'.
        // [OUTPUT / SIDE EFFECT]: Regex string.
        // [VARIATIONS TO TRY]: Trie traversal.
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\+", "[^/]+")
            .Replace("\\#", ".*") + "$";

        // [KYA KARTA HAI]: Regex match evaluate karta hai.
        // [KYU ZAROORI HAI]: Spec compliant matching.
        // [KAISE CHALTA HAI]: Regex.IsMatch.
        // [OUTPUT / SIDE EFFECT]: Boolean.
        // [VARIATIONS TO TRY]: Source generator regex.
        return Regex.IsMatch(topic, regexPattern);
    }
}

// [KYA KARTA HAI]: MQTT message packet encapsulate karne wala immutable record.
// [KYU ZAROORI HAI]: Topic, payload, QoS, aur timestamp transmit karna.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Packet representation.
// [VARIATIONS TO TRY]: User properties dictionary (MQTT 5.0 feature).
public record MqttMessage(string Topic, string Payload, MqttQosLevel Qos, DateTime PublishedUtc);

// [KYA KARTA HAI]: MQTT Quality of Service (QoS) levels enum.
// [KYU ZAROORI HAI]: AtMostOnce (QoS 0), AtLeastOnce (QoS 1), ExactlyOnce (QoS 2) delivery semantics.
// [KAISE CHALTA HAI]: Enum values 0, 1, 2.
// [OUTPUT / SIDE EFFECT]: QoS specification.
// [VARIATIONS TO TRY]: Add QoS 2 two-phase commit.
public enum MqttQosLevel
{
    AtMostOnce = 0,
    AtLeastOnce = 1,
    ExactlyOnce = 2
}
