// ============================================================================
// File: GaloisField256.cs
// Namespace: KiranaHub.ObjectStorage.Galois
// Description: Finite field GF(2^8) arithmetic implementation with primitive
// polynomial 0x11D (x^8 + x^4 + x^3 + x^2 + 1) for Reed-Solomon Erasure Coding.
// ============================================================================

namespace KiranaHub.ObjectStorage.Galois;

/// <summary>
/// GF(2^8) Galois Field arithmetic engine supporting O(1) log/exp table lookup multiplication.
/// </summary>
public static class GaloisField256
{
    // [KYA KARTA HAI]: Primitive polynomial constant define karta hai (0x11D = 285).
    // [KYU ZAROORI HAI]: GF(2^8) me overflow prevent karne aur field elements cycle karne ke liye standard irreducible polynomial chahiye.
    // [KAISE CHALTA HAI]: x^8 + x^4 + x^3 + x^2 + 1 binary representation (100011101) ko store karta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable primitive polynomial constant.
    // [VARIATIONS TO TRY]: 0x11B (AES Rijndael polynomial) bhi use kar sakte hain specific crypto applications me.
    public const int PrimitivePolynomial = 0x11D;

    // [KYA KARTA HAI]: Exponential (anti-log) lookup table of size 512 allocate karta hai.
    // [KYU ZAROORI HAI]: Modulo 255 ki branching avoid karke O(1) multiplication speed achieve karne ke liye table duplicate (512 length) rakhte hain.
    // [KAISE CHALTA HAI]: Generator element 2 ke powers ExpTable[i] = 2^i store karta hai.
    // [OUTPUT / SIDE EFFECT]: Fast RAM resident lookup array.
    // [VARIATIONS TO TRY]: ReadOnlySpan<byte> se map kar sakte hain statically embedded bytes se.
    private static readonly byte[] ExpTable = new byte[512];

    // [KYA KARTA HAI]: Discrete logarithm lookup table of size 256 allocate karta hai.
    // [KYU ZAROORI HAI]: Har non-zero byte value ka generator power index map karta hai: LogTable[2^i] = i.
    // [KAISE CHALTA HAI]: ExpTable generation ke time inverse mapping calculate karke fill hota hai.
    // [OUTPUT / SIDE EFFECT]: 256 bytes memory footprint table.
    // [VARIATIONS TO TRY]: LogTable[0] undefined hota hai, isliye 0 check lazmi hai.
    private static readonly byte[] LogTable = new byte[256];

