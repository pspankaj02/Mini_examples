// [KYA KARTA HAI]: Cryptography and Math namespaces import karta hai
// [KYU ZAROORI HAI]: RandomNumberGenerator, SHA256 aur Polynomial ring arithmetic use karne ke liye
// [KAISE CHALTA HAI]: Namespaces mapped
// [OUTPUT / SIDE EFFECT]: Cryptographic math available
// [VARIATIONS TO TRY]: System.Security.Cryptography.Algorithms
using System.Security.Cryptography;
using KiranaHub.PostQuantumCrypto.Math;

// [KYA KARTA HAI]: Kyber ML-KEM namespace define karta hai
// [KYU ZAROORI HAI]: Module-LWE Key Encapsulation Mechanism components ko organize karne ke liye
// [KAISE CHALTA HAI]: Project Kyber directory map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.PostQuantumCrypto.Kyber
// [VARIATIONS TO TRY]: KiranaHub.Cryptography.Kem
namespace KiranaHub.PostQuantumCrypto.Kyber;

// [KYA KARTA HAI]: ML-KEM Public Key data structure (Matrix A, Vector t)
// [KYU ZAROORI HAI]: Encryption / Encapsulation perform karne ke liye public parameters hold karta hai
// [KAISE CHALTA HAI]: 2x2 matrix of Polynomials A and 2-element vector t
// [OUTPUT / SIDE EFFECT]: Public key container
// [VARIATIONS TO TRY]: Compressed byte encoding
public class MlKemPublicKey
{
    // [KYA KARTA HAI]: 2x2 Public matrix $A \in R_q^{2 \times 2}$
    // [KYU ZAROORI HAI]: Lattice basis transformation ke liye
    // [KAISE CHALTA HAI]: 2D array of Polynomials
    // [OUTPUT / SIDE EFFECT]: Matrix A stored
    // [VARIATIONS TO TRY]: Reconstructed from 32-byte seed $\rho$
    public Polynomial[,] A { get; } = new Polynomial[2, 2];

    // [KYA KARTA HAI]: Public vector $t \in R_q^2$ where $t = A \cdot s + e$
    // [KYU ZAROORI HAI]: Secret vector ko error noise ke sath blind karke public key create karne ke liye
    // [KAISE CHALTA HAI]: 2-element Polynomial array
    // [OUTPUT / SIDE EFFECT]: Vector t stored
    // [VARIATIONS TO TRY]: Byte array serialization
    public Polynomial[] T { get; } = new Polynomial[2];
}

// [KYA KARTA HAI]: ML-KEM Secret Key data structure (Vector s)
// [KYU ZAROORI HAI]: Decapsulation ke dauran shared secret recover karne ke liye
// [KAISE CHALTA HAI]: 2-element secret polynomial vector $s \in R_q^2$
// [OUTPUT / SIDE EFFECT]: Private key container
// [VARIATIONS TO TRY]: Protected memory enclave
public class MlKemPrivateKey
{
    // [KYA KARTA HAI]: Secret vector $s \in R_q^2$ sampled from centered binomial noise
    // [KYU ZAROORI HAI]: Decapsulation formula me $v - s^T u$ compute karne ke liye
    // [KAISE CHALTA HAI]: 2-element Polynomial array
    // [OUTPUT / SIDE EFFECT]: Secret vector s stored
    // [VARIATIONS TO TRY]: Protected memory enclave
    public Polynomial[] S { get; } = new Polynomial[2];
}

// [KYA KARTA HAI]: ML-KEM Ciphertext container (Vector u, Polynomial v)
// [KYU ZAROORI HAI]: Encapsulated key exchange payload wire par transmit karne ke liye
// [KAISE CHALTA HAI]: Vector $u \in R_q^2$ and single polynomial $v \in R_q$
// [OUTPUT / SIDE EFFECT]: Ciphertext payload
// [VARIATIONS TO TRY]: Lossy coefficient compression
public class MlKemCiphertext
{
    // [KYA KARTA HAI]: Ciphertext vector $u = A^T \cdot r + e_1 \pmod q$
    // [KYU ZAROORI HAI]: Ephemeral randomness transport ke liye
    // [KAISE CHALTA HAI]: 2-element Polynomial array
    // [OUTPUT / SIDE EFFECT]: Vector u stored
    // [VARIATIONS TO TRY]: Byte serialization
    public Polynomial[] U { get; } = new Polynomial[2];

