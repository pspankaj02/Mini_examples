// ==============================================================================================
// KiranaHub Zero-Trust Security: Automated Verification & Cryptographic Testing Suite
// [KYA KARTA HAI] Asymmetric RS256 OAuth2 token issuance, Mutual TLS peer certificate verification, aur ABAC dynamic policy gating verify karta hai.
// [KYU ZAROORI HAI] Confirm karna ki zero-trust principles enforce hote hain, untrusted mTLS handshakes drop hote hain, aur customer PII contextually protect rehti hai.
// [KAISE CHALTA HAI] Program Main executes 3 deep tests: OAuth2 Token Authority, mTLS Cryptographic Handshake, and Dynamic ABAC Data Gating.
// [OUTPUT / SIDE EFFECT] Detailed console audit reports and clean exit code 0.
// [VARIATIONS TO TRY] Stress testing RSA signature verification throughput.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Guid, DateTimeOffset types provide karna.
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

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Claims querying ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Security claims namespace import karta hai.
// [KYU ZAROORI HAI] ClaimTypes access karna.
// [KAISE CHALTA HAI] Claims loaded.
// [OUTPUT / SIDE EFFECT] Claims ready.
// [VARIATIONS TO TRY] None.
using System.Security.Claims;

// [KYA KARTA HAI] Identity namespace import karta hai.
// [KYU ZAROORI HAI] OAuth2TokenServer access karna.
// [KAISE CHALTA HAI] References local identity namespace.
// [OUTPUT / SIDE EFFECT] OAuth2 types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ZeroTrustSecurity.Identity;

// [KYA KARTA HAI] Mutual TLS namespace import karta hai.
// [KYU ZAROORI HAI] TlsCertificateValidator and MtlsValidationResult access karna.
// [KAISE CHALTA HAI] References local mTLS namespace.
// [OUTPUT / SIDE EFFECT] mTLS types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ZeroTrustSecurity.MutualTls;

// [KYA KARTA HAI] ABAC authorization namespace import karta hai.
// [KYU ZAROORI HAI] AttributeBasedAccessControl and context records access karna.
// [KAISE CHALTA HAI] References local authorization namespace.
// [OUTPUT / SIDE EFFECT] ABAC types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.ZeroTrustSecurity.Authorization;

