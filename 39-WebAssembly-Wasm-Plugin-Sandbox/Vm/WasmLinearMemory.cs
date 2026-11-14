// [KYA KARTA HAI]: Buffers and BinaryPrimitives namespaces import karta hai
// [KYU ZAROORI HAI]: Little-endian integer read/write linear memory me perform karne ke liye
// [KAISE CHALTA HAI]: System.Buffers.Binary types mapped
// [OUTPUT / SIDE EFFECT]: BinaryPrimitives available
// [VARIATIONS TO TRY]: Direct unsafe memory pointer dereferencing
using System.Buffers.Binary;

// [KYA KARTA HAI]: Wasm VM engine namespace define karta hai
// [KYU ZAROORI HAI]: Isolated linear memory structures ko modularize karne ke liye
// [KAISE CHALTA HAI]: Project ke VM engine directory ko map karta hai
// [OUTPUT / SIDE EFFECT]: WasmLinearMemory registered in KiranaHub.WasmSandbox.Vm
// [VARIATIONS TO TRY]: KiranaHub.Wasm.Memory
namespace KiranaHub.WasmSandbox.Vm;

// [KYA KARTA HAI]: Custom exception thrown when guest code violates linear memory bounds
// [KYU ZAROORI HAI]: Untrusted merchant scripts ko host memory access karne se rokne ke liye
// [KAISE CHALTA HAI]: Inherits from Exception; thrown on out-of-bounds address
// [OUTPUT / SIDE EFFECT]: Traps sandbox violation cleanly
// [VARIATIONS TO TRY]: Include faulting memory address in exception
public class WasmMemoryOutOfBoundsException : Exception
{
    // [KYA KARTA HAI]: Exception constructor with fault details
    // [KYU ZAROORI HAI]: Informative error message reporting ke liye
    // [KAISE CHALTA HAI]: Passes formatted message to base constructor
    // [OUTPUT / SIDE EFFECT]: Initialized exception
    // [VARIATIONS TO TRY]: Telemetry audit logging
    public WasmMemoryOutOfBoundsException(int address, int accessSize, int maxCapacity)
        : base($"WASM Sandboxed Memory Out-Of-Bounds: Attempted to access {accessSize} bytes at address 0x{address:X8}, but total sandbox capacity is {maxCapacity} bytes (0x{maxCapacity:X8})")
    {
    }
}

// [KYA KARTA HAI]: Sandboxed Paged Linear Memory implementation class
// [KYU ZAROORI HAI]: Untrusted merchant plugins ko dedicated 64KB paged memory space me isolate karne ke liye
// [KAISE CHALTA HAI]: WebAssembly standard 64KB pages allocate karta hai aur har read/write par strict bounds check enforce karta hai
// [OUTPUT / SIDE EFFECT]: 100% memory isolation; zero escape to host RAM
// [VARIATIONS TO TRY]: OS virtual memory protection (mprotect/VirtualProtect) guards
public class WasmLinearMemory
{
    // [KYA KARTA HAI]: Wasm standard page size constant (64 KB = 65,536 bytes)
    // [KYU ZAROORI HAI]: WebAssembly specification compliance ke liye
    // [KAISE CHALTA HAI]: Constant integer 65536
    // [OUTPUT / SIDE EFFECT]: 64KB page size
    // [VARIATIONS TO TRY]: Dynamic page size
    public const int PageSize = 65536;

    // [KYA KARTA HAI]: Underlying sandboxed raw byte array buffer
    // [KYU ZAROORI HAI]: Linear memory bytes physically store karne ke liye
    // [KAISE CHALTA HAI]: byte[] array sized to PageSize * PageCount
    // [OUTPUT / SIDE EFFECT]: Isolated memory buffer
    // [VARIATIONS TO TRY]: Native unmanaged memory with guard pages
    private byte[] _buffer;

    // [KYA KARTA HAI]: Currently allocated page count
    // [KYU ZAROORI HAI]: Memory limits and growth bounds track karne ke liye
    // [KAISE CHALTA HAI]: Integer page counter
    // [OUTPUT / SIDE EFFECT]: Page count state
    // [VARIATIONS TO TRY]: Max page limit cap
    private int _pageCount;

    // [KYA KARTA HAI]: Maximum permissible page count cap (e.g. 16 pages = 1 MB)
    // [KYU ZAROORI HAI]: Malicious merchant plugin ko infinite memory allocate karke host OOM crash karne se rokne ke liye
    // [KAISE CHALTA HAI]: Hard ceiling constant integer
    // [OUTPUT / SIDE EFFECT]: Enforces maximum memory boundary
    // [VARIATIONS TO TRY]: Configurable per tenant tier
    public const int MaxPermissiblePages = 16;

