namespace KiranaHub.WarehouseIoT;

using System.Text.Json;
using KiranaHub.WarehouseIoT.ColdChain;
using KiranaHub.WarehouseIoT.Mqtt;
using KiranaHub.WarehouseIoT.Pathfinding;

// [KYA KARTA HAI]: Project 34 ka executable entry point aur automated test suite provide karta hai.
// [KYU ZAROORI HAI]: MQTT 5.0 Edge Broker pub/sub, Cold-Chain telemetry noise filtering, Emergency threshold alerting, aur 2D A* Warehouse AGV pathfinding ko end-to-end execute aur assert karna.
// [KAISE CHALTA HAI]: Broker spin up karta hai, cold chain monitor attach karta hai, simulated temperature streams push karta hai, aur A* router se warehouse floor par autonomous robot route render karta hai.
// [OUTPUT / SIDE EFFECT]: Console test reports, assertions verified, zero errors.
// [VARIATIONS TO TRY]: Connect to live Mosquitto or EMQX MQTT broker.
public class Program
{
    // [KYA KARTA HAI]: Main execution method.
    // [KYU ZAROORI HAI]: Application launch point.
    // [KAISE CHALTA HAI]: Top-level async task.
    // [OUTPUT / SIDE EFFECT]: Returns 0 on success.
    // [VARIATIONS TO TRY]: Dynamic grid dimensions from CLI args.
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================================================");
        Console.WriteLine("  🏭 KiranaHub Project 34: Dark-Store IoT Edge & Warehouse Automation 🏭");
        Console.WriteLine("==================================================================================");

        var broker = new MqttEdgeBroker();
        var coldChainMonitor = new ColdChainMonitor();
        coldChainMonitor.AttachToBroker(broker);

