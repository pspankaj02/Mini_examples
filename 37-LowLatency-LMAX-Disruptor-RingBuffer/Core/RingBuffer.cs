// [KYA KARTA HAI]: Core Disruptor namespace define karta hai
// [KYU ZAROORI HAI]: RingBuffer component ko core module ke andar group karne ke liye
// [KAISE CHALTA HAI]: Namespace block encapsulates the circular ring buffer implementation
// [OUTPUT / SIDE EFFECT]: RingBuffer<T> type core namespace me register hota hai
// [VARIATIONS TO TRY]: Generic data structures namespace me shift kar sakte hain
namespace KiranaHub.Disruptor.Core;

// [KYA KARTA HAI]: Lock-free pre-allocated Circular Ring Buffer data structure
// [KYU ZAROORI HAI]: Zero memory allocation aur sub-microsecond queueing latency achieve karne ke liye
// [KAISE CHALTA HAI]: Power-of-2 size array use karta hai jisme index bitwise AND (&) se compute hota hai bina modulo (%) ke
// [OUTPUT / SIDE EFFECT]: Million ops/sec par bhi GC Gen0/1/2 collection count zero rehta hai
// [VARIATIONS TO TRY]: Struct memory arena backed ring buffer banayein
public class RingBuffer<T> where T : class
{
    // [KYA KARTA HAI]: Pre-allocated event objects ka contiguous circular array
    // [KYU ZAROORI HAI]: Startup par memory reserve karke runtime allocations ko zero karne ke liye
    // [KAISE CHALTA HAI]: Constructor me loop chala kar saare slots populate kiye jaate hain
    // [OUTPUT / SIDE EFFECT]: Fixed memory foot-print in heap
    // [VARIATIONS TO TRY]: Native unmanaged memory block use karein
    private readonly T[] _entries;

    // [KYA KARTA HAI]: Circular buffer ka total capacity (must be power of 2)
    // [KYU ZAROORI HAI]: Ring buffer wrap-around calculate karne ke liye
    // [KAISE CHALTA HAI]: Readonly integer holding buffer capacity (e.g. 1024, 65536)
    // [OUTPUT / SIDE EFFECT]: Fixed size bound
    // [VARIATIONS TO TRY]: Dynamic power of two resize support karein
    private readonly int _bufferSize;

    // [KYA KARTA HAI]: Fast bitwise index mask (bufferSize - 1)
    // [KYU ZAROORI HAI]: Modulo operator (%) CPU me 15-40 cycles leta hai jabki bitwise AND (&) sirf 1 cycle leta hai
    // [KAISE CHALTA HAI]: Binary mask (e.g., 1024 - 1 = 1023 -> 0x3FF) se sequence number ko mask karta hai
    // [OUTPUT / SIDE EFFECT]: Instantaneous array index translation
    // [VARIATIONS TO TRY]: Constant size bitwise literal use karein
    private readonly long _indexMask;

    // [KYA KARTA HAI]: Producer cursor sequence tracker
    // [KYU ZAROORI HAI]: Ring buffer me currently published highest sequence number ko track karne ke liye
    // [KAISE CHALTA HAI]: PaddedSequence instance with 64-byte padding to prevent false sharing
    // [OUTPUT / SIDE EFFECT]: Tracks producer head safely across CPU cores
    // [VARIATIONS TO TRY]: Multi-producer claimed sequence array use karein
    private readonly PaddedSequence _cursor = new(-1L);

    // [KYA KARTA HAI]: Dependent gating sequences array (slowest consumer tracking)
    // [KYU ZAROORI HAI]: Producer ko slowest consumer se aage lap (overwrite) karne se rokne ke liye
    // [KAISE CHALTA HAI]: Array of PaddedSequence references representing consumer cursors
    // [OUTPUT / SIDE EFFECT]: Buffer wrap-around safety guarantee hoti hai
    // [VARIATIONS TO TRY]: ImmutableArray of PaddedSequence use karein
    private PaddedSequence[] _gatingSequences = [];