    // [KYA KARTA HAI]: WasmLinearMemory constructor with initial pages
    // [KYU ZAROORI HAI]: Initial isolated memory buffer allocate karne ke liye
    // [KAISE CHALTA HAI]: Allocates initialPages * PageSize bytes
    // [OUTPUT / SIDE EFFECT]: Sandboxed memory initialized
    // [VARIATIONS TO TRY]: Default 1 page (64KB)
    public WasmLinearMemory(int initialPages = 1)
    {
        // [KYA KARTA HAI]: Initial page count validate karta hai
        // [KYU ZAROORI HAI]: Non-positive ya excessive pages prevent karne ke liye
        // [KAISE CHALTA HAI]: Checks initialPages between 1 and MaxPermissiblePages
        // [OUTPUT / SIDE EFFECT]: Throws ArgumentOutOfRangeException on violation
        // [VARIATIONS TO TRY]: Auto clamp to range
        if (initialPages < 1 || initialPages > MaxPermissiblePages)
        {
            throw new ArgumentOutOfRangeException(nameof(initialPages), $"Initial pages must be between 1 and {MaxPermissiblePages}");
        }

        // [KYA KARTA HAI]: Page count field assign karta hai
        // [KYU ZAROORI HAI]: Current size track karne ke liye
        // [KAISE CHALTA HAI]: _pageCount = initialPages
        // [OUTPUT / SIDE EFFECT]: State stored
        // [VARIATIONS TO TRY]: Readonly property
        _pageCount = initialPages;

        // [KYA KARTA HAI]: Exact memory buffer allocate karta hai
        // [KYU ZAROORI HAI]: Isolated memory space provide karne ke liye
        // [KAISE CHALTA HAI]: new byte[initialPages * PageSize]
        // [OUTPUT / SIDE EFFECT]: Heap byte array allocated
        // [VARIATIONS TO TRY]: GC.AllocateArray(pinned: true)
        _buffer = new byte[initialPages * PageSize];
    }

    // [KYA KARTA HAI]: Total allocated bytes capacity property
    // [KYU ZAROORI HAI]: Memory bounds checks ke liye
    // [KAISE CHALTA HAI]: Returns _buffer.Length
    // [OUTPUT / SIDE EFFECT]: Capacity in bytes
    // [VARIATIONS TO TRY]: PageCount * PageSize
    public int CapacityBytes => _buffer.Length;

    // [KYA KARTA HAI]: Current page count property
    // [KYU ZAROORI HAI]: Wasm memory.size opcode implement karne ke liye
    // [KAISE CHALTA HAI]: Returns _pageCount
    // [OUTPUT / SIDE EFFECT]: Current pages
    // [VARIATIONS TO TRY]: Page count integer
    public int PageCount => _pageCount;

    // [KYA KARTA HAI]: Memory bounds validation internal helper
    // [KYU ZAROORI HAI]: Har memory access se pehle boundary check enforce karne ke liye
    // [KAISE CHALTA HAI]: Checks if address < 0 or address + accessSize > _buffer.Length
    // [OUTPUT / SIDE EFFECT]: Throws WasmMemoryOutOfBoundsException on invalid address
    // [VARIATIONS TO TRY]: Unchecked mode for trusted modules
    private void ValidateBounds(int address, int accessSize)
    {
        // [KYA KARTA HAI]: Boundary check evaluation
        // [KYU ZAROORI HAI]: Memory safety guarantee karne ke liye
        // [KAISE CHALTA HAI]: Boolean bounds condition
        // [OUTPUT / SIDE EFFECT]: Throws trap if violation occurs
        // [VARIATIONS TO TRY]: Hardware MMU signal trap
        if (address < 0 || accessSize < 0 || (long)address + accessSize > _buffer.Length)
        {
            throw new WasmMemoryOutOfBoundsException(address, accessSize, _buffer.Length);
        }
    }

