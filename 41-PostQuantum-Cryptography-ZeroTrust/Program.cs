// [KYA KARTA HAI]: Diagnostic and Cryptography namespaces import karta hai
// [KYU ZAROORI HAI]: High-resolution Stopwatch aur byte conversions ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Timing and formatting ready
// [VARIATIONS TO TRY]: BenchmarkDotNet integration
using System.Diagnostics;
using KiranaHub.PostQuantumCrypto.Dilithium;
using KiranaHub.PostQuantumCrypto.Kyber;
using KiranaHub.PostQuantumCrypto.Ledger;

// [KYA KARTA HAI]: PostQuantumCrypto main namespace
// [KYU ZAROORI HAI]: Program entrypoint encapsulate karne ke liye
// [KAISE CHALTA HAI]: Namespace wrapping
// [OUTPUT / SIDE EFFECT]: Program class registered
// [VARIATIONS TO TRY]: KiranaHub.Crypto.Main
namespace KiranaHub.PostQuantumCrypto;

public static class Program
{
    // [KYA KARTA HAI]: Main executable entry point
    // [KYU ZAROORI HAI]: Post-Quantum Cryptography (ML-KEM & ML-DSA) verification test suite run karne ke liye
    // [KAISE CHALTA HAI]: Runs Kyber KEM key exchange, Dilithium ledger signing, tamper detection, and confidential decryption
    // [OUTPUT / SIDE EFFECT]: Console output with crypto verification metrics
    // [VARIATIONS TO TRY]: File-backed ledger auditor
    public static void Main(string[] args)
    {
        // [KYA KARTA HAI]: Header banner print karta hai
        // [KYU ZAROORI HAI]: Scenario identification ke liye
        // [KAISE CHALTA HAI]: Console.WriteLine
        // [OUTPUT / SIDE EFFECT]: Banner printed
        // [VARIATIONS TO TRY]: ANSI color formatting
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 9: Post-Quantum Cryptography & Zero-Trust Ledger (FIPS 203/204)");
        Console.WriteLine("  Features: ML-KEM (Kyber) Lattice Key Exchange, ML-DSA (Dilithium) Signatures");
        Console.WriteLine("================================================================================\n");

        // ---------------------------------------------------------------------------------
        // TEST 1: ML-KEM (Kyber-512) Key Encapsulation & Decapsulation
        // ---------------------------------------------------------------------------------
        Console.WriteLine("[TEST 1] Post-Quantum Key Encapsulation Mechanism (ML-KEM / Crystals-Kyber-512)...");
        Stopwatch swKem = Stopwatch.StartNew();

        // 1. KeyGen
        (MlKemPublicKey pk, MlKemPrivateKey sk) = MlKemEngine.KeyGen();
        Console.WriteLine("  - Generated ML-KEM Asymmetric Keypair (Lattice Ring R_q, n=256, q=3329, k=2)");

        // 2. Encapsulate (Sender/Client side)
        (MlKemCiphertext ciphertext, byte[] aliceSharedSecret) = MlKemEngine.Encapsulate(pk);
        Console.WriteLine($"  - Encapsulated 256-bit Shared Secret -> Ciphertext (u: 2 polynomials, v: 1 polynomial)");
        Console.WriteLine($"  - Alice Shared Key (Sender)   : {Convert.ToHexString(aliceSharedSecret)[..32]}... (32 bytes)");

        // 3. Decapsulate (Receiver/Server side)
        byte[] bobSharedSecret = MlKemEngine.Decapsulate(sk, ciphertext);
        Console.WriteLine($"  - Bob Recovered Key (Receiver): {Convert.ToHexString(bobSharedSecret)[..32]}... (32 bytes)");
        swKem.Stop();

        bool kemMatched = aliceSharedSecret.SequenceEqual(bobSharedSecret);
        Console.WriteLine($"  - Elapsed KEM Operation Time  : {swKem.ElapsedMilliseconds} ms ({swKem.Elapsed.TotalMicroseconds:F1} \u00b5s)");

        if (kemMatched)
        {
            Console.WriteLine("  [RESULT] -> SUCCESS: Alice and Bob established identical 256-bit Post-Quantum symmetric key!\n");
        }
        else
        {
            Console.WriteLine("  [RESULT] -> FAILED: Shared secret key mismatch!\n");
            return;
        }

        // ---------------------------------------------------------------------------------
        // TEST 2: ML-DSA (Dilithium) Quantum-Resistant Digital Signature on Ledger Block
        // ---------------------------------------------------------------------------------
        Console.WriteLine("[TEST 2] Post-Quantum Lattice Digital Signature (ML-DSA / Crystals-Dilithium)...");
        Stopwatch swDsa = Stopwatch.StartNew();

        // 1. KeyGen
        MlDsaKeyPair dsaKeyPair = MlDsaSignatureEngine.KeyGen();
        Console.WriteLine("  - Generated ML-DSA Signing & Verification Keypair (Lattice SIS Problem)");

        // 2. Create Double-Entry Ledger Record
        QuantumSafeAuditRecord record = new()
        {
            BlockIndex = 4821,
            Timestamp = DateTime.UtcNow,
            DebitAccount = "ACC_ESCROW_SETTLEMENT",
            CreditAccount = "ACC_VENDOR_NESTLE_INDIA",
            AmountPaise = 50000000, // Rs 5,00,000.00
            PrevBlockHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        };

        // Encrypt confidential invoice notes using ML-KEM shared secret
        string secretNotes = "Invoice #NESTLE-MUM-2026-991: Bulk Supply of 5,000 units Maggi Masala Noodles (GSTIN: 27AABCN1234F1Z8)";
        record.EncryptNotes(secretNotes, aliceSharedSecret);

        // 3. Sign Ledger Record using ML-DSA
        byte[] signingBytes = record.GetSigningBytes();
        record.Signature = MlDsaSignatureEngine.Sign(signingBytes, dsaKeyPair);
        Console.WriteLine($"  - Sealed Financial Ledger Record #{record.BlockIndex} with ML-DSA Lattice Signature");
        Console.WriteLine($"  - Settlement: {record.DebitAccount} -> {record.CreditAccount} of Rs {record.AmountPaise / 100.0:N2}");

        // 4. Verify Signature using ML-DSA Public Key
        bool isValid = MlDsaSignatureEngine.Verify(signingBytes, record.Signature, dsaKeyPair);
        swDsa.Stop();

        Console.WriteLine($"  - Verification Result        : {(isValid ? "VALID AUTHENTIC SIGNATURE" : "INVALID")}");
        Console.WriteLine($"  - Elapsed DSA Operation Time : {swDsa.ElapsedMilliseconds} ms ({swDsa.Elapsed.TotalMicroseconds:F1} \u00b5s)");

        if (isValid)
        {
            Console.WriteLine("  [RESULT] -> SUCCESS: Post-quantum digital signature verified authentic!\n");
        }
        else
        {
            Console.WriteLine("  [RESULT] -> FAILED: Signature verification failed!\n");
            return;
        }

        // ---------------------------------------------------------------------------------
        // TEST 3: Tamper Detection (Simulated Fraud Attack)
        // ---------------------------------------------------------------------------------
        Console.WriteLine("[TEST 3] Simulating Malicious Financial Fraud (Tampering 1 byte in Ledger Block)...");
        // Attacker attempts to change Rs 5,00,000 (50,000,000 paise) to Rs 50,00,000 (500,000,000 paise)
        record.AmountPaise = 500000000;
        byte[] tamperedBytes = record.GetSigningBytes();

        bool isTamperAccepted = MlDsaSignatureEngine.Verify(tamperedBytes, record.Signature!, dsaKeyPair);
        Console.WriteLine($"  - Attacker altered Amount to : Rs {record.AmountPaise / 100.0:N2}");
        Console.WriteLine($"  - ML-DSA Tamper Verification: {(isTamperAccepted ? "ACCEPTED (VULNERABLE!)" : "REJECTED & TRAPPED")}");

        if (!isTamperAccepted)
        {
            Console.WriteLine("  [RESULT] -> SUCCESS: Cryptographic tamper detection trapped fraudulent mutation!\n");
        }
        else
        {
            Console.WriteLine("  [RESULT] -> FAILED: Tampered transaction was erroneously accepted!\n");
            return;
        }

        // Revert back for final test
        record.AmountPaise = 50000000;

        // ---------------------------------------------------------------------------------
        // TEST 4: Confidential Invoice Decryption using ML-KEM Key
        // ---------------------------------------------------------------------------------
        Console.WriteLine("[TEST 4] Decrypting Confidential Commercial Notes using ML-KEM Recovered Key...");
        string decryptedNotes = record.DecryptNotes(bobSharedSecret);
        Console.WriteLine($"  - Decrypted Plaintext Notes : \"{decryptedNotes}\"");

        if (decryptedNotes == secretNotes)
        {
            Console.WriteLine("  [RESULT] -> SUCCESS: Confidential invoice notes decrypted accurately with zero loss!\n");
        }
        else
        {
            Console.WriteLine("  [RESULT] -> FAILED: Decrypted notes mismatch!\n");
            return;
        }

        Console.WriteLine("================================================================================");
        Console.WriteLine("  [SUMMARY] All 4 Post-Quantum Cryptography & Zero-Trust Tests PASSED!");
        Console.WriteLine("================================================================================\n");
    }
}
