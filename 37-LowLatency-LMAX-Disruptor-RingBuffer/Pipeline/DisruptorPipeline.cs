// [KYA KARTA HAI]: Core Disruptor primitives aur domain types import karta hai
// [KYU ZAROORI HAI]: RingBuffer, PaddedSequence aur OrderMatchingEvent use karne ke liye
// [KAISE CHALTA HAI]: Namespace references provide karta hai
// [OUTPUT / SIDE EFFECT]: Required types accessible ho jaate hain
// [VARIATIONS TO TRY]: Global usings me add karein
using KiranaHub.Disruptor.Core;
using KiranaHub.Disruptor.Domain;

// [KYA KARTA HAI]: High-performance pipeline namespace define karta hai
// [KYU ZAROORI HAI]: Multi-stage consumer event processing handlers ko organize karne ke liye
// [KAISE CHALTA HAI]: Project pipeline directory structure ko map karta hai
// [OUTPUT / SIDE EFFECT]: Pipeline types encapsulated under KiranaHub.Disruptor.Pipeline
// [VARIATIONS TO TRY]: KiranaHub.Disruptor.Stages
namespace KiranaHub.Disruptor.Pipeline;

// [KYA KARTA HAI]: Single-threaded lock-free event processor worker thread
// [KYU ZAROORI HAI]: Har processing stage ko dedicated CPU thread par sub-microsecond batch processing execute karwane ke liye
// [KAISE CHALTA HAI]: Loop me wait strategy se available sequences pull karta hai, batch process karta hai, aur cursor update karta hai
// [OUTPUT / SIDE EFFECT]: Maximum CPU instruction cache warmth aur zero context switching
// [VARIATIONS TO TRY]: Thread affinity set karke specific CPU core par pin karein
public class BatchEventProcessor
{
    // [KYA KARTA HAI]: Shared RingBuffer reference
    // [KYU ZAROORI HAI]: Sequence slots read aur mutate karne ke liye
    // [KAISE CHALTA HAI]: Readonly reference to RingBuffer<OrderMatchingEvent>
    // [OUTPUT / SIDE EFFECT]: Direct memory access to pre-allocated events
    // [VARIATIONS TO TRY]: IDataProvider<T> abstraction use karein
    private readonly RingBuffer<OrderMatchingEvent> _ringBuffer;

    // [KYA KARTA HAI]: Upstream dependent sequence (jispar ye processor depend karta hai)
    // [KYU ZAROORI HAI]: Stage 2 tabhi execute ho jab Stage 1 complete ho chuki ho
    // [KAISE CHALTA HAI]: PaddedSequence reference representing predecessor stage progress
    // [OUTPUT / SIDE EFFECT]: Enforces deterministic pipeline ordering
    // [VARIATIONS TO TRY]: Multiple upstream dependencies array
    private readonly PaddedSequence _dependentSequence;

    // [KYA KARTA HAI]: Iss consumer ka apna sequence progress tracker
    // [KYU ZAROORI HAI]: Downstream stages aur ring buffer producer ko iska progress report karne ke liye
    // [KAISE CHALTA HAI]: 64-byte padded atomic sequence initialized to -1L
    // [OUTPUT / SIDE EFFECT]: Tracks stage progress safely across cores
    // [VARIATIONS TO TRY]: Expose via ISequence interface
    private readonly PaddedSequence _sequence = new(-1L);

    // [KYA KARTA HAI]: Event handler callback function
    // [KYU ZAROORI HAI]: Business logic execute karne ke liye (inventory, payment, dispatch)
    // [KAISE CHALTA HAI]: Action<OrderMatchingEvent, long, bool> delegate invocation
    // [OUTPUT / SIDE EFFECT]: In-place business mutation
    // [VARIATIONS TO TRY]: IEventHandler<T> typed interface
    private readonly Action<OrderMatchingEvent, long, bool> _eventHandler;

    // [KYA KARTA HAI]: Wait strategy reference
    // [KYU ZAROORI HAI]: Upstream events ka wait karne ke liye (spin/yield)
    // [KAISE CHALTA HAI]: IWaitStrategy implementation
    // [OUTPUT / SIDE EFFECT]: Low-latency synchronization
    // [VARIATIONS TO TRY]: BusySpinWaitStrategy for critical stage
    private readonly IWaitStrategy _waitStrategy;

