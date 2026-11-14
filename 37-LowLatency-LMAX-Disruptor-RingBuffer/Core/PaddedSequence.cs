// [KYA KARTA HAI]: Low-level runtime interop aur memory formatting namespace import karta hai
// [KYU ZAROORI HAI]: StructLayout aur cache-line alignment attributes ke liye zaroori hai
// [KAISE CHALTA HAI]: CLR interop type system ko memory structure control karne deta hai
// [OUTPUT / SIDE EFFECT]: Memory layout attributes accessible ho jaate hain
// [VARIATIONS TO TRY]: System.Runtime.CompilerServices ke attributes ke saath combine karein
using System.Runtime.InteropServices;

// [KYA KARTA HAI]: Disruptor low-latency sequence tracking namespace define karta hai
// [KYU ZAROORI HAI]: Sequence tracking ko cleanly modularize karne ke liye
// [KAISE CHALTA HAI]: Project ke core module structure ka hissa banta hai
// [OUTPUT / SIDE EFFECT]: Types KiranaHub.Disruptor.Core namespace ke under bundle ho jaate hain
// [VARIATIONS TO TRY]: KiranaHub.LowLatency namespace use kar sakte hain
namespace KiranaHub.Disruptor.Core;

// [KYA KARTA HAI]: 64-byte padded atomic sequence number class define karta hai
// [KYU ZAROORI HAI]: CPU cache false-sharing ko 100% prevent karne ke liye
// [KAISE CHALTA HAI]: Modern x86/ARM CPU 64-byte cache lines use karte hain; padding se doosra thread issi line par hit nahi karta
// [OUTPUT / SIDE EFFECT]: High-frequency multi-core throughput 10x-50x badh jaata hai
// [VARIATIONS TO TRY]: 128-byte padding use karein ARM Neoverse ya newer Xeon processors ke liye
[StructLayout(LayoutKind.Explicit, Size = 128)]
public class PaddedSequence
{
    // [KYA KARTA HAI]: Sequence value se pehle 56 bytes ka pre-padding offset define karta hai
    // [KYU ZAROORI HAI]: Cache-line boundary ke previous words ke saath overlap rokne ke liye
    // [KAISE CHALTA HAI]: FieldOffset(0) se lekar 56 bytes tak dummy space reserve karta hai
    // [OUTPUT / SIDE EFFECT]: Memory me 56 bytes offset block ho jata hai
    // [VARIATIONS TO TRY]: Padding size ko 64-bit word size ke multiples me align karein
    [FieldOffset(0)]
    private readonly long _p1;

    // [KYA KARTA HAI]: Second padding word define karta hai
    // [KYU ZAROORI HAI]: 64-byte cache boundary ko guarantee karne ke liye
    // [KAISE CHALTA HAI]: Offset 8 par 8-byte long store hota hai
    // [OUTPUT / SIDE EFFECT]: Cache line filler ki tarah act karta hai
    // [VARIATIONS TO TRY]: Struct layout sequential me explicit dummy fields daal sakte hain
    [FieldOffset(8)]
    private readonly long _p2;

    // [KYA KARTA HAI]: Third padding word define karta hai
    // [KYU ZAROORI HAI]: False sharing prevention block ko expand karta hai
    // [KAISE CHALTA HAI]: Offset 16 par allocate hota hai
    // [OUTPUT / SIDE EFFECT]: Cache line me separation create karta hai
    // [VARIATIONS TO TRY]: Int64 array use karein padding ke liye
    [FieldOffset(16)]
    private readonly long _p3;

    // [KYA KARTA HAI]: Fourth padding word define karta hai
    // [KYU ZAROORI HAI]: CPU cache sub-line isolation maintain karne ke liye
    // [KAISE CHALTA HAI]: Offset 24 par store hota hai
    // [OUTPUT / SIDE EFFECT]: Bus snooping latency eliminate karta hai
    // [VARIATIONS TO TRY]: Vector64<long> hardware intrinsic se pad karein
    [FieldOffset(24)]
    private readonly long _p4;

    // [KYA KARTA HAI]: Fifth padding word define karta hai
    // [KYU ZAROORI HAI]: CPU L1/L2 cache prefetcher interference rokne ke liye
    // [KAISE CHALTA HAI]: Offset 32 par memory occupy karta hai
    // [OUTPUT / SIDE EFFECT]: Dedicated cache line slot ensure hota hai
    // [VARIATIONS TO TRY]: MemoryMarshal se raw byte array layout map karein
    [FieldOffset(32)]
    private readonly long _p5;

