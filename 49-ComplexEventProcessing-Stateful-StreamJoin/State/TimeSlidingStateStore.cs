namespace KiranaHub.StreamingCep.State;

/// <summary>
/// Bounded Sliding Temporal State Store for Stream Processing.
/// Maintains events within an active time window and automatically purges
/// expired historical state past the trailing watermark boundary.
/// Prevents Out-Of-Memory (OOM) leaks in continuous 24/7 stream processing.
/// </summary>
public sealed class TimeSlidingStateStore<TKey, TEvent> where TKey : notnull
{
    // [KYA KARTA HAI]: Primary state map: Key -> Sorted List of (Timestamp, Event).
    // [KYU ZAROORI HAI]: Fast lookups by key (e.g. OrderId) while preserving event-time order.
    // [KAISE CHALTA HAI]: Dictionary of key to chronological event list.
    // [OUTPUT / SIDE EFFECT]: In-memory multi-event state.
    // [VARIATIONS TO TRY]: RocksDB-backed off-heap state backend.
    private readonly Dictionary<TKey, List<(long Timestamp, TEvent Event)>> _state = new();

    // [KYA KARTA HAI]: Total currently stored events count.
    // [KYU ZAROORI HAI]: Memory usage and state retention monitoring.
    // [KAISE CHALTA HAI]: Integer counter tracking inserts and evictions.
    // [OUTPUT / SIDE EFFECT]: Active elements count.
    // [VARIATIONS TO TRY]: Atomic count for multi-threaded state backends.
    private int _totalEventsCount = 0;

    // [KYA KARTA HAI]: Naya state event append karta hai.
    // [KYU ZAROORI HAI]: Streaming join candidate windows ko RAM me hold karna.
    // [KAISE CHALTA HAI]: Key search karke chronological list me (timestamp, event) add karta hai.
    // [OUTPUT / SIDE EFFECT]: State size increments by 1.
    // [VARIATIONS TO TRY]: Binary search insertion for out-of-order inserts within window.
    public void Put(TKey key, long timestamp, TEvent eventData)
    {
        if (!_state.TryGetValue(key, out var list))
        {
            list = new List<(long, TEvent)>();
            _state[key] = list;
        }

        list.Add((timestamp, eventData));
        _totalEventsCount++;
    }

    // [KYA KARTA HAI]: Key ke corresponding saare active window events return karta hai.
    // [KYU ZAROORI HAI]: Stream join matching ke waqt candidate events probe karna.
    // [KAISE CHALTA HAI]: Dictionary lookup; returns matching list or empty.
    // [OUTPUT / SIDE EFFECT]: List of (Timestamp, TEvent).
    // [VARIATIONS TO TRY]: Range query by [minTime, maxTime].
    public IReadOnlyList<(long Timestamp, TEvent Event)> Get(TKey key)
    {
        return _state.TryGetValue(key, out var list) ? list : Array.Empty<(long, TEvent)>();
    }

    // [KYA KARTA HAI]: Watermark boundary se purane saare expired events ko RAM se delete karta hai.
    // [KYU ZAROORI HAI]: Memory leak prevention: purane un-matched events ko permanently garbage collect karna.
    // [KAISE CHALTA HAI]: Timestamp < watermark threshold check karke list se remove karta hai; empty lists delete karta hai.
    // [OUTPUT / SIDE EFFECT]: Expired events evicted; memory reclaimed.
    // [VARIATIONS TO TRY]: Bulk batch eviction.
    public int EvictOlderThan(long watermarkTimestamp)
    {
        int evictedCount = 0;
        var keysToRemove = new List<TKey>();

        foreach (var (key, list) in _state)
        {
            int removedFromList = list.RemoveAll(item => item.Timestamp < watermarkTimestamp);
            evictedCount += removedFromList;
            _totalEventsCount -= removedFromList;

            if (list.Count == 0)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _state.Remove(key);
        }

        return evictedCount;
    }

    public int TotalEventsCount => _totalEventsCount;
    public int ActiveKeysCount => _state.Count;
}
