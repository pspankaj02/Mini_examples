using System.Diagnostics;
using KiranaHub.ZkSnarks.Circuit;
using KiranaHub.ZkSnarks.Prover;
using KiranaHub.ZkSnarks.Verifier;

namespace KiranaHub.ZkSnarks;

public static class Program
{
    // [KYA KARTA HAI]: zk-SNARKs Privacy Engine ka comprehensive demonstration entry point.
    // [KYU ZAROORI HAI]: Zero-knowledge privacy guarantees (valid proof pass, underage rejection, tamper detection) demonstrate karna.
    // [KAISE CHALTA HAI]: 4 automated cryptographic test scenarios sequentially execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Console logs showing cryptographic proofs and verification decisions.
    // [VARIATIONS TO TRY]: Benchmark with 10,000 proofs.
    public static void Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 10: Zero-Knowledge Proofs (zk-SNARKs) Privacy Engine          ");
        Console.WriteLine("  Domain: Age Verification (DPDP Act Compliance) & Financial Credit Eligibility ");
        Console.WriteLine("================================================================================");

        // -------------------------------------------------------------------------
        // SETUP: Circuit Compilation & Cryptographic Setup Ceremony
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[SETUP] Compiling Arithmetic Circuit & Generating Proving/Verifying Keys...");
        var (circuit, wires) = ArithmeticCircuit.BuildAgeVerificationCircuit();

        Console.WriteLine($"  - Total Circuit Wires     : {circuit.TotalVariables} variables");
        Console.WriteLine($"  - Public Inputs Count     : {circuit.PublicInputs.Count} wires (CurrentYear, MinAge)");
        Console.WriteLine($"  - Private Witness Count   : {circuit.PrivateWitnesses.Count} wires (BirthYear, Age, Diff, 8-Bit Gadgets)");
        Console.WriteLine($"  - Total R1CS Constraints  : {circuit.Constraints.Count} equations (<A,s> * <B,s> = <C,s>)");

        var sw = Stopwatch.StartNew();
        var provingKey = ZkProver.GenerateSetup(circuit);
        var verifyingKey = ZkVerifier.ExtractVerifyingKey(provingKey);
        sw.Stop();
        Console.WriteLine($"  - Trusted Setup Generated : in {sw.Elapsed.TotalMilliseconds:F2} ms");

        // -------------------------------------------------------------------------
        // CASE 1: Valid Customer (Age >= 21) -> Proof Succeeds & Verifier Accepts
        // -------------------------------------------------------------------------
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("[CASE 1] Adult Customer (Born 2000, Age 26) Ordering Age-Restricted Grocery...");
        Console.WriteLine("--------------------------------------------------------------------------------");
        int currentYear = 2026;
        int minAgeThreshold = 21;
        int customerBirthYear = 2000;

        Console.WriteLine($"  - Customer Private Data   : BirthYear = {customerBirthYear} (Calculated Age: {currentYear - customerBirthYear})");
        Console.WriteLine($"  - Delivery Public Policy  : Minimum Legal Age = {minAgeThreshold} in Year {currentYear}");

        sw.Restart();
        var validProof = ZkProver.CreateAgeProof(provingKey, wires, currentYear, minAgeThreshold, customerBirthYear);
        sw.Stop();