    // [KYA KARTA HAI]: Waiting strategy instance
    // [KYU ZAROORI HAI]: Producer-consumer synchronization strategy provide karne ke liye
    // [KAISE CHALTA HAI]: IWaitStrategy instance jo yielding ya spin-wait handle karti hai
    // [OUTPUT / SIDE EFFECT]: Synchronization behaviour defined
    // [VARIATIONS TO TRY]: BusySpinWaitStrategy pass karein
    private readonly IWaitStrategy _waitStrategy;

    // [KYA KARTA HAI]: RingBuffer constructor jo buffer size, factory aur wait strategy accept karta hai
    // [KYU ZAROORI HAI]: Pre-allocation aur initialization ko atomically perform karne ke liye
    // [KAISE CHALTA HAI]: Size ko power-of-two validate karta hai aur entries array ko pre-populate karta hai
    // [OUTPUT / SIDE EFFECT]: Initialized ready-to-use ring buffer instance
    // [VARIATIONS TO TRY]: Buffer size auto-round to nearest power of two
    public RingBuffer(int bufferSize, Func<T> eventFactory, IWaitStrategy waitStrategy)
    {
        // [KYA KARTA HAI]: Buffer size ko power of 2 hone ke liye validate karta hai
        // [KYU ZAROORI HAI]: Bitwise AND masking tabhi kaam karti hai jab size $2^n$ ho
        // [KAISE CHALTA HAI]: Bitwise check: (size & (size - 1)) == 0
        // [OUTPUT / SIDE EFFECT]: Throws ArgumentException if size is not power of 2
        // [VARIATIONS TO TRY]: NextPowerOfTwo utility se auto adjust karein
        if (bufferSize < 1 || (bufferSize & (bufferSize - 1)) != 0)
        {
            // [KYA KARTA HAI]: Invalid size error throw karta hai
            // [KYU ZAROORI HAI]: Non-power-of-two sizes se masking crash ya buffer overrun ho sakta hai
            // [KAISE CHALTA HAI]: Throws ArgumentException
            // [OUTPUT / SIDE EFFECT]: Process halts with clear error message
            // [VARIATIONS TO TRY]: Custom RingBufferException throw karein
            throw new ArgumentException("RingBuffer size must be a positive power of 2", nameof(bufferSize));
        }

        // [KYA KARTA HAI]: Buffer size field initialize karta hai
        // [KYU ZAROORI HAI]: Internal capacity state store karne ke liye
        // [KAISE CHALTA HAI]: Assigns validated bufferSize
        // [OUTPUT / SIDE EFFECT]: _bufferSize populated
        // [VARIATIONS TO TRY]: Readonly property se expose karein
        _bufferSize = bufferSize;

        // [KYA KARTA HAI]: Index mask compute karta hai (size - 1)
        // [KYU ZAROORI HAI]: Mask se slot index instantly compute hota hai: (seq & _indexMask)
        // [KAISE CHALTA HAI]: Subtraction of 1 from power of 2
        // [OUTPUT / SIDE EFFECT]: _indexMask ready
        // [VARIATIONS TO TRY]: Const expression if templated
        _indexMask = bufferSize - 1;

        // [KYA KARTA HAI]: Wait strategy field assign karta hai
        // [KYU ZAROORI HAI]: Consumer coordination mechanism attach karne ke liye
        // [KAISE CHALTA HAI]: Reference assignment
        // [OUTPUT / SIDE EFFECT]: _waitStrategy ready
        // [VARIATIONS TO TRY]: Default to YieldingWaitStrategy if null
        _waitStrategy = waitStrategy;

        // [KYA KARTA HAI]: Fixed size entries array allocate karta hai
        // [KYU ZAROORI HAI]: Memory footprint ko fix karne ke liye
        // [KAISE CHALTA HAI]: Array instantiation with bufferSize capacity
        // [OUTPUT / SIDE EFFECT]: Heap block allocated once at startup
        // [VARIATIONS TO TRY]: GC.AllocateArray with Pinned flag
        _entries = new T[bufferSize];

        // [KYA KARTA HAI]: Array ke har ek slot me pre-allocated object instantiate karta hai
        // [KYU ZAROORI HAI]: Runtime par kabhi bhi 'new' operator na call karna pade
        // [KAISE CHALTA HAI]: Loop runs from 0 to bufferSize - 1 and calls factory()
        // [OUTPUT / SIDE EFFECT]: All slots populated with non-null live objects
        // [VARIATIONS TO TRY]: Parallel.For se large buffer initialize karein
        for (int i = 0; i < bufferSize; i++)
        {
            // [KYA KARTA HAI]: Factory delegate call karke slot populate karta hai
            // [KYU ZAROORI HAI]: Clean fresh domain event instance store karne ke liye
            // [KAISE CHALTA HAI]: _entries[i] = eventFactory()
            // [OUTPUT / SIDE EFFECT]: Slot filled
            // [VARIATIONS TO TRY]: Activator.CreateInstance<T>()
            _entries[i] = eventFactory();
        }
    }

