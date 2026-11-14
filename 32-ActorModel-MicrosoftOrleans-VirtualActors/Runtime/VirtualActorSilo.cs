namespace KiranaHub.VirtualActors.Runtime;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Microsoft Orleans Silo runtime ka in-memory virtual actor host implement karta hai.
// [KYU ZAROORI HAI]: Virtual Actors ki location transparency, on-demand activation, aur idle garbage collection ko manage karna.
// [KAISE CHALTA HAI]: ConcurrentDictionary me active grains cache karta hai, on-demand instantiate karta hai, aur background sweeper se inactive grains sweep karta hai.
// [OUTPUT / SIDE EFFECT]: High-performance virtual actor runtime host.
// [VARIATIONS TO TRY]: Multi-silo clustering over TCP socket with consistent hashing placement.
public class VirtualActorSilo : IAsyncDisposable
{
    // [KYA KARTA HAI]: Active grain instances ko Type aur GrainId ke composite key ke against cache karta hai.
    // [KYU ZAROORI HAI]: O(1) grain address resolution provide karna bina database query ke.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, IVirtualGrain> thread-safe collection.
    // [OUTPUT / SIDE EFFECT]: In-memory active grain directory.
    // [VARIATIONS TO TRY]: WeakReference based cache use karein.
    private readonly ConcurrentDictionary<string, IVirtualGrain> _activeGrains = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Grain activation counter track karta hai.
    // [KYU ZAROORI HAI]: Total activations metric observe karna.
    // [KAISE CHALTA HAI]: Interlocked.Increment on grain activation.
    // [OUTPUT / SIDE EFFECT]: Long counter.
    // [VARIATIONS TO TRY]: OpenTelemetry counter metric me publish karein.
    private long _totalActivations = 0;

    // [KYA KARTA HAI]: Grain deactivation sweep counter track karta hai.
    // [KYU ZAROORI HAI]: Garbage collection efficiency monitor karna.
    // [KAISE CHALTA HAI]: Interlocked.Increment on grain eviction.
    // [OUTPUT / SIDE EFFECT]: Long counter.
    // [VARIATIONS TO TRY]: Memory freed estimator add karein.
    private long _totalDeactivations = 0;

    // [KYA KARTA HAI]: Total active grains count return karta hai.
    // [KYU ZAROORI HAI]: Current memory footprint monitor karna.
    // [KAISE CHALTA HAI]: _activeGrains.Count property read.
    // [OUTPUT / SIDE EFFECT]: Active grains count integer.
    // [VARIATIONS TO TRY]: Cluster-wide distributed count query.
    public int ActiveGrainCount => _activeGrains.Count;

    // [KYA KARTA HAI]: Total lifetime activations count return karta hai.
    // [KYU ZAROORI HAI]: Silo throughput metrics track karna.
    // [KAISE CHALTA HAI]: Interlocked.Read field.
    // [OUTPUT / SIDE EFFECT]: Long count.
    // [VARIATIONS TO TRY]: Activations/sec velocity rate calculate karein.
    public long TotalActivations => Interlocked.Read(ref _totalActivations);

    // [KYA KARTA HAI]: Total deactivations count return karta hai.
    // [KYU ZAROORI HAI]: GC activity metric.
    // [KAISE CHALTA HAI]: Interlocked.Read field.
    // [OUTPUT / SIDE EFFECT]: Long count.
    // [VARIATIONS TO TRY]: Grain churn ratio calculate karein.
    public long TotalDeactivations => Interlocked.Read(ref _totalDeactivations);

    // [KYA KARTA HAI]: Grain ke liye unique composite key generate karta hai.
    // [KYU ZAROORI HAI]: Ek hi ID ke different types (e.g. RiderGrain "101" vs CartGrain "101") me collision na ho.
    // [KAISE CHALTA HAI]: $"{typeof(T).FullName}:{grainId}".
    // [OUTPUT / SIDE EFFECT]: Composite key string.
    // [VARIATIONS TO TRY]: Struct-based zero-allocation composite key use karein.
    private static string BuildKey<T>(string grainId) => $"{typeof(T).FullName}:{grainId}";

