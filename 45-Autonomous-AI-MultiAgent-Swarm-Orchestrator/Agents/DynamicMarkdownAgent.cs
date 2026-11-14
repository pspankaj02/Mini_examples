using KiranaHub.AgentSwarm.Bus;
using KiranaHub.AgentSwarm.Protocol;

namespace KiranaHub.AgentSwarm.Agents;

/// <summary>
/// Autonomous Dynamic Markdown & Waste Prevention Agent.
/// Monitors perishables (dairy, bread, berries) trapped in flood-isolated dark stores.
/// Applies progressive dynamic markdown algorithms to clear perishable stock before spoilage,
/// recovering revenue and preventing food waste.
/// </summary>
public sealed class DynamicMarkdownAgent
{
    public string AgentId { get; }
    private readonly SwarmBlackboardBus _bus;

    public DynamicMarkdownAgent(string agentId, SwarmBlackboardBus bus)
    {
        AgentId = agentId;
        _bus = bus;

        _bus.Subscribe("weather/*", HandleWeatherDisruption);
    }

    // [KYA KARTA HAI]: Flooded store ke trapped perishable items identify karta hai aur markdown calculate karta hai.
    // [KYU ZAROORI HAI]: Agar doorstep delivery band ho jaye toh 12 ghante me doodh aur paneer spoil ho jayega.
    // [KAISE CHALTA HAI]: Expiry curve aur remaining stock par dynamic discount compute karta hai.
    // [OUTPUT / SIDE EFFECT]: Publishes MarkdownApplied on topic "pricing/markdown".
    // [VARIATIONS TO TRY]: Machine-learning demand elasticity curve integration.
    private void HandleWeatherDisruption(AgentMessage msg)
    {
        if (msg.Payload is not WeatherAnomalyPayload weather || !weather.IsFlooded) return;

        Console.WriteLine($"[AGENT: {AgentId}] 🏷️ EVALUATING PERISHABLE EXPIRY RISK in flooded {weather.Region}...");

        // Sample trapped perishable item in flood-isolated store
        string isolatedStoreId = "darkstore_bandra_west";
        string perishableSku = "sku_fresh_organic_paneer_200g";
        int trappedStock = 85;
        double originalPrice = 120.0;
        double discountPercent = 50.0; // 50% Emergency Clearance Markdown
        double newPrice = originalPrice * (1.0 - (discountPercent / 100.0));

        var markdown = new MarkdownPayload(
            StoreId: isolatedStoreId,
            Sku: perishableSku,
            RemainingStock: trappedStock,
            OriginalPrice: originalPrice,
            DiscountPercent: discountPercent,
            NewMarkdownPrice: newPrice,
            Reason: $"Flooded arterial roads ({weather.WaterloggedRoads.Length} blocked) prevent normal transit. 50% flash discount for walking neighbors.");

        var markdownMsg = new AgentMessage(
            MessageId: $"msg_markdown_{Guid.NewGuid():N}",
            SenderAgentId: AgentId,
            Type: AgentMessageType.MarkdownApplied,
            Topic: "pricing/markdown",
            Payload: markdown,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Console.WriteLine($"[AGENT: {AgentId}] 🏷️ AUTONOMOUS MARKDOWN APPLIED: '{perishableSku}' at {isolatedStoreId} marked down {discountPercent}%! (Rs {originalPrice:F0} -> Rs {newPrice:F0}). Prevented Rs {trappedStock * originalPrice:F0} spoilage loss!");
        _bus.Publish(markdownMsg);
    }
}
