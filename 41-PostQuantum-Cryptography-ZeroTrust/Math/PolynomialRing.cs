// [KYA KARTA HAI]: Security and Cryptography namespaces import karta hai
// [KYU ZAROORI HAI]: RandomNumberGenerator aur SHA256 hashing use karne ke liye
// [KAISE CHALTA HAI]: System.Security.Cryptography namespace mapped
// [OUTPUT / SIDE EFFECT]: Cryptographic primitives available
// [VARIATIONS TO TRY]: System.Security.Cryptography.Cng
using System.Security.Cryptography;

// [KYA KARTA HAI]: Post-Quantum mathematical ring namespace define karta hai
// [KYU ZAROORI HAI]: Lattice-based polynomial arithmetic ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project math directory map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.PostQuantumCrypto.Math
// [VARIATIONS TO TRY]: KiranaHub.Cryptography.Lattices
namespace KiranaHub.PostQuantumCrypto.Math;

// [KYA KARTA HAI]: Lattice Polynomial Ring $R_q = \mathbb{Z}_q[X] / (X^n + 1)$ implementation class
// [KYU ZAROORI HAI]: ML-KEM (Kyber) aur ML-DSA (Dilithium) post-quantum lattice cryptography ke core arithmetic ke liye
// [KAISE CHALTA HAI]: 256 coefficients modulo $q=3329$ maintain karta hai aur negacyclic multiplication ($X^{256} \equiv -1$) execute karta hai
// [OUTPUT / SIDE EFFECT]: Foundation for quantum-resistant encryption and digital signatures
// [VARIATIONS TO TRY]: Hardware AVX2 vectorized polynomial multiplication
public class Polynomial
{
    // [KYA KARTA HAI]: Polynomial degree parameter $n = 256$
    // [KYU ZAROORI HAI]: Kyber aur Dilithium standard polynomial dimension ke liye
    // [KAISE CHALTA HAI]: Constant integer 256
    // [OUTPUT / SIDE EFFECT]: 256 dimensions
    // [VARIATIONS TO TRY]: n = 512 for higher security levels
    public const int N = 256;

    // [KYA KARTA HAI]: Primary finite field modulus $q = 3329$
    // [KYU ZAROORI HAI]: FIPS 203 (ML-KEM / Crystals-Kyber) standard prime modulus
    // [KAISE CHALTA HAI]: Prime integer $q = 13 \times 256 + 1 = 3329$
    // [OUTPUT / SIDE EFFECT]: Field size $\mathbb{Z}_q$ defined
    // [VARIATIONS TO TRY]: q = 8380417 for Dilithium
    public const int Q = 3329;

    // [KYA KARTA HAI]: 256 integer coefficients array
    // [KYU ZAROORI HAI]: Polynomial terms $a_0 + a_1 X + \dots + a_{255} X^{255}$ hold karne ke liye
    // [KAISE CHALTA HAI]: int[256] array where each element is in $[0, Q-1]$
    // [OUTPUT / SIDE EFFECT]: Polynomial state stored
    // [VARIATIONS TO TRY]: short[256] to conserve memory
    public readonly int[] Coeffs = new int[N];

    // [KYA KARTA HAI]: Polynomial default constructor
    // [KYU ZAROORI HAI]: Zero polynomial initialize karne ke liye
    // [KAISE CHALTA HAI]: Allocates Coeffs array with zeros
    // [OUTPUT / SIDE EFFECT]: Zero polynomial
    // [VARIATIONS TO TRY]: Pass initial coefficient array
    public Polynomial()
    {
    }

    // [KYA KARTA HAI]: Positive modulo reduction helper
    // [KYU ZAROORI HAI]: Negative numbers ko $[0, Q-1]$ range me cleanly wrap karne ke liye
    // [KAISE CHALTA HAI]: ((val % Q) + Q) % Q
    // [OUTPUT / SIDE EFFECT]: Value strictly in $[0, Q-1]$
    // [VARIATIONS TO TRY]: Montgomery reduction
    public static int ModQ(int val)
    {
        int r = val % Q;
        return r < 0 ? r + Q : r;
    }

    // [KYA KARTA HAI]: Polynomial addition modulo $q$
    // [KYU ZAROORI HAI]: $C(X) = A(X) + B(X) \pmod q$ calculate karne ke liye
    // [KAISE CHALTA HAI]: Coefficient-wise addition followed by ModQ
    // [OUTPUT / SIDE EFFECT]: New Polynomial instance with sum
    // [VARIATIONS TO TRY]: In-place mutation AddInPlace
    public static Polynomial Add(Polynomial a, Polynomial b)
    {
        Polynomial result = new();
        for (int i = 0; i < N; i++)
        {
            result.Coeffs[i] = ModQ(a.Coeffs[i] + b.Coeffs[i]);
        }
        return result;
    }

