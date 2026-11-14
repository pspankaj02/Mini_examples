namespace KiranaHub.ColumnarOlap.Compression;

/// <summary>
/// A single Run-Length Encoded entry storing a value and its repeat length.
/// </summary>
public readonly record struct RleRun<T>(T Value, int Length) where T : struct;

/// <summary>
/// Run-Length Encoding (RLE) Compressor for Columnar Data.
/// Compresses consecutive repeating values (e.g. sorted order dates, order statuses)
/// into compact (Value, RunLength) pairs.
/// Enables vectorized operators to skip entire runs in O(1) time!
/// </summary>
public static class RunLengthEncoder
{
    // [KYA KARTA HAI]: Contiguous array ko Run-Length Encoded runs me compress karta hai.
    // [KYU ZAROORI HAI]: Sorted columns (dates, status) me 90%+ storage compression ratio achieve karna.
    // [KAISE CHALTA HAI]: Adjacent identical elements count karke (Value, Count) tuples emit karta hai.
    // [OUTPUT / SIDE EFFECT]: List of RleRun<T> items.
    // [VARIATIONS TO TRY]: Streaming RLE iterator with IEnumerable.
    public static List<RleRun<T>> Compress<T>(ReadOnlySpan<T> input) where T : struct, IEquatable<T>
    {
        var runs = new List<RleRun<T>>();
        if (input.IsEmpty)
        {
            return runs;
        }

        T currentValue = input[0];
        int currentLength = 1;

        for (int i = 1; i < input.Length; i++)
        {
            if (input[i].Equals(currentValue))
            {
                currentLength++;
            }
            else
            {
                runs.Add(new RleRun<T>(currentValue, currentLength));
                currentValue = input[i];
                currentLength = 1;
            }
        }

        runs.Add(new RleRun<T>(currentValue, currentLength));
        return runs;
    }

    // [KYA KARTA HAI]: RLE runs ko wapas contiguous decompressed array me expand karta hai.
    // [KYU ZAROORI HAI]: Point lookups aur baseline validation ke liye original array reconstruct karna.
    // [KAISE CHALTA HAI]: Target array me har run ka value run.Length times repeat likhta hai.
    // [OUTPUT / SIDE EFFECT]: Reconstructed array of original values.
    // [VARIATIONS TO TRY]: SIMD memset or Span.Fill for fast decompression.
    public static T[] Decompress<T>(IReadOnlyList<RleRun<T>> runs, int totalElements) where T : struct
    {
        var result = new T[totalElements];
        int offset = 0;

        foreach (var run in runs)
        {
            Array.Fill(result, run.Value, offset, run.Length);
            offset += run.Length;
        }

        return result;
    }

    // [KYA KARTA HAI]: Compression ratio calculate karta hai: (originalBytes / compressedBytes).
    // [KYU ZAROORI HAI]: Storage savings telemetry monitor karne ke liye.
    // [KAISE CHALTA HAI]: Input elements vs run entries count ratio compute karta hai.
    // [OUTPUT / SIDE EFFECT]: Floating point compression multiplier.
    // [VARIATIONS TO TRY]: Express as percentage saved.
    public static double CalculateCompressionRatio<T>(int totalRows, int runCount) where T : struct
    {
        if (runCount == 0) return 1.0;
        return (double)totalRows / runCount;
    }
}
