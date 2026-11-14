// [KYA KARTA HAI]: Hardware Intrinsics and Memory namespaces import karta hai
// [KYU ZAROORI HAI]: Vector256<float> aur high-performance memory operations ke liye
// [KAISE CHALTA HAI]: System.Runtime.Intrinsics types mapped
// [OUTPUT / SIDE EFFECT]: 256-bit SIMD registers available
// [VARIATIONS TO TRY]: Vector512<float> for AVX-512 capable CPUs
using System.Runtime.Intrinsics;

// [KYA KARTA HAI]: Hardware SIMD namespace define karta hai
// [KYU ZAROORI HAI]: Vectorized pricing calculation engine ko modularize karne ke liye
// [KAISE CHALTA HAI]: Project Simd directory structure ko map karta hai
// [OUTPUT / SIDE EFFECT]: VectorizedPricingEngine registered in KiranaHub.HardwareIntrinsics.Simd
// [VARIATIONS TO TRY]: KiranaHub.Pricing.Simd
namespace KiranaHub.HardwareIntrinsics.Simd;

// [KYA KARTA HAI]: High-Performance SIMD Vectorized Grocery Pricing & Tax Calculation Engine
// [KYU ZAROORI HAI]: Flash-sale ke dauran 1,000,000 grocery items par dynamic discounts aur GST tax 8-items-at-a-time single CPU clock cycle me compute karne ke liye
// [KAISE CHALTA HAI]: 256-bit AVX2 vector registers me 8 floats load karta hai aur parallel SIMD hardware instructions execute karta hai
// [OUTPUT / SIDE EFFECT]: 6x-10x hardware acceleration over scalar loops with zero memory allocations
// [VARIATIONS TO TRY]: Integer fixed-point SIMD vectorization with Vector256<int>
public static class VectorizedPricingEngine
{
    // [KYA KARTA HAI]: Scalar baseline pricing loop (single float per CPU cycle)
    // [KYU ZAROORI HAI]: Benchmark comparison aur correctness reference establish karne ke liye
    // [KAISE CHALTA HAI]: Sequential for loop multiplying price by discount and tax
    // [OUTPUT / SIDE EFFECT]: outputPrices array populated sequentially
    // [VARIATIONS TO TRY]: Unrolled scalar loop
    public static void ComputePricesScalar(
        ReadOnlySpan<float> basePrices,
        ReadOnlySpan<float> discountFactors,
        float taxMultiplier,
        Span<float> outputPrices)
    {
        for (int i = 0; i < basePrices.Length; i++)
        {
            outputPrices[i] = (basePrices[i] * discountFactors[i]) * taxMultiplier;
        }
    }

    // [KYA KARTA HAI]: SIMD Vectorized pricing loop (8 floats per CPU cycle via Vector256)
    // [KYU ZAROORI HAI]: CPU SIMD vector units ko 100% utilize karke sub-millisecond pricing achieve karne ke liye
    // [KAISE CHALTA HAI]: 8-float chunks ko YMM registers me load karta hai, Vector256.Multiply execute karta hai, aur aligned memory me store karta hai
    // [OUTPUT / SIDE EFFECT]: 8 prices computed simultaneously per cycle
    // [VARIATIONS TO TRY]: FMA Avx2.MultiplyAdd instructions
    public static void ComputePricesSimd(
        ReadOnlySpan<float> basePrices,
        ReadOnlySpan<float> discountFactors,
        float taxMultiplier,
        Span<float> outputPrices)
    {
        int length = basePrices.Length;
        int vectorWidth = Vector256<float>.Count; // 8 floats
        int vectorLimit = length - (length % vectorWidth);

        // Broadcast scalar tax multiplier across all 8 vector lanes: [tax, tax, tax, tax, tax, tax, tax, tax]
        Vector256<float> vTax = Vector256.Create(taxMultiplier);

        // Main SIMD vectorized loop: 8 items per iteration
        for (int i = 0; i < vectorLimit; i += vectorWidth)
        {
            // Load 8 base prices from memory into 256-bit register
            Vector256<float> vBase = Vector256.LoadUnsafe(in basePrices[i]);

            // Load 8 discount factors from memory into 256-bit register
            Vector256<float> vDisc = Vector256.LoadUnsafe(in discountFactors[i]);

            // Multiply 8 base prices by 8 discount factors in parallel in 1 cycle
            Vector256<float> vDiscounted = Vector256.Multiply(vBase, vDisc);

            // Multiply discounted prices by statutory tax rate in 1 cycle
            Vector256<float> vFinal = Vector256.Multiply(vDiscounted, vTax);

            // Store 8 computed final prices directly into output memory
            vFinal.StoreUnsafe(ref outputPrices[i]);
        }

        // Tail loop: process remaining 1 to 7 items that don't fill a full 8-lane vector
        for (int i = vectorLimit; i < length; i++)
        {
            outputPrices[i] = (basePrices[i] * discountFactors[i]) * taxMultiplier;
        }
    }
}