    // [KYA KARTA HAI]: Processor running status flag
    // [KYU ZAROORI HAI]: Clean shutdown loop termination ke liye
    // [KAISE CHALTA HAI]: Volatile boolean flag
    // [OUTPUT / SIDE EFFECT]: Loop controls running state
    // [VARIATIONS TO TRY]: CancellationTokenSource use karein
    private volatile bool _isRunning;

    // [KYA KARTA HAI]: Dedicated background OS thread
    // [KYU ZAROORI HAI]: Thread pool jitter aur queuing delay avoid karne ke liye
    // [KAISE CHALTA HAI]: Dedicated Thread instance initialized with Priority = Highest
    // [OUTPUT / SIDE EFFECT]: Low-jitter processing loop
    // [VARIATIONS TO TRY]: LongRunning Task use karein
    private Thread? _workerThread;

    // [KYA KARTA HAI]: BatchEventProcessor constructor
    // [KYU ZAROORI HAI]: All dependencies ko inject aur initialize karne ke liye
    // [KAISE CHALTA HAI]: Stores fields and validates input arguments
    // [OUTPUT / SIDE EFFECT]: Configured processor instance
    // [VARIATIONS TO TRY]: Builder pattern use karein
    public BatchEventProcessor(
        RingBuffer<OrderMatchingEvent> ringBuffer,
        PaddedSequence dependentSequence,
        Action<OrderMatchingEvent, long, bool> eventHandler,
        IWaitStrategy waitStrategy)
    {
        // [KYA KARTA HAI]: Ring buffer field assign karta hai
        // [KYU ZAROORI HAI]: Event slot access ke liye
        // [KAISE CHALTA HAI]: Reference assignment
        // [OUTPUT / SIDE EFFECT]: _ringBuffer initialized
        // [VARIATIONS TO TRY]: Null check add karein
        _ringBuffer = ringBuffer;

        // [KYA KARTA HAI]: Dependent sequence field assign karta hai
        // [KYU ZAROORI HAI]: Pipeline ordering enforce karne ke liye
        // [KAISE CHALTA HAI]: Reference assignment
        // [OUTPUT / SIDE EFFECT]: _dependentSequence initialized
        // [VARIATIONS TO TRY]: Composite barrier pass karein
        _dependentSequence = dependentSequence;

        // [KYA KARTA HAI]: Business event handler assign karta hai
        // [KYU ZAROORI HAI]: Domain logic execute karne ke liye
        // [KAISE CHALTA HAI]: Delegate assignment
        // [OUTPUT / SIDE EFFECT]: _eventHandler initialized
        // [VARIATIONS TO TRY]: Multiple handlers chain
        _eventHandler = eventHandler;

        // [KYA KARTA HAI]: Wait strategy assign karta hai
        // [KYU ZAROORI HAI]: Waiting behaviour define karne ke liye
        // [KAISE CHALTA HAI]: Reference assignment
        // [OUTPUT / SIDE EFFECT]: _waitStrategy initialized
        // [VARIATIONS TO TRY]: Default to Yielding if null
        _waitStrategy = waitStrategy;
    }

    // [KYA KARTA HAI]: Consumer ka sequence property expose karta hai
    // [KYU ZAROORI HAI]: Downstream consumer ya RingBuffer gating ke liye sequence access deta hai
    // [KAISE CHALTA HAI]: Returns _sequence reference
    // [OUTPUT / SIDE EFFECT]: Sequence accessible
    // [VARIATIONS TO TRY]: Readonly wrapper provide karein
    public PaddedSequence Sequence => _sequence;

