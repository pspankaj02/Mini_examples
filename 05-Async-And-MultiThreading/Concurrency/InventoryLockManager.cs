namespace KiranaHub.AsyncAndThreads.Concurrency;

// =========================================================================================
// [CLASS PURPOSE]:
// Thread synchronization primitives demonstrating synchronous `lock` (Monitor)
// and asynchronous throttling using `SemaphoreSlim`.
// =========================================================================================
public class InventoryLockManager
{
    // [SYNCHRONIZATION OBJECT]:
    // Dedicated object allocated on heap used exclusively for monitor lock token.
    private readonly object _syncLock = new();

    // [SHARED MUTABLE STATE]:
    private int _inMemoryStock = 100;

    // [SEMAPHORESLIM CONCURRENCY THROTTLER]:
    // [INITIALCOUNT: 3, MAXCOUNT: 3]: Sirf 3 tasks ek sath parallel me access kar sakte hain.
    private readonly SemaphoreSlim _throttler = new(initialCount: 3, maxCount: 3);

    // [SYNCHRONOUS LOCK METHOD]:
    public bool TryDeductStockSynchronous(int quantity)
    {
        // [MONITOR CRITICAL SECTION]:
        // Monitor.Enter(_syncLock) is called; guarantees single-thread exclusive access.
        lock (_syncLock)
        {
            // [CONDITION CHECK]:
            if (_inMemoryStock >= quantity)
            {
                // [STATE MUTATION]: Safe from race conditions
                _inMemoryStock -= quantity;
                return true;
            }
            return false;
        } // Monitor.Exit(_syncLock) called automatically
    }

    // [SAFE READ METHOD]:
    public int GetStock()
    {
        // Read lock prevents reading half-written data on multi-core processors
        lock (_syncLock) return _inMemoryStock;
    }

    // [ASYNCHRONOUS THROTTLED METHOD]:
    public async Task<bool> ReserveStockAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        // [ASYNC WAIT ON SEMAPHORE]:
        // Does not block OS thread! Caller task waits in async queue until a slot is free.
        await _throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // [SIMULATED DB WORK]:
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            // [CRITICAL RELEASE IN FINALLY]:
            // Counter incremented back, waking up next queued task.
            _throttler.Release();
        }
    }
}