    // [KYA KARTA HAI]: Virtual grain reference fetch karta hai, aur agar memory me nahi hai toh on-demand activate karta hai.
    // [KYU ZAROORI HAI]: Virtual Actor Model ka core principle: "Grains always exist conceptually; they are activated in memory on-demand".
    // [KAISE CHALTA HAI]: GetOrAdd me check karta hai, agar missing ho toh activator se new instance banata hai aur OnActivateAsync call karta hai.
    // [OUTPUT / SIDE EFFECT]: Activated and ready grain instance.
    // [VARIATIONS TO TRY]: Custom DI container factory se grains resolve karein.
    public async Task<TGrain> GetGrainAsync<TGrain>(string grainId) where TGrain : class, IVirtualGrain, new()
    {
        // [KYA KARTA HAI]: Argument null/empty check karta hai.
        // [KYU ZAROORI HAI]: Deterministic ID hona lazmi hai.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Exception on invalid ID.
        // [VARIATIONS TO TRY]: Format checking.
        ArgumentException.ThrowIfNullOrWhiteSpace(grainId);

        // [KYA KARTA HAI]: Composite key build karta hai.
        // [KYU ZAROORI HAI]: Dictionary lookup key nikalna.
        // [KAISE CHALTA HAI]: BuildKey<TGrain>(grainId).
        // [OUTPUT / SIDE EFFECT]: Lookup string.
        // [VARIATIONS TO TRY]: Span-based key.
        var key = BuildKey<TGrain>(grainId);

        // [KYA KARTA HAI]: Check karta hai kya grain pehle se active memory me maujood hai.
        // [KYU ZAROORI HAI]: Instant zero-allocation cache hit path.
        // [KAISE CHALTA HAI]: TryGetValue call.
        // [OUTPUT / SIDE EFFECT]: Existing grain return if found.
        // [VARIATIONS TO TRY]: Memory barrier check.
        if (_activeGrains.TryGetValue(key, out var existingGrain))
        {
            // [KYA KARTA HAI]: Existing grain ko TGrain me cast karke return karta hai.
            // [KYU ZAROORI HAI]: Fast path execution.
            // [KAISE CHALTA HAI]: Strong cast (TGrain)existingGrain.
            // [OUTPUT / SIDE EFFECT]: Cached grain reference.
            // [VARIATIONS TO TRY]: UnsafeAs for zero runtime cast overhead.
            return (TGrain)existingGrain;
        }

        // [KYA KARTA HAI]: Naya grain instance instantiate karta hai.
        // [KYU ZAROORI HAI]: On-demand grain activation (cold start).
        // [KAISE CHALTA HAI]: new TGrain() constructor call.
        // [OUTPUT / SIDE EFFECT]: Uninitialized grain instance.
        // [VARIATIONS TO TRY]: ActivatorUtilities with dependency injection.
        var grain = new TGrain();

        // [KYA KARTA HAI]: Grain ko identity key ke sath initialize karta hai agar base class VirtualGrainBase ho.
        // [KYU ZAROORI HAI]: Actor ko apni identity maloom honi chahiye.
        // [KAISE CHALTA HAI]: Pattern matching cast to VirtualGrainBase.
        // [OUTPUT / SIDE EFFECT]: GrainId established.
        // [VARIATIONS TO TRY]: Interface property setter.
        if (grain is VirtualGrainBase baseGrain)
        {
            baseGrain.Initialize(grainId);
        }

        // [KYA KARTA HAI]: Grain ko active registry me add karta hai.
        // [KYU ZAROORI HAI]: Subsequent calls ko wahi instance serve ho sake.
        // [KAISE CHALTA HAI]: GetOrAdd thread-safe addition.
        // [OUTPUT / SIDE EFFECT]: Registry updated.
        // [VARIATIONS TO TRY]: TryAdd with concurrency check.
        var effectiveGrain = _activeGrains.GetOrAdd(key, grain);

        // [KYA KARTA HAI]: Check karta hai kya hamara banaya gaya instance jeeta ya kisi concurrent thread ne pehle register kar diya.
        // [KYU ZAROORI HAI]: Duplicate activation hook aur overcounting prevent karna.
        // [KAISE CHALTA HAI]: ReferenceEquals check.
        // [OUTPUT / SIDE EFFECT]: Exactly 1 activation call per unique grain ID.
        // [VARIATIONS TO TRY]: SpinWait / Lazy initializer pattern.
        if (ReferenceEquals(effectiveGrain, grain))
        {
            // [KYA KARTA HAI]: Grain activation hook invoke karta hai.
            // [KYU ZAROORI HAI]: State hydration aur startup preparation sirf winning instance par honi chahiye.
            // [KAISE CHALTA HAI]: await grain.OnActivateAsync().
            // [OUTPUT / SIDE EFFECT]: Grain state becomes Active.
            // [VARIATIONS TO TRY]: Snapshot loading from distributed storage.
            await grain.OnActivateAsync().ConfigureAwait(false);

            // [KYA KARTA HAI]: Activations counter increment karta hai.
            // [KYU ZAROORI HAI]: Metric tracking.
            // [KAISE CHALTA HAI]: Interlocked.Increment.
            // [OUTPUT / SIDE EFFECT]: _totalActivations += 1.
            // [VARIATIONS TO TRY]: Meter counter.
            Interlocked.Increment(ref _totalActivations);
        }

        // [KYA KARTA HAI]: Effective grain return karta hai.
        // [KYU ZAROORI HAI]: Caller gets active actor proxy.
        // [KAISE CHALTA HAI]: Cast to TGrain.
        // [OUTPUT / SIDE EFFECT]: TGrain instance.
        // [VARIATIONS TO TRY]: Return interface proxy.
        return (TGrain)effectiveGrain;
    }

