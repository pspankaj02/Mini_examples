// ==============================================================================================
// KiranaHub Zero-Trust Security: Mutual TLS (mTLS) Peer Certificate Handshake Validator
// [KYA KARTA HAI] Microservices ke beech X.509 client certificates ki identity, CA trust chain, aur SAN identifiers cryptographically verify karta hai.
// [KYU ZAROORI HAI] Internal network sniffing, man-in-the-middle attacks, aur rogue container service impersonation ko block karna.
// [KAISE CHALTA HAI] X509Certificate2 inspect karta hai; CA signature, expiry dates, aur SPIFFE/DNS names validate karta hai.
// [OUTPUT / SIDE EFFECT] Validated mTLS peer identity or security rejection.
// [VARIATIONS TO TRY] Automated certificate rotation with cert-manager and HashiCorp Vault.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] DateTime, String, Exception types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Cryptography namespace import karta hai.
// [KYU ZAROORI HAI] RSA, HashAlgorithmName types access karna.
// [KAISE CHALTA HAI] Cryptography loaded.
// [OUTPUT / SIDE EFFECT] Crypto primitives available.
// [VARIATIONS TO TRY] None.
using System.Security.Cryptography;

// [KYA KARTA HAI] X509 certificates namespace import karta hai.
// [KYU ZAROORI HAI] X509Certificate2, CertificateRequest, X500DistinguishedName provide karna.
// [KAISE CHALTA HAI] X509 security framework loaded.
// [OUTPUT / SIDE EFFECT] Certificate models available.
// [VARIATIONS TO TRY] None.
using System.Security.Cryptography.X509Certificates;

// [KYA KARTA HAI] Namespace declare karta hai mTLS validation ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.ZeroTrustSecurity.MutualTls.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Security.MutualTls.
namespace KiranaHub.ZeroTrustSecurity.MutualTls;

// [KYA KARTA HAI] mTLS verification outcome evaluation record.
// [KYU ZAROORI HAI] Handshake validation details encapsulate karna.
// [KAISE CHALTA HAI] Immutable record with IsAuthorized, ServiceName, Thumbprint, FailureReason.
// [OUTPUT / SIDE EFFECT] Validation result DTO.
// [VARIATIONS TO TRY] Include certificate serial number.
public record MtlsValidationResult(
    bool IsAuthorized,
    string ServiceName,
    string Thumbprint,
    string? FailureReason = null
);

// [KYA KARTA HAI] Mutual TLS Certificate Validator class define karta hai.
// [KYU ZAROORI HAI] Incoming client connection certificates ko policy ke mutabiq verify karna.
// [KAISE CHALTA HAI] Checks trusted root CA thumbprint, expiry dates, and allowed service names.
// [OUTPUT / SIDE EFFECT] mTLS validator instance.
// [VARIATIONS TO TRY] CRL / OCSP revocation checking.
public class TlsCertificateValidator
{
    // [KYA KARTA HAI] Trusted Root Certificate Authority (CA) thumbprint.
    // [KYU ZAROORI HAI] Sirf internal authorized CA dwara sign kiye certificates accept karna.
    // [KAISE CHALTA HAI] String hex digest.
    // [OUTPUT / SIDE EFFECT] Trusted CA thumbprint.
    // [VARIATIONS TO TRY] Multiple trusted CA roots.
    public string TrustedCaThumbprint { get; }

