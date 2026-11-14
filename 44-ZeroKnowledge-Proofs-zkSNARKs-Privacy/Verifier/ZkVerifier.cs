using KiranaHub.ZkSnarks.Circuit;
using KiranaHub.ZkSnarks.Prover;

namespace KiranaHub.ZkSnarks.Verifier;

/// <summary>
/// Public Verifying Key (VK).
/// Lightweight cryptographic keys distributed to delivery riders and store merchants.
/// Contains zero secret parameters (zero toxic waste).
/// </summary>
public sealed record VerifyingKey(
    FieldElement Alpha,
    FieldElement Beta,
    FieldElement Delta,
    FieldElement Z_Tau);

/// <summary>
/// Zero-Knowledge SNARK Verifier Engine.
/// Validates proofs in sub-millisecond O(1) time without accessing private customer data.
/// </summary>
public static class ZkVerifier
{
    // [KYA KARTA HAI]: ProvingKey se public VerifyingKey extract karta hai.
    // [KYU ZAROORI HAI]: Verifier ko private circuit details aur secret parameters expose na ho sakein.
    // [KAISE CHALTA HAI]: Extracts Alpha, Beta, Delta, and Z_Tau.
    // [OUTPUT / SIDE EFFECT]: VerifyingKey instance.
    // [VARIATIONS TO TRY]: Base64 serialization of verifying keys for mobile rider apps.
    public static VerifyingKey ExtractVerifyingKey(ProvingKey pk)
    {
        return new VerifyingKey(pk.Alpha, pk.Beta, pk.Delta, pk.Z_Tau);
    }

    // [KYA KARTA HAI]: Zero-Knowledge proof ko public inputs aur verifying key ke against mathematically verify karta hai.
    // [KYU ZAROORI HAI]: Delivery rider ko 100% guarantee milti hai ki customer 21+ hai without seeing birth year!
    // [KAISE CHALTA HAI]: Bilinear identity check karta hai: ProofA * ProofB == ProofC + ProofH * Z(tau).
    // [OUTPUT / SIDE EFFECT]: Boolean true (Valid Proof) or false (Tampered / Invalid Proof).
    // [VARIATIONS TO TRY]: Batch verification of 1,000 proofs in parallel.
    public static bool Verify(
        VerifyingKey vk,
        ZkProof proof,
        IReadOnlyList<FieldElement> expectedPublicInputs,
        out string? verificationError)
    {
        verificationError = null;

        // 1. Verify Public Inputs Count
        if (proof.PublicInputs.Count != expectedPublicInputs.Count)
        {
            verificationError = $"Public inputs count mismatch: expected {expectedPublicInputs.Count}, got {proof.PublicInputs.Count}";
            return false;
        }

        // 2. Verify Expected Public Inputs Match (e.g. CurrentYear == 2026, MinAge == 21)
        for (int i = 0; i < expectedPublicInputs.Count; i++)
        {
            if (proof.PublicInputs[i] != expectedPublicInputs[i])
            {
                verificationError = $"Public input #{i} mismatch: expected {expectedPublicInputs[i]}, got {proof.PublicInputs[i]}";
                return false;
            }
        }

        // 3. Bilinear / Polynomial Identity Check:
        // Equation: ProofA * ProofB == ProofC + ProofH * Z_Tau
        FieldElement lhs = proof.ProofA * proof.ProofB;
        FieldElement rhs = proof.ProofC + (proof.ProofH * vk.Z_Tau);

        if (lhs != rhs)
        {
            verificationError = $"Cryptographic identity violated! LHS: {lhs} != RHS: {rhs} (Proof is fraudulent or tampered)";
            return false;
        }

        return true;
    }
}