    // [KYA KARTA HAI]: Polynomial subtraction modulo $q$
    // [KYU ZAROORI HAI]: $C(X) = A(X) - B(X) \pmod q$ calculate karne ke liye
    // [KAISE CHALTA HAI]: Coefficient-wise subtraction followed by ModQ
    // [OUTPUT / SIDE EFFECT]: New Polynomial instance with difference
    // [VARIATIONS TO TRY]: In-place subtraction
    public static Polynomial Subtract(Polynomial a, Polynomial b)
    {
        Polynomial result = new();
        for (int i = 0; i < N; i++)
        {
            result.Coeffs[i] = ModQ(a.Coeffs[i] - b.Coeffs[i]);
        }
        return result;
    }

    // [KYA KARTA HAI]: Negacyclic polynomial multiplication in quotient ring $\mathbb{Z}_q[X] / (X^{256} + 1)$
    // [KYU ZAROORI HAI]: Lattice ring multiplication execute karne ke liye where $X^{256} \equiv -1$
    // [KAISE CHALTA HAI]: Convolution jisme powers $\ge 256$ subtract hoti hain: $X^{i+j} \equiv -X^{i+j-256}$
    // [OUTPUT / SIDE EFFECT]: Multiplied polynomial in ring $R_q$
    // [VARIATIONS TO TRY]: Number Theoretic Transform (NTT) $O(N \log N)$
    public static Polynomial Multiply(Polynomial a, Polynomial b)
    {
        Polynomial result = new();
        // Negacyclic schoolbook convolution
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                long prod = (long)a.Coeffs[i] * b.Coeffs[j];
                int targetIdx = i + j;
                if (targetIdx < N)
                {
                    result.Coeffs[targetIdx] = (int)((result.Coeffs[targetIdx] + prod) % Q);
                }
                else
                {
                    // X^256 = -1, so wrap around with subtraction
                    result.Coeffs[targetIdx - N] = (int)((result.Coeffs[targetIdx - N] - prod) % Q);
                }
            }
        }

        // Final normalization modulo Q
        for (int i = 0; i < N; i++)
        {
            result.Coeffs[i] = ModQ(result.Coeffs[i]);
        }

        return result;
    }

    // [KYA KARTA HAI]: Uniformly random polynomial sample karta hai
    // [KYU ZAROORI HAI]: Public key matrix $A$ generate karne ke liye
    // [KAISE CHALTA HAI]: Cryptographic RNG se coefficients in $[0, Q-1]$ sample karta hai
    // [OUTPUT / SIDE EFFECT]: Uniform random polynomial
    // [VARIATIONS TO TRY]: SHAKE-128 XOF rejection sampler
    public static Polynomial SampleUniform(RandomNumberGenerator rng)
    {
        Polynomial p = new();
        byte[] bytes = new byte[2];
        for (int i = 0; i < N; i++)
        {
            while (true)
            {
                rng.GetBytes(bytes);
                int val = (bytes[0] | (bytes[1] << 8)) & 0x0FFF; // 12-bit sample
                if (val < Q)
                {
                    p.Coeffs[i] = val;
                    break;
                }
            }
        }
        return p;
    }

    // [KYA KARTA HAI]: Centered Binomial Distribution $\chi_\eta$ ($\eta = 2$) error polynomial sample karta hai
    // [KYU ZAROORI HAI]: Learning With Errors (LWE) secret aur error vectors generate karne ke liye
    // [KAISE CHALTA HAI]: $a - b$ where $a, b \sim \text{Binomial}(\eta, 1/2)$, producing small noise near zero
    // [OUTPUT / SIDE EFFECT]: Small-noise error polynomial
    // [VARIATIONS TO TRY]: $\eta = 3$ for Kyber-768
    public static Polynomial SampleCenteredBinomial(RandomNumberGenerator rng, int eta = 2)
    {
        Polynomial p = new();
        byte[] buffer = new byte[eta * 2];

        for (int i = 0; i < N; i++)
        {
            rng.GetBytes(buffer);
            int a = 0;
            int b = 0;
            for (int j = 0; j < eta; j++)
            {
                a += (buffer[j] & 1);
                b += (buffer[eta + j] & 1);
            }
            int diff = a - b; // in [-eta, eta]
            p.Coeffs[i] = ModQ(diff);
        }

        return p;
    }
}