        // -----------------------------------------------------------------------------------------------------
        // STAGE 1: MQTT 5.0 BROKER WILDCARD ROUTING
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 1] Testing MQTT 5.0 Broker Wildcard Topic Matching...");

        int wildcardReceived = 0;
        // Subscribe with single level '+' and multi-level '#'
        broker.Subscribe("kirana/edge/+/telemetry/#", msg =>
        {
            Interlocked.Increment(ref wildcardReceived);
            return Task.CompletedTask;
        });

        // Publish to matching topic
        await broker.PublishAsync("kirana/edge/store-01/telemetry/chiller/sensor-01", "{\"status\":\"ok\"}");
        await broker.PublishAsync("kirana/edge/store-02/telemetry/scale/sensor-99", "{\"weight_g\":500}");
        // Publish to non-matching topic
        await broker.PublishAsync("kirana/cloud/orders/placed", "{}");

        Console.WriteLine($" -> Wildcard Messages Received: {wildcardReceived} (Expected: 2)");
        if (wildcardReceived != 2)
        {
            throw new InvalidOperationException($"MQTT wildcard routing mismatch! Expected 2, got {wildcardReceived}");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 2: COLD-CHAIN TELEMETRY & TRANSIENT NOISE SUPPRESSION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 2] Testing Cold-Chain Telemetry, Transient Noise Filtering & Alerting...");

        // Helper to push temperature reading
        async Task SendTempAsync(string storeId, string zone, string sensorId, double temp)
        {
            var packet = new TelemetryPacket(storeId, zone, sensorId, temp, 75.0, DateTime.UtcNow);
            var json = JsonSerializer.Serialize(packet);
            var topic = $"kirana/edge/{storeId}/coldchain/{zone}/telemetry";
            await broker.PublishAsync(topic, json);
        }

        // 1. Normal Dairy reading (+4.0°C) -> Safe
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 4.0);
        Console.WriteLine(" -> Reading 1 (Milk +4.0°C): Normal, no alert.");

        // 2. Single transient glitch/spike (+11.0°C) -> Noise, should be suppressed!
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 11.0);
        Console.WriteLine(" -> Reading 2 (Milk +11.0°C Spike): Transient noise, suppressed.");

        // 3. Normal reading (+4.2°C) -> Counter resets to 0!
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 4.2);
        Console.WriteLine(" -> Reading 3 (Milk +4.2°C): Back to normal, anomaly counter reset.");

        if (coldChainMonitor.ActiveAlertCount != 0)
        {
            throw new InvalidOperationException("Single transient spike should not trigger an emergency alert!");
        }

        // 4. Genuine Sustained Chiller Failure: 3 consecutive readings above +6.0°C
        Console.WriteLine(" -> Simulating sustained chiller breakdown (3 consecutive breaches)...");
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 8.5);  // Breach 1
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 9.2);  // Breach 2
        await SendTempAsync("store-delhi-01", "DAIRY", "sens-milk-01", 10.4); // Breach 3 -> ALERT TRIGGERED!

        // 5. Deep Freeze Ice Cream Failure (should be <= -18°C, reading is -12°C sustained)
        await SendTempAsync("store-delhi-01", "ICE_CREAM", "sens-freeze-01", -12.0);
        await SendTempAsync("store-delhi-01", "ICE_CREAM", "sens-freeze-01", -11.5);
        await SendTempAsync("store-delhi-01", "ICE_CREAM", "sens-freeze-01", -10.0); // 3rd -> ALERT TRIGGERED!

        Console.WriteLine($" -> Active Cold-Chain Alerts: {coldChainMonitor.ActiveAlertCount}");
        foreach (var alert in coldChainMonitor.GetAlerts())
        {
            Console.WriteLine($"    ⚠️ [{alert.Severity}] Store={alert.StoreId}, Zone={alert.Zone}, Temp={alert.CurrentTemperature:F1}°C (Safe: {alert.SafeMinTemperature}°C to {alert.SafeMaxTemperature}°C), Breaches={alert.ConsecutiveBreaches}");
        }

        if (coldChainMonitor.ActiveAlertCount != 2)
        {
            throw new InvalidOperationException($"Expected exactly 2 cold-chain alerts, got {coldChainMonitor.ActiveAlertCount}");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 3: AUTONOMOUS AGV WAREHOUSE A* PATHFINDING
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 3] Testing A* AGV Pathfinding on Dark-Store 2D Floor Map...");

        // Create 12x8 warehouse floor
        int width = 12;
        int height = 8;
        var router = new AStarWarehouseRouter(width, height);

        // Place storage shelf racks as obstacles (#)
        // Shelf Rack 1: Vertical aisle wall
        for (int y = 1; y <= 5; y++) router.SetObstacle(3, y);
        // Shelf Rack 2: Vertical aisle wall with doorway at y=3
        for (int y = 2; y <= 6; y++)
        {
            if (y != 3) router.SetObstacle(7, y);
        }

        var start = new GridPoint(0, 0); // Robot docking station
        var goal = new GridPoint(11, 7); // Packing counter station

        var path = router.FindPath(start, goal);
        Console.WriteLine($" -> Shortest Path Computed: {path.Count} steps | Traversal Distance: {path.Count - 1} meters");
        Console.WriteLine($" -> Waypoints: {string.Join(" -> ", path.Take(5))} ... -> {path.Last()}");

        Console.WriteLine("\n[WAREHOUSE FLOOR MAP VISUALIZATION]");
        Console.WriteLine("Legend: S = Start Dock | G = Goal Counter | * = AGV Path | # = Shelf Rack | . = Walkable Aisle");
        Console.WriteLine(router.RenderAscii(path, start, goal));

        if (path.Count == 0 || path.First() != start || path.Last() != goal)
        {
            throw new InvalidOperationException("A* Pathfinding failed to compute valid collision-free route!");
        }

        Console.WriteLine("==================================================================================");
        Console.WriteLine("  ✅ [SUCCESS] ALL IOT EDGE, COLD-CHAIN & WAREHOUSE AGV TESTS PASSED! ✅");
        Console.WriteLine("==================================================================================");

        return 0;
    }
}
