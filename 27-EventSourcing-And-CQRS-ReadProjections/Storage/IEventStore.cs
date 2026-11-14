// ==============================================================================================
// KiranaHub Event Sourcing: Append-Only Event Store Contract with Optimistic Concurrency
// [KYA KARTA HAI] Immutable event stream persistence, version concurrency control, aur stream retrieval ka interface define karta hai.
// [KYU ZAROORI HAI] Two simultaneous requests same order aggregate ko corrupt na kar sakein (Lost Update prevention).
// [KAISE CHALTA HAI] ExpectedVersion parameter check karke append karta hai, fail hone par OptimisticConcurrencyException throw karta hai.
// [OUTPUT / SIDE EFFECT] Type-safe event store interface.
// [VARIATIONS TO TRY] Marten with PostgreSQL event stream or EventStoreDB gRPC client.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, Exception, Action types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Core types available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] IReadOnlyList for immutable event sequence.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task based async methods support.
// [KAISE CHALTA HAI] Tasks loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Domain events namespace import karta hai.
// [KYU ZAROORI HAI] IDomainEvent interface reference ke liye.
// [KAISE CHALTA HAI] References local events namespace.
// [OUTPUT / SIDE EFFECT] Event types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.EventSourcing.Events;

// [KYA KARTA HAI] Namespace declare karta hai storage interfaces ke liye.
// [KYU ZAROORI HAI] Storage logic encapsulate karna.
// [KAISE CHALTA HAI] Scoped to KiranaHub.EventSourcing.Storage.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] None.
namespace KiranaHub.EventSourcing.Storage;

// [KYA KARTA HAI] Optimistic concurrency conflict exception class.
// [KYU ZAROORI HAI] Concurrency race condition pakadne par caller ko alert karna.
// [KAISE CHALTA HAI] Thrown when currentVersion != expectedVersion in stream append.
// [OUTPUT / SIDE EFFECT] Concurrency exception type.
// [VARIATIONS TO TRY] Include attempted events in exception.
public class OptimisticConcurrencyException : Exception
{
    // [KYA KARTA HAI] Target aggregate stream ID.
    // [KYU ZAROORI HAI] Identify karna kis order stream me conflict aaya.
    // [KAISE CHALTA HAI] Guid property.
    // [OUTPUT / SIDE EFFECT] Stream ID stored.
    // [VARIATIONS TO TRY] None.
    public Guid AggregateId { get; }

    // [KYA KARTA HAI] Caller dwara expected version.
    // [KYU ZAROORI HAI] Conflict diagnostics ke liye.
    // [KAISE CHALTA HAI] Integer version.
    // [OUTPUT / SIDE EFFECT] Expected version stored.
    // [VARIATIONS TO TRY] None.
    public int ExpectedVersion { get; }

    // [KYA KARTA HAI] Actual store me existing version.
    // [KYU ZAROORI HAI] Real store version verify karna.
    // [KAISE CHALTA HAI] Integer version.
    // [OUTPUT / SIDE EFFECT] Actual version stored.
    // [VARIATIONS TO TRY] None.
    public int ActualVersion { get; }

    // [KYA KARTA HAI] Exception constructor initialize karta hai.
    // [KYU ZAROORI HAI] Informative diagnostic error message construct karna.
    // [KAISE CHALTA HAI] Builds formatted error string and sets properties.
    // [OUTPUT / SIDE EFFECT] Exception instance ready.
    // [VARIATIONS TO TRY] None.
    public OptimisticConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"[CONCURRENCY CONFLICT] Stream '{aggregateId}' expected version {expectedVersion}, but store currently at version {actualVersion}!")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

// [KYA KARTA HAI] Append-only Event Store interface contract.
// [KYU ZAROORI HAI] Storage operations ko business logic se abstract out karna.
// [KAISE CHALTA HAI] Defines async append and read stream operations.
// [OUTPUT / SIDE EFFECT] Interface contract.
// [VARIATIONS TO TRY] Add snapshotting methods SaveSnapshot/GetSnapshot.
public interface IEventStore
{
    // [KYA KARTA HAI] Events ko aggregate stream me atomically append karta hai.
    // [KYU ZAROORI HAI] Optimistic concurrency check ke sath safe append ensure karna.
    // [KAISE CHALTA HAI] Validates expectedVersion against current stream version.
    // [OUTPUT / SIDE EFFECT] Persists events to append-only log.
    // [VARIATIONS TO TRY] Pass cancellation token.
    Task AppendEventsAsync(Guid aggregateId, int expectedVersion, IReadOnlyList<IDomainEvent> events);

    // [KYA KARTA HAI] Aggregate ke event stream ko load karta hai fromVersion se toVersion tak.
    // [KYU ZAROORI HAI] Aggregate state reconstruct karne ya time-travel debug karne ke liye.
    // [KAISE CHALTA HAI] Returns ordered slice of domain events.
    // [OUTPUT / SIDE EFFECT] Event sequence returned.
    // [VARIATIONS TO TRY] Stream reading with IAsyncEnumerable.
    Task<IReadOnlyList<IDomainEvent>> GetStreamAsync(Guid aggregateId, int fromVersion = 0, int toVersion = int.MaxValue);

    // [KYA KARTA HAI] Read projections ke liye event subscriber register karta hai.
    // [KYU ZAROORI HAI] Har new event append hone par read model projections ko real-time update karna.
    // [KAISE CHALTA HAI] Registers async callback handler.
    // [OUTPUT / SIDE EFFECT] Subscriber registered.
    // [VARIATIONS TO TRY] Outbox / Change Data Capture (CDC) stream integration.
    void Subscribe(Func<IDomainEvent, Task> onEventAppended);
}
