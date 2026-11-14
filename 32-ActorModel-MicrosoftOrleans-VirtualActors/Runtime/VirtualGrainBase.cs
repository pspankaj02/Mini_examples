namespace KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: Sabhi concrete virtual grains ke liye foundational base class provide karta hai.
// [KYU ZAROORI HAI]: Single-threaded turn serialization, lifecycle hooks, aur identity management code har actor me duplicate na ho.
// [KAISE CHALTA HAI]: SemaphoreSlim(1,1) ke zariye per-actor strict sequential turn queueing execute karta hai.
// [OUTPUT / SIDE EFFECT]: Subclasses ko zero-lock thread-safety aur automatic lifecycle state management milta hai.
// [VARIATIONS TO TRY]: System.Threading.Channels based bounded queue use kar sakte hain.
public abstract class VirtualGrainBase : IVirtualGrain
{
    // [KYA KARTA HAI]: Grain ka unique string identifier store karta hai.
    // [KYU ZAROORI HAI]: Har grain instance ko globally unique key assign hona zaroori hai.
    // [KAISE CHALTA HAI]: Constructor me initialize hota hai aur readonly rehta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable grain identity.
    // [VARIATIONS TO TRY]: Hierarchical key naming convention (e.g. "darkstore:mumbai:rider:101") use karein.
    public string GrainId { get; private set; } = string.Empty;

    // [KYA KARTA HAI]: Grain ka current lifecycle status hold karta hai.
    // [KYU ZAROORI HAI]: Silo ko pata rahe actor active hai ya decommission ho chuka hai.
    // [KAISE CHALTA HAI]: Activation/Deactivation methods ise update karte hain.
    // [OUTPUT / SIDE EFFECT]: Grain state indicator.
    // [VARIATIONS TO TRY]: State change events raise kar sakte hain.
    public GrainLifecycleState LifecycleState { get; private set; } = GrainLifecycleState.Uninitialized;

    // [KYA KARTA HAI]: Grain ko last access karne ka UTC timestamp record karta hai.
    // [KYU ZAROORI HAI]: Idle timeout GC sweepers ke liye LRU tracking zaroori hai.
    // [KAISE CHALTA HAI]: Har turn execution ke sath DateTime.UtcNow se refresh hota hai.
    // [OUTPUT / SIDE EFFECT]: Microsecond-accurate last activity time.
    // [VARIATIONS TO TRY]: Sliding window access frequency metric record karein.
    public DateTime LastAccessedUtc { get; private set; } = DateTime.UtcNow;

    // [KYA KARTA HAI]: Per-grain single-threaded turn lock provide karta hai.
    // [KYU ZAROORI HAI]: Actor model ka fundamental rule hai: Ek actor par ek waqt me sirf ek message execute hoga.
    // [KAISE CHALTA HAI]: Initial capacity 1 aur max capacity 1 ke sath SemaphoreSlim instantiate hota hai.
    // [OUTPUT / SIDE EFFECT]: Complete thread isolation without global locks.
    // [VARIATIONS TO TRY]: TaskCompletionSource chaining use kar sakte hain.
    private readonly SemaphoreSlim _turnLock = new(1, 1);

    // [KYA KARTA HAI]: Grain ko identity key ke sath initialize karta hai.
    // [KYU ZAROORI HAI]: Factory instantiation ke baad key establish karna zaroori hai.
    // [KAISE CHALTA HAI]: Silo activation flow ke dauran key set karta hai.
    // [OUTPUT / SIDE EFFECT]: Grain ready for activation.
    // [VARIATIONS TO TRY]: Custom metadata parameters inject karein.
    public void Initialize(string grainId)
    {
        // [KYA KARTA HAI]: Argument validation check karta hai.
        // [KYU ZAROORI HAI]: Null ya empty key actor cluster me corruption faila sakti hai.
        // [KAISE CHALTA HAI]: string.IsNullOrWhiteSpace se check karta hai.
        // [OUTPUT / SIDE EFFECT]: Invalid hone par ArgumentException throw karta hai.
        // [VARIATIONS TO TRY]: Specific format regex validation laga sakte hain.
        ArgumentException.ThrowIfNullOrWhiteSpace(grainId);

        // [KYA KARTA HAI]: GrainId property assign karta hai.
        // [KYU ZAROORI HAI]: Identity bind karna zaroori hai.
        // [KAISE CHALTA HAI]: Field assignment.
        // [OUTPUT / SIDE EFFECT]: GrainId set ho jata hai.
        // [VARIATIONS TO TRY]: Hash code pre-compute karke rakh sakte hain.
        GrainId = grainId;
    }

