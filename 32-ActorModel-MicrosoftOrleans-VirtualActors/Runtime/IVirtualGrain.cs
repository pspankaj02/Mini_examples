namespace KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: Virtual Actor Grain ke basic lifecycle aur identity contract ko define karta hai.
// [KYU ZAROORI HAI]: Har actor (Rider, Cart, Order) ko cluster ke andar uniquely identify karne aur automatically activate/deactivate karne ke liye standardized contract lazmi hai.
// [KAISE CHALTA HAI]: Silo grain ke Type aur Key ke combination se unique address banata hai aur lifecycle hooks call karta hai.
// [OUTPUT / SIDE EFFECT]: Polymorphic grain collection aur uniform lifecycle invocation enable hoti hai.
// [VARIATIONS TO TRY]: Integer key ya Compound key (e.g. OrgId + UserId) support add kar sakte hain.
public interface IVirtualGrain
{
    // [KYA KARTA HAI]: Grain ka unique string identifier return karta hai (e.g. "rider-101", "cart-cust-99").
    // [KYU ZAROORI HAI]: Actor identity deterministic honi chahiye taaki har request sahi stateful actor tak pahuche.
    // [KAISE CHALTA HAI]: Actor creation ke waqt assign kiya gaya ID property se read hota hai.
    // [OUTPUT / SIDE EFFECT]: Grain identity string return hoti hai.
    // [VARIATIONS TO TRY]: Guid key ya composite key me switch kiya ja sakta hai.
    string GrainId { get; }

    // [KYA KARTA HAI]: Grain ka current lifecycle status return karta hai (Active, Idle, Deactivated).
    // [KYU ZAROORI HAI]: Silo garbage collector ko pata chal sake kaunsa actor memory se discard karne layak hai.
    // [KAISE CHALTA HAI]: Inactive duration ke basis par state check hoti hai.
    // [OUTPUT / SIDE EFFECT]: Lifecycle enum return hota hai.
    // [VARIATIONS TO TRY]: Activating ya Hydrating state shamil ki ja sakti hai.
    GrainLifecycleState LifecycleState { get; }

    // [KYA KARTA HAI]: Grain ka last accessed timestamp track karta hai.
    // [KYU ZAROORI HAI]: Idle timeout eviction policy ke liye LRU (Least Recently Used) calculation zaroori hai.
    // [KAISE CHALTA HAI]: Har incoming message dispatch par DateTime.UtcNow update hota hai.
    // [OUTPUT / SIDE EFFECT]: Utc timestamp return hota hai.
    // [VARIATIONS TO TRY]: Access count frequency counters add kiye ja sakte hain.
    DateTime LastAccessedUtc { get; }

    // [KYA KARTA HAI]: Grain activation hook — jab pehli baar actor memory me load hota hai tab execute hota hai.
    // [KYU ZAROORI HAI]: State hydration, database loading, ya external connection initialization yahan hoti hai.
    // [KAISE CHALTA HAI]: Silo grain instantiate karne ke turant baad ise await karta hai.
    // [OUTPUT / SIDE EFFECT]: Grain Active state me chala jata hai.
    // [VARIATIONS TO TRY]: Snapshot loading ya external cache sync logic add karein.
    Task OnActivateAsync();

    // [KYA KARTA HAI]: Grain deactivation hook — jab actor idle timeout ke baad memory se sweep hota hai tab call hota hai.
    // [KYU ZAROORI HAI]: Dirty state database me flush karna aur unmanaged resources free karna lazmi hai.
    // [KAISE CHALTA HAI]: Background garbage collector timer sweep ke waqt execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Grain Deactivated state me jaakar GC ke liye eligible ho jata hai.
    // [VARIATIONS TO TRY]: Graceful state snapshot persistence trigger karein.
    Task OnDeactivateAsync();

    // [KYA KARTA HAI]: Grain ke single-threaded turn queue me function enqueue karta hai.
    // [KYU ZAROORI HAI]: Virtual Actor Model ki sabse badi guarantee hai: No two messages execute concurrently on the same actor.
    // [KAISE CHALTA HAI]: Internal SemaphoreSlim ya Channel ke zariye turn-by-turn sequential execution ensure karta hai.
    // [OUTPUT / SIDE EFFECT]: Race conditions aur concurrency deadlocks 100% eliminate ho jate hain bina DB locks ke.
    // [VARIATIONS TO TRY]: Priority queue ya reentrant execution options add karein.
    Task<TResult> EnqueueTurnAsync<TResult>(Func<Task<TResult>> work);

    // [KYA KARTA HAI]: Void returning turn execution ko serialize karta hai.
    // [KYU ZAROORI HAI]: Non-returning commands (e.g. UpdateLocation) ko safe sequential order me chalana hota hai.
    // [KAISE CHALTA HAI]: Internal mailbox queue me request push karke await karta hai.
    // [OUTPUT / SIDE EFFECT]: Sequential thread execution without race conditions.
    // [VARIATIONS TO TRY]: Timeout policy attach karein agar turn block ho jaye.
    Task EnqueueTurnAsync(Func<Task> work);
}

// [KYA KARTA HAI]: Grain ke lifecycle states ko represent karne wala enum.
// [KYU ZAROORI HAI]: Silo scheduler aur monitoring dashboards ko grain health pata chal sake.
// [KAISE CHALTA HAI]: Initialized -> Active -> Idle -> Deactivated transitions follow hoti hain.
// [OUTPUT / SIDE EFFECT]: Explicit lifecycle states provide karta hai.
// [VARIATIONS TO TRY]: Suspended ya Tombstoned states introduce ki ja sakti hain.
public enum GrainLifecycleState
{
    Uninitialized,
    Active,
    Idle,
    Deactivated
}
