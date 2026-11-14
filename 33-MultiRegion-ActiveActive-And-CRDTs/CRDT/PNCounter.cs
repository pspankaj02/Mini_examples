namespace KiranaHub.MultiRegionCRDT.CRDT;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: Conflict-Free Replicated Data Type (CRDT) ka Positive-Negative Counter (PN-Counter) implement karta hai.
// [KYU ZAROORI HAI]: Multi-region active-active datacenters (e.g. Mumbai aur Bengaluru) me bina distributed locks ke inventory stock counts synchronize karna.
// [KAISE CHALTA HAI]: Har replica apne local positive increments (P) aur negative decrements (N) maintain karta hai; merge operation me element-wise maximum liya jata hai ($P = \max(A.P, B.P)$).
// [OUTPUT / SIDE EFFECT]: Commutative aur associative eventual consistency; order of arrival se farq nahi padta.
// [VARIATIONS TO TRY]: Bounded PN-Counter implement karein jahan counter negative hone se strictly block ho jaye.
public class PNCounter
{
    // [KYA KARTA HAI]: Current node/replica ka unique ID store karta hai (e.g. "mumbai-dc1", "bengaluru-dc1").
    // [KYU ZAROORI HAI]: Har region ko sirf apne vector slot me increment/decrement karne ki authority hoti hai.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: Immutable replica identifier.
    // [VARIATIONS TO TRY]: Dynamic cloud region code resolver use karein.
    public string ReplicaId { get; }

    // [KYA KARTA HAI]: Positive increments vector store karta hai (ReplicaId -> Count).
    // [KYU ZAROORI HAI]: Har node ke cumulative increments track karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, long>.
    // [OUTPUT / SIDE EFFECT]: Positive count mapping.
    // [VARIATIONS TO TRY]: ImmutableDictionary use karein.
    private readonly ConcurrentDictionary<string, long> _positive = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Negative decrements vector store karta hai (ReplicaId -> Count).
    // [KYU ZAROORI HAI]: Decrements ko alag track karna taaki merge me monotonically increasing semantics preserve ho sakein.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, long>.
    // [OUTPUT / SIDE EFFECT]: Negative count mapping.
    // [VARIATIONS TO TRY]: Bounded reservations vector shamil karein.
    private readonly ConcurrentDictionary<string, long> _negative = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: PN-Counter instance initialize karta hai.
    // [KYU ZAROORI HAI]: Replica identity bind karna.
    // [KAISE CHALTA HAI]: new PNCounter("mumbai-dc1").
    // [OUTPUT / SIDE EFFECT]: Initialized counter with 0 value.
    // [VARIATIONS TO TRY]: Initial state snapshot pass karein.
    public PNCounter(string replicaId)
    {
        // [KYA KARTA HAI]: Argument null check.
        // [KYU ZAROORI HAI]: Valid replica ID required for vector slotting.
        // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Format checking.
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        // [KYA KARTA HAI]: Field set.
        // [KYU ZAROORI HAI]: Identification.
        // [KAISE CHALTA HAI]: Assignment.
        // [OUTPUT / SIDE EFFECT]: ReplicaId property initialized.
        // [VARIATIONS TO TRY]: Hash code precompute karein.
        ReplicaId = replicaId;
    }

    // [KYA KARTA HAI]: Current aggregated counter value calculate karta hai: $\sum P - \sum N$.
    // [KYU ZAROORI HAI]: Real-time readable stock count nikalna.
    // [KAISE CHALTA HAI]: Sabhi positive values ka sum karta hai aur sabhi negative values ka sum subtract karta hai.
    // [OUTPUT / SIDE EFFECT]: Net counter value long integer.
    // [VARIATIONS TO TRY]: Decimal value support for weighted items (e.g. grams).
    public long Value
    {
        get
        {
            // [KYA KARTA HAI]: Positive vector sum calculate karta hai.
            // [KYU ZAROORI HAI]: Total additions across all datacenters.
            // [KAISE CHALTA HAI]: LINQ Sum().
            // [OUTPUT / SIDE EFFECT]: Positive sum.
            // [VARIATIONS TO TRY]: Fast foreach over values array.
            long sumP = _positive.Values.Sum();

            // [KYA KARTA HAI]: Negative vector sum calculate karta hai.
            // [KYU ZAROORI HAI]: Total subtractions across all datacenters.
            // [KAISE CHALTA HAI]: LINQ Sum().
            // [OUTPUT / SIDE EFFECT]: Negative sum.
            // [VARIATIONS TO TRY]: SIMD vectorization.
            long sumN = _negative.Values.Sum();

            // [KYA KARTA HAI]: Net difference return karta hai.
            // [KYU ZAROORI HAI]: Current CRDT value.
            // [KAISE CHALTA HAI]: sumP - sumN.
            // [OUTPUT / SIDE EFFECT]: Final counter value.
            // [VARIATIONS TO TRY]: Floor at 0 if negative counts are illegal in domain.
            return sumP - sumN;
        }
    }

