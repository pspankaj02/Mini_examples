namespace KiranaHub.MultiRegionCRDT.CRDT;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Observed-Remove Set (OR-Set) CRDT implement karta hai.
// [KYU ZAROORI HAI]: Clock skew ya timestamp synchronization issues hone par bhi bina clock depend kare mathematically perfect set synchronization provide karna.
// [KAISE CHALTA HAI]: Har addition par ek globally unique GUID tag generate hota hai; removal par sirf currently observed tags ko tombstone set me dala jata hai; agar koi doosra node concurrent add kar raha hai, toh naya tag tombstone me nahi hoga, isliye item safely retain rehta hai!
// [OUTPUT / SIDE EFFECT]: True causality-based add/remove resolution without physical clocks.
// [VARIATIONS TO TRY]: Dot-Kernel based optimized OR-Set.
public class OrSet<T> where T : notnull
{
    // [KYA KARTA HAI]: Added element instances ko unke unique GUID tags ke sath store karta hai.
    // [KYU ZAROORI HAI]: Har individual addition event ko uniquely distinguish karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<(T Element, Guid Tag), byte>.
    // [OUTPUT / SIDE EFFECT]: Tagged add set.
    // [VARIATIONS TO TRY]: Compact bitmap identifier representation.
    private readonly ConcurrentDictionary<(T Element, Guid Tag), byte> _elements = new();

    // [KYA KARTA HAI]: Removed element tags (tombstones) store karta hai.
    // [KYU ZAROORI HAI]: Causal deletion propagation ensure karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<Guid, byte>.
    // [OUTPUT / SIDE EFFECT]: Tombstone tags collection.
    // [VARIATIONS TO TRY]: Bloom filter for old tombstones.
    private readonly ConcurrentDictionary<Guid, byte> _tombstones = new();

    // [KYA KARTA HAI]: Replica identity return karta hai.
    // [KYU ZAROORI HAI]: Source tracking.
    // [KAISE CHALTA HAI]: Property.
    // [OUTPUT / SIDE EFFECT]: Replica ID string.
    // [VARIATIONS TO TRY]: Node enum.
    public string ReplicaId { get; }

    // [KYA KARTA HAI]: OrSet instance initialize karta hai.
    // [KYU ZAROORI HAI]: Setup data structure.
    // [KAISE CHALTA HAI]: Constructor.
    // [OUTPUT / SIDE EFFECT]: Empty OR-Set.
    // [VARIATIONS TO TRY]: Seed initial collection.
    public OrSet(string replicaId)
    {
        // [KYA KARTA HAI]: Argument validation.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Valid replicaId.
        // [VARIATIONS TO TRY]: Format check.
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        // [KYA KARTA HAI]: Field assign.
        // [KYU ZAROORI HAI]: Store identity.
        // [KAISE CHALTA HAI]: Assignment.
        // [OUTPUT / SIDE EFFECT]: Bound property.
        // [VARIATIONS TO TRY]: Hash code precomputation.
        ReplicaId = replicaId;
    }

    // [KYA KARTA HAI]: Set me element add karta hai with a fresh unique tag.
    // [KYU ZAROORI HAI]: Customer basket me item addition with unique identity tag.
    // [KAISE CHALTA HAI]: Guid.NewGuid() banata hai aur (element, tag) ko _elements dictionary me insert karta hai.
    // [OUTPUT / SIDE EFFECT]: Element tag registered.
    // [VARIATIONS TO TRY]: Return generated tag to caller.
    public Guid Add(T element)
    {
        // [KYA KARTA HAI]: Unique random UUID tag generate karta hai.
        // [KYU ZAROORI HAI]: Concurrent additions ko alag identity dena.
        // [KAISE CHALTA HAI]: Guid.NewGuid().
        // [OUTPUT / SIDE EFFECT]: 128-bit unique tag.
        // [VARIATIONS TO TRY]: Sequential UUID (COMB Guid) for indexing.
        var tag = Guid.NewGuid();

        // [KYA KARTA HAI]: Pair ko _elements dictionary me insert karta hai.
        // [KYU ZAROORI HAI]: Add-set registration.
        // [KAISE CHALTA HAI]: TryAdd((element, tag), 1).
        // [OUTPUT / SIDE EFFECT]: Stored in set.
        // [VARIATIONS TO TRY]: Concurrent collection write.
        _elements.TryAdd((element, tag), 1);

        // [KYA KARTA HAI]: Tag return karta hai.
        // [KYU ZAROORI HAI]: Caller tracking.
        // [KAISE CHALTA HAI]: return tag.
        // [OUTPUT / SIDE EFFECT]: Guid tag.
        // [VARIATIONS TO TRY]: Custom tag struct.
        return tag;
    }

