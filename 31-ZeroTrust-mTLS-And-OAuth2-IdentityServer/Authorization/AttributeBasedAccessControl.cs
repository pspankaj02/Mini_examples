// ==============================================================================================
// KiranaHub Zero-Trust Security: Attribute-Based Access Control (ABAC) Context Policy Engine
// [KYA KARTA HAI] Static roles ke bajaye dynamic Subject, Resource, aur Environment attributes ke context me data access policies evaluate karta hai.
// [KYU ZAROORI HAI] Customer PII (Phone number, home address) data privacy — rider sirf tabhi address dekh sake jab order actually uske paas dispatched ho.
// [KAISE CHALTA HAI] Subject attributes, resource state, aur time window constraints ko policy matrix me evaluate karke dynamic masking provide karta hai.
// [OUTPUT / SIDE EFFECT] PolicyDecision (Permit / Deny) with contextual data masking.
// [VARIATIONS TO TRY] XACML standard or OPA (Open Policy Agent) Rego policies.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] String, DateTimeOffset, Exception types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] Dictionary, List provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Namespace declare karta hai authorization components ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.ZeroTrustSecurity.Authorization.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Security.Abac.
namespace KiranaHub.ZeroTrustSecurity.Authorization;

// [KYA KARTA HAI] Caller Subject attributes model record.
// [KYU ZAROORI HAI] Caller ki identity, role, aur assigned store capture karna.
// [KAISE CHALTA HAI] Immutable record with SubjectId, Role, AssignedDarkStoreId.
// [OUTPUT / SIDE EFFECT] Subject attributes context.
// [VARIATIONS TO TRY] Include device trust score.
public record SubjectContext(
    string SubjectId,
    string Role,
    string? AssignedDarkStoreId
);

// [KYA KARTA HAI] Target Resource attributes model record.
// [KYU ZAROORI HAI] Order state, customer PII, aur store location encapsulate karna.
// [KAISE CHALTA HAI] Immutable record with OrderId, CustomerId, DarkStoreId, Status, CustomerPhone, DeliveryAddress.
// [OUTPUT / SIDE EFFECT] Resource attributes context.
// [VARIATIONS TO TRY] Sensitivity classification tag (Confidential, Restricted).
public record OrderResourceContext(
    string OrderId,
    string CustomerId,
    string DarkStoreId,
    string Status,
    string CustomerPhone,
    string DeliveryAddress,
    DateTimeOffset? DispatchedAtUtc
);

// [KYA KARTA HAI] Environment contextual attributes record.
// [KYU ZAROORI HAI] Time of day, IP address, aur temporal limits capture karna.
// [KAISE CHALTA HAI] Immutable record with CurrentTimeUtc and ClientIp.
// [OUTPUT / SIDE EFFECT] Environment context.
// [VARIATIONS TO TRY] Threat level indicator.
public record EnvironmentContext(
    DateTimeOffset CurrentTimeUtc,
    string ClientIp
);

// [KYA KARTA HAI] ABAC Policy evaluation decision result record.
// [KYU ZAROORI HAI] Decision (Permit/Deny) aur masked/sanitized data payload return karna.
// [KAISE CHALTA HAI] Immutable record with IsPermitted, Reason, MaskedPhone, MaskedAddress.
// [OUTPUT / SIDE EFFECT] ABAC authorization decision.
// [VARIATIONS TO TRY] Field-level encryption keys.
public record AbacDecision(
    bool IsPermitted,
    string Reason,
    string SafePhone,
    string SafeAddress
);