    // [KYA KARTA HAI]: Slowest consumer sequences ko register karta hai
    // [KYU ZAROORI HAI]: Ring buffer wrap-around prevent karne ke liye
    // [KAISE CHALTA HAI]: Assigns gating sequences array
    // [OUTPUT / SIDE EFFECT]: Buffer safety gate activated
    // [VARIATIONS TO TRY]: AddGatingSequence dynamic method provide karein
    public void SetGatingSequences(params PaddedSequence[] gatingSequences)
    {
        // [KYA KARTA HAI]: Gating sequence array assign karta hai
        // [KYU ZAROORI HAI]: Wrap-around check me in sequences ka minimum calculate hota hai
        // [KAISE CHALTA HAI]: Reference assignment
        // [OUTPUT / SIDE EFFECT]: _gatingSequences updated
        // [VARIATIONS TO TRY]: Copy array for immutability
        _gatingSequences = gatingSequences;
    }

    // [KYA KARTA HAI]: Producer cursor sequence return karta hai
    // [KYU ZAROORI HAI]: Consumers ko producer sequence monitor karne ke liye cursor access deta hai
    // [KAISE CHALTA HAI]: Returns _cursor reference
    // [OUTPUT / SIDE EFFECT]: Producer cursor sequence accessible
    // [VARIATIONS TO TRY]: Expose via ISequence interface
    public PaddedSequence Cursor => _cursor;

    // [KYA KARTA HAI]: Buffer size property expose karta hai
    // [KYU ZAROORI HAI]: Monitoring aur telemetry metrics ke liye
    // [KAISE CHALTA HAI]: Returns _bufferSize value
    // [OUTPUT / SIDE EFFECT]: Readonly capacity returned
    // [VARIATIONS TO TRY]: Dynamic utilization percentage calculate karein
    public int BufferSize => _bufferSize;

    // [KYA KARTA HAI]: Sequence number ke corresponding pre-allocated slot event fetch karta hai
    // [KYU ZAROORI HAI]: In-place read aur write operations ke liye
    // [KAISE CHALTA HAI]: Bitwise masking: _entries[sequence & _indexMask]
    // [OUTPUT / SIDE EFFECT]: Returns slot reference instantly
    // [VARIATIONS TO TRY]: Ref return for struct types
    public T this[long sequence] => _entries[sequence & _indexMask];

