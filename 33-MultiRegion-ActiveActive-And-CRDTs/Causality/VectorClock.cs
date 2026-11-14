namespace KiranaHub.MultiRegionCRDT.Causality;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Distributed systems causality tracking ke liye Vector Clock implement karta hai.
// [KYU ZAROORI HAI]: Multi-datacenter updates me pata lagana ki kya Event A ne Event B ko causally cause kiya tha (Happened-Before, $A \to B$) ya dono updates independently aur simultaneously execute hue (Concurrent Conflict, $A \parallel B$).
// [KAISE CHALTA HAI]: Har node apne logical counter ko increment karta hai; cross-node sync par vector ka element-wise maximum liya jata hai ($V_{\text{merged}}[i] = \max(V_A[i], V_B[i])$).
// [OUTPUT / SIDE EFFECT]: Precise causal ordering comparisons.
// [VARIATIONS TO TRY]: Matrix clock for complete knowledge horizon tracking.
public class VectorClock
{
    // [KYA KARTA HAI]: Current node ka unique ID store karta hai.
    // [KYU ZAROORI HAI]: Local vector slot increment karne ke liye identity zaroori hai.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: Node ID string.
    // [VARIATIONS TO TRY]: Cluster peer enum.
    public string NodeId { get; }

    // [KYA KARTA HAI]: NodeId se logical tick counter map karta hai.
    // [KYU ZAROORI HAI]: Distributed vector state hold karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, long>.
    // [OUTPUT / SIDE EFFECT]: Logical clock mapping.
    // [VARIATIONS TO TRY]: Immutable array representation.
    private readonly ConcurrentDictionary<string, long> _clock = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: VectorClock instance initialize karta hai.
    // [KYU ZAROORI HAI]: Setup vector clock for this replica.
    // [KAISE CHALTA HAI]: Constructor call.
    // [OUTPUT / SIDE EFFECT]: Initial clock with node ticket set to 0.
    // [VARIATIONS TO TRY]: Preload initial cluster topology nodes.
    public VectorClock(string nodeId)
    {
        // [KYA KARTA HAI]: NodeId validation.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Valid NodeId.
        // [VARIATIONS TO TRY]: Regex checking.
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        // [KYA KARTA HAI]: Field set.
        // [KYU ZAROORI HAI]: Identity bind.
        // [KAISE CHALTA HAI]: Assignment.
        // [OUTPUT / SIDE EFFECT]: NodeId set.
        // [VARIATIONS TO TRY]: Upper-case normalization.
        NodeId = nodeId;

        // [KYA KARTA HAI]: Local node ka initial logical counter 0 initialize karta hai.
        // [KYU ZAROORI HAI]: Baseline timestamp establishment.
        // [KAISE CHALTA HAI]: _clock[NodeId] = 0.
        // [OUTPUT / SIDE EFFECT]: Clock initialized.
        // [VARIATIONS TO TRY]: Lamport start tick.
        _clock[NodeId] = 0;
    }

    // [KYA KARTA HAI]: Local event execute hone par node ka logical counter increment karta hai.
    // [KYU ZAROORI HAI]: Causal time progression record karna.
    // [KAISE CHALTA HAI]: _clock.AddOrUpdate(NodeId, 1, (_, old) => old + 1).
    // [OUTPUT / SIDE EFFECT]: Local logical tick incremented.
    // [VARIATIONS TO TRY]: Increment by custom delta.
    public void Tick()
    {
        // [KYA KARTA HAI]: Counter increment karta hai.
        // [KYU ZAROORI HAI]: Advance logical time.
        // [KAISE CHALTA HAI]: AddOrUpdate.
        // [OUTPUT / SIDE EFFECT]: Node clock advanced.
        // [VARIATIONS TO TRY]: Interlocked addition.
        _clock.AddOrUpdate(NodeId, 1, (_, old) => old + 1);
    }

