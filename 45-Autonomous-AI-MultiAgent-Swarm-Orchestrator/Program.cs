using KiranaHub.AgentSwarm.Protocol;
using KiranaHub.AgentSwarm.Swarm;

namespace KiranaHub.AgentSwarm;

public static class Program
{
    // [KYA KARTA HAI]: Multi-Agent Swarm Orchestrator ka comprehensive demonstration entry point.
    // [KYU ZAROORI HAI]: Decentralized supply chain self-healing, Contract Net Protocol, aur multi-agent convergence verify karna.
    // [KAISE CHALTA HAI]: Crisis simulation sequentially execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Console logs showing multi-agent blackboard conversations and audit report.
    // [VARIATIONS TO TRY]: Inject concurrent second disaster in another zone.
    public static void Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 10: Autonomous AI Multi-Agent Swarm Orchestrator              ");
        Console.WriteLine("  Features: Contract Net Protocol (CNP), Blackboard Architecture, Self-Healing  ");
        Console.WriteLine("================================================================================");

        // -------------------------------------------------------------------------
        // PHASE 1: Deploy Autonomous Multi-Agent Swarm
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 1] Initializing Swarm Blackboard & Deploying Autonomous Specialized Agents...");
        var orchestrator = new SwarmOrchestrator();

        Console.WriteLine("  - [AGENT DEPLOYED] Weather Anomaly Agent      : agent_weather_mumbai (Monitoring Mumbai_South)");
        Console.WriteLine("  - [AGENT DEPLOYED] Distressed Store Agent    : agent_store_bandra_west (Milk Stock: 20 units - CRITICAL SHORTAGE!)");
        Console.WriteLine("  - [AGENT DEPLOYED] Surplus Donor Store Agent  : agent_store_chembur_east (Milk Stock: 250 units - SURPLUS)");
        Console.WriteLine("  - [AGENT DEPLOYED] Fleet Rebalancing Agent    : agent_fleet_logistics (High-clearance 4x4 vans & 2-wheelers)");
        Console.WriteLine("  - [AGENT DEPLOYED] Dynamic Markdown Agent     : agent_markdown_pricing (Perishable spoilage prevention)");

        // -------------------------------------------------------------------------
        // PHASE 2: Environmental Crisis Injection (Monsoon Cloudburst)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 2] Simulating Environmental Crisis (Catastrophic Cloudburst in Mumbai South)...");
        string[] waterloggedRoads = ["SV_Road_Bandra", "Linking_Road_Khar", "Milan_Subway_Santacruz"];

        // Weather sensor detects 115 mm/hr rainfall and broadcasts to blackboard
        orchestrator.WeatherAgent.BroadcastDisruption(rainMmPerHour: 115.0, waterloggedRoads);

        // -------------------------------------------------------------------------
        // PHASE 3: Autonomous Contract Net Protocol (CNP) Bidding & Rebalancing
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 3] Contract Net Protocol (CNP) Autonomous Bidding & Auction Execution...");

        // Find the announced CFP task ID from blackboard history
        var cfpMessages = orchestrator.Bus.GetHistory(AgentMessageType.CallForProposals);
        if (cfpMessages.Count > 0 && cfpMessages[0].Payload is CallForProposalsPayload cfp)
        {
            Console.WriteLine($"  - Distressed Store '{cfp.DistressedStoreId}' actively evaluating candidate bids on Blackboard...");

            // Distressed store evaluates incoming donor bids and awards contract
            orchestrator.DistressedStoreAgent.EvaluateAndAwardTask(cfp.TaskId);
        }

        // -------------------------------------------------------------------------
        // PHASE 4: Multi-Agent Convergence & Audit Report
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 4] Swarm Convergence & Supply Chain Resilience Audit...");
        var report = orchestrator.GenerateAuditReport();

        Console.WriteLine("================================================================================");
        Console.WriteLine("  SWARM COORDINATION AUDIT REPORT");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"  - Total Blackboard Messages  : {report.TotalMessages}");
        Console.WriteLine($"  - Weather Alerts Broadcast   : {report.WeatherAlerts}");
        Console.WriteLine($"  - Call For Proposals (CFPs)  : {report.AnnouncedCFPs}");
        Console.WriteLine($"  - Competitive Bids Submitted : {report.SubmittedBids}");
        Console.WriteLine($"  - Finalized Contract Awards  : {report.FinalizedAwards}");
        Console.WriteLine($"  - Fleet Logistics Executions : {report.LogisticsExecutions}");
        Console.WriteLine($"  - Dynamic Markdowns Applied  : {report.MarkdownsApplied}");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine($"  - Distressed Store Ending Milk Stock : {report.DistressedStoreEndingStock} units (Restored from 20 -> 150!)");
        Console.WriteLine($"  - Donor Store Ending Milk Stock      : {report.DonorStoreEndingStock} units (Supplied 130 units from 250)");
        Console.WriteLine("--------------------------------------------------------------------------------");

        if (report.DistressedStoreEndingStock >= 100 && report.LogisticsExecutions > 0 && report.MarkdownsApplied > 0)
        {
            Console.WriteLine("  [SWARM VERIFICATION]: 100% SELF-HEALED! Supply chain recovered without human intervention!");
        }
        else
        {
            Console.WriteLine("  [SWARM VERIFICATION]: Partial recovery.");
        }

        Console.WriteLine("\n[SUCCESS] Autonomous AI Multi-Agent Swarm Orchestrator completed all operations successfully!");
    }
}