// [KYA KARTA HAI] Attribute-Based Access Control (ABAC) Policy Evaluator class.
// [KYU ZAROORI HAI] Zero-trust contextual data filtering rules execute karna.
// [KAISE CHALTA HAI] Multi-attribute policy conditions evaluate karke fine-grained data access allow ya mask karta hai.
// [OUTPUT / SIDE EFFECT] ABAC evaluation engine.
// [VARIATIONS TO TRY] Policy Administration Point (PAP) dynamic rules.
public class AttributeBasedAccessControl
{
    // [KYA KARTA HAI] Order PII access request ko Subject, Resource, aur Environment attributes ke context me evaluate karta hai.
    // [KYU ZAROORI HAI] Fine-grained customer data protection (Rider can only view address during active delivery).
    // [KAISE CHALTA HAI] Checks Role, Store match, Dispatched status, and 30-minute time boundary.
    // [OUTPUT / SIDE EFFECT] AbacDecision with permitted status and safe fields.
    // [VARIATIONS TO TRY] Audit log emission on every evaluation.
    public AbacDecision AuthorizeOrderPiiAccess(
        SubjectContext subject,
        OrderResourceContext resource,
        EnvironmentContext env)
    {
        // -------------------------------------------------------------
        // Rule 1: Administrators have complete audit access
        // -------------------------------------------------------------
        if (subject.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return new AbacDecision(
                IsPermitted: true,
                Reason: "Administrator override permission granted.",
                SafePhone: resource.CustomerPhone,
                SafeAddress: resource.DeliveryAddress
            );
        }

        // -------------------------------------------------------------
        // Rule 2: Customers can only access their OWN orders
        // -------------------------------------------------------------
        if (subject.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            if (subject.SubjectId.Equals(resource.CustomerId, StringComparison.OrdinalIgnoreCase))
            {
                return new AbacDecision(
                    IsPermitted: true,
                    Reason: "Customer viewing their own order.",
                    SafePhone: resource.CustomerPhone,
                    SafeAddress: resource.DeliveryAddress
                );
            }
            return new AbacDecision(
                IsPermitted: false,
                Reason: "Access Denied: Customer cannot view other users' orders!",
                SafePhone: "REDACTED",
                SafeAddress: "REDACTED"
            );
        }

        // -------------------------------------------------------------
        // Rule 3: Rider Dynamic ABAC Access Policy
        // Condition A: Must be assigned to the same dark store.
        // Condition B: Order MUST be actively in 'Dispatched' status.
        // Condition C: Must be within 30 minutes of dispatch time window.
        // -------------------------------------------------------------
        if (subject.Role.Equals("Rider", StringComparison.OrdinalIgnoreCase))
        {
            // Dark Store boundary check
            if (!string.Equals(subject.AssignedDarkStoreId, resource.DarkStoreId, StringComparison.OrdinalIgnoreCase))
            {
                return new AbacDecision(
                    IsPermitted: false,
                    Reason: $"Access Denied: Rider assigned to store '{subject.AssignedDarkStoreId}' cannot access orders of store '{resource.DarkStoreId}'!",
                    SafePhone: MaskPhone(resource.CustomerPhone),
                    SafeAddress: "Address Masked (Cross-Store Protection)"
                );
            }

            // Order Status boundary check
            if (!resource.Status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase))
            {
                return new AbacDecision(
                    IsPermitted: false,
                    Reason: $"Access Denied: Rider cannot access PII for order in '{resource.Status}' state (PII only visible when actively Dispatched).",
                    SafePhone: MaskPhone(resource.CustomerPhone),
                    SafeAddress: "Address Masked (Order Not Dispatched)"
                );
            }

            // Time Window boundary check (within 30 minutes)
            if (resource.DispatchedAtUtc.HasValue)
            {
                var elapsedMinutes = (env.CurrentTimeUtc - resource.DispatchedAtUtc.Value).TotalMinutes;
                if (elapsedMinutes > 30.0)
                {
                    return new AbacDecision(
                        IsPermitted: false,
                        Reason: $"Access Denied: 30-minute delivery time window expired ({elapsedMinutes:F0} mins elapsed)!",
                        SafePhone: MaskPhone(resource.CustomerPhone),
                        SafeAddress: "Address Masked (Window Expired)"
                    );
                }
            }

            // All ABAC dynamic conditions met -> Grant full PII access for delivery
            return new AbacDecision(
                IsPermitted: true,
                Reason: "Active delivery context verified. Full PII access granted for doorstep fulfillment.",
                SafePhone: resource.CustomerPhone,
                SafeAddress: resource.DeliveryAddress
            );
        }

        // Default Deny
        return new AbacDecision(
            IsPermitted: false,
            Reason: $"Access Denied: Role '{subject.Role}' has no authorization for order PII.",
            SafePhone: "REDACTED",
            SafeAddress: "REDACTED"
        );
    }

    // [KYA KARTA HAI] Customer phone number mask karta hai (+91-9876543210 -> +91-XXXXX-43210).
    // [KYU ZAROORI HAI] Partial view security sanitization.
    // [KAISE CHALTA HAI] String manipulation masking prefix digits.
    // [OUTPUT / SIDE EFFECT] Masked phone string.
    // [VARIATIONS TO TRY] Regex masking.
    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 6) return "+91-XXXXX-XXXXX";
        return string.Concat("+91-XXXXX-", phone.AsSpan(phone.Length - 5));
    }
}
