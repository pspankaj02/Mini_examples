// ==============================================================================================
// KiranaHub Event Sourcing: Temporal Time-Travel Debugging & State Reconstruction Engine
// [KYA KARTA HAI] Kisi bhi order stream ko historical past me kisi specific timestamp ya version tak replay karke aggregate state reconstruct karta hai.
// [KYU ZAROORI HAI] Customer complaints aur financial audits me exact historical state inspect karna (e.g. "Payment hone se 1 minute pehle cart me kya kya tha?").
// [KAISE CHALTA HAI] Event store se raw events load karta hai, target cutoff filter lagata hai aur SourcedOrderAggregate.RehydrateFromHistory() call karta hai.
// [OUTPUT / SIDE EFFECT] Historical aggregate snapshot without modifying current live state.
// [VARIATIONS TO TRY] Generate historical timeline visual graph.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Guid, DateTimeOffset types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] IReadOnlyList collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Where timestamp filtering ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] Query operators available.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task async methods ke liye.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Domain aggregates namespace import karta hai.
// [KYU ZAROORI HAI] SourcedOrderAggregate reference ke liye.
// [KAISE CHALTA HAI] References local aggregates namespace.
// [OUTPUT / SIDE EFFECT] Aggregate types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Aggregates;

// [KYA KARTA HAI] Storage namespace import karta hai.
// [KYU ZAROORI HAI] IEventStore reference ke liye.
// [KAISE CHALTA HAI] References local storage namespace.
// [OUTPUT / SIDE EFFECT] Event store types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Storage;

// [KYA KARTA HAI] Namespace declare karta hai time travel debugging ke liye.
// [KYU ZAROORI HAI] Architectural separation of diagnostic tools.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.TimeTravel.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Diagnostics.TimeTravel.
namespace KiranaHub.EventSourcing.TimeTravel;

// [KYA KARTA HAI] TimeTravelDebugger class define karta hai.
// [KYU ZAROORI HAI] Historical state replay methods provide karna.
// [KAISE CHALTA HAI] Queries event store and filters streams.
// [OUTPUT / SIDE EFFECT] Time-travel inspection engine.
// [VARIATIONS TO TRY] Bi-directional stepping (step forward / step backward).
public class TimeTravelDebugger
{
    // [KYA KARTA HAI] Event store dependency reference.
    // [KYU ZAROORI HAI] Event streams load karne ke liye.
    // [KAISE CHALTA HAI] Injected via constructor.
    // [OUTPUT / SIDE EFFECT] Event store handle stored.
    // [VARIATIONS TO TRY] None.
    private readonly IEventStore _eventStore;

    // [KYA KARTA HAI] Time travel debugger constructor initialize karta hai.
    // [KYU ZAROORI HAI] IEventStore dependency receive karna.
    // [KAISE CHALTA HAI] Assigns private field.
    // [OUTPUT / SIDE EFFECT] Debugger instance ready.
    // [VARIATIONS TO TRY] None.
    public TimeTravelDebugger(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    // [KYA KARTA HAI] Specific Version sequence number tak aggregate replay karta hai.
    // [KYU ZAROORI HAI] Batana ki Version 2 ya Version 4 par order kis condition me tha.
    // [KAISE CHALTA HAI] Loads stream slice with toVersion: targetVersion, rehydrates aggregate.
    // [OUTPUT / SIDE EFFECT] Historical aggregate reconstructed at target version.
    // [VARIATIONS TO TRY] Return diff between versions.
    public async Task<SourcedOrderAggregate> ReplayToVersionAsync(Guid orderId, int targetVersion)
    {
        // [KYA KARTA HAI] Stream slice load karta hai version 1 se targetVersion tak.
        // [KYU ZAROORI HAI] Subsequent events ignore karke historical cutoff lagana.
        // [KAISE CHALTA HAI] Calls _eventStore.GetStreamAsync with toVersion.
        // [OUTPUT / SIDE EFFECT] Sliced event history.
        // [VARIATIONS TO TRY] Throw error if targetVersion is beyond head.
        var history = await _eventStore.GetStreamAsync(orderId, fromVersion: 1, toVersion: targetVersion);

        // [KYA KARTA HAI] Aggregate state rehydrate karta hai.
        // [KYU ZAROORI HAI] Reconstructed object create karna.
        // [KAISE CHALTA HAI] Calls SourcedOrderAggregate.RehydrateFromHistory.
        // [OUTPUT / SIDE EFFECT] Historical aggregate state.
        // [VARIATIONS TO TRY] Snapshot lookup if version > 1000.
        return SourcedOrderAggregate.RehydrateFromHistory(history);
    }

    // [KYA KARTA HAI] Specific wall-clock UTC Timestamp tak aggregate replay karta hai.
    // [KYU ZAROORI HAI] Batana ki specific time (e.g. 19:30:00 UTC) par order ka status kya tha.
    // [KAISE CHALTA HAI] Loads full stream, filters events OccurredOnUtc <= targetTimestamp, rehydrates aggregate.
    // [OUTPUT / SIDE EFFECT] Historical aggregate reconstructed at target timestamp.
    // [VARIATIONS TO TRY] Interpolate delivery coordinates.
    public async Task<SourcedOrderAggregate> ReplayToTimestampAsync(Guid orderId, DateTimeOffset targetTimestamp)
    {
        // [KYA KARTA HAI] Poora stream load karta hai.
        // [KYU ZAROORI HAI] Saare events inspect karke timestamp filtering apply karna.
        // [KAISE CHALTA HAI] Calls _eventStore.GetStreamAsync.
        // [OUTPUT / SIDE EFFECT] Full event list.
        // [VARIATIONS TO TRY] Binary search on indexed timestamps.
        var fullHistory = await _eventStore.GetStreamAsync(orderId);

        // [KYA KARTA HAI] Target cutoff timestamp ke baad wale events strip off karta hai.
        // [KYU ZAROORI HAI] Cutoff point se aage ki information leak hone se rokna.
        // [KAISE CHALTA HAI] LINQ Where OccurredOnUtc <= targetTimestamp.
        // [OUTPUT / SIDE EFFECT] Filtered event list.
        // [VARIATIONS TO TRY] Exact second rounding.
        var filteredHistory = fullHistory
            .Where(e => e.OccurredOnUtc <= targetTimestamp)
            .ToList();

        // [KYA KARTA HAI] Filtered events se historical state rehydrate karta hai.
        // [KYU ZAROORI HAI] Exact temporal reconstruction.
        // [KAISE CHALTA HAI] Calls RehydrateFromHistory.
        // [OUTPUT / SIDE EFFECT] Reconstructed aggregate at exact timestamp.
        // [VARIATIONS TO TRY] Return metadata along with state.
        return SourcedOrderAggregate.RehydrateFromHistory(filteredHistory);
    }
}