    // [KYA KARTA HAI]: Ciphertext polynomial $v = t^T \cdot r + e_2 + \lceil q/2 \rceil \cdot m \pmod q$
    // [KYU ZAROORI HAI]: Encrypted 256-bit message carry karne ke liye
    // [KAISE CHALTA HAI]: Single Polynomial instance
    // [OUTPUT / SIDE EFFECT]: Polynomial v stored
    // [VARIATIONS TO TRY]: 4-bit compression
    public Polynomial V { get; set; } = new();
}

// [KYA KARTA HAI]: FIPS 203 ML-KEM (Crystals-Kyber-512) Key Encapsulation Engine class
// [KYU ZAROORI HAI]: Shor's quantum algorithm immune symmetric key exchange provide karne ke liye
// [KAISE CHALTA HAI]: Module Learning With Errors (M-LWE) lattice mathematics execute karta hai
// [OUTPUT / SIDE EFFECT]: 256-bit quantum-secure shared secret negotiated
// [VARIATIONS TO TRY]: Kyber-768 ($k=3$) or Kyber-1024 ($k=4$)
public static class MlKemEngine
{
    // [KYA KARTA HAI]: Half modulus constant $\lfloor q/2 \rfloor = 1664$
    // [KYU ZAROORI HAI]: Binary bits (0 and 1) ko lattice field me scale karne ke liye
    // [KAISE CHALTA HAI]: Constant integer $3329 / 2 = 1664$
    // [OUTPUT / SIDE EFFECT]: Bit scaling factor
    // [VARIATIONS TO TRY]: Rounding threshold tune
    private const int HalfQ = Polynomial.Q / 2; // 1664

    // [KYA KARTA HAI]: ML-KEM Asymmetric Keypair Generator (KeyGen)
    // [KYU ZAROORI HAI]: Public key matrix $A$, public vector $t$, aur secret vector $s$ create karne ke liye
    // [KAISE CHALTA HAI]: Samples $A \sim U(R_q)$, samples $s, e \sim \chi_\eta$, computes $t = A s + e$
    // [OUTPUT / SIDE EFFECT]: Returns (PublicKey, PrivateKey)
    // [VARIATIONS TO TRY]: Deterministic KeyGen from seed
    public static (MlKemPublicKey PublicKey, MlKemPrivateKey PrivateKey) KeyGen()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        MlKemPublicKey pk = new();
        MlKemPrivateKey sk = new();

