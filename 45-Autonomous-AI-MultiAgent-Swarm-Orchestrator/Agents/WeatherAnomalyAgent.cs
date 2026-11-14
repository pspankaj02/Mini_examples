using KiranaHub.AgentSwarm.Bus;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Agents;

/// <summary>
/// Autonomous Sensor Agent monitoring weather radar, rain gauges, and road flood sensors.
/// Detects sudden severe anomalies (monsoon cloudbursts, waterlogging) and alerts the swarm.
/// </summary>
public sealed class WeatherAnomalyAgent
{
    // [KYA KARTA HAI]: Agent ka unique identifier store karta hai.
    // [KYU ZAROORI HAI]: Blackboard messages me sender provenance track karne ke liye.
    // [KAISE CHALTA HAI]: String ID assigned in constructor.
    // [OUTPUT / SIDE EFFECT]: Immutable Agent ID.
    // [VARIATIONS TO TRY]: GUID-based random agent identity.
    public string AgentId { get; }

    // [KYA KARTA HAI]: Shared swarm blackboard reference store karta hai.
    // [KYU ZAROORI HAI]: Environmental alerts broadcast karne ke liye.
    // [KAISE CHALTA HAI]: Injected via constructor.
    // [OUTPUT / SIDE EFFECT]: Blackboard bus handle.
    // [VARIATIONS TO TRY]: Message sender abstraction interface.
    private readonly SwarmBlackboardBus _bus;

    // [KYA KARTA HAI]: Monitored geographical territory define karta hai.
    // [KYU ZAROORI HAI]: Localized micro-climate spatial bounding.
    // [KAISE CHALTA HAI]: Region name string (e.g. "Mumbai_South").
    // [OUTPUT / SIDE EFFECT]: Region identifier.
    // [VARIATIONS TO TRY]: Geohash or polygon boundary coordinates.
    public string MonitoredRegion { get; }

    // [KYA KARTA HAI]: WeatherAnomalyAgent constructor.
    // [KYU ZAROORI HAI]: Agent initialize karke blackboard se connect karna.
    // [KAISE CHALTA HAI]: Fields initialize karta hai.
    // [OUTPUT / SIDE EFFECT]: WeatherAnomalyAgent ready.
    // [VARIATIONS TO TRY]: Configurable rainfall alert threshold (e.g. > 50 mm/hr).
    public WeatherAnomalyAgent(string agentId, string monitoredRegion, SwarmBlackboardBus bus)
    {
        AgentId = agentId;
        MonitoredRegion = monitoredRegion;
        _bus = bus;
    }

    // [KYA KARTA HAI]: Sudden severe weather disruption broadcast karta hai swarm ko.
    // [KYU ZAROORI HAI]: Dark-store aur fleet agents ko autonomous emergency mitigation ke liye trigger karna.
    // [KAISE CHALTA HAI]: WeatherAnomalyPayload construct karke AgentMessage publish karta hai.
    // [OUTPUT / SIDE EFFECT]: Message published on topic "weather/{region}".
    // [VARIATIONS TO TRY]: Real-time IoT MQTT sensor feed ingestion.
    public void BroadcastDisruption(double rainMmPerHour, string[] waterloggedRoads)
    {
        bool isFlooded = rainMmPerHour > 50.0;
        var payload = new WeatherAnomalyPayload(MonitoredRegion, rainMmPerHour, isFlooded, waterloggedRoads);

        var msg = new AgentMessage(
            MessageId: $"msg_weather_{Guid.NewGuid():N}",
            SenderAgentId: AgentId,
            Type: AgentMessageType.WeatherAlert,
            Topic: $"weather/{MonitoredRegion.ToLowerInvariant()}",
            Payload: payload,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Console.WriteLine($"[AGENT: {AgentId}] ⛈️ WEATHER ANOMALY DETECTED in {MonitoredRegion}: {rainMmPerHour} mm/hr rain! Roads Blocked: {string.Join(", ", waterloggedRoads)}");
        _bus.Publish(msg);
    }
}
