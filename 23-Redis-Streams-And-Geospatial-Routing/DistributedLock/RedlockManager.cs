// [KYA KARTA HAI]: System namespace import karta hai for timing.
// [KYU ZAROORI HAI]: Lease TTL expiration aur concurrency synchronization ke liye.
// [KAISE CHALTA HAI]: .NET core BCL.
// [OUTPUT / SIDE EFFECT]: Basic types.
// [VARIATIONS TO TRY]: Redis SET NX PX commands.
using System;

// [KYA KARTA HAI]: Namespace define karta hai Distributed Locking engine ke liye.
// [KYU ZAROORI HAI]: Redlock concurrency primitives encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: DistributedLock namespace.
// [VARIATIONS TO TRY]: RedLock.net official package.
namespace KiranaHub.RedisAdvanced.DistributedLock;

// [KYA KARTA HAI]: Internal lock state metadata class.
// [KYU ZAROORI HAI]: Lock owner aur expiration time track karne ke liye.
// [KAISE CHALTA HAI]: Owner string aur Expiry timestamp encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: LockState class.
// [VARIATIONS TO TRY]: Reentrant lock count for recursive calls.
internal class LockState
{
    public string OwnerId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

// [KYA KARTA HAI]: Redlock Distributed Lock Manager jo Redis atomic SETNX with TTL emulate karta hai.
// [KYU ZAROORI HAI]: Flash-sale me jab 1 stock bacha ho aur 50 customers ek hi millisecond me order place karein, toh race condition aur overselling rokne ke liye.
// [KAISE CHALTA HAI]: Atomic check-and-set dictionary use karta hai jisme expired locks automatically invalidate ho jate hain.
// [OUTPUT / SIDE EFFECT]: RedlockManager class.
// [VARIATIONS TO TRY]: Multi-node Redis Redlock quorum algorithm.
public class RedlockManager
{
    private readonly Dictionary<string, LockState> _activeLocks = new();
    private readonly object _lock = new();

    // [KYA KARTA HAI]: Resource par distributed lock acquire karne ki koshish karta hai (SET key owner NX PX ttl).
    // [KYU ZAROORI HAI]: Critical section me sirf ek worker ko enter karne ki permission dena.
    // [KAISE CHALTA HAI]: Check karta hai ki key exist karti hai ya nahi ya expire ho chuki hai. Agar free hai toh owner bind karta hai.
    // [OUTPUT / SIDE EFFECT]: Boolean true if lock acquired, false if contended.
    // [VARIATIONS TO TRY]: Retry loop with randomized backoff.
    public bool TryAcquireLock(string resourceKey, string ownerId, TimeSpan ttl)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (_activeLocks.TryGetValue(resourceKey, out var existingLock))
            {
                // Agar existing lock expire ho chuka hai, toh naya lock allow karein
                if (existingLock.ExpiresAtUtc > now)
                {
                    return false; // Lock is currently held by someone else!
                }
            }

            _activeLocks[resourceKey] = new LockState
            {
                OwnerId = ownerId,
                ExpiresAtUtc = now.Add(ttl)
            };
            return true;
        }
    }

    // [KYA KARTA HAI]: Resource lock safely release karta hai (Lua script emulation: only owner can release).
    // [KYU ZAROORI HAI]: Kisi doosre process ka lock accidental delete hone se rokne ke liye.
    // [KAISE CHALTA HAI]: Owner match hone par hi dictionary se key remove karta hai.
    // [OUTPUT / SIDE EFFECT]: Boolean true if released, false if unauthorized or expired.
    // [VARIATIONS TO TRY]: Redis Lua EVAL script.
    public bool ReleaseLock(string resourceKey, string ownerId)
    {
        lock (_lock)
        {
            if (_activeLocks.TryGetValue(resourceKey, out var existingLock))
            {
                if (existingLock.OwnerId == ownerId)
                {
                    _activeLocks.Remove(resourceKey);
                    return true;
                }
            }
            return false;
        }
    }
}
