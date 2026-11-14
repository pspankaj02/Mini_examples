using System.ComponentModel.DataAnnotations;

namespace KiranaHub.AuthAndSecurity.Models;

// =========================================================================================
// [TOPIC]: Authentication, Identity, and RBAC Models
// [PURPOSE]: User entity, registration/login contracts, and JWT authentication response.
// =========================================================================================

// [KYA KARTA HAI]: User identity record represent karta hai in-memory store me.
// [KYU ZAROORI HAI]: Claims, roles, hashed passwords, aur loyalty tier manage karna.
public class ApplicationUser
{
    // [KYA KARTA HAI]: User unique identifier.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Unique email address (username).
    public string Email { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Customer full name.
    public string FullName { get; set; } = string.Empty;

    // [KYA KARTA HAI]: PBKDF2 salted hash string (plain text password KABHI store nahi hota).
    public string PasswordHash { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Assigned security roles list (e.g. "Customer", "Admin", "StoreManager").
    public List<string> Roles { get; set; } = new();

    // [KYA KARTA HAI]: Customer reward points integer.
    public int LoyaltyPoints { get; set; }

    // [KYA KARTA HAI]: Prime VIP membership flag.
    public bool IsPrimeMember { get; set; }
}

// [KYA KARTA HAI]: POST /api/v1/auth/register request body contract.
// [KYU ZAROORI HAI]: New account creation payload validation.
public class RegisterRequest
{
    // [KYA KARTA HAI]: Email mandatory aur valid email format validation enforce karta hai.
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Password minimum 6 characters validation enforce karta hai.
    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Full name required validation.
    [Required]
    public string FullName { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Admin role request flag for test harness.
    public bool IsAdmin { get; set; } = false;

    // [KYA KARTA HAI]: Prime status flag.
    public bool IsPrime { get; set; } = false;

    // [KYA KARTA HAI]: Initial loyalty points balance.
    public int LoyaltyPoints { get; set; } = 50;
}

// [KYA KARTA HAI]: POST /api/v1/auth/login request contract.
// [KYU ZAROORI HAI]: User authentication credentials payload.
public class LoginRequest
{
    // [KYA KARTA HAI]: Email input field.
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Plaintext password attempt field for verification.
    [Required]
    public string Password { get; set; } = string.Empty;
}

// [KYA KARTA HAI]: JWT authentication response payload.
// [KYU ZAROORI HAI]: Client ko signed Bearer token, user claims, aur status communicate karna.
public record AuthResponse(
    // [KYA KARTA HAI]: Login/Registration success boolean.
    bool Success,
    // [KYA KARTA HAI]: Cryptographically signed Base64 JWT token string.
    string? Token,
    // [KYA KARTA HAI]: Authenticated email.
    string? Email,
    // [KYA KARTA HAI]: Authenticated full name.
    string? FullName,
    // [KYA KARTA HAI]: Assigned roles list.
    List<string>? Roles,
    // [KYA KARTA HAI]: Status or error message.
    string? Message
);