    // [KYA KARTA HAI]: Idle grains ko sweep karta hai jo specified timeout se zyada inactive rahe hain.
    // [KYU ZAROORI HAI]: Virtual Actor Model me grains indefinitely memory me nahi rehte; idle grains ko GC karna zaroori hai.
    // [KAISE CHALTA HAI]: Sabhi active grains iterate karta hai, LastAccessedUtc compare karta hai, aur stale grains ko deactivate karke remove karta hai.
    // [OUTPUT / SIDE EFFECT]: Inactive grains removed from memory, OnDeactivateAsync called.
    // [VARIATIONS TO TRY]: Memory pressure threshold ke mutabiq dynamic timeout calculate karein.
    public async Task<int> SweepIdleGrainsAsync(TimeSpan idleTimeout)
    {
        // [KYA KARTA HAI]: Cutoff UTC timestamp compute karta hai.
        // [KYU ZAROORI HAI]: Stale timestamp comparison reference point.
        // [KAISE CHALTA HAI]: DateTime.UtcNow - idleTimeout.
        // [OUTPUT / SIDE EFFECT]: Threshold timestamp.
        // [VARIATIONS TO TRY]: Configurable per grain type timeout.
        var cutoff = DateTime.UtcNow - idleTimeout;

        // [KYA KARTA HAI]: Deactivated grains counter.
        // [KYU ZAROORI HAI]: Sweep count reporting.
        // [KAISE CHALTA HAI]: Integer counter.
        // [OUTPUT / SIDE EFFECT]: Count of evicted grains.
        // [VARIATIONS TO TRY]: Memory bytes released tracking.
        int sweptCount = 0;

        // [KYA KARTA HAI]: Sabhi registered active grains iterate karta hai.
        // [KYU ZAROORI HAI]: Idle candidate inspection.
        // [KAISE CHALTA HAI]: KeyValuePair traversal over _activeGrains.
        // [OUTPUT / SIDE EFFECT]: Candidate inspection.
        // [VARIATIONS TO TRY]: Priority queue based LRU eviction list.
        foreach (var (key, grain) in _activeGrains)
        {
            // [KYA KARTA HAI]: Check karta hai kya grain cutoff time se pehle access hua tha.
            // [KYU ZAROORI HAI]: Active grains ko evict nahi karna hai, sirf idle ko karna hai.
            // [KAISE CHALTA HAI]: grain.LastAccessedUtc < cutoff.
            // [OUTPUT / SIDE EFFECT]: True if grain is idle candidate.
            // [VARIATIONS TO TRY]: Minimum keep-alive grace period add karein.
            if (grain.LastAccessedUtc < cutoff)
            {
                // [KYA KARTA HAI]: Registry se grain remove karta hai atomically.
                // [KYU ZAROORI HAI]: Memory leak prevention and dereferencing.
                // [KAISE CHALTA HAI]: _activeGrains.TryRemove(key, out var removedGrain).
                // [OUTPUT / SIDE EFFECT]: Grain removed from dictionary.
                // [VARIATIONS TO TRY]: Re-check last access before removal.
                if (_activeGrains.TryRemove(key, out var removedGrain))
                {
                    // [KYA KARTA HAI]: Grain deactivation hook invoke karta hai.
                    // [KYU ZAROORI HAI]: Unsaved state flush karna aur cleanup karna.
                    // [KAISE CHALTA HAI]: await removedGrain.OnDeactivateAsync().
                    // [OUTPUT / SIDE EFFECT]: Grain lifecycle state becomes Deactivated.
                    // [VARIATIONS TO TRY]: Catch and log deactivation exceptions.
                    await removedGrain.OnDeactivateAsync().ConfigureAwait(false);

                    // [KYA KARTA HAI]: Deactivations counter increment karta hai.
                    // [KYU ZAROORI HAI]: Global metrics.
                    // [KAISE CHALTA HAI]: Interlocked.Increment.
                    // [OUTPUT / SIDE EFFECT]: _totalDeactivations += 1.
                    // [VARIATIONS TO TRY]: Gauge metric.
                    Interlocked.Increment(ref _totalDeactivations);

                    // [KYA KARTA HAI]: Swept counter increment.
                    // [KYU ZAROORI HAI]: Local function return value.
                    // [KAISE CHALTA HAI]: sweptCount++.
                    // [OUTPUT / SIDE EFFECT]: Counter increment.
                    // [VARIATIONS TO TRY]: Aggregate statistics.
                    sweptCount++;
                }
            }
        }

        // [KYA KARTA HAI]: Total swept grains count return karta hai.
        // [KYU ZAROORI HAI]: Caller ko eviction yield report karna.
        // [KAISE CHALTA HAI]: return sweptCount.
        // [OUTPUT / SIDE EFFECT]: Evicted grains count.
        // [VARIATIONS TO TRY]: Eviction report object with IDs.
        return sweptCount;
    }

