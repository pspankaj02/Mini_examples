// ==============================================================================================
// KiranaHub Event Sourcing: High-Fidelity In-Memory Event Store Implementation
// [KYA KARTA HAI] In-memory concurrent dictionary me event streams ko persist karta hai with lock-safe optimistic concurrency.
// [KYU ZAROORI HAI] Zero external dependencies ke sath EventStoreDB / PostgreSQL append-only semantics accurately simulate karna.
// [KAISE CHALTA HAI] Stream-level sync locks se race conditions guard karta hai aur subscribers ko events dispatch karta hai.
// [OUTPUT / SIDE EFFECT] Fast, thread-safe append-only storage.
// [VARIATIONS TO TRY] Disk-backed SQLite WAL event store.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Guid, Func, Action types ke liye.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, IReadOnlyList provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] ConcurrentDictionary for thread-safe stream routing.
// [KAISE CHALTA HAI] Thread-safe hash table loaded.
// [OUTPUT / SIDE EFFECT] Multi-threaded dictionary available.
// [VARIATIONS TO TRY] ReaderWriterLockSlim.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Stream slicing, Where, OrderBy, ToList operations.
// [KAISE CHALTA HAI] LINQ operators loaded.
// [OUTPUT / SIDE EFFECT] Query operators ready.
// [VARIATIONS TO TRY] Span slices.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task asynchronous workflows ke liye.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Domain events namespace import karta hai.
// [KYU ZAROORI HAI] IDomainEvent references.
// [KAISE CHALTA HAI] Local event namespace loaded.
// [OUTPUT / SIDE EFFECT] Event types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Events;

// [KYA KARTA HAI] Namespace declare karta hai storage implementation ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.Storage.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] None.
namespace KiranaHub.EventSourcing.Storage;

// [KYA KARTA HAI] InMemoryEventStore class define karta hai implementing IEventStore.
// [KYU ZAROORI HAI] Production-grade event store semantics in memory provide karna.
// [KAISE CHALTA HAI] Thread-safe dictionary keeps event lists mapped by AggregateId.
// [OUTPUT / SIDE EFFECT] Event store instance.
// [VARIATIONS TO TRY] Redis streams backed event store.
public class InMemoryEventStore : IEventStore
{
    // [KYA KARTA HAI] Event streams storage map: AggregateId -> Ordered List of IDomainEvent.
    // [KYU ZAROORI HAI] Har aggregate ka apna isolated linear timeline maintain karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary handles concurrent stream creations safely.
    // [OUTPUT / SIDE EFFECT] In-memory event stream tables.
    // [VARIATIONS TO TRY] Sharded memory tables.
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _streams = new();

    // [KYA KARTA HAI] Stream-level sync locks map: AggregateId -> lock object.
    // [KYU ZAROORI HAI] Do concurrent threads same aggregate stream me simultaneously append na karein.
    // [KAISE CHALTA HAI] ConcurrentDictionary caches dedicated lock objects per aggregate ID.
    // [OUTPUT / SIDE EFFECT] Fine-grained per-stream concurrency isolation.
    // [VARIATIONS TO TRY] SemaphoreSlim for async locks.
    private readonly ConcurrentDictionary<Guid, object> _streamLocks = new();

    // [KYA KARTA HAI] Event appended subscribers callback list.
    // [KYU ZAROORI HAI] Read model projections ko new events forward karna.
    // [KAISE CHALTA HAI] Thread-safe list of async delegates.
    // [OUTPUT / SIDE EFFECT] Subscribers registry.
    // [VARIATIONS TO TRY] Channel<IDomainEvent> for async decoupled publishing.
    private readonly List<Func<IDomainEvent, Task>> _subscribers = new();

    // [KYA KARTA HAI] Subscribers list sync lock object.
    // [KYU ZAROORI HAI] Registration time me thread safety ensure karna.
    // [KAISE CHALTA HAI] Standard lock object.
    // [OUTPUT / SIDE EFFECT] Lock token.
    // [VARIATIONS TO TRY] None.
    private readonly object _subscribersLock = new();

