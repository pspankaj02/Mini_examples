namespace KiranaHub.ColumnarOlap.Execution;

/// <summary>
/// Vectorized Execution Batch (Standard size: 1,024 elements).
/// Instead of tuple-at-a-time (Volcano model), query operators process data in batches of 1,024 items.
/// Contains a Selection Vector (filter bitmask index array) that avoids physical memory copies.
/// </summary>
public sealed class VectorBatch
{
    // [KYA KARTA HAI]: Standard batch size define karta hai (1,024 elements).
    // [KYU ZAROORI HAI]: 1,024 elements fit comfortably inside CPU L1 Data Cache (32KB-48KB).
    // [KAISE CHALTA HAI]: Compile-time constant.
    // [OUTPUT / SIDE EFFECT]: Standard batch chunk dimension.
    // [VARIATIONS TO TRY]: 2,048 or 4,096 batch size for large L2 caches.
    public const int DefaultBatchCapacity = 1024;

    // [KYA KARTA HAI]: Table ke overall context me iss batch ka starting row offset store karta hai.
    // [KYU ZAROORI HAI]: Global row positioning aur joins ke liye.
    // [KAISE CHALTA HAI]: Offset integer.
    // [OUTPUT / SIDE EFFECT]: Base row offset.
    // [VARIATIONS TO TRY]: Partition ID + local offset tuple.
    public int BaseRowOffset { get; set; }

    // [KYA KARTA HAI]: Iss batch me total active elements count store karta hai.
    // [KYU ZAROORI HAI]: Last batch me rows < 1,024 ho sakti hain.
    // [KAISE CHALTA HAI]: Integer count <= DefaultBatchCapacity.
    // [OUTPUT / SIDE EFFECT]: Active elements count.
    // [VARIATIONS TO TRY]: Clamp to capacity.
    public int Length { get; set; }

    // [KYA KARTA HAI]: Selection Vector: un local indices ki array jo filters ko satisfy karte hain.
    // [KYU ZAROORI HAI]: Rows ko physically copy kiye bina filtered rows pass karna (Zero-Copy Filtering).
    // [KAISE CHALTA HAI]: Pre-allocated int array of size DefaultBatchCapacity.
    // [OUTPUT / SIDE EFFECT]: Indices buffer [0, 3, 7, 12, ...].
    // [VARIATIONS TO TRY]: 64-bit bitmask bitmap for even more compact representation.
    public int[] SelectionVector { get; }

    // [KYA KARTA HAI]: Filter pass karne wale elements ki exact count.
    // [KYU ZAROORI HAI]: Selection vector me valid elements limit define karna.
    // [KAISE CHALTA HAI]: Integer count <= Length.
    // [OUTPUT / SIDE EFFECT]: Selected rows count.
    // [VARIATIONS TO TRY]: Atomic count for multi-threaded filters.
    public int SelectedCount { get; set; }

    // [KYA KARTA HAI]: Flag indicate karta hai ki kya selection vector active hai.
    // [KYU ZAROORI HAI]: Agar koi filter nahi chala toh selection vector bypass karke direct sequential loop chalana (Faster).
    // [KAISE CHALTA HAI]: Boolean flag.
    // [OUTPUT / SIDE EFFECT]: True if filtered; false if full batch active.
    // [VARIATIONS TO TRY]: Ternary enum (AllSelected, NoneSelected, PartiallySelected).
    public bool HasSelectionVector { get; set; }

    // [KYA KARTA HAI]: VectorBatch constructor.
    // [KYU ZAROORI HAI]: Pre-allocated selection vector ke sath batch initialize karna.
    // [KAISE CHALTA HAI]: Allocates int[DefaultBatchCapacity].
    // [OUTPUT / SIDE EFFECT]: Ready-to-use VectorBatch instance.
    // [VARIATIONS TO TRY]: Recyclable object pool of VectorBatch instances.
    public VectorBatch(int capacity = DefaultBatchCapacity)
    {
        SelectionVector = new int[capacity];
        Reset(0, 0);
    }

    // [KYA KARTA HAI]: Batch ko reuse karne ke liye state reset karta hai without GC allocations.
    // [KYU ZAROORI HAI]: High-throughput execution me har batch par new object na banana pade (Zero GC).
    // [KAISE CHALTA HAI]: BaseRowOffset aur Length update karta hai, selection vector flag false karta hai.
    // [OUTPUT / SIDE EFFECT]: Re-initialized clean batch.
    // [VARIATIONS TO TRY]: Buffer clear if sensitive data is involved.
    public void Reset(int baseRowOffset, int length)
    {
        BaseRowOffset = baseRowOffset;
        Length = length;
        SelectedCount = length;
        HasSelectionVector = false;
    }

    // [KYA KARTA HAI]: Filter ke pass hone wale local index ko selection vector me record karta hai.
    // [KYU ZAROORI HAI]: Predicate evaluate hone ke baad matched rows index karna.
    // [KAISE CHALTA HAI]: SelectionVector[SelectedCount++] = localIndex.
    // [OUTPUT / SIDE EFFECT]: Index appended to selection vector.
    // [VARIATIONS TO TRY]: SIMD vector mask extraction.
    public void AddSelected(int localIndex)
    {
        SelectionVector[SelectedCount++] = localIndex;
    }
}
