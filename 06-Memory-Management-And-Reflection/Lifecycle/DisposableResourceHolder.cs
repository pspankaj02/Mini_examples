namespace KiranaHub.MemoryAndReflection.Lifecycle;

// =========================================================================================
// [CLASS PURPOSE]:
// Full IDisposable and IAsyncDisposable pattern with Finalizer and GC generation inspection.
// =========================================================================================
public class KiranaLogFileHandle : IDisposable, IAsyncDisposable
{
    // [DISPOSAL STATE TRACKING FIELD]:
    private bool _disposed;

    // [MANAGED RESOURCE FIELD]:
    private readonly MemoryStream _unmanagedBufferSimulation;

    // [CONSTRUCTOR]:
    public KiranaLogFileHandle()
    {
        // Allocates stream resource
        _unmanagedBufferSimulation = new MemoryStream();
    }

    // [OPERATIONAL METHOD]:
    public void WriteLog(byte[] data)
    {
        // [DISPOSAL GUARD]: Throws ObjectDisposedException if already cleaned up
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Writes data to stream
        _unmanagedBufferSimulation.Write(data, 0, data.Length);
    }

    // [SYNCHRONOUS DISPOSE METHOD]:
    public void Dispose()
    {
        // [STEP 1: DISPOSE MANAGED + UNMANAGED RESOURCES]:
        Dispose(disposing: true);

        // [STEP 2: SUPPRESS FINALIZER]:
        // [KYU ZAROORI HAI]: Tells Garbage Collector that resource cleanup is complete.
        // Bypasses the finalizer queue, preventing unnecessary promotion to GC Generation 2!
        GC.SuppressFinalize(this);
    }

    // [VIRTUAL DISPOSE PATTERN]:
    protected virtual void Dispose(bool disposing)
    {
        // [REDUNDANCY CHECK]: Only execute if not previously disposed
        if (!_disposed)
        {
            if (disposing)
            {
                // [CLEAN MANAGED OBJECTS]:
                _unmanagedBufferSimulation.Dispose();
            }

            // [MARK DISPOSED STATE]:
            _disposed = true;
        }
    }

    // [ASYNCHRONOUS DISPOSE METHOD]:
    public async ValueTask DisposeAsync()
    {
        // [STEP 1: ASYNC CLEANUP]:
        await DisposeAsyncCore().ConfigureAwait(false);

        // [STEP 2: CLEANUP UNMANAGED ONLY]:
        Dispose(disposing: false);

        // [STEP 3: SUPPRESS FINALIZER]:
        GC.SuppressFinalize(this);
    }

    // [ASYNC CORE CLEANUP]:
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Asynchronously flushes and releases stream
        await _unmanagedBufferSimulation.DisposeAsync().ConfigureAwait(false);
    }

    // [FINALIZER / DESTRUCTOR]:
    // Safety net in case caller forgot to call Dispose()
    ~KiranaLogFileHandle()
    {
        // Calls dispose with disposing: false (managed objects might already be collected)
        Dispose(disposing: false);
    }
}
