// [KYA KARTA HAI]: System collection namespaces import karta hai.
// [KYU ZAROORI HAI]: High-throughput stream buffer manage karne ke liye.
// [KAISE CHALTA HAI]: .NET core BCL.
// [OUTPUT / SIDE EFFECT]: Collections available.
// [VARIATIONS TO TRY]: Redis client StackExchange.Redis StreamPosition.
using System.Collections.Concurrent;

// [KYA KARTA HAI]: Namespace define karta hai Redis Streams engine ke liye.
// [KYU ZAROORI HAI]: Telemetry stream processing encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Streams namespace.
// [VARIATIONS TO TRY]: Kafka streaming module.
namespace KiranaHub.RedisAdvanced.Streams;

// [KYA KARTA HAI]: Stream message packet record.
// [KYU ZAROORI HAI]: Redis stream me entry representation ke liye (Timestamp-Sequence ID).
// [KAISE CHALTA HAI]: MessageId, StreamName, aur dictionary payload encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: StreamMessage record.
// [VARIATIONS TO TRY]: Byte array payload.
public record StreamMessage(
    string MessageId,
    string StreamName,
    IReadOnlyDictionary<string, string> Fields,
    DateTime IngestedAtUtc);

// [KYA KARTA HAI]: Redis Streams & Consumer Groups engine jo XADD, XREADGROUP, XACK emulate karta hai.
// [KYU ZAROORI HAI]: 10,000 riders ki continuous GPS telemetry ko bina data loss ke high-throughput queue me ingest karne ke liye.
// [KAISE CHALTA HAI]: Append-only log maintain karta hai aur consumer groups ko unacknowledged messages distribute karta hai.
// [OUTPUT / SIDE EFFECT]: RiderTelemetryStreamEngine class.
// [VARIATIONS TO TRY]: Auto-trimming with MAXLEN ~ 10000.
public class RiderTelemetryStreamEngine
{
    private readonly List<StreamMessage> _streamLog = new();
    private readonly HashSet<string> _acknowledgedMessages = new();
    private readonly object _lock = new();
    private long _sequence = 0;

    // [KYA KARTA HAI]: Stream me naya message append karta hai (XADD stream * field value).
    // [KYU ZAROORI HAI]: Rider GPS ping ingest karne ke liye.
    // [KAISE CHALTA HAI]: Generates monotonic ID and appends to log.
    // [OUTPUT / SIDE EFFECT]: Returns generated MessageId string.
    // [VARIATIONS TO TRY]: Approximate trimming (~ MAXLEN).
    public string XAdd(string streamName, IReadOnlyDictionary<string, string> fields)
    {
        lock (_lock)
        {
            _sequence++;
            var msgId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{_sequence}";
            var message = new StreamMessage(msgId, streamName, fields, DateTime.UtcNow);
            _streamLog.Add(message);
            return msgId;
        }
    }

    // [KYA KARTA HAI]: Consumer Group ke zariye unread messages read karta hai (XREADGROUP GROUP group consumer STREAMS stream >).
    // [KYU ZAROORI HAI]: Multiple background telemetry workers me load distribute karne ke liye.
    // [KAISE CHALTA HAI]: Unacknowledged messages filter karke batch return karta hai.
    // [OUTPUT / SIDE EFFECT]: List of StreamMessages.
    // [VARIATIONS TO TRY]: Block parameter with timeout.
    public IReadOnlyList<StreamMessage> XReadGroup(string streamName, int batchSize = 10)
    {
        lock (_lock)
        {
            return _streamLog
                .Where(m => m.StreamName == streamName && !_acknowledgedMessages.Contains(m.MessageId))
                .Take(batchSize)
                .ToList();
        }
    }

    // [KYA KARTA HAI]: Processed message ko acknowledge karta hai (XACK stream group id).
    // [KYU ZAROORI HAI]: Message successfully DB me persist hone ke baad queue se clear karne ke liye.
    // [KAISE CHALTA HAI]: MessageId ko acknowledged set me add karta hai.
    // [OUTPUT / SIDE EFFECT]: Message acknowledged.
    // [VARIATIONS TO TRY]: Batch acknowledgement.
    public void XAck(string streamName, string messageId)
    {
        lock (_lock)
        {
            _acknowledgedMessages.Add(messageId);
        }
    }

    // [KYA KARTA HAI]: Total unacknowledged pending count return karta hai.
    // [KYU ZAROORI HAI]: Consumer lag monitoring ke liye.
    // [KAISE CHALTA HAI]: Count calculation.
    // [OUTPUT / SIDE EFFECT]: Integer count.
    // [VARIATIONS TO TRY]: Pending Entries List (PEL) inspection.
    public int GetPendingCount(string streamName)
    {
        lock (_lock)
        {
            return _streamLog.Count(m => m.StreamName == streamName && !_acknowledgedMessages.Contains(m.MessageId));
        }
    }
}