    // [KYA KARTA HAI]: Local replica ke positive vector me count add karta hai.
    // [KYU ZAROORI HAI]: Stock addition ya warehouse replenishment record karna.
    // [KAISE CHALTA HAI]: _positive.AddOrUpdate(ReplicaId, delta, (_, old) => old + delta).
    // [OUTPUT / SIDE EFFECT]: Local positive counter incremented.
    // [VARIATIONS TO TRY]: Negative delta validation.
    public void Increment(long delta = 1)
    {
        // [KYA KARTA HAI]: Negative delta check karta hai.
        // [KYU ZAROORI HAI]: Increment strictly positive hona chahiye.
        // [KAISE CHALTA HAI]: ArgumentOutOfRangeException check.
        // [OUTPUT / SIDE EFFECT]: Exception if delta <= 0.
        // [VARIATIONS TO TRY]: Auto route delta < 0 to Decrement.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delta);

        // [KYA KARTA HAI]: Thread-safe vector slot increment.
        // [KYU ZAROORI HAI]: Monotonically increasing vector value.
        // [KAISE CHALTA HAI]: AddOrUpdate on dictionary.
        // [OUTPUT / SIDE EFFECT]: Replica's P-slot increased.
        // [VARIATIONS TO TRY]: Interlocked addition.
        _positive.AddOrUpdate(ReplicaId, delta, (_, old) => old + delta);
    }

    // [KYA KARTA HAI]: Local replica ke negative vector me count add karta hai.
    // [KYU ZAROORI HAI]: Customer order sale ya stock deduction record karna.
    // [KAISE CHALTA HAI]: _negative.AddOrUpdate(ReplicaId, delta, (_, old) => old + delta).
    // [OUTPUT / SIDE EFFECT]: Local negative counter incremented.
    // [VARIATIONS TO TRY]: Check if current Value >= delta before deducting.
    public void Decrement(long delta = 1)
    {
        // [KYA KARTA HAI]: Positive delta check.
        // [KYU ZAROORI HAI]: Decrement amount must be positive.
        // [KAISE CHALTA HAI]: ThrowIfNegativeOrZero.
        // [OUTPUT / SIDE EFFECT]: Exception if invalid.
        // [VARIATIONS TO TRY]: Allow 0 delta as no-op.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delta);

        // [KYA KARTA HAI]: Negative vector slot update.
        // [KYU ZAROORI HAI]: Record consumption.
        // [KAISE CHALTA HAI]: AddOrUpdate on dictionary.
        // [OUTPUT / SIDE EFFECT]: Replica's N-slot increased.
        // [VARIATIONS TO TRY]: Stock reservation token attach karein.
        _negative.AddOrUpdate(ReplicaId, delta, (_, old) => old + delta);
    }

    // [KYA KARTA HAI]: Doosre remote replica ke PN-Counter state ko local state ke sath merge karta hai.
    // [KYU ZAROORI HAI]: Cross-region replication aur eventual consistency achieve karna bina data loss ke.
    // [KAISE CHALTA HAI]: Element-wise pairwise maximum compute karta hai: $P[k] = \max(local.P[k], remote.P[k])$, $N[k] = \max(local.N[k], remote.N[k])$.
    // [OUTPUT / SIDE EFFECT]: Local counter reaches least upper bound (LUB / join-semilattice).
    // [VARIATIONS TO TRY]: Return boolean indicating whether local state changed.
    public void Merge(PNCounter remote)
    {
        // [KYA KARTA HAI]: Null remote check.
        // [KYU ZAROORI HAI]: Invalid replication payload protection.
        // [KAISE CHALTA HAI]: ArgumentNullException.ThrowIfNull.
        // [OUTPUT / SIDE EFFECT]: Exception if remote is null.
        // [VARIATIONS TO TRY]: Ignore null silently.
        ArgumentNullException.ThrowIfNull(remote);

        // [KYA KARTA HAI]: Remote positive vector iterate karta hai.
        // [KYU ZAROORI HAI]: Pairwise maximum merge of additions.
        // [KAISE CHALTA HAI]: foreach over remote._positive.
        // [OUTPUT / SIDE EFFECT]: Monotonic ceiling update.
        // [VARIATIONS TO TRY]: Parallel merge with PLINQ.
        foreach (var (replica, remoteVal) in remote._positive)
        {
            // [KYA KARTA HAI]: Maximum value set karta hai local dictionary me.
            // [KYU ZAROORI HAI]: Semilattice join property ensure karna ($\max(a, b)$).
            // [KAISE CHALTA HAI]: AddOrUpdate with Math.Max.
            // [OUTPUT / SIDE EFFECT]: _positive[replica] becomes max of both.
            // [VARIATIONS TO TRY]: Audit delta logging.
            _positive.AddOrUpdate(replica, remoteVal, (_, localVal) => Math.Max(localVal, remoteVal));
        }

        // [KYA KARTA HAI]: Remote negative vector iterate karta hai.
        // [KYU ZAROORI HAI]: Pairwise maximum merge of subtractions.
        // [KAISE CHALTA HAI]: foreach over remote._negative.
        // [OUTPUT / SIDE EFFECT]: Monotonic ceiling update of decrements.
        // [VARIATIONS TO TRY]: Audit decrement logging.
        foreach (var (replica, remoteVal) in remote._negative)
        {
            // [KYA KARTA HAI]: Maximum value set karta hai.
            // [KYU ZAROORI HAI]: Semilattice join.
            // [KAISE CHALTA HAI]: AddOrUpdate with Math.Max.
            // [OUTPUT / SIDE EFFECT]: _negative[replica] becomes max of both.
            // [VARIATIONS TO TRY]: Event notification.
            _negative.AddOrUpdate(replica, remoteVal, (_, localVal) => Math.Max(localVal, remoteVal));
        }
    }

    // [KYA KARTA HAI]: Counter state ka debug representation return karta hai.
    // [KYU ZAROORI HAI]: Logs aur automated tests me vectors inspect karna.
    // [KAISE CHALTA HAI]: Formatted string of replica IDs and net value.
    // [OUTPUT / SIDE EFFECT]: Debug string.
    // [VARIATIONS TO TRY]: JSON serialization.
    public override string ToString()
    {
        return $"[PNCounter on '{ReplicaId}': Net Value={Value}]";
    }
}
