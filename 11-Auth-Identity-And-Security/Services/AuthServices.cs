using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KiranaHub.AuthAndSecurity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace KiranaHub.AuthAndSecurity.Services;

// =========================================================================================
// [TOPIC]: Password Hashing (PBKDF2) & Cryptographic JWT Token Generation
// [PURPOSE]: Industry standard credential security, HMAC SHA-256 signing, and claims management.
// =========================================================================================

// [KYA KARTA HAI]: Microsoft.AspNetCore.Identity.PasswordHasher wrapper jo standard PBKDF2 algorithm use karta hai.
// [KYU ZAROORI HAI]: Passwords ko salt + cryptographic iterations ke sath irreversibly hash karna.
public class PasswordHasherService
{
    // [KYA KARTA HAI]: Framework PasswordHasher generic instance create karta hai for ApplicationUser.
    private readonly PasswordHasher<ApplicationUser> _hasher = new();

    // [KYA KARTA HAI]: Plain text password ko cryptographic salted hash string me convert karta hai.
    // [OUTPUT / RETURN]: Base64 encoded hash string (e.g. "AQAAAAIAAA...").
    public string HashPassword(ApplicationUser user, string plainPassword)
    {
        // [KYA KARTA HAI]: PBKDF2 hash computation execute karta hai.
        return _hasher.HashPassword(user, plainPassword);
    }

    // [KYA KARTA HAI]: User dwara diye gaye password ko stored hash ke against timing-safe verify karta hai.
    // [OUTPUT / RETURN]: True agar password match kare, false otherwise.
    public bool VerifyPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        // [KYA KARTA HAI]: Hash verification result compute karta hai.
        var result = _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        // [KYA KARTA HAI]: Success ya SuccessRehashNeeded dono states ko valid authenticate maanta hai.
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}

// [KYA KARTA HAI]: Stateless signed JWT Bearer tokens create karta hai for authenticated API sessions.
// [KYU ZAROORI HAI]: Microservices aur APIs ke liye stateless, scalable authentication token provide karna.
public class JwtTokenService
{
    // [KYA KARTA HAI]: 256-bit symmetric encryption key string.
    public const string SecretKey = "KiranaHub_Super_Secret_Key_For_Mastering_DotNet_Security_2026!";

    // [KYA KARTA HAI]: Token issuer identity URI/string ("KiranaHub.AuthServer").
    public const string Issuer = "KiranaHub.AuthServer";

    // [KYA KARTA HAI]: Allowed audience identity ("KiranaHub.Clients").
    public const string Audience = "KiranaHub.Clients";

    // [KYA KARTA HAI]: User identity attributes se claims pack karke signed JWT string return karta hai.
    // [OUTPUT / RETURN]: Signed JWT string (Header.Payload.Signature).
    public string GenerateToken(ApplicationUser user)
    {
        // [KYA KARTA HAI]: Claims list initialize karta hai with standard identity claims.
        var claims = new List<Claim>
        {
            // [KYA KARTA HAI]: Subject claim (User ID).
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // [KYA KARTA HAI]: Email claim.
            new(JwtRegisteredClaimNames.Email, user.Email),
            // [KYA KARTA HAI]: Unique JWT ID claim (JTI) for replay attack prevention.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // [KYA KARTA HAI]: Custom claim: Full Name.
            new("FullName", user.FullName),
            // [KYA KARTA HAI]: Custom claim: Loyalty points for policy authorization.
            new("LoyaltyPoints", user.LoyaltyPoints.ToString()),
            // [KYA KARTA HAI]: Custom claim: Prime status boolean string.
            new("IsPrimeMember", user.IsPrimeMember.ToString().ToLower())
        };

        // [KYA KARTA HAI]: User ke assign kiye gaye roles loop karta hai.
        foreach (var role in user.Roles)
        {
            // [KYA KARTA HAI]: Standard ClaimTypes.Role claim add karta hai for RBAC.
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // [KYA KARTA HAI]: Secret key string ko UTF-8 byte array me encode karke symmetric key object banata hai.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

        // [KYA KARTA HAI]: HMAC SHA-256 digital signing credentials prepare karta hai.
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // [KYA KARTA HAI]: JWT token descriptor configuration define karta hai (claims, expiry, issuer, audience, signature).
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // [KYA KARTA HAI]: Claims identity attach karta hai.
            Subject = new ClaimsIdentity(claims),
            // [KYA KARTA HAI]: Expiry 2 hours in the future set karta hai.
            Expires = DateTime.UtcNow.AddHours(2),
            // [KYA KARTA HAI]: Issuer set karta hai.
            Issuer = Issuer,
            // [KYA KARTA HAI]: Audience set karta hai.
            Audience = Audience,
            // [KYA KARTA HAI]: Digital signature credentials attach karta hai.
            SigningCredentials = creds
        };

        // [KYA KARTA HAI]: JwtSecurityTokenHandler instantiate karta hai.
        var tokenHandler = new JwtSecurityTokenHandler();

        // [KYA KARTA HAI]: SecurityToken object create karta hai descriptor se.
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // [KYA KARTA HAI]: Token ko standard compact Base64URL string (xxx.yyy.zzz) me serialize karke return karta hai.
        return tokenHandler.WriteToken(token);
    }
}