    // [KYA KARTA HAI]: Silo shutdown ke waqt sabhi remaining active grains ko gracefully deactivate karta hai.
    // [KYU ZAROORI HAI]: Clean application termination aur state consistency ensure karna.
    // [KAISE CHALTA HAI]: Sabhi grains par OnDeactivateAsync call karke collection clear karta hai.
    // [OUTPUT / SIDE EFFECT]: Silo memory completely drained.
    // [VARIATIONS TO TRY]: Parallel deactivation with Task.WhenAll.
    public async ValueTask DisposeAsync()
    {
        // [KYA KARTA HAI]: Remaining active grains iterate karta hai.
        // [KYU ZAROORI HAI]: Graceful shutdown sequence.
        // [KAISE CHALTA HAI]: foreach over _activeGrains.Values.
        // [OUTPUT / SIDE EFFECT]: Deactivation invocation.
        // [VARIATIONS TO TRY]: Timeout cancellation token.
        foreach (var grain in _activeGrains.Values)
        {
            // [KYA KARTA HAI]: Grain teardown hook call karta hai.
            // [KYU ZAROORI HAI]: Graceful cleanup.
            // [KAISE CHALTA HAI]: await grain.OnDeactivateAsync().
            // [OUTPUT / SIDE EFFECT]: Grain state Deactivated.
            // [VARIATIONS TO TRY]: Catch exceptions during shutdown.
            await grain.OnDeactivateAsync().ConfigureAwait(false);
        }

        // [KYA KARTA HAI]: Dictionary clear karta hai.
        // [KYU ZAROORI HAI]: Memory release for garbage collector.
        // [KAISE CHALTA HAI]: _activeGrains.Clear().
        // [OUTPUT / SIDE EFFECT]: Empty collection.
        // [VARIATIONS TO TRY]: GC.SuppressFinalize call.
        _activeGrains.Clear();

        // [KYA KARTA HAI]: SuppressFinalize call karta hai.
        // [KYU ZAROORI HAI]: Finalizer queue bypass karna for performance.
        // [KAISE CHALTA HAI]: GC.SuppressFinalize(this).
        // [OUTPUT / SIDE EFFECT]: Optimized GC.
        // [VARIATIONS TO TRY]: Log shutdown completion.
        GC.SuppressFinalize(this);
    }
}
