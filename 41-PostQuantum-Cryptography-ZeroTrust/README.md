# Project 41: Post-Quantum Cryptography & Zero-Trust Ledger (ML-KEM / Kyber & ML-DSA / Dilithium)

## 1. Reading Roadmap ("Iss project ko scratch se kaise padhein")
Agar aap Post-Quantum Cryptography (PQC), lattice-based mathematics, Learning With Errors (LWE), ya NIST FIPS 203/204 standards पहली बार padh rahe hain, toh files ko iss sequence me padhein:
1. **`Math/PolynomialRing.cs`**: Dekhein ki cyclotomic polynomial ring $R_q = \mathbb{Z}_q[X] / (X^{256} + 1)$ kaise kaam karta hai, negacyclic convolution ($X^{256} \equiv -1$) kya hota hai, aur centered binomial noise $\chi_\eta$ kaise sample hoti hai.
2. **`Kyber/MlKemEngine.cs`**: Padhein ki NIST FIPS 203 (ML-KEM / Crystals-Kyber-512) me Module-LWE ke through Alice aur Bob bina private keys reveal kiye 256-bit symmetric shared secret kaise negotiate karte hain.
3. **`Dilithium/MlDsaSignatureEngine.cs`**: Samjhein ki NIST FIPS 204 (ML-DSA / Crystals-Dilithium) me Short Integer Solution (SIS) aur Fiat-Shamir transformation ke through non-interactive digital signatures kaise bante hain aur verify hote hain.
4. **`Ledger/QuantumSafeAuditRecord.cs`**: Dekhein ki double-entry financial ledger records ko ML-DSA signature se seal karke aur ML-KEM derived key se confidential invoice notes ko encrypt karke tamper-evident audit block kaise banaya jata hai.
5. **`Program.cs`**: Run karke charon security tests (KEM key agreement, DSA digital signature, Fraudulent tamper rejection, Confidential payload decryption) ko live verify karein.

---

