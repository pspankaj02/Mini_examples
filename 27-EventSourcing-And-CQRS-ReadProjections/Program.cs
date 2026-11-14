// ==============================================================================================
// KiranaHub Event Sourcing: Automated Verification & Time-Travel Diagnostic Suite
// [KYA KARTA HAI] Pure event sourcing, optimistic concurrency control, CQRS materialized read projections, aur time-travel debugging verify karta hai.
// [KYU ZAROORI HAI] Confirm karna ki immutable event log se aggregate state 100% accurately reconstruct hoti hai aur concurrency collisions catch hoti hain.
// [KAISE CHALTA HAI] Program Main executes 3 automated tests: Concurrency Conflict, Materialized Projection, and Time-Travel Replay.
// [OUTPUT / SIDE EFFECT] Detailed console logs and clean exit code 0.
// [VARIATIONS TO TRY] Rebuild projections by replaying from version 0.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Guid, Exception types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Threading tasks import karta hai async entrypoint ke liye.
// [KYU ZAROORI HAI] Async task execution support.
// [KAISE CHALTA HAI] Tasks loaded.
// [OUTPUT / SIDE EFFECT] Async runtime ready.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Domain aggregates namespace import karta hai.
// [KYU ZAROORI HAI] SourcedOrderAggregate reference ke liye.
// [KAISE CHALTA HAI] References local aggregates namespace.
// [OUTPUT / SIDE EFFECT] Aggregate types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Aggregates;

// [KYA KARTA HAI] Storage namespace import karta hai.
// [KYU ZAROORI HAI] InMemoryEventStore and OptimisticConcurrencyException references.
// [KAISE CHALTA HAI] References local storage namespace.
// [OUTPUT / SIDE EFFECT] Event store types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Storage;

// [KYA KARTA HAI] Projections namespace import karta hai.
// [KYU ZAROORI HAI] OrderSummaryReadModelProjection reference ke liye.
// [KAISE CHALTA HAI] References local projections namespace.
// [OUTPUT / SIDE EFFECT] Projection types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Projections;

// [KYA KARTA HAI] Time travel namespace import karta hai.
// [KYU ZAROORI HAI] TimeTravelDebugger reference ke liye.
// [KAISE CHALTA HAI] References local time travel namespace.
// [OUTPUT / SIDE EFFECT] Debugger types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.TimeTravel;