    // [KYA KARTA HAI]: Static constructor me ExpTable aur LogTable precompute karta hai.
    // [KYU ZAROORI HAI]: Runtime par mathematical calculations fast table lookups me convert ho jayein.
    // [KAISE CHALTA HAI]: Val 1 se start hota hai, har step par multiply by 2 aur polynomial reduction apply hoti hai.
    // [OUTPUT / SIDE EFFECT]: Lookup tables populate ho jaate hain class first-access par.
    // [VARIATIONS TO TRY]: Compile-time source generator se const static arrays generate kar sakte hain.
    static GaloisField256()
    {
        // [KYA KARTA HAI]: Initial field element value 1 set karta hai (generator alpha^0 = 1).
        // [KYU ZAROORI HAI]: Power sequence 0 index se initialize karna zaroori hai.
        // [KAISE CHALTA HAI]: Value 1 se start hoti hai.
        // [OUTPUT / SIDE EFFECT]: Local tracking variable init.
        // [VARIATIONS TO TRY]: Non-zero primitive root se initialize kar sakte hain.
        int val = 1;

        // [KYA KARTA HAI]: Loop run karta hai 0 se 254 tak (total 255 non-zero elements).
        // [KYU ZAROORI HAI]: GF(2^8) me 2^8 - 1 = 255 unique non-zero field elements hote hain.
        // [KAISE CHALTA HAI]: Iteration index 'i' power exponent represent karta hai.
        // [OUTPUT / SIDE EFFECT]: 255 elements log/exp maps generate hote hain.
        // [VARIATIONS TO TRY]: Loop unrolling for SIMD vector initialization.
        for (int i = 0; i < 255; i++)
        {
            // [KYA KARTA HAI]: ExpTable me current power ki byte value store karta hai.
            // [KYU ZAROORI HAI]: Anti-log lookup allow karta hai exponent to field element mapping.
            // [KAISE CHALTA HAI]: Index i par val cast karke assign karta hai.
            // [OUTPUT / SIDE EFFECT]: ExpTable[i] populated.
            // [VARIATIONS TO TRY]: Direct unsafe memory write.
            ExpTable[i] = (byte)val;

            // [KYA KARTA HAI]: Duplicated range [i + 255] me same value store karta hai.
            // [KYU ZAROORI HAI]: Multiplication sum (logA + logB) jab 255 exceed karta hai to modulo % 255 avoid hota hai.
            // [KAISE CHALTA HAI]: Higher array index mirror copy receive karta hai.
            // [OUTPUT / SIDE EFFECT]: ExpTable[i + 255] populated without modulo cost.
            // [VARIATIONS TO TRY]: Modulo check in Multiply function instead of 512 array.
            ExpTable[i + 255] = (byte)val;

            // [KYA KARTA HAI]: LogTable me current element ka log value i set karta hai.
            // [KYU ZAROORI HAI]: Element to exponent conversion enabling addition in log space.
            // [KAISE CHALTA HAI]: LogTable[val] = (byte)i mapping create hoti hai.
            // [OUTPUT / SIDE EFFECT]: LogTable initialized.
            // [VARIATIONS TO TRY]: Precomputed lookup struct.
            LogTable[val] = (byte)i;

            // [KYA KARTA HAI]: Generator 2 se multiply karta hai via bitwise left-shift by 1.
            // [KYU ZAROORI HAI]: Polynomial multiplication by 'x' bitwise shift equivalent hoti hai.
            // [KAISE CHALTA HAI]: Shift left val << 1.
            // [OUTPUT / SIDE EFFECT]: val doubles in size.
            // [VARIATIONS TO TRY]: Inline assembly shl.
            val <<= 1;

            // [KYA KARTA HAI]: Degree-8 overflow check karta hai (agar bit 8 set ho gaya ya val >= 256).
            // [KYU ZAROORI HAI]: GF(2^8) me polynomials degree strictly < 8 (max 255) hona mandatory hai.
            // [KAISE CHALTA HAI]: Bitmask checking (val & 0x100) != 0.
            // [OUTPUT / SIDE EFFECT]: Conditional reduction trigger.
            // [VARIATIONS TO TRY]: Branchless bit twiddling: val ^= (-(val >> 8) & PrimitivePolynomial).
            if ((val & 0x100) != 0)
            {
                // [KYA KARTA HAI]: Primitive irreducible polynomial se XOR reduction karta hai.
                // [KYU ZAROORI HAI]: Modulo arithmetic in Galois fields XOR se implement hoti hai.
                // [KAISE CHALTA HAI]: val ^= PrimitivePolynomial (0x11D).
                // [OUTPUT / SIDE EFFECT]: val stays inside [0, 255] boundary.
                // [VARIATIONS TO TRY]: Constant-time hardware AES-NI pclmulqdq instruction.
                val ^= PrimitivePolynomial;
            }
        }
    }

    // [KYA KARTA HAI]: Galois Field addition karta hai do bytes ka.
    // [KYU ZAROORI HAI]: Erasure coding parity matrix multiplication me accumulate karne ke liye addition zaroori hai.
    // [KAISE CHALTA HAI]: Characteristic-2 finite fields me addition bitwise XOR (a ^ b) hoti hai, without any carry bit.
    // [OUTPUT / SIDE EFFECT]: Returns sum byte in GF(2^8).
    // [VARIATIONS TO TRY]: Vectorized XOR using Vector128 or Vector256 for entire byte blocks.
    public static byte Add(byte a, byte b)
    {
        // [KYA KARTA HAI]: Bitwise XOR calculate karke byte return karta hai.
        // [KYU ZAROORI HAI]: GF(2^8) addition has no carry bits, exactly identical to XOR.
        // [KAISE CHALTA HAI]: a ^ b execute hota hai single CPU cycle me.
        // [OUTPUT / SIDE EFFECT]: Clean GF addition result.
        // [VARIATIONS TO TRY]: SIMD XOR intrinsics.
        return (byte)(a ^ b);
    }