    // [KYA KARTA HAI]: Grain activation virtual hook.
    // [KYU ZAROORI HAI]: Subclasses apni state load ya initialize kar sakein.
    // [KAISE CHALTA HAI]: Base implementation state ko Active mark karti hai.
    // [OUTPUT / SIDE EFFECT]: LifecycleState = Active.
    // [VARIATIONS TO TRY]: Database se aggregate state rehydrate karein.
    public virtual Task OnActivateAsync()
    {
        // [KYA KARTA HAI]: Lifecycle state ko Active me transition karta hai.
        // [KYU ZAROORI HAI]: Actor ready for traffic indicator set hona chahiye.
        // [KAISE CHALTA HAI]: Property update.
        // [OUTPUT / SIDE EFFECT]: LifecycleState = Active.
        // [VARIATIONS TO TRY]: Activation metrics record karein.
        LifecycleState = GrainLifecycleState.Active;

        // [KYA KARTA HAI]: CompletedTask return karta hai for async signature compliance.
        // [KYU ZAROORI HAI]: Non-blocking base implementation ensure karna.
        // [KAISE CHALTA HAI]: Task.CompletedTask return.
        // [OUTPUT / SIDE EFFECT]: Zero-overhead async task.
        // [VARIATIONS TO TRY]: ValueTask use karein for zero heap allocation.
        return Task.CompletedTask;
    }

    // [KYA KARTA HAI]: Grain deactivation virtual hook.
    // [KYU ZAROORI HAI]: Subclasses uncommitted state save ya resources free kar sakein.
    // [KAISE CHALTA HAI]: Base implementation state ko Deactivated mark karti hai.
    // [OUTPUT / SIDE EFFECT]: LifecycleState = Deactivated.
    // [VARIATIONS TO TRY]: Final state snapshot database me persist karein.
    public virtual Task OnDeactivateAsync()
    {
        // [KYA KARTA HAI]: Lifecycle state ko Deactivated me transition karta hai.
        // [KYU ZAROORI HAI]: Actor teardown status register karna.
        // [KAISE CHALTA HAI]: Property update.
        // [OUTPUT / SIDE EFFECT]: LifecycleState = Deactivated.
        // [VARIATIONS TO TRY]: Teardown reasons track karein (e.g. IdleTimeout, SiloShutdown).
        LifecycleState = GrainLifecycleState.Deactivated;

        // [KYA KARTA HAI]: CompletedTask return karta hai.
        // [KYU ZAROORI HAI]: Standard asynchronous signature return.
        // [KAISE CHALTA HAI]: Return Task.CompletedTask.
        // [OUTPUT / SIDE EFFECT]: Completed async promise.
        // [VARIATIONS TO TRY]: Flush buffer await karein.
        return Task.CompletedTask;
    }

