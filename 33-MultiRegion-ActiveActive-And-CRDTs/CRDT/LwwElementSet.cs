namespace KiranaHub.MultiRegionCRDT.CRDT;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Last-Write-Wins Element Set (LWW-Element-Set) CRDT implement karta hai.
// [KYU ZAROORI HAI]: Multi-region active-active datacenters ya offline mobile devices par customer shopping cart items synchronize karna bina locks ke.
// [KAISE CHALTA HAI]: Add-set aur Remove-set dono me items ke sath microsecond timestamps store karta hai; membership rule: item active hai agar $T_{\text{add}} > T_{\text{remove}}$.
// [OUTPUT / SIDE EFFECT]: Commutative, associative, idempotent set reconciliation.
// [VARIATIONS TO TRY]: Bias policy configure karein (e.g. Add-bias vs Remove-bias when timestamps are equal).
public class LwwElementSet<T> where T : notnull
{
    // [KYA KARTA HAI]: Added items aur unke addition timestamps store karta hai.
    // [KYU ZAROORI HAI]: Har element ki latest addition timestamp track karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<T, DateTime>.
    // [OUTPUT / SIDE EFFECT]: Add-set state.
    // [VARIATIONS TO TRY]: High-precision logical clock timestamps.
    private readonly ConcurrentDictionary<T, DateTime> _addSet = new();

    // [KYA KARTA HAI]: Removed items aur unke removal timestamps store karta hai.
    // [KYU ZAROORI HAI]: Deletions ko tombstone ki tarah record karna taaki concurrent re-sync me deletion lost na ho.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<T, DateTime>.
    // [OUTPUT / SIDE EFFECT]: Remove-set (tombstone) state.
    // [VARIATIONS TO TRY]: Tombstone pruning / compaction after TTL.
    private readonly ConcurrentDictionary<T, DateTime> _removeSet = new();

    // [KYA KARTA HAI]: Current node/replica identity return karta hai.
    // [KYU ZAROORI HAI]: Audit aur tracing information track karna.
    // [KAISE CHALTA HAI]: Property read.
    // [OUTPUT / SIDE EFFECT]: Replica ID string.
    // [VARIATIONS TO TRY]: Guid node identifier.
    public string ReplicaId { get; }

    // [KYA KARTA HAI]: LwwElementSet instance initialize karta hai.
    // [KYU ZAROORI HAI]: Set instance create karna.
    // [KAISE CHALTA HAI]: Constructor initialization.
    // [OUTPUT / SIDE EFFECT]: Empty LWW set.
    // [VARIATIONS TO TRY]: Initial collection hydration.
    public LwwElementSet(string replicaId)
    {
        // [KYA KARTA HAI]: Replica ID validation check.
        // [KYU ZAROORI HAI]: Valid identifier zaroori hai.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Prefix validation.
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        // [KYA KARTA HAI]: ReplicaId set.
        // [KYU ZAROORI HAI]: Field storage.
        // [KAISE CHALTA HAI]: Assignment.
        // [OUTPUT / SIDE EFFECT]: Property bound.
        // [VARIATIONS TO TRY]: Upper-case normalization.
        ReplicaId = replicaId;
    }

    // [KYA KARTA HAI]: Set me element add karta hai with timestamp.
    // [KYU ZAROORI HAI]: Customer basket me item insert karna.
    // [KAISE CHALTA HAI]: Timestamp agar null ho toh UtcNow use karta hai; Add-set me element ka timestamp update karta hai (max).
    // [OUTPUT / SIDE EFFECT]: Element added or refreshed in add-set.
    // [VARIATIONS TO TRY]: Explicit monotonic counter timestamp pass karein.
    public void Add(T element, DateTime? timestamp = null)
    {
        // [KYA KARTA HAI]: Timestamp determine karta hai.
        // [KYU ZAROORI HAI]: Microsecond causal ordering.
        // [KAISE CHALTA HAI]: timestamp ?? DateTime.UtcNow.
        // [OUTPUT / SIDE EFFECT]: Valid Utc DateTime.
        // [VARIATIONS TO TRY]: Stopwatch ticks for higher resolution.
        var ts = timestamp ?? DateTime.UtcNow;

        // [KYA KARTA HAI]: AddSet me latest timestamp store karta hai.
        // [KYU ZAROORI HAI]: Re-addings must take precedence over older adds.
        // [KAISE CHALTA HAI]: AddOrUpdate with Math.Max of ticks.
        // [OUTPUT / SIDE EFFECT]: Add-set updated.
        // [VARIATIONS TO TRY]: Audit log emit karein.
        _addSet.AddOrUpdate(element, ts, (_, oldTs) => oldTs > ts ? oldTs : ts);
    }

    // [KYA KARTA HAI]: Set se element remove (tombstone) karta hai with timestamp.
    // [KYU ZAROORI HAI]: Item deletion record karna without permanently losing tombstone.
    // [KAISE CHALTA HAI]: Remove-set me element ka deletion timestamp record karta hai.
    // [OUTPUT / SIDE EFFECT]: Element marked removed in remove-set.
    // [VARIATIONS TO TRY]: Soft-delete TTL tagging.
    public void Remove(T element, DateTime? timestamp = null)
    {
        // [KYA KARTA HAI]: Timestamp resolve karta hai.
        // [KYU ZAROORI HAI]: Removal time ordering.
        // [KAISE CHALTA HAI]: timestamp ?? DateTime.UtcNow.
        // [OUTPUT / SIDE EFFECT]: Deletion Utc DateTime.
        // [VARIATIONS TO TRY]: Logical clock timestamp.
        var ts = timestamp ?? DateTime.UtcNow;

        // [KYA KARTA HAI]: RemoveSet me latest timestamp store karta hai.
        // [KYU ZAROORI HAI]: Monotonically latest deletion record karna.
        // [KAISE CHALTA HAI]: AddOrUpdate with latest timestamp.
        // [OUTPUT / SIDE EFFECT]: Remove-set updated.
        // [VARIATIONS TO TRY]: Metric counter increment.
        _removeSet.AddOrUpdate(element, ts, (_, oldTs) => oldTs > ts ? oldTs : ts);
    }