## 2. Foundations ("Kya pehle bna, kaise bna")
Ab tak financial ledgers aur HTTPS connections me traditional public-key cryptography use hoti thi:
- **RSA (2048/4096 bits)**: Integer factorization problem ($N = p \cdot q$) par based.
- **ECDSA / ECDH (secp256k1 / NIST P-256)**: Elliptic curve discrete logarithm problem ($Q = d \cdot G$) par based.
- **The Quantum Threat (Shor's Algorithm)**:
  - Peter Shor ne 1994 me prove kiya ki polynomial-time quantum computers (running Shor's algorithm) RSA aur Elliptic Curve cryptography ko seconds me tod sakte hain ($O((\log N)^3)$ time complexity).
  - **"Harvest Now, Decrypt Later" (HNDL) Attacks**: Cybercriminals aur adversarial nation-states aaj ka encrypted financial traffic aur audit logs store kar rahe hain taaki 5-10 saal baad quantum computer aate hi unhe decrypt kar sakein aur purane settlement signatures ko forge kar sakein!

---

## 3. Evolution & Mechanics ("Phir kya aaya, kyu aaya, kaise aaya, kya karta hai")

### A. Lattice-Based Cryptography & NIST Standardization
NIST ne August 2024 me FIPS 203 (ML-KEM) aur FIPS 204 (ML-DSA) ko official post-quantum standards ke taur par finalize kiya:
1. **Hardness of Module Learning With Errors (M-LWE)**:
   - Lattice cryptography high-dimensional vector spaces par based hai (e.g. 256 dimensions).
   - Quantum computers Shor's algorithm ke zariye lattice problems (Shortest Vector Problem - SVP, Closest Vector Problem - CVP) ko solve nahi kar sakte kyunki inme koi periodic hidden subgroup structure nahi hota.
2. **ML-KEM (Kyber-512) Mechanism**:
   - Sender public key $(A, t)$ use karke ek 256-bit random message $m$ ko ciphertext $(u, v)$ me encapsulate karta hai.
   - Receiver apne secret vector $s$ se $M' = v - s^T u \pmod q$ calculate karta hai. Noise ko round karke exact message $m$ recover kar leta hai.
   - Dono parties SHA-256 hash karke exact 256-bit symmetric session key create kar leti hain ($K_A == K_B$).
3. **ML-DSA (Dilithium) Lattice Signatures**:
   - Private key holder commitment $w = A \cdot y$ banata hai, message aur $w$ ka cryptographic hash karke challenge $c$ nikalta hai, aur signature response $z = y + c \cdot s_1$ publish karta hai.
   - Verifier $A \cdot z - c \cdot t$ compute karke commitment $w'$ reconstruct karta hai aur challenge verify karta hai.
   - Agar ledger me 1 byte bhi tamper ho, toh challenge match nahi hota aur transaction reject ho jaati hai!

---

## 4. Counter-Factual Risk Analysis ("⚠️ Uske na hone se kya hoga")
1. **Agar Post-Quantum Cryptography adopt na karein**:
   - Jab CRQC (Cryptanalytically Relevant Quantum Computer) aayega, RSA/ECDSA based private keys expose ho jayengi. Attackers past ke saare encrypted financial ledgers padh lenge aur fake debit/credit transactions authenticate kar denge (Double-Entry Financial Collapse).
2. **Agar ML-DSA Signatures na hon (Tamper-proofing absence)**:
   - Koi bhi rogue insider ledger database me amount update kar dega (e.g., Rs 5,00,000 $\to$ Rs 50,00,000), aur without lattice non-repudiation, fraud prove karna impossible hoga.
3. **Agar Polynomial Ring Negacyclic Convolution ($X^{256} \equiv -1$) properly handle na ho**:
   - Mathematical decryption failure aayegi, jisse Alice aur Bob ka negotiated symmetric key mismatch ho jayega aur valid transactions fail ho jayengi.

---

## 5. How to Run & Verify
Run the standalone benchmark using .NET 10:
```powershell
dotnet run --project 41-PostQuantum-Cryptography-ZeroTrust/KiranaHub.PostQuantumCrypto.csproj
```

### Expected Output
```text
================================================================================
  KiranaHub Level 9: Post-Quantum Cryptography & Zero-Trust Ledger (FIPS 203/204)
  Features: ML-KEM (Kyber) Lattice Key Exchange, ML-DSA (Dilithium) Signatures
================================================================================

[TEST 1] Post-Quantum Key Encapsulation Mechanism (ML-KEM / Crystals-Kyber-512)...
  - Generated ML-KEM Asymmetric Keypair (Lattice Ring R_q, n=256, q=3329, k=2)
  - Encapsulated 256-bit Shared Secret -> Ciphertext (u: 2 polynomials, v: 1 polynomial)
  - Alice Shared Key (Sender)   : 7A8B9C... (32 bytes)
  - Bob Recovered Key (Receiver): 7A8B9C... (32 bytes)
  - Elapsed KEM Operation Time  : 14 ms (14201.5 µs)
  [RESULT] -> SUCCESS: Alice and Bob established identical 256-bit Post-Quantum symmetric key!

[TEST 2] Post-Quantum Lattice Digital Signature (ML-DSA / Crystals-Dilithium)...
  - Generated ML-DSA Signing & Verification Keypair (Lattice SIS Problem)
  - Sealed Financial Ledger Record #4821 with ML-DSA Lattice Signature
  - Settlement: ACC_ESCROW_SETTLEMENT -> ACC_VENDOR_NESTLE_INDIA of Rs 500,000.00
  - Verification Result        : VALID AUTHENTIC SIGNATURE
  - Elapsed DSA Operation Time : 8 ms (8120.3 µs)
  [RESULT] -> SUCCESS: Post-quantum digital signature verified authentic!

[TEST 3] Simulating Malicious Financial Fraud (Tampering 1 byte in Ledger Block)...
  - Attacker altered Amount to : Rs 5,000,000.00
  - ML-DSA Tamper Verification: REJECTED & TRAPPED
  [RESULT] -> SUCCESS: Cryptographic tamper detection trapped fraudulent mutation!

[TEST 4] Decrypting Confidential Commercial Notes using ML-KEM Recovered Key...
  - Decrypted Plaintext Notes : "Invoice #NESTLE-MUM-2026-991: Bulk Supply of 5,000 units Maggi Masala Noodles (GSTIN: 27AABCN1234F1Z8)"
  [RESULT] -> SUCCESS: Confidential invoice notes decrypted accurately with zero loss!

================================================================================
  [SUMMARY] All 4 Post-Quantum Cryptography & Zero-Trust Tests PASSED!
================================================================================
```
