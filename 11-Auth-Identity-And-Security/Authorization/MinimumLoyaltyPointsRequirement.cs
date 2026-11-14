using Microsoft.AspNetCore.Authorization;

namespace KiranaHub.AuthAndSecurity.Authorization;

// =========================================================================================
// [TOPIC]: Custom Policy-Based Authorization (Requirement & Handler)
// [PURPOSE]: Fine-grained dynamic business rule evaluation beyond static roles.
// =========================================================================================

// [KYA KARTA HAI]: IAuthorizationRequirement marker interface implement karta hai minimum loyalty points constraint specify karne ke liye.
// [KYU ZAROORI HAI]: Policy definition me configurable threshold parameter pass karna (e.g. 100 points).
public class MinimumLoyaltyPointsRequirement : IAuthorizationRequirement
{
    // [KYA KARTA HAI]: Required points threshold property.
    public int RequiredPoints { get; }

    // [KYA KARTA HAI]: Requirement constructor.
    public MinimumLoyaltyPointsRequirement(int requiredPoints)
    {
        // [KYA KARTA HAI]: Threshold property assign karta hai.
        RequiredPoints = requiredPoints;
    }
}

// [KYA KARTA HAI]: AuthorizationHandler<MinimumLoyaltyPointsRequirement> implement karta hai jo actual policy logic evaluate karta hai.
// [KYU ZAROORI HAI]: JWT claims inspect karke user ko access grant (Succeed) ya deny karna.
public class MinimumLoyaltyPointsHandler : AuthorizationHandler<MinimumLoyaltyPointsRequirement>
{
    // [KYA KARTA HAI]: Core requirement evaluation method.
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumLoyaltyPointsRequirement requirement)
    {
        // ---------------------------------------------------------------------------------
        // 1. PRIME MEMBER BYPASS CHECK
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: User ke ClaimsPrincipal se "IsPrimeMember" claim value extract karta hai.
        var isPrimeClaim = context.User.FindFirst("IsPrimeMember")?.Value;

        // [KYA KARTA HAI]: Check karta hai ki claim boolean true hai ya nahi.
        if (bool.TryParse(isPrimeClaim, out bool isPrime) && isPrime)
        {
            // [KYA KARTA HAI]: Requirement mark as succeeded (Access Granted!).
            // [OUTPUT / SIDE EFFECT]: Authorization policy passes.
            context.Succeed(requirement);

            // [KYA KARTA HAI]: Task completed return karta hai.
            return Task.CompletedTask;
        }

        // ---------------------------------------------------------------------------------
        // 2. LOYALTY POINTS THRESHOLD CHECK
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: User claims se "LoyaltyPoints" integer claim value extract karta hai.
        var pointsClaim = context.User.FindFirst("LoyaltyPoints")?.Value;

        // [KYA KARTA HAI]: Check karta hai ki points required points se greater than or equal to hain ya nahi.
        if (int.TryParse(pointsClaim, out int points) && points >= requirement.RequiredPoints)
        {
            // [KYA KARTA HAI]: Requirement mark as succeeded (Access Granted!).
            context.Succeed(requirement);

            // [KYA KARTA HAI]: Task completed return karta hai.
            return Task.CompletedTask;
        }

        // [KYA KARTA HAI]: Agar koi condition meet na ho, context.Succeed call nahi hota; request automatically 403 Forbidden ban jati hai.
        return Task.CompletedTask;
    }
}
