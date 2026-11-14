namespace KiranaHub.GpuMatrixCompute.Compute;

/// <summary>
/// 2D Dimension specification (representing GridDim, BlockDim, BlockIdx, or ThreadIdx).
/// Mirrors CUDA / OpenCL dimension coordinates for SIMT parallel thread grids.
/// </summary>
public readonly record struct Dim2(int X, int Y)
{
    // [KYA KARTA HAI]: 2D coordinate representation string format me print karta hai.
    // [KYU ZAROORI HAI]: Debugging kernel block coordinates and thread indices.
    // [KAISE CHALTA HAI]: (X, Y) formatting.
    // [OUTPUT / SIDE EFFECT]: String formatted coordinate.
    // [VARIATIONS TO TRY]: 3D Dim3 (X, Y, Z) support.
    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Thread execution coordinate context passed to every SIMT worker kernel.
/// Provides the exact same coordinate variables found in CUDA/OpenCL (blockIdx, threadIdx, blockDim, gridDim).
/// </summary>
public readonly struct SimtThreadContext
{
    // [KYA KARTA HAI]: Current block coordinate in the execution grid (blockIdx).
    public readonly Dim2 BlockIdx;

    // [KYA KARTA HAI]: Current thread coordinate within its parent block (threadIdx).
    public readonly Dim2 ThreadIdx;

    // [KYA KARTA HAI]: Number of threads per block dimension (blockDim, e.g. 16x16).
    public readonly Dim2 BlockDim;

    // [KYA KARTA HAI]: Number of blocks in the grid dimension (gridDim).
    public readonly Dim2 GridDim;

    // [KYA KARTA HAI]: Global 2D Row coordinate in the target matrix.
    // [KYU ZAROORI HAI]: Har thread ko pata chal sake ki wo output matrix ke kis exact row ko compute kar raha hai.
    // [KAISE CHALTA HAI]: BlockIdx.Y * BlockDim.Y + ThreadIdx.Y (Standard CUDA global index formula).
    // [OUTPUT / SIDE EFFECT]: Global row integer.
    // [VARIATIONS TO TRY]: Linearized 1D global index.
    public int GlobalRow => (BlockIdx.Y * BlockDim.Y) + ThreadIdx.Y;

    // [KYA KARTA HAI]: Global 2D Column coordinate in the target matrix.
    // [KYU ZAROORI HAI]: Output matrix ke exact column index par write karne ke liye.
    // [KAISE CHALTA HAI]: BlockIdx.X * BlockDim.X + ThreadIdx.X.
    // [OUTPUT / SIDE EFFECT]: Global column integer.
    // [VARIATIONS TO TRY]: Strided global index.
    public int GlobalCol => (BlockIdx.X * BlockDim.X) + ThreadIdx.X;

    public SimtThreadContext(Dim2 blockIdx, Dim2 threadIdx, Dim2 blockDim, Dim2 gridDim)
    {
        BlockIdx = blockIdx;
        ThreadIdx = threadIdx;
        BlockDim = blockDim;
        GridDim = gridDim;
    }
}

/// <summary>
/// Massively Parallel SIMT Grid Scheduler.
/// Simulates GPU Streaming Multiprocessor (SM) hardware dispatch:
/// maps 2D thread blocks across CPU cores using cache-friendly work-stealing partitions.
/// </summary>
public static class SimtGridScheduler
{
    // [KYA KARTA HAI]: 2D SIMT execution grid ko parallel CPU threads par launch karta hai.
    // [KYU ZAROORI HAI]: GPU Compute Shader execution model ko CPU multicore par simulate karna with zero native driver requirements.
    // [KAISE CHALTA HAI]: Parallel.For over blocks; each block runs its local threads.
    // [OUTPUT / SIDE EFFECT]: Executes kernel across all (gridDim.X * gridDim.Y * blockDim.X * blockDim.Y) thread contexts.
    // [VARIATIONS TO TRY]: Work-stealing threadpool with custom hardware thread affinity.
    public static void Launch2D(
        Dim2 gridDim,
        Dim2 blockDim,
        Action<SimtThreadContext> kernel)
    {
        int totalBlocks = gridDim.X * gridDim.Y;

        // Parallel dispatch of blocks across CPU worker cores
        Parallel.For(0, totalBlocks, blockLinearIdx =>
        {
            int blockX = blockLinearIdx % gridDim.X;
            int blockY = blockLinearIdx / gridDim.X;
            var blockIdx = new Dim2(blockX, blockY);

            // Execute threads inside this thread block
            for (int ty = 0; ty < blockDim.Y; ty++)
            {
                for (int tx = 0; tx < blockDim.X; tx++)
                {
                    var threadIdx = new Dim2(tx, ty);
                    var ctx = new SimtThreadContext(blockIdx, threadIdx, blockDim, gridDim);
                    kernel(ctx);
                }
            }
        });
    }
}