    // [KYA KARTA HAI]: Typed value returning work ko actor turn queue me serialize karta hai.
    // [KYU ZAROORI HAI]: Concurrent calls aane par race conditions prevent karna aur single-threaded safety dena.
    // [KAISE CHALTA HAI]: SemaphoreSlim await karke lock acquire karta hai, work execute karta hai, aur finally block me lock release karta hai.
    // [OUTPUT / SIDE EFFECT]: Thread-safe result return hota hai aur LastAccessedUtc refresh hota hai.
    // [VARIATIONS TO TRY]: Execution time stopwatch attach karke slow turns alert karein.
    public async Task<TResult> EnqueueTurnAsync<TResult>(Func<Task<TResult>> work)
    {
        // [KYA KARTA HAI]: Actor turn lock acquire karta hai asynchronously.
        // [KYU ZAROORI HAI]: Thread starvation ke bina sequential turn execution guarantee karna.
        // [KAISE CHALTA HAI]: WaitAsync non-blocking wait karta hai jab tak turn available na ho.
        // [OUTPUT / SIDE EFFECT]: Turn lock acquired for this actor.
        // [VARIATIONS TO TRY]: Cancellation token aur timeout pass karein taaki deadlocks na ho sakein.
        await _turnLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // [KYA KARTA HAI]: Last access timestamp refresh karta hai.
            // [KYU ZAROORI HAI]: Actor ko active keep-alive rakhna.
            // [KAISE CHALTA HAI]: UtcNow assignment.
            // [OUTPUT / SIDE EFFECT]: LRU timestamp updated.
            // [VARIATIONS TO TRY]: High-performance Stopwatch timestamp use karein.
            LastAccessedUtc = DateTime.UtcNow;

            // [KYA KARTA HAI]: Actual actor method work execute karta hai.
            // [KYU ZAROORI HAI]: Business logic run karna inside the actor boundary.
            // [KAISE CHALTA HAI]: Function delegate invoke karke result await karta hai.
            // [OUTPUT / SIDE EFFECT]: TResult return value.
            // [VARIATIONS TO TRY]: Retry logic ya exception boundary wrapping karein.
            return await work().ConfigureAwait(false);
        }
        finally
        {
            // [KYA KARTA HAI]: Actor turn lock release karta hai agle queued message ke liye.
            // [KYU ZAROORI HAI]: Lock release hona strictly mandatory hai warna actor permanently deadlock ho jayega.
            // [KAISE CHALTA HAI]: Release() call se semaphore count restore hota hai.
            // [OUTPUT / SIDE EFFECT]: Next waiting message turns unblocked.
            // [VARIATIONS TO TRY]: Pending queue length inspect karein.
            _turnLock.Release();
        }
    }

    // [KYA KARTA HAI]: Void returning work ko actor turn queue me serialize karta hai.
    // [KYU ZAROORI HAI]: State modifying commands ko sequential execution guarantee dena.
    // [KAISE CHALTA HAI]: SemaphoreSlim acquire karta hai, work execute karta hai, lock release karta hai.
    // [OUTPUT / SIDE EFFECT]: Thread-safe state mutation.
    // [VARIATIONS TO TRY]: Fire-and-forget background queueing option.
    public async Task EnqueueTurnAsync(Func<Task> work)
    {
        // [KYA KARTA HAI]: Turn lock acquire karta hai.
        // [KYU ZAROORI HAI]: Actor single-threaded execution safety.
        // [KAISE CHALTA HAI]: WaitAsync().
        // [OUTPUT / SIDE EFFECT]: Mutex lock acquired.
        // [VARIATIONS TO TRY]: Turn timeout policy shamil karein.
        await _turnLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // [KYA KARTA HAI]: Access time refresh karta hai.
            // [KYU ZAROORI HAI]: Idle sweep timing track karna.
            // [KAISE CHALTA HAI]: DateTime.UtcNow.
            // [OUTPUT / SIDE EFFECT]: LastAccessedUtc updated.
            // [VARIATIONS TO TRY]: Performance tick counter use karein.
            LastAccessedUtc = DateTime.UtcNow;

            // [KYA KARTA HAI]: Action delegate execute karta hai.
            // [KYU ZAROORI HAI]: State mutation inside single-threaded boundary.
            // [KAISE CHALTA HAI]: Await delegate.
            // [OUTPUT / SIDE EFFECT]: Side effects applied to actor state.
            // [VARIATIONS TO TRY]: State change audit logging attach karein.
            await work().ConfigureAwait(false);
        }
        finally
        {
            // [KYA KARTA HAI]: Mutex release karta hai.
            // [KYU ZAROORI HAI]: Turn complete hone par agle turn ko chance dena.
            // [KAISE CHALTA HAI]: Release().
            // [OUTPUT / SIDE EFFECT]: Turn lock released.
            // [VARIATIONS TO TRY]: Metric counter decrement karein.
            _turnLock.Release();
        }
    }
}
