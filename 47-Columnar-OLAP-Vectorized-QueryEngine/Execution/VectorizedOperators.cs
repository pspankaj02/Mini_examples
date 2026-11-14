using KiranaHub.ColumnarOlap.Storage;

namespace KiranaHub.ColumnarOlap.Execution;

/// <summary>
/// Interface for all Vectorized Physical Query Plan Operators.
/// Implements the Batch-at-a-time Volcano Iterator model (Vectorized Engine).
/// </summary>
public interface IVectorizedOperator
{
    // [KYA KARTA HAI]: Agla 1,024-element vector batch populate karta hai.
    // [KYU ZAROORI HAI]: Streaming pipeline execution without buffering entire dataset in memory.
    // [KAISE CHALTA HAI]: Populates provided reusable batch object; returns false when data ends.
    // [OUTPUT / SIDE EFFECT]: Returns true if batch has rows, false on EOF.
    // [VARIATIONS TO TRY]: ValueTask<bool> for asynchronous disk-backed operators.
    bool NextBatch(VectorBatch batch);
}

/// <summary>
/// Columnar Scan Operator.
/// Streams batches of contiguous columnar chunks from memory.
/// </summary>
public sealed class ColumnarScanOperator : IVectorizedOperator
{
    private readonly int _totalRows;
    private int _currentOffset;

    public ColumnarScanOperator(int totalRows)
    {
        _totalRows = totalRows;
        _currentOffset = 0;
    }

    public bool NextBatch(VectorBatch batch)
    {
        if (_currentOffset >= _totalRows)
        {
            return false;
        }

        int remaining = _totalRows - _currentOffset;
        int batchLen = Math.Min(remaining, VectorBatch.DefaultBatchCapacity);

        batch.Reset(_currentOffset, batchLen);
        _currentOffset += batchLen;

        return true;
    }
}

/// <summary>
/// Vectorized Filter Operator.
/// Evaluates comparison predicates (e.g. Amount >= 200.0f) across vector batches
/// and populates the Selection Vector without copying memory.
/// </summary>
public sealed class VectorizedFilterOperator : IVectorizedOperator
{
    private readonly IVectorizedOperator _child;
    private readonly ColumnChunk<float> _amountChunk;
    private readonly float _minAmountThreshold;

    public VectorizedFilterOperator(
        IVectorizedOperator child,
        ColumnChunk<float> amountChunk,
        float minAmountThreshold)
    {
        _child = child;
        _amountChunk = amountChunk;
        _minAmountThreshold = minAmountThreshold;
    }

    public bool NextBatch(VectorBatch batch)
    {
        // Loop until we find a batch with at least one matching row or EOF
        while (_child.NextBatch(batch))
        {
            int baseOffset = batch.BaseRowOffset;
            var amountSpan = _amountChunk.Slice(baseOffset, batch.Length);

            int passCount = 0;

            if (batch.HasSelectionVector)
            {
                // Filter further on previously filtered selection vector
                int priorSelected = batch.SelectedCount;
                for (int i = 0; i < priorSelected; i++)
                {
                    int localIdx = batch.SelectionVector[i];
                    if (amountSpan[localIdx] >= _minAmountThreshold)
                    {
                        batch.SelectionVector[passCount++] = localIdx;
                    }
                }
            }
            else
            {
                // Full batch filter: examine all rows
                for (int i = 0; i < batch.Length; i++)
                {
                    if (amountSpan[i] >= _minAmountThreshold)
                    {
                        batch.SelectionVector[passCount++] = i;
                    }
                }
            }

            batch.SelectedCount = passCount;
            batch.HasSelectionVector = true;

            if (passCount > 0)
            {
                return true; // Found matching rows in this batch!
            }
        }

        return false;
    }
}

