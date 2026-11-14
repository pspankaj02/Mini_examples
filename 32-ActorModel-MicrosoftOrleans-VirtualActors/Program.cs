namespace KiranaHub.VirtualActors;

using KiranaHub.VirtualActors.Grains;
using KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: Project 32 ka executable entry point aur automated test suite provide karta hai.
// [KYU ZAROORI HAI]: Virtual Actor Model (Microsoft Orleans style) ke core principles — location transparency, lock-free turn serialization, on-demand activation, aur idle garbage collection ko end-to-end demonstrate aur verify karna.
// [KAISE CHALTA HAI]: VirtualActorSilo start karta hai, 50 riders ko 1,000 concurrent GPS pings dispatch karta hai, customer cart build karta hai, coupon discount verify karta hai, aur idle grain sweep test karta hai.
// [OUTPUT / SIDE EFFECT]: Console test reports, assertions verified, zero race conditions.
// [VARIATIONS TO TRY]: BenchmarkDotNet harness add karke turns/sec throughput profile karein.
public class Program
{
    // [KYA KARTA HAI]: Main execution method.
    // [KYU ZAROORI HAI]: Application start point.
    // [KAISE CHALTA HAI]: Top-level async task execution.
    // [OUTPUT / SIDE EFFECT]: Returns 0 on success.
    // [VARIATIONS TO TRY]: Command-line arguments for grain count and thread concurrency.
    public static async Task<int> Main(string[] args)
    {
        // [KYA KARTA HAI]: Console header print karta hai.
        // [KYU ZAROORI HAI]: Visual verification tracking.
        // [KAISE CHALTA HAI]: WriteLine calls.
        // [OUTPUT / SIDE EFFECT]: Formatted terminal header.
        // [VARIATIONS TO TRY]: ANSI color escape sequences.
        Console.WriteLine("==================================================================================");
        Console.WriteLine("  🚀 KiranaHub Project 32: Virtual Actor Model (Microsoft Orleans Grains) 🚀");
        Console.WriteLine("==================================================================================");

        // [KYA KARTA HAI]: VirtualActorSilo instantiate karta hai.
        // [KYU ZAROORI HAI]: Actor cluster runtime host start karna.
        // [KAISE CHALTA HAI]: new VirtualActorSilo() call inside using declaration.
        // [OUTPUT / SIDE EFFECT]: In-memory silo runtime initialized.
        // [VARIATIONS TO TRY]: Cluster membership gossip protocol configure karein.
        await using var silo = new VirtualActorSilo();

        // -----------------------------------------------------------------------------------------------------
        // TEST 1: ON-DEMAND ACTIVATION & CONCURRENT TELEMETRY (1,000 GPS UPDATES TO 50 RIDERS)
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 1] Testing On-Demand Virtual Actor Activation & Concurrent Telemetry Stream...");

        // [KYA KARTA HAI]: 50 riders ke liye concurrent location update tasks generate karta hai.
        // [KYU ZAROORI HAI]: Heavy load me actor mailbox concurrency test karna.
        // [KAISE CHALTA HAI]: 50 riders * 20 updates = 1,000 parallel async operations via Task.WhenAll.
        // [OUTPUT / SIDE EFFECT]: 1,000 updates dispatched concurrently.
        // [VARIATIONS TO TRY]: 10,000 updates to test higher queue depth.
        const int riderCount = 50;
        const int pingsPerRider = 20;
        var telemetryTasks = new List<Task>();

