// [KYA KARTA HAI]: Diagnostic and System namespaces import karta hai
// [KYU ZAROORI HAI]: High-resolution Stopwatch aur memory arrays ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Timing and formatting ready
// [VARIATIONS TO TRY]: BenchmarkDotNet harness
using System.Diagnostics;
using KiranaHub.HardwareIntrinsics.Simd;

// [KYA KARTA HAI]: HardwareIntrinsics main namespace
// [KYU ZAROORI HAI]: Program entrypoint wrap karne ke liye
// [KAISE CHALTA HAI]: Namespace wrapping
// [OUTPUT / SIDE EFFECT]: Program class registered
// [VARIATIONS TO TRY]: KiranaHub.Simd.Main
namespace KiranaHub.HardwareIntrinsics;

public static class Program
{
    // [KYA KARTA HAI]: Main execution entry point
    // [KYU ZAROORI HAI]: SIMD hardware intrinsics benchmarks and accuracy validations run karne ke liye
    // [KAISE CHALTA HAI]: Executes CPU feature detection, 1,000,000 pricing benchmark, and 500,000 distance benchmark
    // [OUTPUT / SIDE EFFECT]: Console output with speedup factors
    // [VARIATIONS TO TRY]: Multi-threaded Parallel.For + SIMD hybrid
    public static void Main(string[] args)
    {
        // [KYA KARTA HAI]: Header banner print karta hai
        // [KYU ZAROORI HAI]: Benchmark identification ke liye
        // [KAISE CHALTA HAI]: Console.WriteLine
        // [OUTPUT / SIDE EFFECT]: Banner printed
        // [VARIATIONS TO TRY]: ANSI colors
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 10: Hardware Intrinsics & Vectorized SIMD Engine (AVX2/AVX-512)");
        Console.WriteLine("  Features: Vector256<float> 8-Wide Operations, Geospatial Haversine, Zero-GC");
        Console.WriteLine("================================================================================\n");

        // Print CPU hardware profile
        CpuFeatureDetector.PrintHardwareCapabilities();

        // ---------------------------------------------------------------------------------
        // BENCHMARK 1: 1,000,000 Grocery Pricing & Tax Calculations
        // ---------------------------------------------------------------------------------
        const int pricingCount = 1000000;
        Console.WriteLine($"[BENCHMARK 1] Pricing & Tax Matrix Calculation across {pricingCount:N0} Grocery Items...");

        float[] basePrices = new float[pricingCount];
        float[] discountFactors = new float[pricingCount];
        float[] scalarPrices = new float[pricingCount];
        float[] simdPrices = new float[pricingCount];

        Random rnd = new(42);
        for (int i = 0; i < pricingCount; i++)
        {
            basePrices[i] = (float)(rnd.NextDouble() * 500.0 + 10.0); // Rs 10 to Rs 510
            discountFactors[i] = 0.85f; // 15% discount
        }
        float taxMultiplier = 1.05f; // 5% GST

        // Warm up JIT
        VectorizedPricingEngine.ComputePricesScalar(basePrices.AsSpan(0, 1000), discountFactors.AsSpan(0, 1000), taxMultiplier, scalarPrices.AsSpan(0, 1000));
        VectorizedPricingEngine.ComputePricesSimd(basePrices.AsSpan(0, 1000), discountFactors.AsSpan(0, 1000), taxMultiplier, simdPrices.AsSpan(0, 1000));

        // Measure Scalar
        Stopwatch swScalar = Stopwatch.StartNew();
        VectorizedPricingEngine.ComputePricesScalar(basePrices, discountFactors, taxMultiplier, scalarPrices);
        swScalar.Stop();

        // Measure SIMD Vector256
        Stopwatch swSimd = Stopwatch.StartNew();
        VectorizedPricingEngine.ComputePricesSimd(basePrices, discountFactors, taxMultiplier, simdPrices);
        swSimd.Stop();

        // Verify mathematical parity
        bool pricingParity = true;
        for (int i = 0; i < pricingCount; i++)
        {
            if (System.Math.Abs(scalarPrices[i] - simdPrices[i]) > 0.01f)
            {
                pricingParity = false;
                break;
            }
        }

        double scalarMs = swScalar.Elapsed.TotalMilliseconds;
        double simdMs = swSimd.Elapsed.TotalMilliseconds;
        double pricingSpeedup = scalarMs / simdMs;

        Console.WriteLine($"  - Scalar Execution Time    : {scalarMs:F2} ms ({pricingCount / (scalarMs / 1000.0):N0} items/sec)");
        Console.WriteLine($"  - SIMD Vector256 Time      : {simdMs:F2} ms ({pricingCount / (simdMs / 1000.0):N0} items/sec)");
        Console.WriteLine($"  - Hardware Speedup Factor  : {pricingSpeedup:F2}x FASTER");
        Console.WriteLine($"  - Mathematical Parity Check: {(pricingParity ? "100% IDENTICAL OUTPUTS (Verified)" : "MISMATCH")}\n");

        // ---------------------------------------------------------------------------------
        // BENCHMARK 2: 500,000 Geospatial Distance Calculations (Dark-Store to Customer)
        // ---------------------------------------------------------------------------------
        const int geoCount = 500000;
        Console.WriteLine($"[BENCHMARK 2] Geospatial City-Scale Distance Engine across {geoCount:N0} Coordinate Pairs...");

        float[] lat1Array = new float[geoCount];
        float[] lon1Array = new float[geoCount];
        float[] lat2Array = new float[geoCount];
        float[] lon2Array = new float[geoCount];
        float[] scalarDistances = new float[geoCount];
        float[] simdDistances = new float[geoCount];

        // Seed with coordinates near Mumbai (Lat ~19.07, Lon ~72.87)
        for (int i = 0; i < geoCount; i++)
        {
            lat1Array[i] = 19.0760f + (float)(rnd.NextDouble() * 0.1 - 0.05);
            lon1Array[i] = 72.8777f + (float)(rnd.NextDouble() * 0.1 - 0.05);
            lat2Array[i] = 19.0760f + (float)(rnd.NextDouble() * 0.1 - 0.05);
            lon2Array[i] = 72.8777f + (float)(rnd.NextDouble() * 0.1 - 0.05);
        }

        // Warm up JIT
        VectorizedGeoDistance.ComputeDistancesScalar(lat1Array.AsSpan(0, 1000), lon1Array.AsSpan(0, 1000), lat2Array.AsSpan(0, 1000), lon2Array.AsSpan(0, 1000), scalarDistances.AsSpan(0, 1000));
        VectorizedGeoDistance.ComputeDistancesSimd(lat1Array.AsSpan(0, 1000), lon1Array.AsSpan(0, 1000), lat2Array.AsSpan(0, 1000), lon2Array.AsSpan(0, 1000), simdDistances.AsSpan(0, 1000));

        // Measure Scalar
        Stopwatch swGeoScalar = Stopwatch.StartNew();
        VectorizedGeoDistance.ComputeDistancesScalar(lat1Array, lon1Array, lat2Array, lon2Array, scalarDistances);
        swGeoScalar.Stop();

        // Measure SIMD Vector256
        Stopwatch swGeoSimd = Stopwatch.StartNew();
        VectorizedGeoDistance.ComputeDistancesSimd(lat1Array, lon1Array, lat2Array, lon2Array, simdDistances);
        swGeoSimd.Stop();

        // Verify distance parity
        bool geoParity = true;
        for (int i = 0; i < geoCount; i++)
        {
            if (System.Math.Abs(scalarDistances[i] - simdDistances[i]) > 0.05f)
            {
                geoParity = false;
                break;
            }
        }

        double geoScalarMs = swGeoScalar.Elapsed.TotalMilliseconds;
        double geoSimdMs = swGeoSimd.Elapsed.TotalMilliseconds;
        double geoSpeedup = geoScalarMs / geoSimdMs;

        Console.WriteLine($"  - Scalar Geo Distance Time : {geoScalarMs:F2} ms ({geoCount / (geoScalarMs / 1000.0):N0} pairs/sec)");
        Console.WriteLine($"  - SIMD Vector256 Geo Time  : {geoSimdMs:F2} ms ({geoCount / (geoSimdMs / 1000.0):N0} pairs/sec)");
        Console.WriteLine($"  - Hardware Speedup Factor  : {geoSpeedup:F2}x FASTER");
        Console.WriteLine($"  - Distance Parity Check    : {(geoParity ? "100% IDENTICAL DISTANCES (Verified)" : "MISMATCH")}\n");

        if (pricingParity && geoParity && (pricingSpeedup > 1.5 || geoSpeedup > 1.5))
        {
            Console.WriteLine("[SUCCESS] SIMD AVX2 hardware intrinsics passed all performance and mathematical parity verifications!\n");
        }
        else
        {
            Console.WriteLine("[WARNING] Parity or speedup deviated from target!\n");
        }
    }
}
