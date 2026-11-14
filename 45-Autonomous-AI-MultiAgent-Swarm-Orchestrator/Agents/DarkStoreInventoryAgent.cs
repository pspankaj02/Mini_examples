using System.Collections.Concurrent;
using KiranaHub.AgentSwarm.Bus;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Agents;

/// <summary>
/// Autonomous Dark-Store Inventory Agent.
/// Manages local micro-warehouse stock, monitors stock depletion,
/// issues Contract Net Protocol (CFP) replenishment requests when in crisis,
/// or bids as a surplus donor to rescue adjacent dark stores.
/// </summary>
public sealed class DarkStoreInventoryAgent
{
    public string AgentId { get; }
    public string StoreId { get; }
    public string Region { get; }

    private readonly SwarmBlackboardBus _bus;
    private readonly ConcurrentDictionary<string, int> _inventory = new();
    private readonly ConcurrentDictionary<string, List<ProposalBidPayload>> _activeTaskBids = new();

    public DarkStoreInventoryAgent(
        string agentId,
        string storeId,
        string region,
        SwarmBlackboardBus bus,
        IDictionary<string, int> initialStock)
    {
        AgentId = agentId;
        StoreId = storeId;
        Region = region;
        _bus = bus;

        foreach (var (sku, qty) in initialStock)
        {
            _inventory[sku] = qty;
        }

        // Subscribe to relevant blackboard topics
        _bus.Subscribe($"weather/{Region.ToLowerInvariant()}", HandleWeatherAlert);
        _bus.Subscribe("cnp/cfp", HandleCallForProposals);
        _bus.Subscribe("cnp/proposals", HandleProposalBid);
        _bus.Subscribe("cnp/award", HandleContractAward);
    }

    // [KYA KARTA HAI]: Local stock quantity return karta hai.
    // [KYU ZAROORI HAI]: Inventory audit aur bidding calculations.
    // [KAISE CHALTA HAI]: ConcurrentDictionary lookup.
    // [OUTPUT / SIDE EFFECT]: Available integer quantity.
    // [VARIATIONS TO TRY]: Real-time reserve locking for unfulfilled carts.
    public int GetStock(string sku) => _inventory.TryGetValue(sku, out int qty) ? qty : 0;