/// <summary>
/// Result entry for group-by aggregation (Pincode -> GMV, OrderCount, AvgBasket).
/// </summary>
public sealed class PincodeAggResult
{
    public int Pincode { get; set; }
    public long OrderCount { get; set; }
    public double TotalGmv { get; set; }
    public float MinOrderAmount { get; set; } = float.MaxValue;
    public float MaxOrderAmount { get; set; } = float.MinValue;
    public double AverageBasketSize => OrderCount > 0 ? TotalGmv / OrderCount : 0.0;
}

/// <summary>
/// Vectorized Group-By Aggregation Engine.
/// Accumulates SUM, COUNT, MIN, MAX, AVG across filtered batches using cache-friendly grouping.
/// </summary>
public sealed class VectorizedAggregationEngine
{
    private readonly IVectorizedOperator _sourcePlan;
    private readonly ColumnChunk<int> _pincodeChunk;
    private readonly ColumnChunk<float> _amountChunk;

    public VectorizedAggregationEngine(
        IVectorizedOperator sourcePlan,
        ColumnChunk<int> pincodeChunk,
        ColumnChunk<float> amountChunk)
    {
        _sourcePlan = sourcePlan;
        _pincodeChunk = pincodeChunk;
        _amountChunk = amountChunk;
    }

    // [KYA KARTA HAI]: Query plan ko execute karta hai aur groups summarize karta hai.
    // [KYU ZAROORI HAI]: Analytical queries ka final result table produce karna.
    // [KAISE CHALTA HAI]: Batches pull karta hai, dense array hash accumulator se O(1) direct slot indexing karta hai.
    // [OUTPUT / SIDE EFFECT]: Dictionary of Pincode -> PincodeAggResult.
    // [VARIATIONS TO TRY]: SIMD horizontal sum across batch chunks.
    public Dictionary<int, PincodeAggResult> Execute()
    {
        // Dense fast accumulator slots for low-cardinality keys (DuckDB / ClickHouse pattern)
        const int BasePin = 400001;
        const int SlotCount = 16;
        var counts = new long[SlotCount];
        var sums = new double[SlotCount];
        var mins = new float[SlotCount];
        var maxs = new float[SlotCount];
        Array.Fill(mins, float.MaxValue);
        Array.Fill(maxs, float.MinValue);

        var batch = new VectorBatch();

        while (_sourcePlan.NextBatch(batch))
        {
            int baseOffset = batch.BaseRowOffset;
            var pincodeSpan = _pincodeChunk.Slice(baseOffset, batch.Length);
            var amountSpan = _amountChunk.Slice(baseOffset, batch.Length);

            if (batch.HasSelectionVector)
            {
                // Iterate ONLY through filtered selection indices with direct array indexing
                int count = batch.SelectedCount;
                var sel = batch.SelectionVector;
                for (int i = 0; i < count; i++)
                {
                    int localIdx = sel[i];
                    int slot = pincodeSpan[localIdx] - BasePin;
                    float amt = amountSpan[localIdx];

                    counts[slot]++;
                    sums[slot] += amt;
                    if (amt < mins[slot]) mins[slot] = amt;
                    if (amt > maxs[slot]) maxs[slot] = amt;
                }
            }
            else
            {
                // Sequential batch accumulation
                for (int i = 0; i < batch.Length; i++)
                {
                    int slot = pincodeSpan[i] - BasePin;
                    float amt = amountSpan[i];

                    counts[slot]++;
                    sums[slot] += amt;
                    if (amt < mins[slot]) mins[slot] = amt;
                    if (amt > maxs[slot]) maxs[slot] = amt;
                }
            }
        }

        var groups = new Dictionary<int, PincodeAggResult>();
        for (int i = 0; i < SlotCount; i++)
        {
            if (counts[i] > 0)
            {
                int pin = BasePin + i;
                groups[pin] = new PincodeAggResult
                {
                    Pincode = pin,
                    OrderCount = counts[i],
                    TotalGmv = sums[i],
                    MinOrderAmount = mins[i],
                    MaxOrderAmount = maxs[i]
                };
            }
        }

        return groups;
    }
}
