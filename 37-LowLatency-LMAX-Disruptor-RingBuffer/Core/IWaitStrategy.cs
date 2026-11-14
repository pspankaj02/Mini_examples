// [KYA KARTA HAI]: Core namespace define karta hai wait strategy contract ke liye
// [KYU ZAROORI HAI]: Disruptor waiting mechanisms ko pluggable aur modular rakhne ke liye
// [KAISE CHALTA HAI]: Wait strategy interface declare karta hai
// [OUTPUT / SIDE EFFECT]: Contract standard define hota hai
// [VARIATIONS TO TRY]: KiranaHub.Disruptor.Strategies namespace use karein
namespace KiranaHub.Disruptor.Core;

// [KYA KARTA HAI]: Consumer thread wait strategy interface define karta hai
// [KYU ZAROORI HAI]: Low-latency busy spin, yielding, ya blocking wait strategies ko interchange karne ke liye
// [KAISE CHALTA HAI]: Consumer sequence barrier is method ko call karta hai jab expected sequence available na ho
// [OUTPUT / SIDE EFFECT]: Pluggable latency vs CPU trade-off enable hota hai
// [VARIATIONS TO TRY]: SleepingWaitStrategy ya TimeoutWaitStrategy implement karein
public interface IWaitStrategy
{
    // [KYA KARTA HAI]: Target sequence available hone tak wait karta hai
    // [KYU ZAROORI HAI]: Consumer ko tab tak hold karne ke liye jab tak producer naya event publish na kar de
    // [KAISE CHALTA HAI]: Dependent sequence barrier ko monitor karta hai
    // [OUTPUT / SIDE EFFECT]: Returns available sequence number jo process karne ke liye safe hai
    // [VARIATIONS TO TRY]: CancellationToken pass karke graceful shutdown support karein
    long WaitFor(long sequence, PaddedSequence cursor, PaddedSequence dependentSequence);

    // [KYA KARTA HAI]: Blocked consumers ko signal karta hai jab naya event publish hota hai
    // [KYU ZAROORI HAI]: Blocking wait strategies me sleeping threads ko wake up karne ke liye
    // [KAISE CHALTA HAI]: Condition variable ya monitor pulse emit karta hai
    // [OUTPUT / SIDE EFFECT]: Waiting consumer threads wake up ho jaate hain
    // [VARIATIONS TO TRY]: Multi-consumer broadcast notification support karein
    void SignalAllWhenBlocking();
}