    // [KYA KARTA HAI] Events ko aggregate stream me atomically append karta hai.
    // [KYU ZAROORI HAI] Optimistic concurrency verification ke sath stream append karna.
    // [KAISE CHALTA HAI] Acquires per-stream lock, checks version match, assigns versions, appends, and notifies subscribers.
    // [OUTPUT / SIDE EFFECT] New events appended to stream.
    // [VARIATIONS TO TRY] Batch persistence to durable disk.
    public async Task AppendEventsAsync(Guid aggregateId, int expectedVersion, IReadOnlyList<IDomainEvent> events)
    {
        // [KYA KARTA HAI] Empty events array check.
        // [KYU ZAROORI HAI] Zero-event appends par unneeded processing skip karna.
        // [KAISE CHALTA HAI] Compares count to 0.
        // [OUTPUT / SIDE EFFECT] Early exit if empty.
        // [VARIATIONS TO TRY] None.
        if (events == null || events.Count == 0) return;

        // [KYA KARTA HAI] Stream-specific lock object acquire karta hai.
        // [KYU ZAROORI HAI] Same stream par concurrent writers ko serialize karna.
        // [KAISE CHALTA HAI] GetOrAdd from _streamLocks.
        // [OUTPUT / SIDE EFFECT] Dedicated lock token for this aggregate.
        // [VARIATIONS TO TRY] Striped locking.
        var lockObj = _streamLocks.GetOrAdd(aggregateId, _ => new object());

        List<IDomainEvent> eventsToPublish = new();

        lock (lockObj)
        {
            // [KYA KARTA HAI] Existing stream list retrieve ya initialize karta hai.
            // [KYU ZAROORI HAI] Target stream buffer access karna.
            // [KAISE CHALTA HAI] GetOrAdd from _streams.
            // [OUTPUT / SIDE EFFECT] Stream list ready.
            // [VARIATIONS TO TRY] Read from file system.
            var stream = _streams.GetOrAdd(aggregateId, _ => new List<IDomainEvent>());

            // [KYA KARTA HAI] Current stream version evaluate karta hai.
            // [KYU ZAROORI HAI] Check karna ki store me kitne events pehle se commit hain.
            // [KAISE CHALTA HAI] stream.Count represents current head version.
            // [OUTPUT / SIDE EFFECT] Integer currentVersion.
            // [VARIATIONS TO TRY] Version metadata property.
            var currentVersion = stream.Count;

            // [KYA KARTA HAI] Optimistic Concurrency check execute karta hai!
            // [KYU ZAROORI HAI] Agar doosra process stream me pehle hi append kar chuka hai, toh overwrite rokhna.
            // [KAISE CHALTA HAI] Compares expectedVersion against currentVersion.
            // [OUTPUT / SIDE EFFECT] Throws OptimisticConcurrencyException on mismatch.
            // [VARIATIONS TO TRY] Retry with reloaded aggregate.
            if (expectedVersion != currentVersion)
            {
                throw new OptimisticConcurrencyException(aggregateId, expectedVersion, currentVersion);
            }

            // [KYA KARTA HAI] Naye events ko sequential version number assign karke stream me append karta hai.
            // [KYU ZAROORI HAI] Har event ka monotonic incrementing version hona mandatory hai.
            // [KAISE CHALTA HAI] Loops through events, sets Version = currentVersion + 1, adds to stream.
            // [OUTPUT / SIDE EFFECT] Stream updated with versioned events.
            // [VARIATIONS TO TRY] Immutable with-expression copy.
            foreach (var evt in events)
            {
                currentVersion++;
                var versionedEvt = evt.WithVersion(currentVersion);
                stream.Add(versionedEvt);
                eventsToPublish.Add(versionedEvt);
            }
        }

        // [KYA KARTA HAI] Naye committed events ko registered read model projections me publish karta hai.
        // [KYU ZAROORI HAI] Read side projections ko immediately update karna (CQRS synchronization).
        // [KAISE CHALTA HAI] Iterates subscribers list and invokes async delegates.
        // [OUTPUT / SIDE EFFECT] Projections updated.
        // [VARIATIONS TO TRY] Background queue publishing.
        List<Func<IDomainEvent, Task>> subs;
        lock (_subscribersLock)
        {
            subs = new List<Func<IDomainEvent, Task>>(_subscribers);
        }

        foreach (var evt in eventsToPublish)
        {
            foreach (var sub in subs)
            {
                await sub(evt);
            }
        }
    }

    // [KYA KARTA HAI] Stream ko slice karke load karta hai fromVersion se toVersion tak.
    // [KYU ZAROORI HAI] Aggregate state reconstruction aur Time-Travel debugging ke liye events retrieve karna.
    // [KAISE CHALTA HAI] Queries _streams map and filters by Version range.
    // [OUTPUT / SIDE EFFECT] Ordered event slice returned.
    // [VARIATIONS TO TRY] Stream backwards iteration.
    public Task<IReadOnlyList<IDomainEvent>> GetStreamAsync(Guid aggregateId, int fromVersion = 0, int toVersion = int.MaxValue)
    {
        // [KYA KARTA HAI] Stream presence check karta hai.
        // [KYU ZAROORI HAI] Agar stream exist nahi karti toh empty array return karna.
        // [KAISE CHALTA HAI] TryGetValue on _streams.
        // [OUTPUT / SIDE EFFECT] Returns empty list if not found.
        // [VARIATIONS TO TRY] Throw StreamNotFoundException.
        if (!_streams.TryGetValue(aggregateId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }

        var lockObj = _streamLocks.GetOrAdd(aggregateId, _ => new object());
        lock (lockObj)
        {
            // [KYA KARTA HAI] Version boundaries ke according slice karta hai.
            // [KYU ZAROORI HAI] Time-travel up to specific version allow karna.
            // [KAISE CHALTA HAI] LINQ Where and OrderBy Version.
            // [OUTPUT / SIDE EFFECT] Filtered slice.
            // [VARIATIONS TO TRY] Binary search index.
            var slice = stream
                .Where(e => e.Version >= fromVersion && e.Version <= toVersion)
                .OrderBy(e => e.Version)
                .ToList();

            return Task.FromResult<IReadOnlyList<IDomainEvent>>(slice);
        }
    }

    // [KYA KARTA HAI] Projection subscriber register karta hai.
    // [KYU ZAROORI HAI] Read side ko event bus se hook karna.
    // [KAISE CHALTA HAI] Adds delegate to _subscribers under lock.
    // [OUTPUT / SIDE EFFECT] Subscriber stored.
    // [VARIATIONS TO TRY] Unsubscribe token support.
    public void Subscribe(Func<IDomainEvent, Task> onEventAppended)
    {
        lock (_subscribersLock)
        {
            _subscribers.Add(onEventAppended);
        }
    }
}