// [KYA KARTA HAI] Program entry point class define karta hai.
// [KYU ZAROORI HAI] Executable console application container.
// [KAISE CHALTA HAI] C# 13 top-level application wrapper.
// [OUTPUT / SIDE EFFECT] Entry class.
// [VARIATIONS TO TRY] Top-level statements.
public class Program
{
    // [KYA KARTA HAI] Application main entrypoint.
    // [KYU ZAROORI HAI] Zero-trust security tests execute karna.
    // [KAISE CHALTA HAI] CLR executes Main method.
    // [OUTPUT / SIDE EFFECT] Security audit logs.
    // [VARIATIONS TO TRY] None.
    public static void Main()
    {
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 31: Zero-Trust Security, mTLS & OAuth2 Suite   ");
        Console.WriteLine("====================================================================\n");

        // [TEST 1] Testing OAuth2 Asymmetric RS256 Token Authority.
        Console.WriteLine("[TEST 1] Testing OAuth2 Asymmetric (RS256) Token Authority...");
        var tokenServer = new OAuth2TokenServer();

        const string riderSub = "RIDER-SUNIL-99";
        var scopes = new[] { "orders:read", "delivery:update" };

        var token = tokenServer.IssueAccessToken(riderSub, role: "Rider", clientAppId: "kiranahub-mobile-rider", scopes);
        if (string.IsNullOrEmpty(token))
            throw new Exception("OAuth2 server failed to generate JWT access token!");

        // Validate token using ONLY the public key
        var principal = tokenServer.ValidateToken(token);
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

        if (subClaim != riderSub || roleClaim != "Rider")
            throw new Exception("ClaimsPrincipal validation mismatch!");

        Console.WriteLine($"  -> Issued RS256 JWT Token: {token[..25]}...{token[^15..]}");
        Console.WriteLine($"  -> Validated Claims: Sub='{subClaim}', Role='{roleClaim}', Issuer='{OAuth2TokenServer.Issuer}': PASSED");

        // [TEST 2] Testing Mutual TLS (mTLS) Peer Handshake & Whitelist Validation.
        Console.WriteLine("\n[TEST 2] Testing Mutual TLS (mTLS) Peer Handshake & Service Whitelisting...");

        // Generate KiranaHub Root CA in memory
        using var rootCa = TlsCertificateValidator.GenerateTestCertificate("KiranaHub Internal Root CA");
        var allowedServices = new[] { "checkout-api", "inventory-grpc", "payment-service" };
        var mTLSValidator = new TlsCertificateValidator(rootCa.Thumbprint, allowedServices);

        // Scenario A: Valid microservice certificate signed by Root CA ("checkout-api")
        using var validClientCert = TlsCertificateValidator.GenerateTestCertificate("checkout-api", rootCa);
        var validResult = mTLSValidator.ValidateClientCertificate(validClientCert, rootCa);

        if (!validResult.IsAuthorized)
            throw new Exception($"Expected authorized mTLS for valid client, but got: {validResult.FailureReason}");

        Console.WriteLine($"  -> mTLS Handshake for '{validResult.ServiceName}': Authorized={validResult.IsAuthorized}: PASSED");

        // Scenario B: Service not in whitelist ("rogue-monitoring-agent")
        using var rogueClientCert = TlsCertificateValidator.GenerateTestCertificate("rogue-monitoring-agent", rootCa);
        var rogueResult = mTLSValidator.ValidateClientCertificate(rogueClientCert, rootCa);

        if (rogueResult.IsAuthorized)
            throw new Exception("mTLS validator failed to block unauthorized service common name!");

        Console.WriteLine($"  -> Unauthorized Service Blocked: {rogueResult.FailureReason}: PASSED");

        // Scenario C: Certificate issued by untrusted foreign CA
        using var foreignCa = TlsCertificateValidator.GenerateTestCertificate("Untrusted Foreign Attacker CA");
        using var forgedCert = TlsCertificateValidator.GenerateTestCertificate("checkout-api", foreignCa);
        var forgedResult = mTLSValidator.ValidateClientCertificate(forgedCert, foreignCa);

        if (forgedResult.IsAuthorized)
            throw new Exception("mTLS validator accepted a certificate signed by an untrusted CA!");

        Console.WriteLine($"  -> Untrusted Foreign CA Rejected: {forgedResult.FailureReason}: PASSED");

        // [TEST 3] Testing Attribute-Based Access Control (ABAC) Dynamic PII Gating.
        Console.WriteLine("\n[TEST 3] Testing Attribute-Based Access Control (ABAC) Dynamic PII Policy Gating...");
        var abac = new AttributeBasedAccessControl();
        var now = DateTimeOffset.UtcNow;

        var orderResource = new OrderResourceContext(
            OrderId: "ORD-9912",
            CustomerId: "CUST-RAHUL-10",
            DarkStoreId: "DS-NOIDA-62",
            Status: "Dispatched",
            CustomerPhone: "+91-9876543210",
            DeliveryAddress: "Tower B, Flat 402, Sector 62, Noida",
            DispatchedAtUtc: now.AddMinutes(-10)
        );

        var env = new EnvironmentContext(now, "10.0.14.22");

        // Case A: Assigned rider during active 10-minute delivery -> FULL ACCESS
        var activeRider = new SubjectContext("RIDER-SUNIL-99", "Rider", "DS-NOIDA-62");
        var activeDecision = abac.AuthorizeOrderPiiAccess(activeRider, orderResource, env);

        if (!activeDecision.IsPermitted || activeDecision.SafeAddress.Contains("Masked"))
            throw new Exception("Active rider was denied delivery PII access!");

        Console.WriteLine($"  -> Active Delivery Context: Permitted={activeDecision.IsPermitted}, Address='{activeDecision.SafeAddress}': PASSED");

        // Case B: Order delivered -> Access Denied & PII MASKED
        var deliveredOrder = orderResource with { Status = "Delivered" };
        var deliveredDecision = abac.AuthorizeOrderPiiAccess(activeRider, deliveredOrder, env);

        if (deliveredDecision.IsPermitted || !deliveredDecision.SafePhone.Contains("+91-XXXXX"))
            throw new Exception("Customer PII was not masked after order delivery!");

        Console.WriteLine($"  -> Post-Delivery Context: Permitted={deliveredDecision.IsPermitted}, Masked Phone='{deliveredDecision.SafePhone}': PASSED");

        // Case C: Cross-Store Rider (Mumbai rider trying to access Noida order) -> BLOCKED
        var crossStoreRider = new SubjectContext("RIDER-AJAY-01", "Rider", "DS-MUMBAI-01");
        var crossStoreDecision = abac.AuthorizeOrderPiiAccess(crossStoreRider, orderResource, env);

        if (crossStoreDecision.IsPermitted)
            throw new Exception("ABAC engine allowed cross-store dark-store data access!");

        Console.WriteLine($"  -> Cross-Store Access Blocked: {crossStoreDecision.Reason}: PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All Zero-Trust Security, mTLS & ABAC checks PASSED!       ");
        Console.WriteLine("====================================================================\n");
    }
}
