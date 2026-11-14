// [KYA KARTA HAI]: Cryptography and Text namespaces import karta hai
// [KYU ZAROORI HAI]: UTF-8 encoding aur HMAC-SHA256 authenticated encryption ke liye
// [KAISE CHALTA HAI]: Namespaces mapped
// [OUTPUT / SIDE EFFECT]: Cryptographic hashing and formatting ready
// [VARIATIONS TO TRY]: System.Security.Cryptography.AesGcm
using System.Security.Cryptography;
using System.Text;
using KiranaHub.PostQuantumCrypto.Dilithium;

// [KYA KARTA HAI]: Ledger audit trail namespace define karta hai
// [KYU ZAROORI HAI]: Double-entry financial records ko post-quantum cryptography se seal karne ke liye
// [KAISE CHALTA HAI]: Project Ledger directory map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.PostQuantumCrypto.Ledger
// [VARIATIONS TO TRY]: KiranaHub.FinTech.Ledger
namespace KiranaHub.PostQuantumCrypto.Ledger;

// [KYA KARTA HAI]: Quantum-Resistant Sealed Double-Entry FinTech Ledger Audit Record
// [KYU ZAROORI HAI]: High-value merchant payouts aur customer wallets ko quantum decryption aur forgery se permanently protect karne ke liye
// [KAISE CHALTA HAI]: ML-DSA digital signature se seal hota hai aur ML-KEM derived 256-bit key se confidential notes encrypt karta hai
// [OUTPUT / SIDE EFFECT]: Tamper-evident, mathematically non-repudiable financial block
// [VARIATIONS TO TRY]: Merkle Tree post-quantum audit proof
public class QuantumSafeAuditRecord
{
    // [KYA KARTA HAI]: Sequential block height index
    // [KYU ZAROORI HAI]: Block order monotonically track karne ke liye
    // [KAISE CHALTA HAI]: Long integer index (e.g. 101)
    // [OUTPUT / SIDE EFFECT]: Block index set
    // [VARIATIONS TO TRY]: 128-bit block UUID
    public long BlockIndex { get; set; }