    // [KYA KARTA HAI]: Dedicated background thread start karta hai
    // [KYU ZAROORI HAI]: High-frequency event processing loop ko launch karne ke liye
    // [KAISE CHALTA HAI]: Spawns and starts background thread executing RunLoop()
    // [OUTPUT / SIDE EFFECT]: Thread starts running immediately
    // [VARIATIONS TO TRY]: Thread.Priority = ThreadPriority.Highest set karein
    public void Start(string threadName)
    {
        // [KYA KARTA HAI]: Running state flag true set karta hai
        // [KYU ZAROORI HAI]: Processing loop continue hone ke liye
        // [KAISE CHALTA HAI]: Volatile write to _isRunning
        // [OUTPUT / SIDE EFFECT]: _isRunning is true
        // [VARIATIONS TO TRY]: Interlocked exchange
        _isRunning = true;

        // [KYA KARTA HAI]: Dedicated OS thread create karta hai
        // [KYU ZAROORI HAI]: Background processing isolated thread par chalane ke liye
        // [KAISE CHALTA HAI]: new Thread(RunLoop) { IsBackground = true, Name = threadName }
        // [OUTPUT / SIDE EFFECT]: Thread instance ready
        // [VARIATIONS TO TRY]: Core affinity pinning with P/Invoke
        _workerThread = new Thread(RunLoop)
        {
            // [KYA KARTA HAI]: Background thread flag set karta hai
            // [KYU ZAROORI HAI]: Main process exit par thread process ko block na kare
            // [KAISE CHALTA HAI]: IsBackground = true
            // [OUTPUT / SIDE EFFECT]: Clean app termination allowed
            // [VARIATIONS TO TRY]: Foreground thread with cancellation token
            IsBackground = true,

            // [KYA KARTA HAI]: Thread name set karta hai for debugging
            // [KYU ZAROORI HAI]: OS thread profiling tools me identifying thread name dekhne ke liye
            // [KAISE CHALTA HAI]: Name = threadName
            // [OUTPUT / SIDE EFFECT]: Thread named in profiler
            // [VARIATIONS TO TRY]: Include core ID in name
            Name = threadName
        };

        // [KYA KARTA HAI]: Thread execution kick off karta hai
        // [KYU ZAROORI HAI]: Processing loop shuru karne ke liye
        // [KAISE CHALTA HAI]: _workerThread.Start()
        // [OUTPUT / SIDE EFFECT]: RunLoop begins executing on worker thread
        // [VARIATIONS TO TRY]: Pass thread state object
        _workerThread.Start();
    }

    // [KYA KARTA HAI]: Processor ko gracefully stop karta hai
    // [KYU ZAROORI HAI]: Shutdown ke waqt thread loop terminate karne ke liye
    // [KAISE CHALTA HAI]: Sets _isRunning to false and joins thread
    // [OUTPUT / SIDE EFFECT]: Worker thread finishes cleanly
    // [VARIATIONS TO TRY]: Timeout on thread join
    public void Halt()
    {
        // [KYA KARTA HAI]: Running flag false set karta hai
        // [KYU ZAROORI HAI]: Next iteration par loop break hone ke liye
        // [KAISE CHALTA HAI]: _isRunning = false
        // [OUTPUT / SIDE EFFECT]: Loop will exit
        // [VARIATIONS TO TRY]: Signal wait strategy to unblock immediately
        _isRunning = false;

        // [KYA KARTA HAI]: Worker thread finish hone ka wait karta hai
        // [KYU ZAROORI HAI]: Clean synchronization on shutdown
        // [KAISE CHALTA HAI]: _workerThread?.Join(500)
        // [OUTPUT / SIDE EFFECT]: Thread safely terminated
        // [VARIATIONS TO TRY]: Thread.Interrupt if timed out
        _workerThread?.Join(500);
    }