// [KYA KARTA HAI] Program entry point class define karta hai.
// [KYU ZAROORI HAI] Executable console application container.
// [KAISE CHALTA HAI] C# 13 top-level application wrapper.
// [OUTPUT / SIDE EFFECT] Entry class.
// [VARIATIONS TO TRY] Top-level statements.
public class Program
{
    // [KYA KARTA HAI] Application main asynchronous entrypoint.
    // [KYU ZAROORI HAI] Test suite execute karna.
    // [KAISE CHALTA HAI] CLR boots runtime and runs Main().
    // [OUTPUT / SIDE EFFECT] Test results printed to stdout.
    // [VARIATIONS TO TRY] String[] args.
    public static async Task Main()
    {
        // [KYA KARTA HAI] Banner print karta hai.
        // [KYU ZAROORI HAI] Visual separation.
        // [KAISE CHALTA HAI] Console.WriteLine.
        // [OUTPUT / SIDE EFFECT] Banner displayed.
        // [VARIATIONS TO TRY] Color logging.
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 27: Event Sourcing & CQRS Projections Engine    ");
        Console.WriteLine("====================================================================\n");

        // [KYA KARTA HAI] Shared event store aur read projection initialize karta hai.
        // [KYU ZAROORI HAI] SUT (System Under Test) setup karna.
        // [KAISE CHALTA HAI] Instantiates store and projection.
        // [OUTPUT / SIDE EFFECT] Store and projection alive.
        // [VARIATIONS TO TRY] Multiple projections.
        var eventStore = new InMemoryEventStore();
        var readProjection = new OrderSummaryReadModelProjection(eventStore);
        var timeTravelDebugger = new TimeTravelDebugger(eventStore);

        var orderId = Guid.NewGuid();
        const string customerId = "CUST-9921";
        const string darkStoreId = "DS-NOIDA-62";

        // [TEST 1] Stream Append & Optimistic Concurrency Conflict Detection.
        Console.WriteLine("[TEST 1] Testing Event Stream Append & Optimistic Concurrency Protection...");
        
        // Step 1: Create aggregate and add 2 items
        var order = SourcedOrderAggregate.Create(orderId, customerId, darkStoreId);
        order.AddItem("BUTTER-01", "Amul Butter 500g", 2, 275m);
        order.AddItem("MILK-01", "Amul Taaza Milk 1L", 1, 68m);

        var uncommitted1 = order.GetUncommittedEvents();
        if (uncommitted1.Count != 3)
            throw new Exception($"Expected 3 uncommitted events, but found {uncommitted1.Count}!");

        // Commit to store at expected version 0 (initial empty stream)
        await eventStore.AppendEventsAsync(orderId, expectedVersion: 0, uncommitted1);
        order.ClearUncommittedEvents();
        Console.WriteLine("  -> Successfully committed Genesis events (OrderCreated + 2 ItemAdded): PASSED");

        // Step 2: Attempt concurrent write with stale expected version 0
        bool concurrencyCaught = false;
        try
        {
            var staleOrder = SourcedOrderAggregate.Create(orderId, customerId, darkStoreId);
            staleOrder.AddItem("BREAD-01", "Harvest Gold Bread", 1, 45m);
            // Stale expected version 0 (store is already at version 3!)
            await eventStore.AppendEventsAsync(orderId, expectedVersion: 0, staleOrder.GetUncommittedEvents());
        }
        catch (OptimisticConcurrencyException ex)
        {
            concurrencyCaught = true;
            Console.WriteLine($"  -> Caught expected Concurrency Collision: {ex.Message}: PASSED");
        }

        if (!concurrencyCaught)
            throw new Exception("Optimistic Concurrency failed to detect stale stream version conflict!");

        // [TEST 2] Lifecycle Progression & Materialized Read Model Verification.
        Console.WriteLine("\n[TEST 2] Testing Lifecycle Progression & CQRS Materialized Read Model...");

        // Rehydrate aggregate to current version 3
        var rehydrated = SourcedOrderAggregate.RehydrateFromHistory(await eventStore.GetStreamAsync(orderId));
        rehydrated.AllocateStock(darkStoreId);
        rehydrated.CapturePayment("TXN-UPI-99412", rehydrated.TotalAmount);
        rehydrated.DispatchRider("RIDER-77", "Vikram Singh");
        rehydrated.MarkDelivered(DateTimeOffset.UtcNow);

        var uncommitted2 = rehydrated.GetUncommittedEvents();
        await eventStore.AppendEventsAsync(orderId, expectedVersion: rehydrated.Version, uncommitted2);
        rehydrated.ClearUncommittedEvents();

        // Query Materialized Read Model
        var summaryView = readProjection.GetById(orderId);
        if (summaryView == null)
            throw new Exception("Materialized read projection view not found for order!");

        if (summaryView.Status != "Delivered")
            throw new Exception($"Expected read model status 'Delivered', but got '{summaryView.Status}'!");

        if (summaryView.ItemCount != 3)
            throw new Exception($"Expected total item count 3, but got {summaryView.ItemCount}!");

        if (summaryView.TotalAmount != (2 * 275m + 68m))
            throw new Exception($"Expected total amount ₹618, but got ₹{summaryView.TotalAmount}!");

        if (summaryView.RiderName != "Vikram Singh")
            throw new Exception($"Expected rider 'Vikram Singh', but got '{summaryView.RiderName}'!");

        Console.WriteLine($"  -> Materialized View Status: '{summaryView.Status}'");
        Console.WriteLine($"  -> Materialized Total Amount: ₹{summaryView.TotalAmount}");
        Console.WriteLine($"  -> Materialized Item Count: {summaryView.ItemCount}");
        Console.WriteLine($"  -> Materialized Rider: {summaryView.RiderName}");
        Console.WriteLine("  -> CQRS Materialized Read Model fully synchronized: PASSED");

        // [TEST 3] Temporal Time-Travel Debugging & State Reconstruction.
        Console.WriteLine("\n[TEST 3] Testing Temporal Time-Travel Debugging (Replay to Historical Version)...");

        // Replay order back to Version 2 (Right after Amul Butter was added, before Milk or Payment!)
        var historicalV2 = await timeTravelDebugger.ReplayToVersionAsync(orderId, targetVersion: 2);

        if (historicalV2.Version != 2)
            throw new Exception($"Expected historical version 2, but got {historicalV2.Version}!");

        if (historicalV2.Status != OrderState.Created)
            throw new Exception($"Expected status at Version 2 to be 'Created', but got '{historicalV2.Status}'!");

        if (historicalV2.Items.Count != 1)
            throw new Exception($"Expected 1 item at Version 2, but got {historicalV2.Items.Count}!");

        if (historicalV2.RiderId != null)
            throw new Exception("Rider should NOT be assigned at Version 2 historical cutoff!");

        Console.WriteLine($"  -> Reconstructed State at Version 2: Status='{historicalV2.Status}', Items={historicalV2.Items.Count}, Amount=₹{historicalV2.TotalAmount}");
        Console.WriteLine($"  -> Rider Assigned at V2: {(historicalV2.RiderName ?? "None (Correct)")}");
        Console.WriteLine("  -> Temporal Time-Travel State Reconstruction: PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All Event Sourcing, CQRS & Time-Travel checks PASSED!    ");
        Console.WriteLine("====================================================================\n");
    }
}