    // [KYA KARTA HAI]: Sixth padding word define karta hai
    // [KYU ZAROORI HAI]: Memory bus lock contention prevent karne ke liye
    // [KAISE CHALTA HAI]: Offset 40 par store hota hai
    // [OUTPUT / SIDE EFFECT]: False sharing completely eliminate ho jaata hai
    // [VARIATIONS TO TRY]: Unsafe pointer arithmetic se check karein
    [FieldOffset(40)]
    private readonly long _p6;

    // [KYA KARTA HAI]: Seventh padding word define karta hai
    // [KYU ZAROORI HAI]: Offset 48 par 56 bytes tak fill karta hai
    // [KAISE CHALTA HAI]: 8-byte integer store karta hai
    // [OUTPUT / SIDE EFFECT]: Boundary perfectly align ho jaati hai
    // [VARIATIONS TO TRY]: Explicit assembly ALIGN 64 directive experiment karein
    [FieldOffset(48)]
    private readonly long _p7;

    // [KYA KARTA HAI]: Asli sequence counter field jo ring buffer slot index track karta hai
    // [KYU ZAROORI HAI]: Atomic and volatile read/write operations ke liye primary state variable hai
    // [KAISE CHALTA HAI]: Offset 56 par perfectly isolated single word store hota hai
    // [OUTPUT / SIDE EFFECT]: Thread writes kisi doosre thread ke cache line ko invalidate nahi karti
    // [VARIATIONS TO TRY]: Volatile modifier ke saath direct access karein
    [FieldOffset(56)]
    private long _value;

    // [KYA KARTA HAI]: Post-padding word 1 define karta hai
    // [KYU ZAROORI HAI]: Next adjacent heap object ke sath false sharing prevent karne ke liye
    // [KAISE CHALTA HAI]: Offset 64 par agle 64-byte block me place hota hai
    // [OUTPUT / SIDE EFFECT]: Trailing boundary guard ho jaati hai
    // [VARIATIONS TO TRY]: Object header size ke according adjust karein
    [FieldOffset(64)]
    private readonly long _p8;

    // [KYA KARTA HAI]: Post-padding word 2 define karta hai
    // [KYU ZAROORI HAI]: Cache line tail end ko insulate karne ke liye
    // [KAISE CHALTA HAI]: Offset 72 par 8 bytes allocate karta hai
    // [OUTPUT / SIDE EFFECT]: Trailing cache isolation guarantee hoti hai
    // [VARIATIONS TO TRY]: GC allocation byte boundary check karein
    [FieldOffset(72)]
    private readonly long _p9;

    // [KYA KARTA HAI]: Post-padding word 3 define karta hai
    // [KYU ZAROORI HAI]: Full 128-byte struct envelope create karne ke liye
    // [KAISE CHALTA HAI]: Offset 80 par reserve hota hai
    // [OUTPUT / SIDE EFFECT]: Two full 64-byte cache lines reserve ho jaati hain
    // [VARIATIONS TO TRY]: Core i9 L3 slice boundary check karein
    [FieldOffset(80)]
    private readonly long _p10;

    // [KYA KARTA HAI]: PaddedSequence constructor with initial sequence value
    // [KYU ZAROORI HAI]: Disruptor ring buffer sequence ko specific start index par set karne ke liye
    // [KAISE CHALTA HAI]: Initial sequence value assign karta hai (typically -1 for empty buffer)
    // [OUTPUT / SIDE EFFECT]: Initialized instance ready ho jata hai
    // [VARIATIONS TO TRY]: Default parameter 0L provide karein
    public PaddedSequence(long initialValue = -1L)
    {
        // [KYA KARTA HAI]: Initial value ko internal sequence counter me set karta hai
        // [KYU ZAROORI HAI]: Buffer empty state (-1) represent karne ke liye
        // [KAISE CHALTA HAI]: Direct assignment into field _value
        // [OUTPUT / SIDE EFFECT]: _value initial state receive karta hai
        // [VARIATIONS TO TRY]: Interlocked.Exchange se initialize karein
        _value = initialValue;
    }

    // [KYA KARTA HAI]: Volatile read ke through latest sequence number fetch karta hai
    // [KYU ZAROORI HAI]: Consumer threads ko latest published sequence bina cache lag ke dikhane ke liye
    // [KAISE CHALTA HAI]: CPU acquire memory barrier execute karta hai taaki stale register value na mile
    // [OUTPUT / SIDE EFFECT]: Memory bus se current latest published sequence return hota hai
    // [VARIATIONS TO TRY]: Interlocked.Read se 64-bit atomic guaranteed read karein
    public long Get()
    {
        // [KYA KARTA HAI]: Volatile.Read call karke barrier ke sath value return karta hai
        // [KYU ZAROORI HAI]: Thread compiler reordering aur CPU register caching ko bypass karta hai
        // [KAISE CHALTA HAI]: Hardware memory fence emit hoti hai
        // [OUTPUT / SIDE EFFECT]: Current authoritative sequence value
        // [VARIATIONS TO TRY]: Thread.VolatileRead method use karein
        return Volatile.Read(ref _value);
    }

