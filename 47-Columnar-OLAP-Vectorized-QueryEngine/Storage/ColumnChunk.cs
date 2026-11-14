using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KiranaHub.ColumnarOlap.Storage;

/// <summary>
/// Contiguous in-memory Columnar Chunk.
/// Stores a single typed column's values contiguously in memory (cache-friendly array).
/// Eliminates row overhead: scanning this column reads ONLY this column's bytes from RAM.
/// </summary>
public sealed class ColumnChunk<T> where T : struct
{
    // [KYA KARTA HAI]: Column ka human-readable name (e.g. "OrderAmount", "Pincode") store karta hai.
    // [KYU ZAROORI HAI]: Schema inspection aur query planner column binding ke liye.
    // [KAISE CHALTA HAI]: Constructor me assign hota hai.
    // [OUTPUT / SIDE EFFECT]: Immutable column name string.
    // [VARIATIONS TO TRY]: Case-insensitive column name matching.
    public string ColumnName { get; }

    // [KYA KARTA HAI]: Underlying contiguous heap array jisme actual column values store hoti hain.
    // [KYU ZAROORI HAI]: CPU hardware prefetcher contiguous memory blocks ko L1/L2 cache me 100% prefetch karta hai.
    // [KAISE CHALTA HAI]: Standard 1D typed array allocated on GC heap.
    // [OUTPUT / SIDE EFFECT]: In-memory contiguous buffer.
    // [VARIATIONS TO TRY]: NativeMemory.Alloc for off-heap zero-GC storage.
    private T[] _data;

    // [KYA KARTA HAI]: Chunk me successfully appended elements ki current count track karta hai.
    // [KYU ZAROORI HAI]: Array capacity vs actual populated length distinguish karna.
    // [KAISE CHALTA HAI]: Har Append par increment hota hai.
    // [OUTPUT / SIDE EFFECT]: Active elements count integer.
    // [VARIATIONS TO TRY]: Atomic count for multi-threaded chunk appends.
    private int _count;

    // [KYA KARTA HAI]: ColumnChunk constructor with initial capacity.
    // [KYU ZAROORI HAI]: Pre-allocation se resize overhead aur GC fragmentation minimize hoti hai.
    // [KAISE CHALTA HAI]: Array of size initialCapacity allocate karta hai.
    // [OUTPUT / SIDE EFFECT]: Initialized ColumnChunk instance.
    // [VARIATIONS TO TRY]: Default capacity of 65,536 elements.
    public ColumnChunk(string columnName, int initialCapacity = 1024)
    {
        ColumnName = columnName;
        _data = new T[Math.Max(16, initialCapacity)];
        _count = 0;
    }

    // [KYA KARTA HAI]: Ek single element column chunk ke end me append karta hai.
    // [KYU ZAROORI HAI]: Data ingestion aur ETL pipeline me record-by-record ingestion support karna.
    // [KAISE CHALTA HAI]: Capacity check karta hai; full hone par array double karta hai aur value assign karta hai.
    // [OUTPUT / SIDE EFFECT]: _count increments by 1.
    // [VARIATIONS TO TRY]: Bulk append via ReadOnlySpan<T>.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T value)
    {
        if (_count == _data.Length)
        {
            EnsureCapacity(_data.Length * 2);
        }

        _data[_count++] = value;
    }

    // [KYA KARTA HAI]: Bulk elements ko ek sath append karta hai via ReadOnlySpan<T>.
    // [KYU ZAROORI HAI]: Batch loading performance maximize karna without element-by-element loop overhead.
    // [KAISE CHALTA HAI]: Buffer.BlockCopy ya Span.CopyTo se bulk memory copy karta hai.
    // [OUTPUT / SIDE EFFECT]: Multiple elements appended in 1 operation.
    // [VARIATIONS TO TRY]: Unsafe pointer memory copy.
    public void AppendRange(ReadOnlySpan<T> values)
    {
        int required = _count + values.Length;
        if (required > _data.Length)
        {
            EnsureCapacity(Math.Max(_data.Length * 2, required));
        }

        values.CopyTo(_data.AsSpan(_count));
        _count += values.Length;
    }

    // [KYA KARTA HAI]: Internal array capacity ensure karta hai.
    // [KYU ZAROORI HAI]: Array bounds overflow prevent karna.
    // [KAISE CHALTA HAI]: Naya larger array allocate karta hai aur purana data copy karta hai.
    // [OUTPUT / SIDE EFFECT]: _data array resized.
    // [VARIATIONS TO TRY]: ArrayPool<T>.Shared rental.
    private void EnsureCapacity(int minCapacity)
    {
        var newArray = new T[minCapacity];
        Array.Copy(_data, newArray, _count);
        _data = newArray;
    }

    // [KYA KARTA HAI]: Specific index par element access karta hai in O(1) time.
    // [KYU ZAROORI HAI]: Point lookups aur selection vector indexing.
    // [KAISE CHALTA HAI]: Bounds checking followed by direct array indexing.
    // [OUTPUT / SIDE EFFECT]: Returns element at index.
    // [VARIATIONS TO TRY]: Unchecked indexing in release builds.
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new IndexOutOfRangeException($"Index {index} out of bounds for chunk '{ColumnName}' of length {_count}");
            }
            return _data[index];
        }
    }

    // [KYA KARTA HAI]: Underlying contiguous data ko ReadOnlySpan<T> ke roop me expose karta hai.
    // [KYU ZAROORI HAI]: Vectorized operators ko zero-allocation direct memory slice access dena.
    // [KAISE CHALTA HAI]: _data.AsSpan(0, _count).
    // [OUTPUT / SIDE EFFECT]: ReadOnlySpan pointing directly to data buffer.
    // [VARIATIONS TO TRY]: Memory<T> for async processing.
    public ReadOnlySpan<T> AsSpan() => _data.AsSpan(0, _count);

    // [KYA KARTA HAI]: Batch slice (offset se length tak) Span ke roop me return karta hai.
    // [KYU ZAROORI HAI]: 1,024-element batch vector execution me chunk processing.
    // [KAISE CHALTA HAI]: _data.AsSpan(start, length).
    // [OUTPUT / SIDE EFFECT]: Sub-span of elements.
    // [VARIATIONS TO TRY]: Unsafe pointer slice.
    public ReadOnlySpan<T> Slice(int start, int length) => _data.AsSpan(start, length);

    // [KYA KARTA HAI]: Populated elements ki total count return karta hai.
    // [KYU ZAROORI HAI]: Table row count determine karna aur loop limits set karna.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: Returns _count integer.
    // [VARIATIONS TO TRY]: Long row count for tables > 2 billion rows.
    public int Count => _count;

    // [KYA KARTA HAI]: Chunk ka approximate RAM memory footprint (bytes) return karta hai.
    // [KYU ZAROORI HAI]: Storage stats aur memory usage monitoring.
    // [KAISE CHALTA HAI]: Count * Marshal.SizeOf<T>().
    // [OUTPUT / SIDE EFFECT]: Memory size in bytes.
    // [VARIATIONS TO TRY]: Unsafe.SizeOf<T>() for faster compile-time evaluation.
    public long MemorySizeBytes => (long)_count * Unsafe.SizeOf<T>();

    // [KYA KARTA HAI]: Underlying raw array reference return karta hai.
    // [KYU ZAROORI HAI]: SIMD Vector<T> batch loading aur pointer pinning ke liye.
    // [KAISE CHALTA HAI]: Field getter.
    // [OUTPUT / SIDE EFFECT]: Raw T[] array reference.
    // [VARIATIONS TO TRY]: ImmutableArray<T>.
    public T[] RawArray => _data;
}
