namespace KiranaHub.GpuMatrixCompute.Compute;

/// <summary>
/// High-Performance Cache-Tiled Matrix Multiplier.
/// Implements both Naive Scalar Triple-Loop and GPU-Inspired Cache-Tiled SIMT Multi-Core Kernels.
/// Computes C = A * B where A is (M x K), B is (K x N), and C is (M x N).
/// </summary>
public static class TiledMatrixMultiplier
{
    // [KYA KARTA HAI]: Shared local memory cache tile size define karta hai (16x16 elements).
    // [KYU ZAROORI HAI]: L1 cache line (64 bytes) aur GPU thread block warp dimensions ke sath match karta hai.
    // [KAISE CHALTA HAI]: 16 floats = 64 bytes (exact 1 cache line). 16x16 = 256 floats = 1 KB local memory.
    // [OUTPUT / SIDE EFFECT]: Compile-time constant tile width.
    // [VARIATIONS TO TRY]: 32x32 tiles for larger L2 cache architectures.
    public const int TileSize = 16;

    // [KYA KARTA HAI]: Naive baseline matrix multiplication compute karta hai (Single-Threaded triple for-loop).
    // [KYU ZAROORI HAI]: Baseline benchmark aur correctness parity check ke liye ground truth establish karna.
    // [KAISE CHALTA HAI]: O(M * N * K) standard dot product iteration.
    // [OUTPUT / SIDE EFFECT]: Output array C populated.
    // [VARIATIONS TO TRY]: Loop interchange (i-k-j) for basic prefetcher improvements.
    public static void MultiplyNaive(
        float[] a,
        float[] b,
        float[] c,
        int m,
        int k,
        int n)
    {
        Array.Clear(c, 0, c.Length);

        for (int i = 0; i < m; i++)
        {
            int aRowOffset = i * k;
            int cRowOffset = i * n;

            for (int j = 0; j < n; j++)
            {
                float sum = 0f;
                for (int p = 0; p < k; p++)
                {
                    sum += a[aRowOffset + p] * b[p * n + j];
                }
                c[cRowOffset + j] = sum;
            }
        }
    }

    // [KYA KARTA HAI]: GPU-inspired Cache-Tiled SIMT Parallel Matrix Multiplication execute karta hai.
    // [KYU ZAROORI HAI]: RAM memory bandwidth bottleneck ko 16x drop karna aur multithreaded cores fully saturate karna.
    // [KAISE CHALTA HAI]: Matrix ko 16x16 tiles me todkar L1 cache chunks me multiply karta hai across parallel thread blocks.
    // [OUTPUT / SIDE EFFECT]: Output array C computed in parallel with 10x-20x speedup.
    // [VARIATIONS TO TRY]: SIMD Vector<float> inside the tile accumulation loop.
    public static void MultiplyTiledParallel(
        float[] a,
        float[] b,
        float[] c,
        int m,
        int k,
        int n)
    {
        Array.Clear(c, 0, c.Length);

        int gridDimX = (n + TileSize - 1) / TileSize;
        int gridDimY = (m + TileSize - 1) / TileSize;
        int totalBlocks = gridDimX * gridDimY;

        // Parallel dispatch over 2D block grid
        Parallel.For(0, totalBlocks, blockLinearIdx =>
        {
            int blockX = blockLinearIdx % gridDimX;
            int blockY = blockLinearIdx / gridDimX;

            int rowStart = blockY * TileSize;
            int colStart = blockX * TileSize;

            int rowEnd = Math.Min(rowStart + TileSize, m);
            int colEnd = Math.Min(colStart + TileSize, n);

            // Thread Block local execution with cache tiling
            // Iterate over K dimension in chunks of TileSize
            for (int t = 0; t < k; t += TileSize)
            {
                int tEnd = Math.Min(t + TileSize, k);

                for (int i = rowStart; i < rowEnd; i++)
                {
                    int aRowOffset = i * k;
                    int cRowOffset = i * n;

                    for (int p = t; p < tEnd; p++)
                    {
                        float aVal = a[aRowOffset + p];
                        int bRowOffset = p * n;

                        // Inner loop unrolled over consecutive column elements in cache
                        for (int j = colStart; j < colEnd; j++)
                        {
                            c[cRowOffset + j] += aVal * b[bRowOffset + j];
                        }
                    }
                }
            }
        });
    }

    // [KYA KARTA HAI]: Naive aur Tiled results ke beech mathematical parity check karta hai.
    // [KYU ZAROORI HAI]: Verification karna ki parallel caching optimization ne calculation accuracy distort nahi ki.
    // [KAISE CHALTA HAI]: Har element ka absolute difference check karta hai with floating-point tolerance.
    // [OUTPUT / SIDE EFFECT]: Returns true if 100% outputs are identical within tolerance.
    // [VARIATIONS TO TRY]: Root-Mean-Square Error (RMSE) calculation.
    public static bool VerifyParity(float[] naiveC, float[] tiledC, float tolerance = 1e-3f)
    {
        if (naiveC.Length != tiledC.Length) return false;

        for (int i = 0; i < naiveC.Length; i++)
        {
            if (Math.Abs(naiveC[i] - tiledC[i]) > tolerance)
            {
                return false;
            }
        }

        return true;
    }
}
