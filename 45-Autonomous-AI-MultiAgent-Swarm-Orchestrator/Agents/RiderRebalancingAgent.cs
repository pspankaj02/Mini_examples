using KiranaHub.AgentSwarm.Bus;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Agents;

/// <summary>
/// Autonomous Fleet Rebalancing & Rider Allocation Agent.
/// Dynamically shifts vehicle allocations (EV scooters vs high-clearance 4x4 vans),
/// routes around waterlogged choke-points, and bids for emergency transport logistics.
/// </summary>
public sealed class RiderRebalancingAgent
{
    public string AgentId { get; }
    public string FleetBaseRegion { get; }

    private readonly SwarmBlackboardBus _bus;
    private readonly HashSet<string> _waterloggedRoads = new(StringComparer.OrdinalIgnoreCase);
    private int _availableHighClearanceVans = 5;
    private int _availableScooters = 20;

    public RiderRebalancingAgent(string agentId, string fleetBaseRegion, SwarmBlackboardBus bus)
    {
        AgentId = agentId;
        FleetBaseRegion = fleetBaseRegion;
        _bus = bus;

        _bus.Subscribe($"weather/{FleetBaseRegion.ToLowerInvariant()}", HandleWeatherAlert);
        _bus.Subscribe("cnp/award", HandleContractAward);
    }

    // [KYA KARTA HAI]: Flooded roads aur weather alerts track karta hai.
    // [KYU ZAROORI HAI]: Electric scooters ko waterlogged streets par bhejkar breakdown hone se bachana.
    // [KAISE CHALTA HAI]: Roads list ko local hashset me store karta hai aur scooter operations pause karta hai.
    // [OUTPUT / SIDE EFFECT]: Fleet allocation shifted toward high-clearance emergency vans.
    // [VARIATIONS TO TRY]: Real-time GPS rider telemetry heatmap.
    private void HandleWeatherAlert(AgentMessage msg)
    {
        if (msg.Payload is not WeatherAnomalyPayload weather) return;

        foreach (var road in weather.WaterloggedRoads)
        {
            _waterloggedRoads.Add(road);
        }

        Console.WriteLine($"[AGENT: {AgentId}] 🛵 FLEET SAFETY RE-BALANCE: Grounded {_availableScooters} 2-wheelers due to waterlogging! Activated {_availableHighClearanceVans} High-Clearance 4x4 Emergency Vans.");
    }

    // [KYA KARTA HAI]: Emergency transfer award milne par logistics run execute karta hai.
    // [KYU ZAROORI HAI]: Inter-store emergency inventory movement physical transport simulation.
    // [KAISE CHALTA HAI]: Van dispatch karta hai, detour time calculate karta hai, aur execution confirm karta hai.
    // [OUTPUT / SIDE EFFECT]: Publishes ExecutionCompleted on blackboard.
    // [VARIATIONS TO TRY]: Multi-hop transit consolidation.
    private void HandleContractAward(AgentMessage msg)
    {
        if (msg.Payload is not ContractAwardPayload award) return;

        Console.WriteLine($"[AGENT: {AgentId}] 🚚 DISPATCHING HIGH-CLEARANCE 4x4 VAN from {award.SourceStoreId} to {award.DistressedStoreId} via Eastern Freeway (Bypassing {_waterloggedRoads.Count} waterlogged roads)...");

        var completedMsg = new AgentMessage(
            MessageId: $"msg_fleet_done_{Guid.NewGuid():N}",
            SenderAgentId: AgentId,
            Type: AgentMessageType.ExecutionCompleted,
            Topic: "fleet/completed",
            Payload: $"Logistics Transfer for Task '{award.TaskId}' successfully delivered in 28 mins!",
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        _bus.Publish(completedMsg);
    }
}