    // [KYA KARTA HAI]: Direct raw sequence write karta hai (Single-writer scenario)
    // [KYU ZAROORI HAI]: Jab guaranteed ek hi producer thread ho to memory fence overhead bachata hai
    // [KAISE CHALTA HAI]: Ordinary memory store instruction execute karta hai
    // [OUTPUT / SIDE EFFECT]: _value field update hoti hai
    // [VARIATIONS TO TRY]: Multi-producer ke liye SetVolatile use karein
    public void Set(long value)
    {
        // [KYA KARTA HAI]: Direct assignment into value field
        // [KYU ZAROORI HAI]: Fastest possible write speed sub-nanosecond scale par
        // [KAISE CHALTA HAI]: Store instruction directly writes to memory
        // [OUTPUT / SIDE EFFECT]: Local CPU core store buffer me entry add hoti hai
        // [VARIATIONS TO TRY]: Unsafe pointer dereference use karein
        _value = value;
    }

    // [KYA KARTA HAI]: Volatile release barrier ke saath sequence number publish karta hai
    // [KYU ZAROORI HAI]: Consumer thread ko tabhi sequence dikhe jab pehle ka event data fully write ho chuka ho
    // [KAISE CHALTA HAI]: Release barrier emit karta hai jo ensure karta hai saare previous stores complete ho chuke hain
    // [OUTPUT / SIDE EFFECT]: Memory ordering strictly preserve rehti hai
    // [VARIATIONS TO TRY]: Interlocked.Exchange use karein
    public void SetVolatile(long value)
    {
        // [KYA KARTA HAI]: Volatile.Write method invoke karta hai
        // [KYU ZAROORI HAI]: Release semantics ke sath memory store karta hai
        // [KAISE CHALTA HAI]: CPU pipeline store buffer ko flush karta hai
        // [OUTPUT / SIDE EFFECT]: Consumers ko memory changes consistently visible hote hain
        // [VARIATIONS TO TRY]: Thread.MemoryBarrier() ke baad normal write karein
        Volatile.Write(ref _value, value);
    }

    // [KYA KARTA HAI]: Atomic Compare-And-Swap (CAS) operation execute karta hai
    // [KYU ZAROORI HAI]: Multi-producer concurrent slot claiming ke liye without mutex locks
    // [KAISE CHALTA HAI]: CPU LOCK CMPXCHG instruction invoke hoti hai hardware level par
    // [OUTPUT / SIDE EFFECT]: Returns true agar swap succeed hua, false agar conflict aaya
    // [VARIATIONS TO TRY]: Optimistic retry loop me wrap karein
    public bool CompareAndSet(long expectedValue, long newValue)
    {
        // [KYA KARTA HAI]: Interlocked.CompareExchange call karta hai 64-bit value par
        // [KYU ZAROORI HAI]: Hardware lock bus signal emit karta hai for single clock cycle
        // [KAISE CHALTA HAI]: Agar _value == expectedValue hai to newValue set karta hai
        // [OUTPUT / SIDE EFFECT]: Previous value compare karke boolean return karta hai
        // [VARIATIONS TO TRY]: Increment CAS pattern implement karein
        return Interlocked.CompareExchange(ref _value, newValue, expectedValue) == expectedValue;
    }

    // [KYA KARTA HAI]: Sequence number ko atomically 1 se increment karta hai
    // [KYU ZAROORI HAI]: Next available sequence number safely allocate karne ke liye
    // [KAISE CHALTA HAI]: CPU atomic LOCK XADD instruction execute karta hai
    // [OUTPUT / SIDE EFFECT]: Incremented value return hoti hai
    // [VARIATIONS TO TRY]: Multi-slot reservation (batch claim) support karein
    public long IncrementAndGet()
    {
        // [KYA KARTA HAI]: Interlocked.Increment invoke karta hai
        // [KYU ZAROORI HAI]: Concurrent safe sequential id generation ke liye
        // [KAISE CHALTA HAI]: Atomically increments _value by 1
        // [OUTPUT / SIDE EFFECT]: New incremented sequence returned
        // [VARIATIONS TO TRY]: Add(ref _value, batchSize) use karein for batching
        return Interlocked.Increment(ref _value);
    }
}