    // [KYA KARTA HAI]: Check karta hai kya element current active set ka member hai.
    // [KYU ZAROORI HAI]: Cart contents lookup check.
    // [KAISE CHALTA HAI]: Element Add-set me hona chahiye AND (Remove-set me na ho YA Add-timestamp > Remove-timestamp).
    // [OUTPUT / SIDE EFFECT]: Boolean membership result.
    // [VARIATIONS TO TRY]: Equal timestamp par Add-bias vs Remove-bias configure karein.
    public bool Contains(T element)
    {
        // [KYA KARTA HAI]: Check karta hai kya element add-set me hai.
        // [KYU ZAROORI HAI]: Jo element kabhi add hi nahi hua wo member nahi ho sakta.
        // [KAISE CHALTA HAI]: TryGetValue on _addSet.
        // [OUTPUT / SIDE EFFECT]: True if added at least once.
        // [VARIATIONS TO TRY]: Bloom filter acceleration.
        if (!_addSet.TryGetValue(element, out var addTime))
        {
            return false;
        }

        // [KYA KARTA HAI]: Check karta hai kya element remove-set me hai.
        // [KYU ZAROORI HAI]: Deletion comparison.
        // [KAISE CHALTA HAI]: TryGetValue on _removeSet.
        // [OUTPUT / SIDE EFFECT]: Deletion time if found.
        // [VARIATIONS TO TRY]: Secondary tombstone checks.
        if (_removeSet.TryGetValue(element, out var removeTime))
        {
            // [KYA KARTA HAI]: Timestamps compare karta hai (Last-Write-Wins rule).
            // [KYU ZAROORI HAI]: Agar add timestamp remove timestamp se baad ka hai, toh element dubara add kiya gaya tha!
            // [KAISE CHALTA HAI]: addTime > removeTime.
            // [OUTPUT / SIDE EFFECT]: True if latest action was an addition.
            // [VARIATIONS TO TRY]: Equal timestamps par tie-breaking hash compare karein.
            return addTime > removeTime;
        }

        // [KYA KARTA HAI]: Element added hai aur kabhi remove nahi hua.
        // [KYU ZAROORI HAI]: Definite member.
        // [KAISE CHALTA HAI]: Return true.
        // [OUTPUT / SIDE EFFECT]: Membership confirmed.
        // [VARIATIONS TO TRY]: Direct return.
        return true;
    }

    // [KYA KARTA HAI]: Sabhi currently active members ka collection return karta hai.
    // [KYU ZAROORI HAI]: Cart items list display karna.
    // [KAISE CHALTA HAI]: Add-set ke sabhi keys par Contains(key) filter karta hai.
    // [OUTPUT / SIDE EFFECT]: List of active elements.
    // [VARIATIONS TO TRY]: ImmutableHashSet return karein.
    public IReadOnlyList<T> ToList()
    {
        // [KYA KARTA HAI]: Active items filter karta hai.
        // [KYU ZAROORI HAI]: LWW membership rule apply karna.
        // [KAISE CHALTA HAI]: LINQ Where(Contains).
        // [OUTPUT / SIDE EFFECT]: Filtered list.
        // [VARIATIONS TO TRY]: Fast manual array allocation.
        return _addSet.Keys.Where(Contains).ToList().AsReadOnly();
    }

    // [KYA KARTA HAI]: Remote LWW set ko local set ke sath merge karta hai.
    // [KYU ZAROORI HAI]: Cross-region partition healing ke waqt full synchronization.
    // [KAISE CHALTA HAI]: Both Add-set aur Remove-set ka element-wise pairwise timestamp maximum compute karta hai.
    // [OUTPUT / SIDE EFFECT]: Both datacenters reach identical deterministic set state.
    // [VARIATIONS TO TRY]: Delta-state based CRDT optimization (send only changes).
    public void Merge(LwwElementSet<T> remote)
    {
        // [KYA KARTA HAI]: Null check.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if null.
        // [VARIATIONS TO TRY]: Silent ignore.
        ArgumentNullException.ThrowIfNull(remote);

        // [KYA KARTA HAI]: Remote add-set iterate karta hai.
        // [KYU ZAROORI HAI]: Pairwise max of addition timestamps.
        // [KAISE CHALTA HAI]: foreach over remote._addSet.
        // [OUTPUT / SIDE EFFECT]: Latest additions synced.
        // [VARIATIONS TO TRY]: Batch update dictionary.
        foreach (var (elem, rTs) in remote._addSet)
        {
            _addSet.AddOrUpdate(elem, rTs, (_, lTs) => lTs > rTs ? lTs : rTs);
        }

        // [KYA KARTA HAI]: Remote remove-set iterate karta hai.
        // [KYU ZAROORI HAI]: Pairwise max of removal timestamps.
        // [KAISE CHALTA HAI]: foreach over remote._removeSet.
        // [OUTPUT / SIDE EFFECT]: Latest removals synced.
        // [VARIATIONS TO TRY]: Event notification on newly removed items.
        foreach (var (elem, rTs) in remote._removeSet)
        {
            _removeSet.AddOrUpdate(elem, rTs, (_, lTs) => lTs > rTs ? lTs : rTs);
        }
    }
}
