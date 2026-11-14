// [KYA KARTA HAI]: Hardware Intrinsics namespaces import karta hai
// [KYU ZAROORI HAI]: CPU instruction set extensions (AVX, AVX2, AVX-512, FMA, ARM NEON) inspect karne ke liye
// [KAISE CHALTA HAI]: System.Runtime.Intrinsics namespaces provide low-level CPU capability checks
// [OUTPUT / SIDE EFFECT]: Hardware instruction set types accessible
// [VARIATIONS TO TRY]: System.Runtime.Intrinsics.Arm for Apple Silicon / Graviton
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

// [KYA KARTA HAI]: Hardware SIMD namespace define karta hai
// [KYU ZAROORI HAI]: CPU vectorization components ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project Simd directory structure ko map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.HardwareIntrinsics.Simd
// [VARIATIONS TO TRY]: KiranaHub.Performance.Vectorization
namespace KiranaHub.HardwareIntrinsics.Simd;

// [KYA KARTA HAI]: Host CPU Hardware Features and Vector Capabilities Detector class
// [KYU ZAROORI HAI]: Application startup par CPU SIMD instruction sets ko inspect karke dynamic dispatch optimize karne ke liye
// [KAISE CHALTA HAI]: CPUID hardware instruction flags check karta hai (Vector.IsHardwareAccelerated, Avx2.IsSupported, etc.)
// [OUTPUT / SIDE EFFECT]: Hardware profile logging and runtime capability detection
// [VARIATIONS TO TRY]: Cache line size query via native sysinfo
public static class CpuFeatureDetector
{
    // [KYA KARTA HAI]: Host CPU capabilities ka formatted diagnostic report print karta hai
    // [KYU ZAROORI HAI]: Engine run hone se pehle user ko hardware acceleration profile dikhane ke liye
    // [KAISE CHALTA HAI]: Evaluates static boolean properties on X86 and ARM intrinsic classes
    // [OUTPUT / SIDE EFFECT]: Prints CPU instruction set availability
    // [VARIATIONS TO TRY]: Export as JSON hardware profile
    public static void PrintHardwareCapabilities()
    {
        Console.WriteLine("[HARDWARE] Inspecting Host CPU Instruction Set Capabilities:");
        Console.WriteLine($"  - Vector<T> Hardware Accelerated : {Vector.IsHardwareAccelerated}");
        Console.WriteLine($"  - Vector Byte Size               : {Vector<float>.Count * sizeof(float)} bytes ({Vector<float>.Count} floats simultaneously)");
        Console.WriteLine($"  - Intel/AMD AVX2 Supported       : {Avx2.IsSupported}");
        Console.WriteLine($"  - Intel/AMD AVX-512 Supported    : {Avx512F.IsSupported}");
        Console.WriteLine($"  - Fused Multiply-Add (FMA)       : {Fma.IsSupported}");
        Console.WriteLine($"  - ARM NEON / AdvSIMD Supported   : {AdvSimd.IsSupported}");
        Console.WriteLine($"  - Vector256<float> Available     : {Vector256.IsHardwareAccelerated}\n");
    }
}
