// [KYA KARTA HAI]: Core Disruptor namespace import karta hai
// [KYU ZAROORI HAI]: IWaitStrategy aur PaddedSequence access karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping provide karta hai
// [OUTPUT / SIDE EFFECT]: Types available ho jaate hain
// [VARIATIONS TO TRY]: Aliased imports use karein
namespace KiranaHub.Disruptor.Core;

// [KYA KARTA HAI]: Low-latency Yielding wait strategy implementation class
// [KYU ZAROORI HAI]: Ultra-low latency maintain karte hue CPU 100% saturation prevent karne ke liye
// [KAISE CHALTA HAI]: First 100 iterations busy-spin karta hai, fir Thread.Yield() call karta hai
// [OUTPUT / SIDE EFFECT]: Sub-microsecond response time with cooperative multitasking
// [VARIATIONS TO TRY]: Spin count ko hardware core count ke basis par tune karein
public class YieldingWaitStrategy : IWaitStrategy
{
    // [KYA KARTA HAI]: Spinning cycle threshold constant define karta hai
    // [KYU ZAROORI HAI]: Yield karne se pehle kitni baar spin karna hai decide karne ke liye
    // [KAISE CHALTA HAI]: Constant integer 100 cycles set karta hai
    // [OUTPUT / SIDE EFFECT]: 100 tight spins before OS thread yielding
    // [VARIATIONS TO TRY]: 200 ya 500 spin count test karein
    private const int SpinTries = 100;

    // [KYA KARTA HAI]: Target sequence available hone tak spin aur yield karke wait karta hai
    // [KYU ZAROORI HAI]: Consumer ko producer ke saath synchronize karne ke liye
    // [KAISE CHALTA HAI]: Dependent sequence check karta hai; agar chhota hai to spin/yield loop chalaata hai
    // [OUTPUT / SIDE EFFECT]: Available sequence number return karta hai
    // [VARIATIONS TO TRY]: Exponential backoff introduce karein
    public long WaitFor(long sequence, PaddedSequence cursor, PaddedSequence dependentSequence)
    {
        // [KYA KARTA HAI]: Available sequence ko volatile read se fetch karta hai
        // [KYU ZAROORI HAI]: Current published progress check karne ke liye
        // [KAISE CHALTA HAI]: dependentSequence.Get() call karta hai
        // [OUTPUT / SIDE EFFECT]: Initial available sequence value
        // [VARIATIONS TO TRY]: Cursor directly read karein
        long availableSequence;

        // [KYA KARTA HAI]: Spin counter variable initialize karta hai
        // [KYU ZAROORI HAI]: Iteration count track karne ke liye
        // [KAISE CHALTA HAI]: Integer counter initialized to SpinTries
        // [OUTPUT / SIDE EFFECT]: Counter initialized
        // [VARIATIONS TO TRY]: Decrement counter vs increment counter
        int counter = SpinTries;

        // [KYA KARTA HAI]: Polling loop jab tak target sequence available na ho
        // [KYU ZAROORI HAI]: Consumer tabhi proceed kare jab data safely write ho chuka ho
        // [KAISE CHALTA HAI]: availableSequence < sequence condition evaluate karta hai
        // [OUTPUT / SIDE EFFECT]: Loop continues till data available
        // [VARIATIONS TO TRY]: Timeout check add karein
        while ((availableSequence = dependentSequence.Get()) < sequence)
        {
            // [KYA KARTA HAI]: Agar counter abhi positive hai to CPU spin wait karta hai
            // [KYU ZAROORI HAI]: OS context switch avoid karne ke liye tight loop spinning
            // [KAISE CHALTA HAI]: Thread.SpinWait(10) hardware pause instruction emit karta hai
            // [OUTPUT / SIDE EFFECT]: CPU pipeline pipeline stalls prevent hoti hain
            // [VARIATIONS TO TRY]: Thread.SpinWait(1) for faster polling
            if (counter > 0)
            {
                // [KYA KARTA HAI]: Counter decrement karta hai
                // [KYU ZAROORI HAI]: Spin attempts track karne ke liye
                // [KAISE CHALTA HAI]: counter-- execution
                // [OUTPUT / SIDE EFFECT]: Counter reduced
                // [VARIATIONS TO TRY]: Bitwise decrement
                counter--;

                // [KYA KARTA HAI]: Hardware spin wait call karta hai
                // [KYU ZAROORI HAI]: X86 PAUSE instruction trigger karta hai
                // [KAISE CHALTA HAI]: CPU pipeline ko hint deta hai ki ye spin-wait loop hai
                // [OUTPUT / SIDE EFFECT]: Power consumption reduce hota hai
                // [VARIATIONS TO TRY]: Thread.SpinWait(50)
                Thread.SpinWait(10);
            }
            // [KYA KARTA HAI]: Agar spin count exceed ho gaya to OS scheduler ko turn yield karta hai
            // [KYU ZAROORI HAI]: Dusre threads (like producer) ko CPU core mil sake
            // [KAISE CHALTA HAI]: Thread.Yield() calls current OS slice yield
            // [OUTPUT / SIDE EFFECT]: Prevents CPU starvation of producer threads
            // [VARIATIONS TO TRY]: Thread.Sleep(0) use karein
            else
            {
                // [KYA KARTA HAI]: Thread.Yield() execute karta hai
                // [KYU ZAROORI HAI]: Same priority ke doosre thread ko schedule hone deta hai
                // [KAISE CHALTA HAI]: OS dispatcher ko control yield karta hai
                // [OUTPUT / SIDE EFFECT]: Cooperative multitasking
                // [VARIATIONS TO TRY]: Sleep(1) for higher latency tolerance
                Thread.Yield();
            }
        }

        // [KYA KARTA HAI]: Safely available sequence return karta hai
        // [KYU ZAROORI HAI]: Consumer is sequence tak saare events batch me process kar sakta hai
        // [KAISE CHALTA HAI]: Returns computed availableSequence
        // [OUTPUT / SIDE EFFECT]: Unlocks consumer batch processing
        // [VARIATIONS TO TRY]: Math.Min(availableSequence, sequence + batchLimit)
        return availableSequence;
    }

    // [KYA KARTA HAI]: Non-blocking strategy hone ke karan signal method no-op rehta hai
    // [KYU ZAROORI HAI]: Interface compliance maintain karne ke liye
    // [KAISE CHALTA HAI]: Empty method body execution
    // [OUTPUT / SIDE EFFECT]: Zero overhead
    // [VARIATIONS TO TRY]: Metrics counter increment karein
    public void SignalAllWhenBlocking()
    {
        // [KYA KARTA HAI]: Yielding strategy does not block on monitors, no-op needed
        // [KYU ZAROORI HAI]: Lock-free execution guarantees zero context switch
        // [KAISE CHALTA HAI]: Execution passes through
        // [OUTPUT / SIDE EFFECT]: Nothing changed
        // [VARIATIONS TO TRY]: Add trace logging
    }
}
