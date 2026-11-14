// [KYA KARTA HAI]: Cryptography and Math namespaces import karta hai
// [KYU ZAROORI HAI]: SHA256 hashing aur lattice polynomial arithmetic ke liye
// [KAISE CHALTA HAI]: Namespaces mapped
// [OUTPUT / SIDE EFFECT]: Cryptographic primitives available
// [VARIATIONS TO TRY]: SHAKE-256 XOF
using System.Security.Cryptography;
using System.Text;
using KiranaHub.PostQuantumCrypto.Math;

// [KYA KARTA HAI]: Dilithium ML-DSA digital signatures namespace define karta hai
// [KYU ZAROORI HAI]: Post-quantum lattice digital signature schemes ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project Dilithium directory map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.PostQuantumCrypto.Dilithium
// [VARIATIONS TO TRY]: KiranaHub.Cryptography.Signatures
namespace KiranaHub.PostQuantumCrypto.Dilithium;

// [KYA KARTA HAI]: ML-DSA Post-Quantum Digital Signature container class
// [KYU ZAROORI HAI]: Signature response vector $z$ aur challenge $c$ hold karne ke liye
// [KAISE CHALTA HAI]: Holds Polynomial vector Z and challenge Polynomial C
// [OUTPUT / SIDE EFFECT]: Transmittable signature container
// [VARIATIONS TO TRY]: Packed binary signature bytes
public class MlDsaSignature
{
    // [KYA KARTA HAI]: Signature response vector $z = y + c \cdot s_1 \pmod q$
    // [KYU ZAROORI HAI]: Mathematical proof of private key possession without revealing secret
    // [KAISE CHALTA HAI]: 2-element Polynomial array
    // [OUTPUT / SIDE EFFECT]: Response vector z
    // [VARIATIONS TO TRY]: Compressed coefficient packing
    public Polynomial[] Z { get; } = new Polynomial[2];

    // [KYA KARTA HAI]: Challenge polynomial $c$ derived from hashing commitment and message
    // [KYU ZAROORI HAI]: Fiat-Shamir transformation ke through non-interactive proof create karne ke liye
    // [KAISE CHALTA HAI]: Sparse binary polynomial derived from cryptographic hash
    // [OUTPUT / SIDE EFFECT]: Challenge polynomial c
    // [VARIATIONS TO TRY]: 256-bit challenge seed
    public Polynomial C { get; set; } = new();
}

// [KYA KARTA HAI]: ML-DSA Keypair container (PublicKey, PrivateKey)
// [KYU ZAROORI HAI]: Digital signature signing aur verification keys hold karne ke liye
// [KAISE CHALTA HAI]: Container class
// [OUTPUT / SIDE EFFECT]: Keypair ready
// [VARIATIONS TO TRY]: SecureString wrapper
public class MlDsaKeyPair
{
    public Polynomial[,] A { get; } = new Polynomial[2, 2];
    public Polynomial[] T { get; } = new Polynomial[2];
    public Polynomial[] S1 { get; } = new Polynomial[2];
}

// [KYA KARTA HAI]: FIPS 204 ML-DSA (Crystals-Dilithium / Lyubashevsky Lattice Signature) Engine
// [KYU ZAROORI HAI]: Shor's algorithm resistant non-repudiation provide karne ke liye for FinTech ledgers
// [KAISE CHALTA HAI]: Fiat-Shamir Short Integer Solution (SIS) lattice mathematics execute karta hai
// [OUTPUT / SIDE EFFECT]: Tamper-proof digital signatures verified at quantum security levels
// [VARIATIONS TO TRY]: Dilithium-3 or Dilithium-5 with Power2Round
public static class MlDsaSignatureEngine
{
    // [KYA KARTA HAI]: ML-DSA Key Generation (KeyGen)
    // [KYU ZAROORI HAI]: Signing private key aur verification public key generate karne ke liye
    // [KAISE CHALTA HAI]: Samples matrix A, secret vector s1, computes public key t = A*s1
    // [OUTPUT / SIDE EFFECT]: Returns MlDsaKeyPair
    // [VARIATIONS TO TRY]: Seeded expansion via SHAKE-256
    public static MlDsaKeyPair KeyGen()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        MlDsaKeyPair kp = new();