    // [KYA KARTA HAI]: Galois Field subtraction karta hai do bytes ka.
    // [KYU ZAROORI HAI]: Gaussian elimination matrix inversion me row reduction ke liye subtraction zaroori hai.
    // [KAISE CHALTA HAI]: In GF(2^k), addition and subtraction are identical operations (a - b == a + b == a ^ b).
    // [OUTPUT / SIDE EFFECT]: Returns difference byte in GF(2^8).
    // [VARIATIONS TO TRY]: Direct call to Add(a, b).
    public static byte Subtract(byte a, byte b)
    {
        // [KYA KARTA HAI]: a ^ b evaluate karta hai.
        // [KYU ZAROORI HAI]: Subtraction in characteristic 2 fields is self-inverse.
        // [KAISE CHALTA HAI]: Single instruction bitwise XOR.
        // [OUTPUT / SIDE EFFECT]: Result identical to addition.
        // [VARIATIONS TO TRY]: None needed, mathematically exact.
        return (byte)(a ^ b);
    }

    // [KYA KARTA HAI]: GF(2^8) multiplication karta hai using log/exp table lookups.
    // [KYU ZAROORI HAI]: Matrix dot products calculate karne ke liye Reed-Solomon encoding aur decoding me high-speed multiply chahiye.
    // [KAISE CHALTA HAI]: a * b = ExpTable[LogTable[a] + LogTable[b]]; zero check handle karta hai base case.
    // [OUTPUT / SIDE EFFECT]: Multiplied byte in GF(2^8).
    // [VARIATIONS TO TRY]: SSSE3 pshufb SIMD parallel lookup for 32x throughput.
    public static byte Multiply(byte a, byte b)
    {
        // [KYA KARTA HAI]: Check karta hai agar koi bhi operand 0 ho.
        // [KYU ZAROORI HAI]: In finite fields, multiplication by 0 is always 0, aur Log(0) is mathematically undefined.
        // [KAISE CHALTA HAI]: Simple branch check (a == 0 || b == 0).
        // [OUTPUT / SIDE EFFECT]: Returns 0 immediately if either input is 0.
        // [VARIATIONS TO TRY]: Branchless zero masking using bitwise arithmetic.
        if (a == 0 || b == 0)
        {
            // [KYA KARTA HAI]: Zero return karta hai.
            // [KYU ZAROORI HAI]: Prevent accessing invalid LogTable[0].
            // [KAISE CHALTA HAI]: Direct literal return.
            // [OUTPUT / SIDE EFFECT]: Fast zero path.
            // [VARIATIONS TO TRY]: Return constant.
            return 0;
        }

        // [KYA KARTA HAI]: LogTable se exponents lookup karke sum nikalta hai aur ExpTable se result fetch karta hai.
        // [KYU ZAROORI HAI]: ExpTable 512 length ka hai, isliye log sum (max 254 + 254 = 508) bina modulo operator ke lookup ho jata hai.
        // [KAISE CHALTA HAI]: ExpTable[LogTable[a] + LogTable[b]] index access.
        // [OUTPUT / SIDE EFFECT]: Ultra-fast finite field multiplication result.
        // [VARIATIONS TO TRY]: Vectorized 16-byte chunk multiplication.
        return ExpTable[LogTable[a] + LogTable[b]];
    }

