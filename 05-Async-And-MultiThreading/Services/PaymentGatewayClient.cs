namespace KiranaHub.AsyncAndThreads.Services;

// =========================================================================================
// [CLASS PURPOSE]:
// High-performance asynchronous payment verification client demonstrating ValueTask,
// CancellationToken, and ConfigureAwait(false).
// =========================================================================================
public class PaymentGatewayClient
{
    // [IN-MEMORY CACHE]:
    // [KYA KARTA HAI]: Pre-verified transaction dictionary for instant fast-path resolution.
    private readonly Dictionary<string, bool> _instantSuccessCache = new()
    {
        { "TXN-FAST-01", true },
        { "TXN-FAST-02", true }
    };

    // [VALUETASK ASYNCHRONOUS METHOD]:
    // [RETURN TYPE]: ValueTask<bool> is a struct allocated on the thread STACK, avoiding Heap allocation for fast path.
    public ValueTask<bool> VerifyPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        // [FAST-PATH DICTIONARY LOOKUP]:
        // [KYA KARTA HAI]: Checks whether transaction is already in cache.
        if (_instantSuccessCache.TryGetValue(transactionId, out bool cachedResult))
        {
            // [SYNCHRONOUS VALUE WRAPPER RETURN]:
            // [KAISE CHALTA HAI]: Zero heap allocation! Returns immediately without spawning state machine.
            return new ValueTask<bool>(cachedResult);
        }

        // [SLOW-PATH DELEGATION]:
        // [KAISE CHALTA HAI]: Delegates to actual network Task when cache miss occurs.
        return new ValueTask<bool>(VerifyOverNetworkAsync(transactionId, cancellationToken));
    }

    // [PRIVATE ASYNC NETWORK CALL SIMULATION]:
    private async Task<bool> VerifyOverNetworkAsync(string transactionId, CancellationToken cancellationToken)
    {
        // [ASYNCHRONOUS NON-BLOCKING DELAY]:
        // [KYA KARTA HAI]: Thread ko free kar deta hai 200ms ke liye (simulating HTTP latency).
        // [CANCELLATIONTOKEN]: Agar caller ne timeout set kiya hai toh Task.Delay OperationCanceledException throw karega.
        // [CONFIGUREAWAIT(FALSE)]: Context capture disable karta hai for performance and deadlock avoidance.
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        // [BOOLEAN RESULT RETURN]:
        return true;
    }
}