    // [KYA KARTA HAI]: Specific node ka logical timestamp read karta hai.
    // [KYU ZAROORI HAI]: Individual tick inspection.
    // [KAISE CHALTA HAI]: TryGetValue lookup with default 0.
    // [OUTPUT / SIDE EFFECT]: Logical clock value.
    // [VARIATIONS TO TRY]: Throw if node unknown.
    public long GetTime(string nodeId) => _clock.TryGetValue(nodeId, out var val) ? val : 0;

    // [KYA KARTA HAI]: Doosre remote vector clock ke sath merge karta hai.
    // [KYU ZAROORI HAI]: Message receipt ya gossip sync par cross-datacenter causal time catch-up karna.
    // [KAISE CHALTA HAI]: Sabhi nodes ke liye maximum tick let hai: $V[i] = \max(local[i], remote[i])$.
    // [OUTPUT / SIDE EFFECT]: Local clock is updated to causal upper bound.
    // [VARIATIONS TO TRY]: Return boolean if local clock changed.
    public void Merge(VectorClock remote)
    {
        // [KYA KARTA HAI]: Null check.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Ignore.
        ArgumentNullException.ThrowIfNull(remote);

        // [KYA KARTA HAI]: Remote clock iterate karta hai.
        // [KYU ZAROORI HAI]: Pairwise maximum sync.
        // [KAISE CHALTA HAI]: foreach over remote._clock.
        // [OUTPUT / SIDE EFFECT]: Element-wise max values updated.
        // [VARIATIONS TO TRY]: Parallel merge.
        foreach (var (node, rVal) in remote._clock)
        {
            _clock.AddOrUpdate(node, rVal, (_, lVal) => Math.Max(lVal, rVal));
        }

        // [KYA KARTA HAI]: Local node ka clock bhi 1 tick advance karta hai on message receipt.
        // [KYU ZAROORI HAI]: Standard Lamport/Vector clock causality rule on receive: $\max + 1$.
        // [KAISE CHALTA HAI]: Tick().
        // [OUTPUT / SIDE EFFECT]: Local time moves forward.
        // [VARIATIONS TO TRY]: Configurable receive tick toggle.
        Tick();
    }

