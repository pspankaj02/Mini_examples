using KiranaHub.AgentSwarm.Agents;
using KiranaHub.AgentSwarm.Bus;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Swarm;

/// <summary>
/// Swarm Coordinator and Resilience Orchestrator.
/// Deploys decentralized autonomous agents, establishes the shared blackboard,
/// and audits multi-agent convergence during high-severity supply chain crises.
/// </summary>
public sealed class SwarmOrchestrator
{
    public SwarmBlackboardBus Bus { get; } = new();

    public WeatherAnomalyAgent WeatherAgent { get; }
    public DarkStoreInventoryAgent DistressedStoreAgent { get; }
    public DarkStoreInventoryAgent DonorStoreAgent { get; }
    public RiderRebalancingAgent FleetAgent { get; }
    public DynamicMarkdownAgent MarkdownAgent { get; }

    public SwarmOrchestrator()
    {
        // 1. Weather Anomaly Sensor Agent
        WeatherAgent = new WeatherAnomalyAgent(
            agentId: "agent_weather_mumbai",
            monitoredRegion: "Mumbai_South",
            bus: Bus);

        // 2. Distressed Dark Store (Low Milk Stock: 20 units)
        DistressedStoreAgent = new DarkStoreInventoryAgent(
            agentId: "agent_store_bandra_west",
            storeId: "darkstore_bandra_west",
            region: "Mumbai_South",
            bus: Bus,
            initialStock: new Dictionary<string, int>
            {
                ["sku_dairy_milk_1l"] = 20, // Shortage!
                ["sku_fresh_organic_paneer_200g"] = 85
            });

        // 3. Surplus Donor Dark Store (Safe unflooded zone: Chembur, High Stock: 250 units)
        DonorStoreAgent = new DarkStoreInventoryAgent(
            agentId: "agent_store_chembur_east",
            storeId: "darkstore_chembur_east",
            region: "Mumbai_East",
            bus: Bus,
            initialStock: new Dictionary<string, int>
            {
                ["sku_dairy_milk_1l"] = 250, // Massive Surplus!
                ["sku_fresh_organic_paneer_200g"] = 120
            });

        // 4. Autonomous Fleet Allocation Agent
        FleetAgent = new RiderRebalancingAgent(
            agentId: "agent_fleet_logistics",
            fleetBaseRegion: "Mumbai_South",
            bus: Bus);

        // 5. Dynamic Markdown Agent
        MarkdownAgent = new DynamicMarkdownAgent(
            agentId: "agent_markdown_pricing",
            bus: Bus);
    }

    // [KYA KARTA HAI]: Swarm resilience metrics aur telemetry report summarize karta hai.
    // [KYU ZAROORI HAI]: Crisis recovery audit aur supply chain self-healing validation.
    // [KAISE CHALTA HAI]: Blackboard message history inspect karke actions tally karta hai.
    // [OUTPUT / SIDE EFFECT]: SwarmAuditReport record.
    // [VARIATIONS TO TRY]: JSON export to Prometheus / Grafana.
    public SwarmAuditReport GenerateAuditReport()
    {
        var alerts = Bus.GetHistory(AgentMessageType.WeatherAlert).Count;
        var cfps = Bus.GetHistory(AgentMessageType.CallForProposals).Count;
        var bids = Bus.GetHistory(AgentMessageType.ProposalBid).Count;
        var awards = Bus.GetHistory(AgentMessageType.ContractAward).Count;
        var markdowns = Bus.GetHistory(AgentMessageType.MarkdownApplied).Count;
        var executions = Bus.GetHistory(AgentMessageType.ExecutionCompleted).Count;

        return new SwarmAuditReport(
            TotalMessages: Bus.TotalMessagesCount,
            WeatherAlerts: alerts,
            AnnouncedCFPs: cfps,
            SubmittedBids: bids,
            FinalizedAwards: awards,
            MarkdownsApplied: markdowns,
            LogisticsExecutions: executions,
            DistressedStoreEndingStock: DistressedStoreAgent.GetStock("sku_dairy_milk_1l"),
            DonorStoreEndingStock: DonorStoreAgent.GetStock("sku_dairy_milk_1l"));
    }
}

/// <summary>
/// Telemetry record capturing swarm convergence metrics.
/// </summary>
public sealed record SwarmAuditReport(
    int TotalMessages,
    int WeatherAlerts,
    int AnnouncedCFPs,
    int SubmittedBids,
    int FinalizedAwards,
    int MarkdownsApplied,
    int LogisticsExecutions,
    int DistressedStoreEndingStock,
    int DonorStoreEndingStock);