    // [KYA KARTA HAI]: Linear memory se 32-bit little-endian integer read karta hai
    // [KYU ZAROORI HAI]: I32Load opcode execute karne ke liye
    // [KAISE CHALTA HAI]: Bounds validate karta hai aur BinaryPrimitives.ReadInt32LittleEndian call karta hai
    // [OUTPUT / SIDE EFFECT]: Decoded int32 value returned
    // [VARIATIONS TO TRY]: ReadInt64 for 64-bit loads
    public int ReadInt32(int address)
    {
        // [KYA KARTA HAI]: 4 bytes boundary validation
        // [KYU ZAROORI HAI]: Buffer overflow prevent karne ke liye
        // [KAISE CHALTA HAI]: ValidateBounds(address, 4)
        // [OUTPUT / SIDE EFFECT]: Bounds confirmed safe
        // [VARIATIONS TO TRY]: Inline boundary check
        ValidateBounds(address, 4);

        // [KYA KARTA HAI]: 4 bytes ko little-endian int32 me decode karta hai
        // [KYU ZAROORI HAI]: Wasm standard little-endian format follow karne ke liye
        // [KAISE CHALTA HAI]: BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(address, 4))
        // [OUTPUT / SIDE EFFECT]: Value returned
        // [VARIATIONS TO TRY]: Unsafe.ReadUnaligned
        return BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(address, 4));
    }

    // [KYA KARTA HAI]: Linear memory me 32-bit little-endian integer write karta hai
    // [KYU ZAROORI HAI]: I32Store opcode execute karne ke liye
    // [KAISE CHALTA HAI]: Bounds validate karta hai aur BinaryPrimitives.WriteInt32LittleEndian call karta hai
    // [OUTPUT / SIDE EFFECT]: Memory bytes updated at address
    // [VARIATIONS TO TRY]: WriteInt64
    public void WriteInt32(int address, int value)
    {
        // [KYA KARTA HAI]: 4 bytes boundary validation
        // [KYU ZAROORI HAI]: Buffer overflow prevent karne ke liye
        // [KAISE CHALTA HAI]: ValidateBounds(address, 4)
        // [OUTPUT / SIDE EFFECT]: Bounds confirmed safe
        // [VARIATIONS TO TRY]: Inline boundary check
        ValidateBounds(address, 4);

        // [KYA KARTA HAI]: 4 bytes little-endian write karta hai
        // [KYU ZAROORI HAI]: Memory me value store karne ke liye
        // [KAISE CHALTA HAI]: BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(address, 4), value)
        // [OUTPUT / SIDE EFFECT]: Memory mutated
        // [VARIATIONS TO TRY]: Unsafe.WriteUnaligned
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(address, 4), value);
    }

    // [KYA KARTA HAI]: Raw byte slice read karta hai from linear memory
    // [KYU ZAROORI HAI]: String decoding ya host data export ke liye
    // [KAISE CHALTA HAI]: Validates bounds and returns ReadOnlySpan<byte>
    // [OUTPUT / SIDE EFFECT]: Direct span slice over sandboxed memory
    // [VARIATIONS TO TRY]: Copy to new byte array
    public ReadOnlySpan<byte> ReadSpan(int address, int length)
    {
        // [KYA KARTA HAI]: Length bounds check
        // [KYU ZAROORI HAI]: Out-of-bounds read rokne ke liye
        // [KAISE CHALTA HAI]: ValidateBounds(address, length)
        // [OUTPUT / SIDE EFFECT]: Confirmed safe
        // [VARIATIONS TO TRY]: Empty span on zero length
        ValidateBounds(address, length);

        // [KYA KARTA HAI]: Span slice return karta hai
        // [KYU ZAROORI HAI]: Zero memory allocation inspection ke liye
        // [KAISE CHALTA HAI]: _buffer.AsSpan(address, length)
        // [OUTPUT / SIDE EFFECT]: ReadOnlySpan returned
        // [VARIATIONS TO TRY]: Memory<byte> slice
        return _buffer.AsSpan(address, length);
    }

    // [KYA KARTA HAI]: Host data bytes ko linear memory me write karta hai
    // [KYU ZAROORI HAI]: Cart data, pricing info host se guest memory me pass karne ke liye
    // [KAISE CHALTA HAI]: Validates bounds and copies bytes into _buffer
    // [OUTPUT / SIDE EFFECT]: Guest linear memory populated with host data
    // [VARIATIONS TO TRY]: Write UTF-8 string directly
    public void WriteBytes(int address, ReadOnlySpan<byte> data)
    {
        // [KYA KARTA HAI]: Data length boundary validation
        // [KYU ZAROORI HAI]: Memory overflow prevent karne ke liye
        // [KAISE CHALTA HAI]: ValidateBounds(address, data.Length)
        // [OUTPUT / SIDE EFFECT]: Confirmed safe
        // [VARIATIONS TO TRY]: Partial write on overflow
        ValidateBounds(address, data.Length);

        // [KYA KARTA HAI]: Bytes copy karta hai
        // [KYU ZAROORI HAI]: Linear memory update ke liye
        // [KAISE CHALTA HAI]: data.CopyTo(_buffer.AsSpan(address, data.Length))
        // [OUTPUT / SIDE EFFECT]: Memory written
        // [VARIATIONS TO TRY]: Buffer.BlockCopy
        data.CopyTo(_buffer.AsSpan(address, data.Length));
    }

    // [KYA KARTA HAI]: Linear memory ko dynamic pages se grow karta hai (Wasm memory.grow)
    // [KYU ZAROORI HAI]: Guest modules ko additional memory request karne allow karne ke liye
    // [KAISE CHALTA HAI]: Reallocates larger buffer up to MaxPermissiblePages
    // [OUTPUT / SIDE EFFECT]: Returns previous page count on success, or -1 on failure
    // [VARIATIONS TO TRY]: Exponential growth strategy
    public int Grow(int additionalPages)
    {
        // [KYA KARTA HAI]: Zero pages requested check
        // [KYU ZAROORI HAI]: Fast path if no growth required
        // [KAISE CHALTA HAI]: additionalPages == 0 check
        // [OUTPUT / SIDE EFFECT]: Returns current page count
        // [VARIATIONS TO TRY]: Negative pages validation
        if (additionalPages == 0)
        {
            return _pageCount;
        }

        // [KYA KARTA HAI]: Negative or excessive growth validation
        // [KYU ZAROORI HAI]: Memory abuse prevent karne ke liye
        // [KAISE CHALTA HAI]: Checks total pages <= MaxPermissiblePages
        // [OUTPUT / SIDE EFFECT]: Returns -1 to guest indicating memory allocation failure
        // [VARIATIONS TO TRY]: Configurable per-tenant quota
        if (additionalPages < 0 || _pageCount + additionalPages > MaxPermissiblePages)
        {
            return -1;
        }

        // [KYA KARTA HAI]: Previous page count store karta hai
        // [KYU ZAROORI HAI]: Wasm specification mandates returning old page count
        // [KAISE CHALTA HAI]: int oldPages = _pageCount
        // [OUTPUT / SIDE EFFECT]: Saved old page count
        // [VARIATIONS TO TRY]: Local register
        int oldPages = _pageCount;

        // [KYA KARTA HAI]: New page count calculate karta hai
        // [KYU ZAROORI HAI]: New buffer capacity determine karne ke liye
        // [KAISE CHALTA HAI]: _pageCount += additionalPages
        // [OUTPUT / SIDE EFFECT]: _pageCount updated
        // [VARIATIONS TO TRY]: Check overflow
        _pageCount += additionalPages;

        // [KYA KARTA HAI]: Expanded byte buffer allocate karta hai
        // [KYU ZAROORI HAI]: Host memory allocate karne ke liye
        // [KAISE CHALTA HAI]: new byte[_pageCount * PageSize]
        // [OUTPUT / SIDE EFFECT]: Larger buffer allocated
        // [VARIATIONS TO TRY]: Array.Resize
        byte[] newBuffer = new byte[_pageCount * PageSize];

        // [KYA KARTA HAI]: Existing data ko new buffer me copy karta hai
        // [KYU ZAROORI HAI]: Data preserve karne ke liye across memory growth
        // [KAISE CHALTA HAI]: Array.Copy(_buffer, newBuffer, _buffer.Length)
        // [OUTPUT / SIDE EFFECT]: Existing state preserved
        // [VARIATIONS TO TRY]: Memory.Span copy
        Array.Copy(_buffer, newBuffer, _buffer.Length);

        // [KYA KARTA HAI]: Internal buffer reference replace karta hai
        // [KYU ZAROORI HAI]: New buffer active karne ke liye
        // [KAISE CHALTA HAI]: _buffer = newBuffer
        // [OUTPUT / SIDE EFFECT]: Memory capacity expanded
        // [VARIATIONS TO TRY]: Release old buffer to pool
        _buffer = newBuffer;

        // [KYA KARTA HAI]: Returns old page count as per WebAssembly standard
        // [KYU ZAROORI HAI]: Guest runtime compliance ke liye
        // [KAISE CHALTA HAI]: Returns oldPages
        // [OUTPUT / SIDE EFFECT]: Successful growth acknowledged
        // [VARIATIONS TO TRY]: Log memory telemetry event
        return oldPages;
    }
}