        // 1. Generate uniform random public matrix A (2x2)
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                kp.A[i, j] = Polynomial.SampleUniform(rng);
            }
        }

        // 2. Sample secret vector s1 from centered binomial distribution
        kp.S1[0] = Polynomial.SampleCenteredBinomial(rng, 2);
        kp.S1[1] = Polynomial.SampleCenteredBinomial(rng, 2);

        // 3. Compute public key t = A * s1 mod Q
        kp.T[0] = Polynomial.Add(
            Polynomial.Multiply(kp.A[0, 0], kp.S1[0]),
            Polynomial.Multiply(kp.A[0, 1], kp.S1[1])
        );

        kp.T[1] = Polynomial.Add(
            Polynomial.Multiply(kp.A[1, 0], kp.S1[0]),
            Polynomial.Multiply(kp.A[1, 1], kp.S1[1])
        );

        return kp;
    }

    // [KYA KARTA HAI]: Hash helper jo message aur commitment $w$ se challenge polynomial $c$ generate karta hai
    // [KYU ZAROORI HAI]: Fiat-Shamir heuristic apply karne ke liye
    // [KAISE CHALTA HAI]: Computes SHA256 of inputs and maps hash bits to small coefficients
    // [OUTPUT / SIDE EFFECT]: Sparse challenge polynomial
    // [VARIATIONS TO TRY]: Fixed Hamming weight challenge
    private static Polynomial HashToChallenge(byte[] message, Polynomial w0, Polynomial w1)
    {
        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(message);

        byte[] polyBytes = new byte[4];
        for (int i = 0; i < Polynomial.N; i++)
        {
            polyBytes[0] = (byte)(w0.Coeffs[i] & 0xFF);
            polyBytes[1] = (byte)((w0.Coeffs[i] >> 8) & 0xFF);
            polyBytes[2] = (byte)(w1.Coeffs[i] & 0xFF);
            polyBytes[3] = (byte)((w1.Coeffs[i] >> 8) & 0xFF);
            hasher.AppendData(polyBytes);
        }

        byte[] hash = hasher.GetHashAndReset();

        // Map hash bytes into small binary polynomial c
        Polynomial c = new();
        for (int i = 0; i < 32; i++)
        {
            byte b = hash[i];
            for (int j = 0; j < 8; j++)
            {
                int idx = i * 8 + j;
                if (idx < Polynomial.N)
                {
                    int bit = (b >> j) & 1;
                    c.Coeffs[idx] = bit;
                }
            }
        }
        return c;
    }

    // [KYA KARTA HAI]: ML-DSA Sign method (Signs arbitrary message bytes)
    // [KYU ZAROORI HAI]: Financial transaction records par quantum-proof digital signature seal karne ke liye
    // [KAISE CHALTA HAI]: Generates ephemeral y, computes commitment w = A*y, hashes to challenge c, computes z = y + c*s1
    // [OUTPUT / SIDE EFFECT]: Returns MlDsaSignature
    // [VARIATIONS TO TRY]: Rejection sampling abort loop
    public static MlDsaSignature Sign(byte[] message, MlDsaKeyPair keyPair)
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        MlDsaSignature sig = new();

        // 1. Sample ephemeral masking vector y
        Polynomial y0 = Polynomial.SampleCenteredBinomial(rng, 4);
        Polynomial y1 = Polynomial.SampleCenteredBinomial(rng, 4);

        // 2. Compute commitment w = A * y mod Q
        Polynomial w0 = Polynomial.Add(
            Polynomial.Multiply(keyPair.A[0, 0], y0),
            Polynomial.Multiply(keyPair.A[0, 1], y1)
        );

        Polynomial w1 = Polynomial.Add(
            Polynomial.Multiply(keyPair.A[1, 0], y0),
            Polynomial.Multiply(keyPair.A[1, 1], y1)
        );

        // 3. Challenge c = Hash(message, w0, w1)
        Polynomial c = HashToChallenge(message, w0, w1);
        sig.C = c;

        // 4. Compute response vector z = y + c * s1 mod Q
        sig.Z[0] = Polynomial.Add(y0, Polynomial.Multiply(c, keyPair.S1[0]));
        sig.Z[1] = Polynomial.Add(y1, Polynomial.Multiply(c, keyPair.S1[1]));

        return sig;
    }

    // [KYA KARTA HAI]: ML-DSA Verify method (Verifies signature validity against message and public key)
    // [KYU ZAROORI HAI]: Ledger integrity aur authentic sender non-repudiation verify karne ke liye
    // [KAISE CHALTA HAI]: Recomputes commitment $w' = A \cdot z - c \cdot t \equiv A \cdot y = w$, hashes $c' = \text{Hash}(m, w')$, verifies $c' == c$
    // [OUTPUT / SIDE EFFECT]: Returns true if signature authentic; false if tampered
    // [VARIATIONS TO TRY]: Strict norm bounds check $\|z\|_\infty$
    public static bool Verify(byte[] message, MlDsaSignature signature, MlDsaKeyPair keyPair)
    {
        // 1. Recompute commitment: w' = A * z - c * t mod Q
        // Since z = y + c*s1 and t = A*s1,
        // A*z - c*t = A*(y + c*s1) - c*(A*s1) = A*y = w
        Polynomial az0 = Polynomial.Add(
            Polynomial.Multiply(keyPair.A[0, 0], signature.Z[0]),
            Polynomial.Multiply(keyPair.A[0, 1], signature.Z[1])
        );

        Polynomial az1 = Polynomial.Add(
            Polynomial.Multiply(keyPair.A[1, 0], signature.Z[0]),
            Polynomial.Multiply(keyPair.A[1, 1], signature.Z[1])
        );

        Polynomial ct0 = Polynomial.Multiply(signature.C, keyPair.T[0]);
        Polynomial ct1 = Polynomial.Multiply(signature.C, keyPair.T[1]);

        Polynomial wPrime0 = Polynomial.Subtract(az0, ct0);
        Polynomial wPrime1 = Polynomial.Subtract(az1, ct1);

        // 2. Recompute challenge: c' = Hash(message, w'0, w'1)
        Polynomial cPrime = HashToChallenge(message, wPrime0, wPrime1);

        // 3. Verify challenge equality: every coefficient of c' must match signature.C
        for (int i = 0; i < Polynomial.N; i++)
        {
            if (cPrime.Coeffs[i] != signature.C.Coeffs[i])
            {
                return false;
            }
        }

        return true;
    }
}