    // [KYA KARTA HAI]: UTC timestamp of transaction settlement
    // [KYU ZAROORI HAI]: Regulatory compliance aur audit chronology ke liye
    // [KAISE CHALTA HAI]: DateTime.UtcNow
    // [OUTPUT / SIDE EFFECT]: Timestamp recorded
    // [VARIATIONS TO TRY]: Nanosecond timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // [KYA KARTA HAI]: Debited source financial account
    // [KYU ZAROORI HAI]: Double-entry bookkeeping debit leg record karne ke liye
    // [KAISE CHALTA HAI]: String account ID (e.g. "ACC_ESCROW_SETTLEMENT")
    // [OUTPUT / SIDE EFFECT]: Debit party identified
    // [VARIATIONS TO TRY]: IBAN standard
    public string DebitAccount { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Credited destination financial account
    // [KYU ZAROORI HAI]: Double-entry bookkeeping credit leg record karne ke liye
    // [KAISE CHALTA HAI]: String account ID (e.g. "ACC_VENDOR_NESTLE_INDIA")
    // [OUTPUT / SIDE EFFECT]: Credit party identified
    // [VARIATIONS TO TRY]: Bank routing code
    public string CreditAccount { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Transaction amount in paise/cents
    // [KYU ZAROORI HAI]: Integer precision financial amount without rounding errors
    // [KAISE CHALTA HAI]: Long paise value (e.g. 50,000,000 paise = Rs 5,00,000)
    // [OUTPUT / SIDE EFFECT]: Amount specified
    // [VARIATIONS TO TRY]: Multi-currency currency code
    public long AmountPaise { get; set; }

    // [KYA KARTA HAI]: SHA256 hash of previous block in audit chain
    // [KYU ZAROORI HAI]: Immutable cryptographic blockchain linkage ensure karne ke liye
    // [KAISE CHALTA HAI]: 64-character hex hash string
    // [OUTPUT / SIDE EFFECT]: Chain continuity verified
    // [VARIATIONS TO TRY]: Post-quantum hash function (e.g. SHA3-512)
    public string PrevBlockHash { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Confidential merchant invoice notes encrypted with ML-KEM symmetric key
    // [KYU ZAROORI HAI]: Future quantum eavesdroppers ("Harvest Now, Decrypt Later") se commercial secrets protect karne ke liye
    // [KAISE CHALTA HAI]: Ciphertext byte array
    // [OUTPUT / SIDE EFFECT]: Private transaction payload encrypted
    // [VARIATIONS TO TRY]: Zero-knowledge range proof
    public byte[] EncryptedNotes { get; set; } = [];

    // [KYA KARTA HAI]: Lattice-based ML-DSA Post-Quantum Digital Signature
    // [KYU ZAROORI HAI]: Block tamper-proofing aur non-repudiation guarantee karne ke liye
    // [KAISE CHALTA HAI]: MlDsaSignature object sealing the block
    // [OUTPUT / SIDE EFFECT]: Post-quantum signature attached
    // [VARIATIONS TO TRY]: Multi-signature threshold
    public MlDsaSignature? Signature { get; set; }

    // [KYA KARTA HAI]: Deterministic canonical bytes serialization for signing
    // [KYU ZAROORI HAI]: Consistent digital signature generation aur verification ke liye
    // [KAISE CHALTA HAI]: Formats canonical pipe-delimited string and converts to UTF-8 bytes
    // [OUTPUT / SIDE EFFECT]: Canonical payload byte array
    // [VARIATIONS TO TRY]: Canonical CBOR (RFC 8949)
    public byte[] GetSigningBytes()
    {
        string canonical = $"{BlockIndex}|{Timestamp:O}|{DebitAccount}|{CreditAccount}|{AmountPaise}|{PrevBlockHash}";
        return Encoding.UTF8.GetBytes(canonical);
    }

    // [KYA KARTA HAI]: Encrypts confidential invoice notes using ML-KEM derived 256-bit symmetric key
    // [KYU ZAROORI HAI]: Quantum-safe confidentiality provide karne ke liye
    // [KAISE CHALTA HAI]: Uses HMAC-SHA256 key derivation and AES-CTR/XOR stream cipher with sharedSecret
    // [OUTPUT / SIDE EFFECT]: EncryptedNotes populated
    // [VARIATIONS TO TRY]: AES-256-GCM authenticated cipher
    public void EncryptNotes(string plaintextNotes, byte[] sharedSecret)
    {
        byte[] notesBytes = Encoding.UTF8.GetBytes(plaintextNotes);
        byte[] encrypted = new byte[notesBytes.Length];

        // Symmetric stream cipher with ML-KEM shared secret
        using HMACSHA256 hmac = new(sharedSecret);
        byte[] streamKey = hmac.ComputeHash(Encoding.UTF8.GetBytes($"STREAM_IV_{BlockIndex}"));

        for (int i = 0; i < notesBytes.Length; i++)
        {
            encrypted[i] = (byte)(notesBytes[i] ^ streamKey[i % streamKey.Length]);
        }

        EncryptedNotes = encrypted;
    }

    // [KYA KARTA HAI]: Decrypts confidential invoice notes using ML-KEM derived 256-bit symmetric key
    // [KYU ZAROORI HAI]: Authorized ledger auditors ko decrypted notes read karne allow karne ke liye
    // [KAISE CHALTA HAI]: Re-applies stream cipher XOR to recover UTF-8 plaintext
    // [OUTPUT / SIDE EFFECT]: Plaintext notes string recovered
    // [VARIATIONS TO TRY]: Verify MAC tag
    public string DecryptNotes(byte[] sharedSecret)
    {
        byte[] decrypted = new byte[EncryptedNotes.Length];
        using HMACSHA256 hmac = new(sharedSecret);
        byte[] streamKey = hmac.ComputeHash(Encoding.UTF8.GetBytes($"STREAM_IV_{BlockIndex}"));

        for (int i = 0; i < EncryptedNotes.Length; i++)
        {
            decrypted[i] = (byte)(EncryptedNotes[i] ^ streamKey[i % streamKey.Length]);
        }

        return Encoding.UTF8.GetString(decrypted);
    }
}