        // [KYA KARTA HAI]: Riders loop run karta hai.
        // [KYU ZAROORI HAI]: Multiple distinct actor instances trigger karna.
        // [KAISE CHALTA HAI]: for loop 1 to 50.
        // [OUTPUT / SIDE EFFECT]: 50 unique grain IDs.
        // [VARIATIONS TO TRY]: Dynamic GUID keys.
        for (int r = 1; r <= riderCount; r++)
        {
            var riderId = $"rider-delhi-{r:D3}";

            // [KYA KARTA HAI]: Per-rider multiple updates schedule karta hai.
            // [KYU ZAROORI HAI]: Concurrent messages to the exact same actor instance.
            // [KAISE CHALTA HAI]: Inner loop for pings.
            // [OUTPUT / SIDE EFFECT]: Tasks added to list.
            // [VARIATIONS TO TRY]: Variable time delays between pings.
            for (int p = 1; p <= pingsPerRider; p++)
            {
                int pingIndex = p;
                telemetryTasks.Add(Task.Run(async () =>
                {
                    // [KYA KARTA HAI]: Virtual actor proxy fetch karta hai on-demand.
                    // [KYU ZAROORI HAI]: Pehle call par grain memory me activate hoga, subsequent calls par cache hit hoga.
                    // [KAISE CHALTA HAI]: silo.GetGrainAsync<RiderGrain>(riderId).
                    // [OUTPUT / SIDE EFFECT]: Active IRiderGrain reference.
                    // [VARIATIONS TO TRY]: Client proxy wrapper use karein.
                    var rider = await silo.GetGrainAsync<RiderGrain>(riderId).ConfigureAwait(false);

                    // [KYA KARTA HAI]: Location update dispatch karta hai.
                    // [KYU ZAROORI HAI]: Actor mailbox me turn queue karna.
                    // [KAISE CHALTA HAI]: Coordinates calculate karke UpdateLocationAsync call karta hai.
                    // [OUTPUT / SIDE EFFECT]: Telemetry updated in actor.
                    // [VARIATIONS TO TRY]: Random GPS jitter add karein.
                    double lat = 28.6315 + (pingIndex * 0.0005);
                    double lon = 77.2167 + (pingIndex * 0.0005);
                    double speed = 25.0 + (pingIndex % 15);
                    int battery = 98 - (pingIndex / 2);
                    await rider.UpdateLocationAsync(lat, lon, speed, battery).ConfigureAwait(false);
                }));
            }
        }

        // [KYA KARTA HAI]: Sabhi 1,000 telemetry updates complete hone ka wait karta hai.
        // [KYU ZAROORI HAI]: Async execution barrier synchronization.
        // [KAISE CHALTA HAI]: Task.WhenAll(telemetryTasks).
        // [OUTPUT / SIDE EFFECT]: All 1,000 messages processed.
        // [VARIATIONS TO TRY]: Timeout cancellation token.
        await Task.WhenAll(telemetryTasks);

        Console.WriteLine($" -> Dispatched 1,000 concurrent GPS updates to {riderCount} riders successfully.");
        Console.WriteLine($" -> Silo Active Grains: {silo.ActiveGrainCount} | Total Activations: {silo.TotalActivations}");

        // [KYA KARTA HAI]: Sample rider ka state inspect karta hai.
        // [KYU ZAROORI HAI]: Exact 20 pings received hone ka assertion verify karna (zero message loss, zero race condition).
        // [KAISE CHALTA HAI]: silo.GetGrainAsync<RiderGrain>("rider-delhi-007") -> GetStateAsync().
        // [OUTPUT / SIDE EFFECT]: RiderState snapshot.
        // [VARIATIONS TO TRY]: Check all 50 riders in parallel.
        var sampleRider = await silo.GetGrainAsync<RiderGrain>("rider-delhi-007");
        var riderState = await sampleRider.GetStateAsync();
        Console.WriteLine($" -> Sample Rider [rider-delhi-007]: Pings={riderState.TelemetryPingsReceived}, Lat={riderState.Latitude:F4}, Lon={riderState.Longitude:F4}, Speed={riderState.SpeedKmh:F1}km/h, Battery={riderState.BatteryPct}%");

        // [KYA KARTA HAI]: Telemetry count assertion check karta hai.
        // [KYU ZAROORI HAI]: Proof ki single-threaded mailbox ne exactly 20 pings execute kiye without dropping any turn.
        // [KAISE CHALTA HAI]: if (riderState.TelemetryPingsReceived != pingsPerRider) throw.
        // [OUTPUT / SIDE EFFECT]: Throws exception if race condition occurred.
        // [VARIATIONS TO TRY]: Property tolerance ranges.
        if (riderState.TelemetryPingsReceived != pingsPerRider)
        {
            throw new InvalidOperationException($"Rider telemetry count mismatch! Expected {pingsPerRider}, got {riderState.TelemetryPingsReceived}");
        }