    // [KYA KARTA HAI]: GF(2^8) division karta hai: a / b.
    // [KYU ZAROORI HAI]: Gaussian elimination pivot normalization ke liye field division mandatory hai.
    // [KAISE CHALTA HAI]: a / b = ExpTable[(LogTable[a] - LogTable[b] + 255) % 255].
    // [OUTPUT / SIDE EFFECT]: Division quotient byte.
    // [VARIATIONS TO TRY]: Multiply by Inverse(b) instead of inline division.
    public static byte Divide(byte a, byte b)
    {
        // [KYA KARTA HAI]: Check karta hai divisor b zero toh nahi hai.
        // [KYU ZAROORI HAI]: Division by zero finite fields me bhi undefined hoti hai.
        // [KAISE CHALTA HAI]: Throws DivideByZeroException if b == 0.
        // [OUTPUT / SIDE EFFECT]: Exception guard.
        // [VARIATIONS TO TRY]: Return nullable byte or Result type.
        if (b == 0)
        {
            // [KYA KARTA HAI]: Throw exception on zero divisor.
            // [KYU ZAROORI HAI]: Invalid mathematical operation in finite field.
            // [KAISE CHALTA HAI]: Instantiates DivideByZeroException.
            // [OUTPUT / SIDE EFFECT]: Halts execution on illegal divide.
            // [VARIATIONS TO TRY]: Return specialized domain error code.
            throw new DivideByZeroException("Galois field division by zero is illegal.");
        }

        // [KYA KARTA HAI]: Check karta hai numerator a zero hai ya nahi.
        // [KYU ZAROORI HAI]: 0 / b is always 0.
        // [KAISE CHALTA HAI]: Returns 0 immediately.
        // [OUTPUT / SIDE EFFECT]: Fast zero path.
        // [VARIATIONS TO TRY]: None needed.
        if (a == 0)
        {
            // [KYA KARTA HAI]: Return 0.
            // [KYU ZAROORI HAI]: Numerator zero means output zero.
            // [KAISE CHALTA HAI]: Direct return 0.
            // [OUTPUT / SIDE EFFECT]: Zero byte returned.
            // [VARIATIONS TO TRY]: Inlined constant.
            return 0;
        }

        // [KYA KARTA HAI]: Log subtract karke + 255 wrap-around maintain karta hai.
        // [KYU ZAROORI HAI]: Negative index in ExpTable prevent karne ke liye 255 add karke modulo cycle hota hai.
        // [KAISE CHALTA HAI]: Subtraction in log domain: logIndex = (LogTable[a] - LogTable[b] + 255) % 255.
        // [OUTPUT / SIDE EFFECT]: Valid index mapped into ExpTable.
        // [VARIATIONS TO TRY]: Index lookup table for fast division.
        int logDiff = LogTable[a] - LogTable[b];

        // [KYA KARTA HAI]: Agar diff negative ho to + 255 add karta hai.
        // [KYU ZAROORI HAI]: Field is cyclic with period 255.
        // [KAISE CHALTA HAI]: Branch check for negative.
        // [OUTPUT / SIDE EFFECT]: logDiff guaranteed in [0, 254].
        // [VARIATIONS TO TRY]: (logDiff + 255) % 255.
        if (logDiff < 0)
        {
            // [KYA KARTA HAI]: logDiff += 255.
            // [KYU ZAROORI HAI]: Negative offset normalization.
            // [KAISE CHALTA HAI]: Integer addition.
            // [OUTPUT / SIDE EFFECT]: Normalized index.
            // [VARIATIONS TO TRY]: Bitwise mask.
            logDiff += 255;
        }

        // [KYA KARTA HAI]: ExpTable se value fetch karke cast karta hai.
        // [KYU ZAROORI HAI]: Returns final divided byte value.
        // [KAISE CHALTA HAI]: ExpTable[logDiff] lookup.
        // [OUTPUT / SIDE EFFECT]: Final division result.
        // [VARIATIONS TO TRY]: Inlined calculation.
        return ExpTable[logDiff];
    }

    // [KYA KARTA HAI]: Multiplicative inverse nikalta hai: 1 / a in GF(2^8).
    // [KYU ZAROORI HAI]: Matrix inversion me pivot diagonal ko 1 banane ke liye scalar inverse calculate karna padta hai.
    // [KAISE CHALTA HAI]: Inverse of 2^k is 2^(255 - k). ExpTable[255 - LogTable[a]] lookup karta hai.
    // [OUTPUT / SIDE EFFECT]: Inverse byte such that Multiply(a, inv) == 1.
    // [VARIATIONS TO TRY]: Precomputed 256-byte InverseTable for instant O(1) single fetch.
    public static byte Inverse(byte a)
    {
        // [KYA KARTA HAI]: Check karta hai input 0 toh nahi hai.
        // [KYU ZAROORI HAI]: Zero has no multiplicative inverse in any field.
        // [KAISE CHALTA HAI]: Throws DivideByZeroException if a == 0.
        // [OUTPUT / SIDE EFFECT]: Safety guard.
        // [VARIATIONS TO TRY]: Return 0 or Option<byte>.
        if (a == 0)
        {
            // [KYA KARTA HAI]: Throw exception.
            // [KYU ZAROORI HAI]: Division by zero equivalent.
            // [KAISE CHALTA HAI]: Instantiates exception.
            // [OUTPUT / SIDE EFFECT]: Protects mathematical consistency.
            // [VARIATIONS TO TRY]: Custom GaloisException.
            throw new DivideByZeroException("Zero has no multiplicative inverse in GF(2^8).");
        }

        // [KYA KARTA HAI]: 255 - log(a) ka exponent nikalta hai.
        // [KYU ZAROORI HAI]: a * a^-1 = 2^k * 2^(255 - k) = 2^255 = 1 in GF(2^8).
        // [KAISE CHALTA HAI]: ExpTable[255 - LogTable[a]] fetch.
        // [OUTPUT / SIDE EFFECT]: Multiplicative inverse byte.
        // [VARIATIONS TO TRY]: Table lookup.
        return ExpTable[255 - LogTable[a]];
    }
}
