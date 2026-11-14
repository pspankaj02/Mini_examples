namespace KiranaHub.AgentSwarm.Protocol;

/// <summary>
/// Type of message exchanged on the shared Swarm Blackboard.
/// Implements standard FIPA Contract Net Protocol (CNP) and domain event taxonomy.
/// </summary>
public enum AgentMessageType
{
    // [KYA KARTA HAI]: Weather anomaly event (heavy rainfall, waterlogging).
    WeatherAlert = 1,

    // [KYA KARTA HAI]: Dark store inventory low stock warning.
    InventoryLow = 2,

    // [KYA KARTA HAI]: Contract Net Protocol: Call for Proposals (CFP) task announcement.
    CallForProposals = 3,

    // [KYA KARTA HAI]: Contract Net Protocol: Candidate agent bidding proposal.
    ProposalBid = 4,

    // [KYA KARTA HAI]: Contract Net Protocol: Manager agent selects winning bid.
    ContractAward = 5,

    // [KYA KARTA HAI]: Task execution completed confirmation.
    ExecutionCompleted = 6,

    // [KYA KARTA HAI]: Autonomous dynamic price markdown applied to perishable stock.
    MarkdownApplied = 7,

    // [KYA KARTA HAI]: Agent heartbeat and consensus telemetry.
    HealthHeartbeat = 8
}

/// <summary>
/// Immutable message envelope exchanged across autonomous swarm agents.
/// </summary>
public sealed record AgentMessage(
    string MessageId,
    string SenderAgentId,
    AgentMessageType Type,
    string Topic,
    object Payload,
    long Timestamp);

/// <summary>
/// Contract Net Protocol: Call For Proposals (CFP) announced by a distressed dark store.
/// </summary>
public sealed record CallForProposalsPayload(
    string TaskId,
    string DistressedStoreId,
    string Sku,
    int QuantityNeeded,
    int MaxArrivalMinutes,
    string Region);

/// <summary>
/// Contract Net Protocol: Competitive bid submitted by a donor store or fleet agent.
/// </summary>
public sealed record ProposalBidPayload(
    string TaskId,
    string BidderAgentId,
    string SourceStoreId,
    double BidCost,
    int EstimatedDeliveryMinutes,
    double RouteRiskScore,
    int AvailableStock);

/// <summary>
/// Contract Net Protocol: Award decision announced to the winning bidder.
/// </summary>
public sealed record ContractAwardPayload(
    string TaskId,
    string WinningAgentId,
    string SourceStoreId,
    string DistressedStoreId,
    string Sku,
    int AllocatedQuantity,
    double AgreedCost);

/// <summary>
/// Weather telemetry event published by the Weather Anomaly Agent.
/// </summary>
public sealed record WeatherAnomalyPayload(
    string Region,
    double RainMmPerHour,
    bool IsFlooded,
    string[] WaterloggedRoads);

/// <summary>
/// Dynamic markdown decision published by the Dynamic Markdown Agent.
/// </summary>
public sealed record MarkdownPayload(
    string StoreId,
    string Sku,
    int RemainingStock,
    double OriginalPrice,
    double DiscountPercent,
    double NewMarkdownPrice,
    string Reason);