        // 1. Generate uniform random public matrix A (2x2)
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                pk.A[i, j] = Polynomial.SampleUniform(rng);
            }
        }

        // 2. Sample secret vector s (2 elements) and error vector e (2 elements)
        sk.S[0] = Polynomial.SampleCenteredBinomial(rng, 2);
        sk.S[1] = Polynomial.SampleCenteredBinomial(rng, 2);

        Polynomial e0 = Polynomial.SampleCenteredBinomial(rng, 2);
        Polynomial e1 = Polynomial.SampleCenteredBinomial(rng, 2);

        // 3. Compute t = A * s + e
        // t0 = A[0,0]*s[0] + A[0,1]*s[1] + e0
        Polynomial t0 = Polynomial.Add(
            Polynomial.Add(Polynomial.Multiply(pk.A[0, 0], sk.S[0]), Polynomial.Multiply(pk.A[0, 1], sk.S[1])),
            e0
        );

        // t1 = A[1,0]*s[0] + A[1,1]*s[1] + e1
        Polynomial t1 = Polynomial.Add(
            Polynomial.Add(Polynomial.Multiply(pk.A[1, 0], sk.S[0]), Polynomial.Multiply(pk.A[1, 1], sk.S[1])),
            e1
        );

        pk.T[0] = t0;
        pk.T[1] = t1;

        return (pk, sk);
    }

    // [KYA KARTA HAI]: ML-KEM Key Encapsulation (Encapsulate)
    // [KYU ZAROORI HAI]: Random 256-bit symmetric shared secret create aur encrypt karne ke liye
    // [KAISE CHALTA HAI]: Generates message m, computes ciphertext (u, v) using pk, hashes m to produce shared key K
    // [OUTPUT / SIDE EFFECT]: Returns (Ciphertext, SharedSecretBytes)
    // [VARIATIONS TO TRY]: Attach authenticated additional data (AAD)
    public static (MlKemCiphertext Ciphertext, byte[] SharedSecret) Encapsulate(MlKemPublicKey pk)
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        MlKemCiphertext ct = new();

        // 1. Generate 32-byte (256-bit) random secret message m
        byte[] mBytes = new byte[32];
        rng.GetBytes(mBytes);

        // 2. Convert 256 bits of m into scaled polynomial M(X) where bit 1 -> HalfQ (1664)
        Polynomial mPoly = new();
        for (int i = 0; i < 256; i++)
        {
            int byteIdx = i / 8;
            int bitIdx = i % 8;
            int bit = (mBytes[byteIdx] >> bitIdx) & 1;
            mPoly.Coeffs[i] = bit == 1 ? HalfQ : 0;
        }

        // 3. Sample ephemeral randomness vector r (2 elements) and noise e1, e2
        Polynomial r0 = Polynomial.SampleCenteredBinomial(rng, 2);
        Polynomial r1 = Polynomial.SampleCenteredBinomial(rng, 2);

        Polynomial e1_0 = Polynomial.SampleCenteredBinomial(rng, 2);
        Polynomial e1_1 = Polynomial.SampleCenteredBinomial(rng, 2);
        Polynomial e2 = Polynomial.SampleCenteredBinomial(rng, 2);

        // 4. Compute u = A^T * r + e1
        // u0 = A[0,0]*r0 + A[1,0]*r1 + e1_0
        ct.U[0] = Polynomial.Add(
            Polynomial.Add(Polynomial.Multiply(pk.A[0, 0], r0), Polynomial.Multiply(pk.A[1, 0], r1)),
            e1_0
        );

        // u1 = A[0,1]*r0 + A[1,1]*r1 + e1_1
        ct.U[1] = Polynomial.Add(
            Polynomial.Add(Polynomial.Multiply(pk.A[0, 1], r0), Polynomial.Multiply(pk.A[1, 1], r1)),
            e1_1
        );

        // 5. Compute v = t^T * r + e2 + M
        // v = t[0]*r0 + t[1]*r1 + e2 + M
        Polynomial tr0 = Polynomial.Multiply(pk.T[0], r0);
        Polynomial tr1 = Polynomial.Multiply(pk.T[1], r1);
        ct.V = Polynomial.Add(
            Polynomial.Add(Polynomial.Add(tr0, tr1), e2),
            mPoly
        );

        // 6. Compute 256-bit symmetric shared secret: K = SHA256(m)
        byte[] sharedSecret = SHA256.HashData(mBytes);

        return (ct, sharedSecret);
    }

    // [KYA KARTA HAI]: ML-KEM Key Decapsulation (Decapsulate)
    // [KYU ZAROORI HAI]: Ciphertext (u, v) aur private key s se identical 256-bit shared secret recover karne ke liye
    // [KAISE CHALTA HAI]: Computes noisy $M' = v - s^T u \pmod q$, rounds noise away, reconstructs m, hashes to K
    // [OUTPUT / SIDE EFFECT]: Returns exact identical 32-byte shared secret
    // [VARIATIONS TO TRY]: Constant-time rejection on CCA-2 mismatch
    public static byte[] Decapsulate(MlKemPrivateKey sk, MlKemCiphertext ct)
    {
        // 1. Compute s^T * u = s[0]*u[0] + s[1]*u[1]
        Polynomial su0 = Polynomial.Multiply(sk.S[0], ct.U[0]);
        Polynomial su1 = Polynomial.Multiply(sk.S[1], ct.U[1]);
        Polynomial su = Polynomial.Add(su0, su1);

        // 2. Compute noisy difference: M' = v - (s^T * u) mod Q
        // Since v = s^T u + noise + M, M' = noise + M
        Polynomial noisyM = Polynomial.Subtract(ct.V, su);

        // 3. Round noise away: if coefficient is closer to HalfQ (1664) than 0/Q, bit is 1, else 0
        byte[] recoveredBytes = new byte[32];
        for (int i = 0; i < 256; i++)
        {
            int coeff = noisyM.Coeffs[i];
            // Distance to 0 or Q vs distance to HalfQ (1664)
            int distToZero = System.Math.Min(coeff, Polynomial.Q - coeff);
            int distToHalfQ = System.Math.Abs(coeff - HalfQ);

            int bit = distToHalfQ < distToZero ? 1 : 0;
            if (bit == 1)
            {
                int byteIdx = i / 8;
                int bitIdx = i % 8;
                recoveredBytes[byteIdx] |= (byte)(1 << bitIdx);
            }
        }

        // 4. Compute 256-bit symmetric key: K' = SHA256(m')
        return SHA256.HashData(recoveredBytes);
    }
}