        // -----------------------------------------------------------------------------------------------------
        // TEST 2: ORDER ASSIGNMENT & LIFECYCLE
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 2] Testing Rider Order Assignment & Status Transitions...");

        // [KYA KARTA HAI]: Rider ko active delivery order assign karta hai.
        // [KYU ZAROORI HAI]: Dispatching and busy status mark karna.
        // [KAISE CHALTA HAI]: AssignOrderAsync("ORD-98721").
        // [OUTPUT / SIDE EFFECT]: IsAvailable = false, ActiveOrderId = "ORD-98721".
        // [VARIATIONS TO TRY]: Multiple orders assignment rejection check.
        await sampleRider.AssignOrderAsync("ORD-98721");
        var assignedState = await sampleRider.GetStateAsync();
        Console.WriteLine($" -> Assigned Order: {assignedState.ActiveOrderId} | IsAvailable: {assignedState.IsAvailable}");

        // [KYA KARTA HAI]: Availability assertion verify karta hai.
        // [KYU ZAROORI HAI]: Rider busy hona chahiye.
        // [KAISE CHALTA HAI]: if (assignedState.IsAvailable) throw.
        // [OUTPUT / SIDE EFFECT]: Validation.
        // [VARIATIONS TO TRY]: Event assertion.
        if (assignedState.IsAvailable || assignedState.ActiveOrderId != "ORD-98721")
        {
            throw new InvalidOperationException("Rider assignment state mismatch!");
        }

        // [KYA KARTA HAI]: Order delivery completion execute karta hai.
        // [KYU ZAROORI HAI]: Rider wapas free pool me aana chahiye.
        // [KAISE CHALTA HAI]: CompleteOrderAsync().
        // [OUTPUT / SIDE EFFECT]: IsAvailable = true, ActiveOrderId = null.
        // [VARIATIONS TO TRY]: Audit ledger logging.
        await sampleRider.CompleteOrderAsync();
        var completedState = await sampleRider.GetStateAsync();
        Console.WriteLine($" -> Completed Order | IsAvailable: {completedState.IsAvailable} | ActiveOrderId: {completedState.ActiveOrderId ?? "None"}");

        if (!completedState.IsAvailable || completedState.ActiveOrderId != null)
        {
            throw new InvalidOperationException("Rider completion state mismatch!");
        }

        // -----------------------------------------------------------------------------------------------------
        // TEST 3: CUSTOMER SHOPPING CART GRAIN CONCURRENCY & PRICING
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 3] Testing Customer Cart Virtual Actor (Stateful & Lock-Free)...");

        // [KYA KARTA HAI]: Customer shopping cart grain activate karta hai.
        // [KYU ZAROORI HAI]: In-memory stateful cart management for customer "cust-rajesh-404".
        // [KAISE CHALTA HAI]: silo.GetGrainAsync<CartGrain>("cust-rajesh-404").
        // [OUTPUT / SIDE EFFECT]: Active CartGrain instance.
        // [VARIATIONS TO TRY]: Guest user temporary session cart.
        var cart = await silo.GetGrainAsync<CartGrain>("cust-rajesh-404");

        // [KYA KARTA HAI]: Cart me grocery items add karta hai.
        // [KYU ZAROORI HAI]: Items aggregation.
        // [KAISE CHALTA HAI]: AddItemAsync calls.
        // [OUTPUT / SIDE EFFECT]: Items stored in actor state.
        // [VARIATIONS TO TRY]: Add 10 items concurrently.
        await cart.AddItemAsync("PROD-MILK", "Amul Taaza Milk 1L", 66.00m, 2); // 132
        await cart.AddItemAsync("PROD-BREAD", "Harvest Gold Brown Bread", 45.00m, 1); // 45
        await cart.AddItemAsync("PROD-ATTA", "Aashirvaad Shudh Chakki Atta 5kg", 240.00m, 1); // 240
        // Total Subtotal = 132 + 45 + 240 = 417.00

        // [KYA KARTA HAI]: 15% discount coupon apply karta hai.
        // [KYU ZAROORI HAI]: Promotional pricing calculation.
        // [KAISE CHALTA HAI]: ApplyCouponAsync("KIRANA15", 15m).
        // [OUTPUT / SIDE EFFECT]: Discount applied.
        // [VARIATIONS TO TRY]: Invalid or expired coupon rejection.
        await cart.ApplyCouponAsync("KIRANA15", 15.0m);

        // [KYA KARTA HAI]: Cart calculation snapshot read karta hai.
        // [KYU ZAROORI HAI]: Financial accuracy verify karna.
        // [KAISE CHALTA HAI]: GetCartAsync().
        // [OUTPUT / SIDE EFFECT]: CartState record.
        // [VARIATIONS TO TRY]: JSON serialization.
        var cartSnapshot = await cart.GetCartAsync();
        Console.WriteLine($" -> Cart Subtotal: ₹{cartSnapshot.Subtotal:F2}");
        Console.WriteLine($" -> Coupon Applied: {cartSnapshot.CouponCode} (-₹{cartSnapshot.DiscountAmount:F2})");
        Console.WriteLine($" -> Statutory GST (5%): +₹{cartSnapshot.TaxAmount:F2}");
        Console.WriteLine($" -> Net Payable: ₹{cartSnapshot.NetPayable:F2}");

        // Mathematical verification:
        // Subtotal = 417.00
        // Discount 15% of 417 = 62.55
        // Taxable = 417 - 62.55 = 354.45
        // GST 5% of 354.45 = 17.72
        // Net = 354.45 + 17.72 = 372.17
        if (cartSnapshot.Subtotal != 417.00m || cartSnapshot.NetPayable != 372.17m)
        {
            throw new InvalidOperationException($"Cart pricing calculation mismatch! Expected Net ₹372.17, got ₹{cartSnapshot.NetPayable}");
        }

        // [KYA KARTA HAI]: Cart checkout complete karta hai.
        // [KYU ZAROORI HAI]: Cart freeze karna.
        // [KAISE CHALTA HAI]: CheckoutAsync().
        // [OUTPUT / SIDE EFFECT]: IsCheckedOut = true.
        // [VARIATIONS TO TRY]: Try modifying cart after checkout and catch exception.
        await cart.CheckoutAsync();
        var checkedOutCart = await cart.GetCartAsync();
        Console.WriteLine($" -> Checkout Status: {checkedOutCart.IsCheckedOut} (Order Placed Successfully)");

        if (!checkedOutCart.IsCheckedOut)
        {
            throw new InvalidOperationException("Cart checkout status mismatch!");
        }

        // -----------------------------------------------------------------------------------------------------
        // TEST 4: IDLE GRAIN SWEEPER (GARBAGE COLLECTION) & REACTIVATION RESURRECTION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 4] Testing Silo Idle Grain Garbage Collection & Resurrection...");

        int activeBeforeSweep = silo.ActiveGrainCount;
        Console.WriteLine($" -> Active Grains Before Sweep: {activeBeforeSweep}");

        // [KYA KARTA HAI]: Simulated sleep dekar grains ko idle karta hai.
        // [KYU ZAROORI HAI]: Inactivity duration create karna.
        // [KAISE CHALTA HAI]: Task.Delay(50ms).
        // [OUTPUT / SIDE EFFECT]: Time progresses.
        // [VARIATIONS TO TRY]: Configurable sleep.
        await Task.Delay(50);

        // [KYA KARTA HAI]: Idle sweeper run karta hai with 10ms threshold.
        // [KYU ZAROORI HAI]: Inactive grains ko memory se reclaim karna.
        // [KAISE CHALTA HAI]: silo.SweepIdleGrainsAsync(TimeSpan.FromMilliseconds(10)).
        // [OUTPUT / SIDE EFFECT]: Inactive grains deactivated and removed.
        // [VARIATIONS TO TRY]: Sweep specific grain types only.
        int sweptGrains = await silo.SweepIdleGrainsAsync(TimeSpan.FromMilliseconds(10));
        Console.WriteLine($" -> Swept Inactive Grains: {sweptGrains} | Remaining Active: {silo.ActiveGrainCount}");

        if (sweptGrains == 0)
        {
            throw new InvalidOperationException("Expected idle grains to be swept from memory!");
        }

        // [KYA KARTA HAI]: Reactivation resurrection test karta hai.
        // [KYU ZAROORI HAI]: Virtual Actor Model guarantee: "Calling an evicted grain seamlessly reactivates it".
        // [KAISE CHALTA HAI]: Silo se wapas sample rider fetch karta hai.
        // [OUTPUT / SIDE EFFECT]: Grain resurrected on-demand.
        // [VARIATIONS TO TRY]: Test state reload from backing snapshot.
        var resurrectedRider = await silo.GetGrainAsync<RiderGrain>("rider-delhi-007");
        Console.WriteLine($" -> Resurrected Sample Rider State: GrainId={resurrectedRider.GrainId}, Status={resurrectedRider.LifecycleState}");
        Console.WriteLine($" -> Silo Active Grains After Resurrection: {silo.ActiveGrainCount}");

        if (resurrectedRider.LifecycleState != GrainLifecycleState.Active)
        {
            throw new InvalidOperationException("Resurrected grain must be in Active state!");
        }

        // [KYA KARTA HAI]: Final success message print karta hai.
        // [KYU ZAROORI HAI]: Test runner confirmation.
        // [KAISE CHALTA HAI]: Console.WriteLine.
        // [OUTPUT / SIDE EFFECT]: Test passed confirmation.
        // [VARIATIONS TO TRY]: Generate JUnit XML test report.
        Console.WriteLine("\n==================================================================================");
        Console.WriteLine("  ✅ [SUCCESS] ALL VIRTUAL ACTOR & SILO RUNTIME TESTS PASSED WITH 0 ERRORS! ✅");
        Console.WriteLine("==================================================================================");

        return 0;
    }
}
