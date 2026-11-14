using Microsoft.Extensions.Caching.Memory;

namespace KiranaHub.DataAndCaching.Caching;

// =========================================================================================
// [TOPIC]: In-Memory Caching with Cache Stampede (Thundering Herd) Protection
// [PURPOSE]: Thread-safe cache-aside pattern using SemaphoreSlim double-checked locking.
// =========================================================================================

// [KYA KARTA HAI]: High-performance cache wrapper jo IMemoryCache ke around concurrency safety provide karta hai.
// [KYU ZAROORI HAI]: Cache expire hone par lakho concurrent requests ko ek sath database crash karne se rokna.
public class KiranaCacheManager
{
    // [KYA KARTA HAI]: Injected IMemoryCache ka private reference.
    private readonly IMemoryCache _memoryCache;

    // [KYA KARTA HAI]: Concurrency lock semaphore with initial count 1 and max count 1.
    // [KYU ZAROORI HAI]: Non-blocking async lock (Monitor.Enter / lock statement cannot be awaited across async tasks).
    // [KAISE CHALTA HAI]: Ek waqt me sirf 1 thread WaitAsync() pass kar sakta hai, baki queue me wait karte hain.
    private readonly SemaphoreSlim _stampedeLock = new(1, 1);

    // [KYA KARTA HAI]: Constructor DI injection.
    public KiranaCacheManager(IMemoryCache memoryCache)
    {
        // [KYA KARTA HAI]: Null check ke sath cache store karta hai.
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    // [KYA KARTA HAI]: Cache-Aside pattern execute karta hai with double-checked stampede protection.
    // [KYU ZAROORI HAI]: Cache hit par zero-lock instant return, cache miss par serialized single DB call.
    // [KAISE CHALTA HAI]: Fast path try -> Lock acquire -> Second check -> Factory execute -> Set cache -> Release lock.
    public async Task<T> GetOrCreateWithStampedeProtectionAsync<T>(
        string cacheKey,
        Func<Task<T>> databaseFactory,
        TimeSpan absoluteExpiration,
        TimeSpan slidingExpiration)
    {
        // ---------------------------------------------------------------------------------
        // 1. FAST-PATH UNLOCKED CHECK
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: MemoryCache me key check karta hai bina kisi lock overhead ke.
        // [KYU ZAROORI HAI]: 99% requests cache-hits hoti hain, unhe bina semaphore wait ke instant return karna.
        if (_memoryCache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue != null)
        {
            // [KYA KARTA HAI]: Cached item return karta hai immediately.
            return cachedValue;
        }

        // ---------------------------------------------------------------------------------
        // 2. ACQUIRE ASYNC SEMAPHORE LOCK
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Concurrency lock acquire karta hai asynchronously.
        // [KYU ZAROORI HAI]: Sirf 1 worker thread database query execute karega.
        // [KAISE CHALTA HAI]: Thread pool thread block nahi hota; await state machine yield karti hai.
        await _stampedeLock.WaitAsync();

        // [KYA KARTA HAI]: Try block start karta hai taaki finally me lock release guaranteed ho.
        try
        {
            // -----------------------------------------------------------------------------
            // 3. DOUBLE-CHECKED LOCKING (SECOND CHECK INSIDE LOCK)
            // -----------------------------------------------------------------------------

            // [KYA KARTA HAI]: Lock ke andar aakar dubara cache check karta hai.
            // [KYU ZAROORI HAI]: Jab ye thread queue me wait kar raha tha, shayad pichle thread ne cache populate kar diya ho!
            if (_memoryCache.TryGetValue(cacheKey, out cachedValue) && cachedValue != null)
            {
                // [KYA KARTA HAI]: Updated cache value return karta hai without calling DB.
                return cachedValue;
            }

            // -----------------------------------------------------------------------------
            // 4. DATABASE INVOCATION (SINGLE FLIGHT)
            // -----------------------------------------------------------------------------

            // [KYA KARTA HAI]: Cache miss log karta hai console par for demonstration.
            Console.WriteLine($"[CACHE MISS] Executing Database Query for key: '{cacheKey}'");

            // [KYA KARTA HAI]: User-supplied database query factory delegate ko await karta hai.
            // [OUTPUT / RETURN]: Fresh data from database.
            T freshData = await databaseFactory();

            // -----------------------------------------------------------------------------
            // 5. CACHE CONFIGURATION & POST-EVICTION CALLBACK
            // -----------------------------------------------------------------------------

            // [KYA KARTA HAI]: Cache options setup karta hai with absolute and sliding expiration policies.
            var cacheOptions = new MemoryCacheEntryOptions()
                // [KYA KARTA HAI]: Hard expiration limit set karta hai (e.g. 5 minutes ke baad expire hoga hi).
                .SetAbsoluteExpiration(absoluteExpiration)
                // [KYA KARTA HAI]: Inactivity window set karta hai (e.g. agar 1 minute tak kisi ne access nahi kiya toh expire hoga).
                .SetSlidingExpiration(slidingExpiration)
                // [KYA KARTA HAI]: Eviction hook register karta hai jo key purge hone par trigger hota hai.
                // [KYU ZAROORI HAI]: Observability and cache analytics.
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    // [KYA KARTA HAI]: Eviction event log karta hai console par with reason (Expired, Removed, Capacity).
                    Console.WriteLine($"[CACHE EVICTED] Key '{key}' purged from memory. Reason: {reason}");
                });

            // [KYA KARTA HAI]: Freshly fetched data ko memory cache me store karta hai configured options ke sath.
            _memoryCache.Set(cacheKey, freshData, cacheOptions);

            // [KYA KARTA HAI]: Fresh data return karta hai caller ko.
            return freshData;
        }
        // [KYA KARTA HAI]: Finally block jo guarantee karta hai ki semaphore count restore hoga.
        finally
        {
            // [KYA KARTA HAI]: Semaphore lock release karta hai.
            // [KYU ZAROORI HAI]: Deadlock prevent karna agar database factory exception throw kare.
            _stampedeLock.Release();
        }
    }
}