    // [KYA KARTA HAI]: Set se element remove karta hai by moving all currently observed tags to tombstone set.
    // [KYU ZAROORI HAI]: Item deletion record karna without physical clock dependencies.
    // [KAISE CHALTA HAI]: Jo tags is element ke currently _elements me maujood hain unhe _tombstones me mark karta hai.
    // [OUTPUT / SIDE EFFECT]: Observed tags are tombstoned.
    // [VARIATIONS TO TRY]: Return count of tombstones added.
    public void Remove(T element)
    {
        // [KYA KARTA HAI]: Currently observed tags for this element dhundta hai.
        // [KYU ZAROORI HAI]: Sirf un tags ko tombstone karna jo local replica ne 'observe' kiye hain (Observed-Remove semantic).
        // [KAISE CHALTA HAI]: LINQ query over _elements.Keys.
        // [OUTPUT / SIDE EFFECT]: List of matching tags.
        // [VARIATIONS TO TRY]: Inverted index element -> list of tags.
        var observedTags = _elements.Keys
            .Where(k => EqualityComparer<T>.Default.Equals(k.Element, element))
            .Select(k => k.Tag)
            .ToList();

        // [KYA KARTA HAI]: Har observed tag ko tombstones dictionary me add karta hai.
        // [KYU ZAROORI HAI]: Mark as deleted.
        // [KAISE CHALTA HAI]: foreach over observedTags -> TryAdd.
        // [OUTPUT / SIDE EFFECT]: Tags permanently tombstoned.
        // [VARIATIONS TO TRY]: Bulk tombstone array copy.
        foreach (var tag in observedTags)
        {
            _tombstones.TryAdd(tag, 1);
        }
    }

    // [KYA KARTA HAI]: Check karta hai kya element set me maujood hai.
    // [KYU ZAROORI HAI]: Basket item existence check.
    // [KAISE CHALTA HAI]: Check karta hai kya koi aisi (element, tag) pair hai jiska tag _tombstones me NAHI hai.
    // [OUTPUT / SIDE EFFECT]: True if active un-tombstoned tag exists.
    // [VARIATIONS TO TRY]: Fast-path caching.
    public bool Contains(T element)
    {
        // [KYA KARTA HAI]: Any untombstoned tag match check karta hai.
        // [KYU ZAROORI HAI]: Agar 1 bhi active tag bacha hai, toh item present hai!
        // [KAISE CHALTA HAI]: LINQ Any check.
        // [OUTPUT / SIDE EFFECT]: Boolean membership.
        // [VARIATIONS TO TRY]: For loop early exit.
        return _elements.Keys.Any(k =>
            EqualityComparer<T>.Default.Equals(k.Element, element) &&
            !_tombstones.ContainsKey(k.Tag));
    }

    // [KYA KARTA HAI]: Sabhi active unique elements return karta hai.
    // [KYU ZAROORI HAI]: Cart contents list read karna.
    // [KAISE CHALTA HAI]: Active elements group karke distinct list banata hai.
    // [OUTPUT / SIDE EFFECT]: ReadOnlyList<T> of active items.
    // [VARIATIONS TO TRY]: Sorted set return karein.
    public IReadOnlyList<T> ToList()
    {
        // [KYA KARTA HAI]: Untombstoned elements filter karta hai.
        // [KYU ZAROORI HAI]: Causal membership list nikalna.
        // [KAISE CHALTA HAI]: Where(!tombstone).Select(Element).Distinct().
        // [OUTPUT / SIDE EFFECT]: Distinct items list.
        // [VARIATIONS TO TRY]: ImmutableArray return karein.
        return _elements.Keys
            .Where(k => !_tombstones.ContainsKey(k.Tag))
            .Select(k => k.Element)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    // [KYA KARTA HAI]: Remote OR-Set ko local OR-Set ke sath merge karta hai.
    // [KYU ZAROORI HAI]: Cross-region synchronization without locks or clocks.
    // [KAISE CHALTA HAI]: Elements set ka union aur Tombstones set ka union karta hai.
    // [OUTPUT / SIDE EFFECT]: Deterministic convergence across both replicas.
    // [VARIATIONS TO TRY]: Delta replication payload.
    public void Merge(OrSet<T> remote)
    {
        // [KYA KARTA HAI]: Null check.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Silent skip.
        ArgumentNullException.ThrowIfNull(remote);

        // [KYA KARTA HAI]: Remote elements dictionary merge karta hai.
        // [KYU ZAROORI HAI]: Union of all created tags across both regions.
        // [KAISE CHALTA HAI]: foreach over remote._elements -> TryAdd.
        // [OUTPUT / SIDE EFFECT]: Local element tags set is now a superset.
        // [VARIATIONS TO TRY]: Bulk copy.
        foreach (var (key, val) in remote._elements)
        {
            _elements.TryAdd(key, val);
        }

        // [KYA KARTA HAI]: Remote tombstones dictionary merge karta hai.
        // [KYU ZAROORI HAI]: Union of all deletions across both regions.
        // [KAISE CHALTA HAI]: foreach over remote._tombstones -> TryAdd.
        // [OUTPUT / SIDE EFFECT]: Local tombstones set is now a superset.
        // [VARIATIONS TO TRY]: Bulk union.
        foreach (var (tag, val) in remote._tombstones)
        {
            _tombstones.TryAdd(tag, val);
        }
    }
}