    // [KYA KARTA HAI]: Weather alert aane par dark-store vulnerability assess karta hai.
    // [KYU ZAROORI HAI]: Severe flood ke dauraan low stock wali dark-store autonomous emergency transfer request initiate kare.
    // [KAISE CHALTA HAI]: Agar stock critical (< 50) ho toh CallForProposals publish karta hai.
    // [OUTPUT / SIDE EFFECT]: CFP message posted to blackboard.
    // [VARIATIONS TO TRY]: Dynamic re-order threshold based on local order velocity.
    private void HandleWeatherAlert(AgentMessage msg)
    {
        if (msg.Payload is not WeatherAnomalyPayload weather || !weather.IsFlooded) return;

        Console.WriteLine($"[AGENT: {AgentId}] ⚠️ Dark-Store {StoreId} assessing flood vulnerability in {weather.Region}...");

        // Check if any critical item is running low
        foreach (var (sku, qty) in _inventory)
        {
            if (qty < 50) // Critical shortage threshold
            {
                int needed = 150 - qty;
                string taskId = $"task_replenish_{StoreId}_{sku}_{Guid.NewGuid():N}";
                _activeTaskBids[taskId] = new List<ProposalBidPayload>();

                var cfp = new CallForProposalsPayload(
                    TaskId: taskId,
                    DistressedStoreId: StoreId,
                    Sku: sku,
                    QuantityNeeded: needed,
                    MaxArrivalMinutes: 45,
                    Region: Region);

                var cfpMsg = new AgentMessage(
                    MessageId: $"msg_cfp_{Guid.NewGuid():N}",
                    SenderAgentId: AgentId,
                    Type: AgentMessageType.CallForProposals,
                    Topic: "cnp/cfp",
                    Payload: cfp,
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                Console.WriteLine($"[AGENT: {AgentId}] 📢 ANNOUNCING CFP: Need {needed} units of '{sku}' at {StoreId} within 45 mins!");
                _bus.Publish(cfpMsg);
            }
        }
    }

    // [KYA KARTA HAI]: Doosre dark stores ki CFP dekhkar surplus evaluate karta hai aur bid submit karta hai.
    // [KYU ZAROORI HAI]: Peer-to-peer inventory rebalancing without centralized warehouse intervention.
    // [KAISE CHALTA HAI]: Agar khud ke paas safe stock (> 100) hai toh ProposalBid publish karta hai.
    // [OUTPUT / SIDE EFFECT]: Bid posted to "cnp/proposals".
    // [VARIATIONS TO TRY]: Margin-maximizing algorithmic dynamic pricing on transfers.
    private void HandleCallForProposals(AgentMessage msg)
    {
        if (msg.Payload is not CallForProposalsPayload cfp) return;
        if (cfp.DistressedStoreId == StoreId) return; // Ignore own request

        int currentStock = GetStock(cfp.Sku);
        if (currentStock > 100) // We have surplus stock to spare!
        {
            int transferable = Math.Min(cfp.QuantityNeeded, currentStock - 50);
            double estimatedCost = transferable * 42.0; // Rs 42 per unit cost
            int estimatedMins = 25; // Normal transit
            double riskScore = 0.15; // Low risk from safe donor zone

            var bid = new ProposalBidPayload(
                TaskId: cfp.TaskId,
                BidderAgentId: AgentId,
                SourceStoreId: StoreId,
                BidCost: estimatedCost,
                EstimatedDeliveryMinutes: estimatedMins,
                RouteRiskScore: riskScore,
                AvailableStock: transferable);

            var bidMsg = new AgentMessage(
                MessageId: $"msg_bid_{Guid.NewGuid():N}",
                SenderAgentId: AgentId,
                Type: AgentMessageType.ProposalBid,
                Topic: "cnp/proposals",
                Payload: bid,
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Console.WriteLine($"[AGENT: {AgentId}] 🏷️ SUBMITTING BID for Task '{cfp.TaskId}': Can supply {transferable} units of {cfp.Sku} from {StoreId} for Rs {estimatedCost:F0} (ETA: {estimatedMins}m)");
            _bus.Publish(bidMsg);
        }
    }

    // [KYA KARTA HAI]: Distressed store ke taur par incoming bids collect karta hai aur optimal bid choose karta hai.
    // [KYU ZAROORI HAI]: Best trade-off between delivery speed, cost, and travel risk.
    // [KAISE CHALTA HAI]: Bids list me store karta hai aur best bid par ContractAward announce karta hai.
    // [OUTPUT / SIDE EFFECT]: Winning bidder gets contract award.
    // [VARIATIONS TO TRY]: Multi-attribute utility function (MAUT).
    private void HandleProposalBid(AgentMessage msg)
    {
        if (msg.Payload is not ProposalBidPayload bid) return;

        if (_activeTaskBids.TryGetValue(bid.TaskId, out var bidsList))
        {
            lock (bidsList)
            {
                bidsList.Add(bid);
            }
        }
    }

    // [KYA KARTA HAI]: Bids evaluate karke best bidder ko contract award deta hai.
    // [KYU ZAROORI HAI]: Contract Net Protocol auction finalize karna.
    // [KAISE CHALTA HAI]: Lowest risk and optimal cost bidder select karta hai.
    // [OUTPUT / SIDE EFFECT]: Publishes ContractAward on "cnp/award".
    // [VARIATIONS TO TRY]: Vickrey second-price auction.
    public void EvaluateAndAwardTask(string taskId)
    {
        if (!_activeTaskBids.TryGetValue(taskId, out var bidsList)) return;

        ProposalBidPayload? winningBid;
        lock (bidsList)
        {
            if (bidsList.Count == 0) return;
            // Best bid: lowest risk score then lowest cost
            winningBid = bidsList.OrderBy(b => b.RouteRiskScore).ThenBy(b => b.BidCost).First();
        }

        var award = new ContractAwardPayload(
            TaskId: taskId,
            WinningAgentId: winningBid.BidderAgentId,
            SourceStoreId: winningBid.SourceStoreId,
            DistressedStoreId: StoreId,
            Sku: "sku_dairy_milk_1l",
            AllocatedQuantity: winningBid.AvailableStock,
            AgreedCost: winningBid.BidCost);

        var awardMsg = new AgentMessage(
            MessageId: $"msg_award_{Guid.NewGuid():N}",
            SenderAgentId: AgentId,
            Type: AgentMessageType.ContractAward,
            Topic: "cnp/award",
            Payload: award,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Console.WriteLine($"[AGENT: {AgentId}] 🏆 CONTRACT AWARDED: Task '{taskId}' awarded to Agent '{winningBid.BidderAgentId}' (Source: {winningBid.SourceStoreId}) for Rs {winningBid.BidCost:F0}!");
        _bus.Publish(awardMsg);
    }

    // [KYA KARTA HAI]: Contract award execute karta hai (stock transfer).
    // [KYU ZAROORI HAI]: Donor store ka stock decrement aur recipient store ka stock increment karna.
    // [KAISE CHALTA HAI]: Agar donor ho toh subtract, agar recipient ho toh add.
    // [OUTPUT / SIDE EFFECT]: _inventory balances updated.
    // [VARIATIONS TO TRY]: Two-phase commit (2PC) on inventory adjustments.
    private void HandleContractAward(AgentMessage msg)
    {
        if (msg.Payload is not ContractAwardPayload award) return;

        if (award.SourceStoreId == StoreId)
        {
            // We are the donor store: deduct stock
            _inventory.AddOrUpdate(award.Sku, 0, (_, old) => Math.Max(0, old - award.AllocatedQuantity));
            Console.WriteLine($"[AGENT: {AgentId}] 📦 DISPATCHED {award.AllocatedQuantity} units of '{award.Sku}' to {award.DistressedStoreId}. Remaining Stock: {GetStock(award.Sku)}");
        }
        else if (award.DistressedStoreId == StoreId)
        {
            // We are the receiving store: add stock
            _inventory.AddOrUpdate(award.Sku, award.AllocatedQuantity, (_, old) => old + award.AllocatedQuantity);
            Console.WriteLine($"[AGENT: {AgentId}] 🚚 RECEIVED & RESTOCKED {award.AllocatedQuantity} units of '{award.Sku}'! New Stock: {GetStock(award.Sku)}");
        }
    }
}