    // [KYA KARTA HAI]: Producer ke liye agla safe slot claim karta hai (Single Producer)
    // [KYU ZAROORI HAI]: Sequence wrap around na ho aur slowest consumer ko overwrite na kare
    // [KAISE CHALTA HAI]: Slowest consumer sequence check karta hai; agar ring full hai to spin/yield karta hai
    // [OUTPUT / SIDE EFFECT]: Claimed sequence number return hota hai
    // [VARIATIONS TO TRY]: Next(int batchSize) for batch slot reservations
    public long Next()
    {
        // [KYA KARTA HAI]: Current cursor se next sequence calculate karta hai
        // [KYU ZAROORI HAI]: Target write sequence identify karne ke liye
        // [KAISE CHALTA HAI]: _cursor.Get() + 1
        // [OUTPUT / SIDE EFFECT]: nextSequence target calculated
        // [VARIATIONS TO TRY]: Multi-producer CAS loop
        long nextSequence = _cursor.Get() + 1;

        // [KYA KARTA HAI]: Ring full boundary wrap point calculate karta hai
        // [KYU ZAROORI HAI]: Check karne ke liye ki kya producer ne full circle complete kar liya
        // [KAISE CHALTA HAI]: nextSequence - _bufferSize
        // [OUTPUT / SIDE EFFECT]: Wrap point limit sequence
        // [VARIATIONS TO TRY]: Cached wrap point to reduce gating reads
        long wrapPoint = nextSequence - _bufferSize;

        // [KYA KARTA HAI]: Agar gating sequences registered hain to unka slowest progress check karta hai
        // [KYU ZAROORI HAI]: Buffer overflow / data overwrite rokne ke liye
        // [KAISE CHALTA HAI]: Gating sequences array ka minimum find karta hai
        // [OUTPUT / SIDE EFFECT]: Safe boundary evaluated
        // [VARIATIONS TO TRY]: Gating sequence cache optimization
        if (_gatingSequences.Length > 0)
        {
            // [KYA KARTA HAI]: Polling loop jab tak slowest consumer wrap point se aage na nikal jaye
            // [KYU ZAROORI HAI]: Full ring buffer me data overwrite hone se protect karta hai
            // [KAISE CHALTA HAI]: Iterates while minimum gating sequence < wrapPoint
            // [OUTPUT / SIDE EFFECT]: Producer pauses cooperatively if ring is 100% full
            // [VARIATIONS TO TRY]: Custom backoff wait strategy
            while (GetMinimumGatingSequence() < wrapPoint)
            {
                // [KYA KARTA HAI]: CPU spin wait execute karta hai
                // [KYU ZAROORI HAI]: Short pause while consumer processes batch
                // [KAISE CHALTA HAI]: Thread.SpinWait(10) execution
                // [OUTPUT / SIDE EFFECT]: Allows consumer thread to catch up
                // [VARIATIONS TO TRY]: Thread.Yield() if spin count exceeds limit
                Thread.SpinWait(10);
            }
        }

        // [KYA KARTA HAI]: Successfully claimed sequence return karta hai
        // [KYU ZAROORI HAI]: Producer is slot par event data populate kar sake
        // [KAISE CHALTA HAI]: Returns nextSequence
        // [OUTPUT / SIDE EFFECT]: Producer proceeds to mutate ringBuffer[sequence]
        // [VARIATIONS TO TRY]: Batch sequence claim
        return nextSequence;
    }