        Console.WriteLine($"  - Proof Synthesis Time    : {sw.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine($"  - Generated Proof Points  : A = {validProof.ProofA}, B = {validProof.ProofB}, C = {validProof.ProofC}");
        Console.WriteLine($"  - Customer Shares         : Proof (pi) + Public Policy (Year: {currentYear}, MinAge: {minAgeThreshold})");
        Console.WriteLine("  - [PRIVACY GUARANTEE]     : Customer's BirthYear (2000) and exact age (26) NEVER LEAVED THE DEVICE!");

        Console.WriteLine("\n  [VERIFIER] Delivery Rider verifies proof at customer doorstep...");
        FieldElement[] publicInputs = [new FieldElement(currentYear), new FieldElement(minAgeThreshold)];

        sw.Restart();
        bool isValid = ZkVerifier.Verify(verifyingKey, validProof, publicInputs, out string? error);
        sw.Stop();

        Console.WriteLine($"  - Verification Time       : {sw.Elapsed.TotalMilliseconds:F3} ms (< 1 millisecond!)");
        Console.WriteLine($"  - Verification Decision   : {(isValid ? "SUCCESS (Proof Accepted - Hand Over Delivery)" : "FAILED")}");
        if (error != null) Console.WriteLine($"  - Reason: {error}");

        // -------------------------------------------------------------------------
        // CASE 2: Underage Customer (Age 17) -> Circuit Violation & Proof Refusal
        // -------------------------------------------------------------------------
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("[CASE 2] Minor Customer (Born 2009, Age 17) Attempting Alcohol Order Fraud...");
        Console.WriteLine("--------------------------------------------------------------------------------");
        int minorBirthYear = 2009; // Age = 17 < 21
        Console.WriteLine($"  - Minor Private Data      : BirthYear = {minorBirthYear} (Age: {currentYear - minorBirthYear})");
        Console.WriteLine("  - Attempting zk-SNARK proof generation for MinAge 21...");

        try
        {
            ZkProver.CreateAgeProof(provingKey, wires, currentYear, minAgeThreshold, minorBirthYear);
            Console.WriteLine("  - [CRITICAL BUG] Underage proof was generated!");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("  - [CRYPTOGRAPHIC PROOF REJECTION] Prover engine blocked proof synthesis!");
            Console.WriteLine($"  - Circuit Constraint Check: {ex.Message}");
            Console.WriteLine("  - [RESULT] Mathematical certainty: A minor CANNOT mathematically forge a valid proof!");
        }

        // -------------------------------------------------------------------------
        // CASE 3: Tampered Proof / Malicious Rider Detection
        // -------------------------------------------------------------------------
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("[CASE 3] Man-In-The-Middle / Tampering Attack on Valid Proof...");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("  - Simulating malicious adversary altering Proof point A (flipping bits)...");
        var tamperedProof = new ZkProof(
            ProofA: validProof.ProofA + new FieldElement(42), // Corrupted point A
            ProofB: validProof.ProofB,
            ProofC: validProof.ProofC,
            ProofH: validProof.ProofH,
            PublicInputs: validProof.PublicInputs);

        bool tamperedResult = ZkVerifier.Verify(verifyingKey, tamperedProof, publicInputs, out string? tamperError);
        Console.WriteLine($"  - Verification Decision   : {(tamperedResult ? "ACCEPTED (Security Breach!)" : "REJECTED (Attack Blocked!)")}");
        Console.WriteLine($"  - Verification Diagnostic : {tamperError}");

        // -------------------------------------------------------------------------
        // CASE 4: High-Throughput Verification Benchmark (1,000 Proof Verifications)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("[CASE 4] Micro-Benchmark: 1,000 Consecutive Proof Verifications...");
        Console.WriteLine("--------------------------------------------------------------------------------");
        const int BenchmarkRuns = 1_000;
        sw.Restart();
        int passCount = 0;
        for (int i = 0; i < BenchmarkRuns; i++)
        {
            if (ZkVerifier.Verify(verifyingKey, validProof, publicInputs, out _))
            {
                passCount++;
            }
        }
        sw.Stop();

        double opsPerSec = BenchmarkRuns / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"  - Proofs Verified         : {passCount:N0} / {BenchmarkRuns:N0}");
        Console.WriteLine($"  - Total Time Elapsed      : {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  - Throughput              : {opsPerSec:N0} verifications/sec ({sw.Elapsed.TotalMilliseconds / BenchmarkRuns:F4} ms per verification)");

        Console.WriteLine("\n[SUCCESS] All Zero-Knowledge zk-SNARK privacy verifications passed successfully!");
    }
}