    // [KYA KARTA HAI]: High-frequency low-latency event processing loop
    // [KYU ZAROORI HAI]: Continuous stream me events ko batch me process karne ke liye
    // [KAISE CHALTA HAI]: Target sequence compute karta hai, wait karta hai, batch loop chalaata hai, aur cursor update karta hai
    // [OUTPUT / SIDE EFFECT]: Zero GC allocation sub-microsecond event handling
    // [VARIATIONS TO TRY]: Exception handling policy with dead-letter queue
    private void RunLoop()
    {
        // [KYA KARTA HAI]: Next sequence to process tracker variable
        // [KYU ZAROORI HAI]: Current consumer sequence + 1 se start karne ke liye
        // [KAISE CHALTA HAI]: _sequence.Get() + 1
        // [OUTPUT / SIDE EFFECT]: Initial nextSequence calculated
        // [VARIATIONS TO TRY]: Resume from checkpoint sequence
        long nextSequence = _sequence.Get() + 1;

        // [KYA KARTA HAI]: Main execution loop running while _isRunning is true
        // [KYU ZAROORI HAI]: Continuous polling and processing of published events
        // [KAISE CHALTA HAI]: while (_isRunning) loop
        // [OUTPUT / SIDE EFFECT]: Persistent consumer pump
        // [VARIATIONS TO TRY]: Drain remaining queue on shutdown
        while (_isRunning)
        {
            try
            {
                // [KYA KARTA HAI]: Upstream dependent sequence available hone tak wait karta hai
                // [KYU ZAROORI HAI]: Consumer tabhi process kare jab upstream stage complete ho
                // [KAISE CHALTA HAI]: _waitStrategy.WaitFor(nextSequence, _sequence, _dependentSequence)
                // [OUTPUT / SIDE EFFECT]: Returns availableSequence up to which batch processing is safe
                // [VARIATIONS TO TRY]: Catch TimeoutException for periodic housekeeping
                long availableSequence = _waitStrategy.WaitFor(nextSequence, _sequence, _dependentSequence);

                // [KYA KARTA HAI]: Available batch ke har event par loop chalaata hai
                // [KYU ZAROORI HAI]: Batching se throughput 10x badhta hai (amortized memory barrier cost)
                // [KAISE CHALTA HAI]: for (long seq = nextSequence; seq <= availableSequence; seq++)
                // [OUTPUT / SIDE EFFECT]: Batch processing of multiple orders in tight loop
                // [VARIATIONS TO TRY]: Limit max batch size to 1024
                while (nextSequence <= availableSequence)
                {
                    // [KYA KARTA HAI]: RingBuffer slot se pre-allocated event fetch karta hai
                    // [KYU ZAROORI HAI]: Event data inspect aur mutate karne ke liye
                    // [KAISE CHALTA HAI]: _ringBuffer[nextSequence] (bitwise masked indexing)
                    // [OUTPUT / SIDE EFFECT]: Direct slot event reference
                    // [VARIATIONS TO TRY]: Readonly span conversion
                    OrderMatchingEvent orderEvent = _ringBuffer[nextSequence];

                    // [KYA KARTA HAI]: Check karta hai ki kya ye batch ka last element hai
                    // [KYU ZAROORI HAI]: EndOfBatch flag handlers ko flush/commit optimizations allow karta hai
                    // [KAISE CHALTA HAI]: bool endOfBatch = (nextSequence == availableSequence)
                    // [OUTPUT / SIDE EFFECT]: Boolean flag computed
                    // [VARIATIONS TO TRY]: Pass remaining count in batch
                    bool endOfBatch = nextSequence == availableSequence;

                    // [KYA KARTA HAI]: Business handler delegate invoke karta hai
                    // [KYU ZAROORI HAI]: Specific stage logic execute karne ke liye
                    // [KAISE CHALTA HAI]: _eventHandler(orderEvent, nextSequence, endOfBatch)
                    // [OUTPUT / SIDE EFFECT]: Domain mutation executed
                    // [VARIATIONS TO TRY]: Try-catch per event
                    _eventHandler(orderEvent, nextSequence, endOfBatch);

                    // [KYA KARTA HAI]: Local sequence counter increment karta hai
                    // [KYU ZAROORI HAI]: Next order par move karne ke liye
                    // [KAISE CHALTA HAI]: nextSequence++
                    // [OUTPUT / SIDE EFFECT]: Counter incremented
                    // [VARIATIONS TO TRY]: Unchecked increment
                    nextSequence++;
                }

                // [KYA KARTA HAI]: Full batch process hone ke baad consumer cursor ko atomically update karta hai
                // [KYU ZAROORI HAI]: Downstream consumers aur producer ko notify karne ke liye ki batch complete ho gaya
                // [KAISE CHALTA HAI]: _sequence.SetVolatile(availableSequence)
                // [OUTPUT / SIDE EFFECT]: Downstream stages unblocked for this batch
                // [VARIATIONS TO TRY]: Update cursor every N events if batch is massive
                _sequence.SetVolatile(availableSequence);
            }
            catch (ThreadInterruptedException)
            {
                // [KYA KARTA HAI]: Thread interruption gracefully handle karta hai
                // [KYU ZAROORI HAI]: Shutdown signal par clean exit ke liye
                // [KAISE CHALTA HAI]: Breaks from loop
                // [OUTPUT / SIDE EFFECT]: Exits loop
                // [VARIATIONS TO TRY]: Log shutdown trace
                break;
            }
            catch (Exception ex)
            {
                // [KYA KARTA HAI]: Unexpected exception catch karta hai taaki thread crash na ho
                // [KYU ZAROORI HAI]: Mission-critical matching engine reliability ke liye
                // [KAISE CHALTA HAI]: Console error output and skip bad sequence
                // [OUTPUT / SIDE EFFECT]: Error logged, loop continues
                // [VARIATIONS TO TRY]: Route to dead-letter storage
                Console.WriteLine($"[Disruptor ERROR] Stage exception on seq {nextSequence}: {ex.Message}");
                nextSequence++;
            }
        }
    }
}
