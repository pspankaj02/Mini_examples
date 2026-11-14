namespace KiranaHub.ZkSnarks.Circuit;

/// <summary>
/// Mathematical Finite Field F_p (Galois Field GF(p)) arithmetic.
/// In zk-SNARKs, all arithmetic operations, polynomial evaluations, and constraint equations
/// are computed modulo a cryptographic prime p.
/// </summary>
public readonly struct FieldElement : IEquatable<FieldElement>
{
    // [KYA KARTA HAI]: Finite field ka prime modulus p define karta hai (Mersenne prime 2^31 - 1 = 2147483647).
    // [KYU ZAROORI HAI]: Modular arithmetic me division unique multiplicative inverse provide karti hai agar p prime ho.
    // [KAISE CHALTA HAI]: Const long value 2147483647L.
    // [OUTPUT / SIDE EFFECT]: Cryptographic prime boundary constant.
    // [VARIATIONS TO TRY]: 254-bit BN254 / Alt-bn128 curve prime (21888242871839275222246405745257275088548364400416034343698204186575808495617).
    public const long Modulus = 2147483647L;

    // [KYA KARTA HAI]: Finite field element ka canonical normalized value store karta hai (0 <= Value < Modulus).
    // [KYU ZAROORI HAI]: Modular arithmetic calculations ka fundamental unit.
    // [KAISE CHALTA HAI]: Constructor me modulo reduction se hamesha range [0, Modulus-1] me rehta hai.
    // [OUTPUT / SIDE EFFECT]: Immutable field value.
    // [VARIATIONS TO TRY]: Montgomery representation for faster multiplication.
    public readonly long Value;

    // [KYA KARTA HAI]: Finite field element constructor jo value ko prime modulus ke under normalize karta hai.
    // [KYU ZAROORI HAI]: Negative numbers aur overflowed values ko canonical field representation me convert karna.
    // [KAISE CHALTA HAI]: (val % Modulus + Modulus) % Modulus.
    // [OUTPUT / SIDE EFFECT]: Instantiated FieldElement.
    // [VARIATIONS TO TRY]: Unchecked assignment if value is already guaranteed in range.
    public FieldElement(long value)
    {
        long rem = value % Modulus;
        Value = rem < 0 ? rem + Modulus : rem;
    }

    // [KYA KARTA HAI]: Additive identity element (0) return karta hai.
    // [KYU ZAROORI HAI]: Arithmetic circuits me neutral zero addition ke liye.
    // [KAISE CHALTA HAI]: FieldElement(0).
    // [OUTPUT / SIDE EFFECT]: FieldElement representing 0.
    // [VARIATIONS TO TRY]: Static cached instance.
    public static FieldElement Zero => new(0);

    // [KYA KARTA HAI]: Multiplicative identity element (1) return karta hai.
    // [KYU ZAROORI HAI]: One-wire (constant 1) aur multiplication neutral unit ke liye.
    // [KAISE CHALTA HAI]: FieldElement(1).
    // [OUTPUT / SIDE EFFECT]: FieldElement representing 1.
    // [VARIATIONS TO TRY]: Static cached instance.
    public static FieldElement One => new(1);

    // [KYA KARTA HAI]: Do field elements ko add karta hai modulo p: (a + b) mod p.
    // [KYU ZAROORI HAI]: Linear combinations aur circuit gate inputs sum karne ke liye.
    // [KAISE CHALTA HAI]: (a.Value + b.Value) % Modulus.
    // [OUTPUT / SIDE EFFECT]: Resulting sum FieldElement.
    // [VARIATIONS TO TRY]: Conditional subtraction if sum >= Modulus.
    public static FieldElement operator +(FieldElement a, FieldElement b)
    {
        long res = a.Value + b.Value;
        return new FieldElement(res >= Modulus ? res - Modulus : res);
    }

    // [KYA KARTA HAI]: Do field elements ko subtract karta hai modulo p: (a - b) mod p.
    // [KYU ZAROORI HAI]: Differences (e.g. customerAge - minAge) compute karne ke liye.
    // [KAISE CHALTA HAI]: (a.Value - b.Value + Modulus) % Modulus.
    // [OUTPUT / SIDE EFFECT]: Resulting difference FieldElement.
    // [VARIATIONS TO TRY]: Underflow-safe branchless addition.
    public static FieldElement operator -(FieldElement a, FieldElement b)
    {
        long res = a.Value - b.Value;
        return new FieldElement(res < 0 ? res + Modulus : res);
    }

    // [KYA KARTA HAI]: Do field elements ko multiply karta hai modulo p: (a * b) mod p.
    // [KYU ZAROORI HAI]: zk-SNARK R1CS bilinear multiplication constraints: <A,s> * <B,s> = <C,s>.
    // [KAISE CHALTA HAI]: 64-bit integer product followed by modulo reduction.
    // [OUTPUT / SIDE EFFECT]: Resulting product FieldElement.
    // [VARIATIONS TO TRY]: 128-bit unsigned math (Math.BigMul) to prevent 64-bit overflow.
    public static FieldElement operator *(FieldElement a, FieldElement b)
    {
        long res = (long)(((ulong)a.Value * (ulong)b.Value) % (ulong)Modulus);
        return new FieldElement(res);
    }

    // [KYA KARTA HAI]: Modular division perform karta hai: a / b = a * b^(-1) mod p.
    // [KYU ZAROORI HAI]: Inversion aur quotient calculations in finite fields.
    // [KAISE CHALTA HAI]: Multiplies a with multiplicative inverse of b.
    // [OUTPUT / SIDE EFFECT]: Quotient FieldElement.
    // [VARIATIONS TO TRY]: Division by zero check with custom cryptographic exception.
    public static FieldElement operator /(FieldElement a, FieldElement b)
    {
        if (b.Value == 0)
        {
            throw new DivideByZeroException("Cannot divide by Zero in Finite Field F_p.");
        }
        return a * b.Inverse();
    }

    // [KYA KARTA HAI]: Unary negation: -a = (p - a) mod p.
    // [KYU ZAROORI HAI]: Inverting signs in constraint polynomials.
    // [KAISE CHALTA HAI]: Modulus - Value.
    // [OUTPUT / SIDE EFFECT]: Negated FieldElement.
    // [VARIATIONS TO TRY]: Bitwise inversion for binary fields GF(2^n).
    public static FieldElement operator -(FieldElement a)
    {
        return a.Value == 0 ? Zero : new FieldElement(Modulus - a.Value);
    }

    // [KYA KARTA HAI]: Multiplicative inverse a^(-1) compute karta hai using Fermat's Little Theorem.
    // [KYU ZAROORI HAI]: Field me division possible banane ke liye (a^(p-2) = a^(-1) mod p).
    // [KAISE CHALTA HAI]: Modular exponentiation with power (Modulus - 2).
    // [OUTPUT / SIDE EFFECT]: Inverse FieldElement such that a * a^(-1) == 1.
    // [VARIATIONS TO TRY]: Extended Euclidean Algorithm (EEA) for faster inversion.
    public FieldElement Inverse()
    {
        if (Value == 0)
        {
            throw new InvalidOperationException("Zero has no multiplicative inverse in Finite Field.");
        }
        return ModPow(this, Modulus - 2);
    }

    // [KYA KARTA HAI]: Modular exponentiation (base^exp mod p) O(log exp) time me calculate karta hai.
    // [KYU ZAROORI HAI]: Fast inverse aur cryptographic powers compute karne ke liye.
    // [KAISE CHALTA HAI]: Square-and-multiply binary exponentiation algorithm.
    // [OUTPUT / SIDE EFFECT]: Resulting power FieldElement.
    // [VARIATIONS TO TRY]: Montgomery ladder to protect against timing attacks.
    public static FieldElement ModPow(FieldElement baseVal, long exponent)
    {
        FieldElement result = One;
        FieldElement current = baseVal;
        long exp = exponent;

        while (exp > 0)
        {
            if ((exp & 1) == 1)
            {
                result *= current;
            }
            current *= current;
            exp >>= 1;
        }

        return result;
    }

    // [KYA KARTA HAI]: Equality comparison implement karta hai.
    // [KYU ZAROORI HAI]: Circuit constraints verification: <A,s> * <B,s> == <C,s>.
    // [KAISE CHALTA HAI]: Value equality check.
    // [OUTPUT / SIDE EFFECT]: Boolean true if equal.
    // [VARIATIONS TO TRY]: Constant-time equality check to prevent side-channel timing leaks.
    public bool Equals(FieldElement other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is FieldElement other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(FieldElement a, FieldElement b) => a.Value == b.Value;
    public static bool operator !=(FieldElement a, FieldElement b) => a.Value != b.Value;

    public override string ToString() => Value.ToString();

    public static implicit operator FieldElement(long val) => new(val);
    public static implicit operator FieldElement(int val) => new(val);
}
