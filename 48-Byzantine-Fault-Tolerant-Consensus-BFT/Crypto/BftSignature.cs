using System.Security.Cryptography;
using System.Text;

namespace KiranaHub.BftConsensus.Crypto;

/// <summary>
/// Cryptographic Signature and Digest Engine for BFT Consensus.
/// Ensures tamper-evident message authenticity and non-repudiation across multi-party consortium nodes.
/// </summary>
public static class BftCrypto
{
    // [KYA KARTA HAI]: Payload string ka cryptographic SHA-256 hash digest calculate karta hai.
    // [KYU ZAROORI HAI]: PBFT messages me block content ke bajay fixed 32-byte digest exchange kiya jata hai.
    // [KAISE CHALTA HAI]: SHA256.HashData over UTF-8 bytes; returns lowercase hex string.
    // [OUTPUT / SIDE EFFECT]: 64-character hex digest string.
    // [VARIATIONS TO TRY]: Keccak-256 (Ethereum) or BLAKE3 for faster hashing.
    public static string ComputeDigest(string payload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // [KYA KARTA HAI]: Node ke secret key se message par HMAC-SHA256 digital signature generate karta hai.
    // [KYU ZAROORI HAI]: Man-in-the-middle attacks aur node impersonation prevent karna (Non-repudiation).
    // [KAISE CHALTA HAI]: HMACSHA256.HashData with secret key.
    // [OUTPUT / SIDE EFFECT]: 64-character hex signature string.
    // [VARIATIONS TO TRY]: Ed25519 or BLS threshold signatures.
    public static string Sign(string message, byte[] secretKey)
    {
        byte[] msgBytes = Encoding.UTF8.GetBytes(message);
        byte[] sig = HMACSHA256.HashData(secretKey, msgBytes);
        return Convert.ToHexString(sig).ToLowerInvariant();
    }

    // [KYA KARTA HAI]: Digital signature ki cryptographic validity verify karta hai.
    // [KYU ZAROORI HAI]: Agar Byzantine peer ne signature forge kiya ya message mutate kiya toh instantly reject karna.
    // [KAISE CHALTA HAI]: Expected HMAC calculate karke CryptographicOperations.FixedTimeEquals se compare karta hai.
    // [OUTPUT / SIDE EFFECT]: True if authentic; false if forged or tampered.
    // [VARIATIONS TO TRY]: Constant-time verification to prevent timing leaks.
    public static bool Verify(string message, string signatureHex, byte[] secretKey)
    {
        string expectedSig = Sign(message, secretKey);
        byte[] sigA = Encoding.UTF8.GetBytes(signatureHex);
        byte[] sigB = Encoding.UTF8.GetBytes(expectedSig);

        return CryptographicOperations.FixedTimeEquals(sigA, sigB);
    }
}