    // [KYA KARTA HAI]: Claimed sequence par data write hone ke baad use publish karta hai
    // [KYU ZAROORI HAI]: Consumers ko signal karne ke liye ki event read karne ke liye ready hai
    // [KAISE CHALTA HAI]: Volatile write ke through cursor ko update karta hai aur wait strategy signal karta hai
    // [OUTPUT / SIDE EFFECT]: Memory barrier flush aur consumers wake-up
    // [VARIATIONS TO TRY]: Batch publishing (PublishRange)
    public void Publish(long sequence)
    {
        // [KYA KARTA HAI]: Cursor sequence ko volatile write se publish karta hai
        // [KYU ZAROORI HAI]: Ensure karta hai ki all event fields memory me commit hone ke baad hi cursor badhe
        // [KAISE CHALTA HAI]: _cursor.SetVolatile(sequence)
        // [OUTPUT / SIDE EFFECT]: New sequence visible to all consumer CPU cores
        // [VARIATIONS TO TRY]: Memory fence with normal store
        _cursor.SetVolatile(sequence);

        // [KYA KARTA HAI]: Wait strategy ko notify karta hai
        // [KYU ZAROORI HAI]: Blocked consumers ko wake karne ke liye
        // [KAISE CHALTA HAI]: _waitStrategy.SignalAllWhenBlocking()
        // [OUTPUT / SIDE EFFECT]: Waiting consumers unblocked
        // [VARIATIONS TO TRY]: Signal only on empty-to-non-empty transition
        _waitStrategy.SignalAllWhenBlocking();
    }

    // [KYA KARTA HAI]: Saare gating consumer sequences me se minimum sequence find karta hai
    // [KYU ZAROORI HAI]: Ring buffer wrap point validation ke liye
    // [KAISE CHALTA HAI]: Scans _gatingSequences array and tracks minimum .Get() value
    // [OUTPUT / SIDE EFFECT]: Returns lowest consumer sequence
    // [VARIATIONS TO TRY]: Parallel SIMD vector reduction
    private long GetMinimumGatingSequence()
    {
        // [KYA KARTA HAI]: Minimum sequence tracker variable initialize karta hai
        // [KYU ZAROORI HAI]: Initial comparison value long.MaxValue
        // [KAISE CHALTA HAI]: long minimum = long.MaxValue
        // [OUTPUT / SIDE EFFECT]: Initial state
        // [VARIATIONS TO TRY]: Seed with first element
        long minimum = long.MaxValue;

        // [KYA KARTA HAI]: Sabhi gating sequences par iterate karta hai
        // [KYU ZAROORI HAI]: Har ek active consumer barrier ka progress inspect karne ke liye
        // [KAISE CHALTA HAI]: Loop through _gatingSequences
        // [OUTPUT / SIDE EFFECT]: Iteration over sequences
        // [VARIATIONS TO TRY]: Unrolled loop for fixed 3 consumers
        for (int i = 0; i < _gatingSequences.Length; i++)
        {
            // [KYA KARTA HAI]: Consumer ka current progress sequence read karta hai
            // [KYU ZAROORI HAI]: Volatile read ensures accurate cross-thread visibility
            // [KAISE CHALTA HAI]: _gatingSequences[i].Get()
            // [OUTPUT / SIDE EFFECT]: Consumer progress fetched
            // [VARIATIONS TO TRY]: Non-volatile read if single-core
            long seq = _gatingSequences[i].Get();

            // [KYA KARTA HAI]: Minimum value update karta hai agar chhota sequence mila
            // [KYU ZAROORI HAI]: Slowest consumer track karne ke liye
            // [KAISE CHALTA HAI]: Conditional check seq < minimum
            // [OUTPUT / SIDE EFFECT]: Updates minimum tracking variable
            // [VARIATIONS TO TRY]: Math.Min(minimum, seq)
            if (seq < minimum)
            {
                // [KYA KARTA HAI]: Updates minimum with lower sequence
                // [KYU ZAROORI HAI]: Sets new lowest threshold
                // [KAISE CHALTA HAI]: minimum = seq
                // [OUTPUT / SIDE EFFECT]: minimum updated
                // [VARIATIONS TO TRY]: Track index of slowest consumer
                minimum = seq;
            }
        }

        // [KYA KARTA HAI]: Slowest consumer sequence return karta hai
        // [KYU ZAROORI HAI]: Wrap-around comparison complete karne ke liye
        // [KAISE CHALTA HAI]: Returns minimum
        // [OUTPUT / SIDE EFFECT]: Gating threshold returned
        // [VARIATIONS TO TRY]: Diagnostic logging if minimum lags behind
        return minimum;
    }
}