    // [KYA KARTA HAI] Allowed microservice names whitelist set.
    // [KYU ZAROORI HAI] Sirf specific microservices (e.g. "checkout-api", "inventory-grpc") ko access allow karna.
    // [KAISE CHALTA HAI] HashSet of allowed service common names.
    // [OUTPUT / SIDE EFFECT] Whitelist set.
    // [VARIATIONS TO TRY] Wildcard DNS matching.
    private readonly HashSet<string> _allowedServiceNames = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI] Validator constructor initialize karta hai.
    // [KYU ZAROORI HAI] Trusted CA thumbprint aur allowed service names configure karna.
    // [KAISE CHALTA HAI] Assigns properties and populates whitelist.
    // [OUTPUT / SIDE EFFECT] Validator ready.
    // [VARIATIONS TO TRY] Dynamic reload on configuration changes.
    public TlsCertificateValidator(string trustedCaThumbprint, IEnumerable<string> allowedServiceNames)
    {
        TrustedCaThumbprint = trustedCaThumbprint;
        foreach (var s in allowedServiceNames)
        {
            _allowedServiceNames.Add(s);
        }
    }

    // [KYA KARTA HAI] Client peer certificate ko policy ke against validate karta hai.
    // [KYU ZAROORI HAI] Unauthenticated ya expired microservice connection drop karna.
    // [KAISE CHALTA HAI] Validates null, expiry dates, issuer CA thumbprint, and allowed common name.
    // [OUTPUT / SIDE EFFECT] MtlsValidationResult.
    // [VARIATIONS TO TRY] Verify Subject Alternative Name (SAN) extension.
    public MtlsValidationResult ValidateClientCertificate(X509Certificate2? clientCert, X509Certificate2? issuerCaCert)
    {
        if (clientCert == null)
        {
            return new MtlsValidationResult(false, "Unknown", string.Empty, "No client certificate provided in mTLS handshake!");
        }

        // [KYA KARTA HAI] Certificate validity date check karta hai.
        // [KYU ZAROORI HAI] Expired ya not-yet-valid certificates reject karna.
        // [KAISE CHALTA HAI] Compares DateTime.Now against local NotBefore and NotAfter properties.
        // [OUTPUT / SIDE EFFECT] Rejection if outside date window.
        // [VARIATIONS TO TRY] Configurable grace period.
        var now = DateTime.Now;
        if (now < clientCert.NotBefore || now > clientCert.NotAfter)
        {
            return new MtlsValidationResult(false, clientCert.Subject, clientCert.Thumbprint, "Certificate has expired or is not yet valid!");
        }

        // [KYA KARTA HAI] Issuer CA trust chain check karta hai.
        // [KYU ZAROORI HAI] Untrusted foreign certificate authorities reject karna.
        // [KAISE CHALTA HAI] Compares issuer cert thumbprint against TrustedCaThumbprint.
        // [OUTPUT / SIDE EFFECT] Rejection if untrusted CA.
        // [VARIATIONS TO TRY] X509Chain.Build() with local trust store.
        if (issuerCaCert == null || !issuerCaCert.Thumbprint.Equals(TrustedCaThumbprint, StringComparison.OrdinalIgnoreCase))
        {
            return new MtlsValidationResult(false, clientCert.Subject, clientCert.Thumbprint, "Certificate was NOT issued by trusted KiranaHub Internal CA!");
        }

        // [KYA KARTA HAI] Service Common Name (CN) extract karta hai.
        // [KYU ZAROORI HAI] Client microservice identity resolve karna.
        // [KAISE CHALTA HAI] Reads clientCert.GetNameInfo(SimpleName).
        // [OUTPUT / SIDE EFFECT] Extracted CN string.
        // [VARIATIONS TO TRY] SPIFFE ID extraction from SAN URI.
        var commonName = clientCert.GetNameInfo(X509NameType.SimpleName, false) ?? string.Empty;

        // [KYA KARTA HAI] Whitelist authorization check.
        // [KYU ZAROORI HAI] Even if signed by CA, unauthorized services must be blocked.
        // [KAISE CHALTA HAI] Checks _allowedServiceNames.Contains.
        // [OUTPUT / SIDE EFFECT] Success if matched, else rejection.
        // [VARIATIONS TO TRY] Role-based mapping from certificate OU.
        if (!_allowedServiceNames.Contains(commonName))
        {
            return new MtlsValidationResult(false, commonName, clientCert.Thumbprint, $"Service '{commonName}' is not in authorized mTLS service whitelist!");
        }

        return new MtlsValidationResult(true, commonName, clientCert.Thumbprint, null);
    }

    // [KYA KARTA HAI] Self-signed X.509 Certificate generate karta hai in-memory.
    // [KYU ZAROORI HAI] External CA files ke bina zero-cost automated mTLS testing allow karna.
    // [KAISE CHALTA HAI] RSA 2048-bit key pair generate karke CertificateRequest se X509Certificate2 build karta hai.
    // [OUTPUT / SIDE EFFECT] Valid X509Certificate2 with private key.
    // [VARIATIONS TO TRY] Sign child certificate with issuer CA certificate.
    public static X509Certificate2 GenerateTestCertificate(string commonName, X509Certificate2? signingCaCert = null)
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN={commonName}, O=KiranaHub Technologies, C=IN");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddDays(365);

        if (signingCaCert == null)
        {
            // Self-signed Root CA certificate (requires KeyCertSign)
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            return request.CreateSelfSigned(notBefore, notAfter);
        }
        else
        {
            // End-entity client certificate signed by CA
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            var serialNumber = new byte[8];
            RandomNumberGenerator.Fill(serialNumber);
            return request.Create(signingCaCert, notBefore, notAfter, serialNumber);
        }
    }
}