    // [KYA KARTA HAI]: Do vector clocks ko mathematically compare karta hai.
    // [KYU ZAROORI HAI]: Causality determine karna: kya A pehle hua (Before), baad me hua (After), barabar hai (Equal), ya conflict hai (ConcurrentConflict)?
    // [KAISE CHALTA HAI]: Dono clocks ke sabhi keys ka pairwise comparison karta hai; agar ek clock sabhi keys par $\le$ hai aur kam se kam ek key par $<$ hai toh Before; agar dono taraf greater values hain toh Concurrent!
    // [OUTPUT / SIDE EFFECT]: CausalOrder enum value.
    // [VARIATIONS TO TRY]: Partial order lattice return type.
    public CausalOrder Compare(VectorClock other)
    {
        // [KYA KARTA HAI]: Null check.
        // [KYU ZAROORI HAI]: Safeguard.
        // [KAISE CHALTA HAI]: ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if null.
        // [VARIATIONS TO TRY]: Return Unknown.
        ArgumentNullException.ThrowIfNull(other);

        // [KYA KARTA HAI]: Dono clocks ki unique nodes ka set nikalta hai.
        // [KYU ZAROORI HAI]: Complete vector dimension coverage.
        // [KAISE CHALTA HAI]: LINQ Union.
        // [OUTPUT / SIDE EFFECT]: All unique node IDs.
        // [VARIATIONS TO TRY]: HashSet pre-allocation.
        var allNodes = _clock.Keys.Union(other._clock.Keys).ToList();

        // [KYA KARTA HAI]: Comparison tracking flags.
        // [KYU ZAROORI HAI]: Detect if local had smaller or larger components.
        // [KAISE CHALTA HAI]: Boolean flags.
        // [OUTPUT / SIDE EFFECT]: Flags state.
        // [VARIATIONS TO TRY]: Integer score.
        bool hasSmaller = false;
        bool hasLarger = false;

        // [KYA KARTA HAI]: Sabhi nodes ke ticks compare karta hai.
        // [KYU ZAROORI HAI]: Pairwise component comparison.
        // [KAISE CHALTA HAI]: for loop over all nodes.
        // [OUTPUT / SIDE EFFECT]: Compares GetTime(n) vs other.GetTime(n).
        // [VARIATIONS TO TRY]: SIMD vector compare.
        foreach (var node in allNodes)
        {
            long t1 = GetTime(node);
            long t2 = other.GetTime(node);

            if (t1 < t2)
            {
                hasSmaller = true;
            }
            else if (t1 > t2)
            {
                hasLarger = true;
            }
        }

        // [KYA KARTA HAI]: Causal ordering deduce karta hai.
        // [KYU ZAROORI HAI]: Exact causality relationship determine karna.
        // [KAISE CHALTA HAI]: Flag evaluation.
        // [OUTPUT / SIDE EFFECT]: CausalOrder result.
        // [VARIATIONS TO TRY]: Custom dominance scoring.
        if (hasSmaller && hasLarger)
        {
            // [KYA KARTA HAI]: Concurrent conflict return karta hai ($A \parallel B$).
            // [KYU ZAROORI HAI]: Dono nodes ne independently bina sync kiye update kiya tha.
            // [KAISE CHALTA HAI]: Both smaller and larger components exist.
            // [OUTPUT / SIDE EFFECT]: CausalOrder.ConcurrentConflict.
            // [VARIATIONS TO TRY]: Automatic CRDT resolution trigger.
            return CausalOrder.ConcurrentConflict;
        }

        if (hasSmaller)
        {
            // [KYA KARTA HAI]: Local happened before other ($A \to B$).
            // [KYU ZAROORI HAI]: Local clock strictly dominates or is contained.
            // [KAISE CHALTA HAI]: Return Before.
            // [OUTPUT / SIDE EFFECT]: CausalOrder.Before.
            // [VARIATIONS TO TRY]: Relative distance measure.
            return CausalOrder.Before;
        }

        if (hasLarger)
        {
            // [KYA KARTA HAI]: Local happened after other ($B \to A$).
            // [KYU ZAROORI HAI]: Local clock strictly larger.
            // [KAISE CHALTA HAI]: Return After.
            // [OUTPUT / SIDE EFFECT]: CausalOrder.After.
            // [VARIATIONS TO TRY]: Relative distance.
            return CausalOrder.After;
        }

        // [KYA KARTA HAI]: Clocks are identical.
        // [KYU ZAROORI HAI]: Synchronized state.
        // [KAISE CHALTA HAI]: Return Equal.
        // [OUTPUT / SIDE EFFECT]: CausalOrder.Equal.
        // [VARIATIONS TO TRY]: None.
        return CausalOrder.Equal;
    }

    // [KYA KARTA HAI]: Debug string representation.
    // [KYU ZAROORI HAI]: Logging and test verification.
    // [KAISE CHALTA HAI]: Formatted string of key-value pairs.
    // [OUTPUT / SIDE EFFECT]: String representation.
    // [VARIATIONS TO TRY]: JSON format.
    public override string ToString()
    {
        var entries = _clock.Select(kv => $"{kv.Key}:{kv.Value}");
        return $"<{string.Join(", ", entries)}>";
    }
}

// [KYA KARTA HAI]: Vector clock causal relationship enum.
// [KYU ZAROORI HAI]: Explicit causality comparison states.
// [KAISE CHALTA HAI]: 4 discrete ordering outcomes.
// [OUTPUT / SIDE EFFECT]: Strongly-typed comparison result.
// [VARIATIONS TO TRY]: Add Unknown state.
public enum CausalOrder
{
    Before,
    After,
    Equal,
    ConcurrentConflict
}
