// ==============================================================================================
// KiranaHub Zero-Trust Security: OAuth2 & OpenID Connect Asymmetric Token Authority
// [KYA KARTA HAI] RS256 (RSA 2048-bit asymmetric) digital signatures ke sath microservices ke liye cryptographically secure JWT tokens issue aur validate karta hai.
// [KYU ZAROORI HAI] Zero-trust security — internal microservices network me kisi bhi unauthorized caller ko bina cryptographic token ke access na dena.
// [KAISE CHALTA HAI] Private key se token sign hota hai; resource servers public key se token verify karte hain bina shared secret leak kiye.
// [OUTPUT / SIDE EFFECT] Signed OIDC access tokens and validated ClaimsPrincipals.
// [VARIATIONS TO TRY] Ed25519 elliptic curve signatures for sub-microsecond verification.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, DateTimeOffset, Exception types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, IEnumerable collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Cryptography namespace import karta hai.
// [KYU ZAROORI HAI] RSA asymmetric cryptographic algorithm engine provide karna.
// [KAISE CHALTA HAI] Security cryptography loaded.
// [OUTPUT / SIDE EFFECT] RSA primitives available.
// [VARIATIONS TO TRY] ECDsa for elliptic curves.
using System.Security.Cryptography;

// [KYA KARTA HAI] Security claims namespace import karta hai.
// [KYU ZAROORI HAI] Claim, ClaimsIdentity, ClaimsPrincipal types provide karna.
// [KAISE CHALTA HAI] Identity claims loaded.
// [OUTPUT / SIDE EFFECT] Security identity models available.
// [VARIATIONS TO TRY] None.
using System.Security.Claims;

// [KYA KARTA HAI] Microsoft IdentityModel tokens namespace import karta hai.
// [KYU ZAROORI HAI] JwtSecurityToken, RsaSecurityKey, SigningCredentials provide karna.
// [KAISE CHALTA HAI] IdentityModel library loaded.
// [OUTPUT / SIDE EFFECT] Token creation tools ready.
// [VARIATIONS TO TRY] JsonWebTokenHandler for modern .NET.
using Microsoft.IdentityModel.Tokens;

// [KYA KARTA HAI] JWT handler namespace import karta hai.
// [KYU ZAROORI HAI] JwtSecurityTokenHandler for serialization and validation.
// [KAISE CHALTA HAI] JWT handler loaded.
// [OUTPUT / SIDE EFFECT] Handler ready.
// [VARIATIONS TO TRY] None.
using System.IdentityModel.Tokens.Jwt;

// [KYA KARTA HAI] Namespace declare karta hai identity components ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.ZeroTrustSecurity.Identity.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Auth.Identity.
namespace KiranaHub.ZeroTrustSecurity.Identity;

// [KYA KARTA HAI] Dedicated OAuth2 Token Authority class define karta hai.
// [KYU ZAROORI HAI] Asymmetric token generation aur public-key validation centrally handle karna.
// [KAISE CHALTA HAI] Holds RSA 2048-bit key pair in memory and issues RS256 signed JWTs.
// [OUTPUT / SIDE EFFECT] Token server instance.
// [VARIATIONS TO TRY] Load keys from PEM certificate files or Azure Key Vault.
public class OAuth2TokenServer
{
    // [KYA KARTA HAI] OIDC Issuer URI identifier.
    // [KYU ZAROORI HAI] Token issuing authority verify karna (`iss` claim).
    // [KAISE CHALTA HAI] String constant.
    // [OUTPUT / SIDE EFFECT] Issuer set.
    // [VARIATIONS TO TRY] External HTTPS URL.
    public const string Issuer = "https://auth.kiranahub.internal";

    // [KYA KARTA HAI] Target Audience identifier.
    // [KYU ZAROORI HAI] Audience claim verify karna (`aud` claim).
    // [KAISE CHALTA HAI] String constant.
    // [OUTPUT / SIDE EFFECT] Audience set.
    // [VARIATIONS TO TRY] Multi-audience list.
    public const string Audience = "kiranahub.microservices";

    // [KYA KARTA HAI] RSA 2048-bit asymmetric cryptographic engine instance.
    // [KYU ZAROORI HAI] Private key se signing aur public key se verification karna.
    // [KAISE CHALTA HAI] RSA.Create(2048) generates private and public keys.
    // [OUTPUT / SIDE EFFECT] Cryptographic key pair generated.
    // [VARIATIONS TO TRY] 4096-bit for banking grade security.
    private readonly RSA _rsa = RSA.Create(2048);

    // [KYA KARTA HAI] JWT security token handler.
    // [KYU ZAROORI HAI] Token creation aur validation parse karna.
    // [KAISE CHALTA HAI] JwtSecurityTokenHandler instance.
    // [OUTPUT / SIDE EFFECT] Handler ready.
    // [VARIATIONS TO TRY] None.
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    // [KYA KARTA HAI] RS256 Signing credentials (private key).
    // [KYU ZAROORI HAI] Digital signature create karna.
    // [KAISE CHALTA HAI] RsaSecurityKey wraps RSA instance with SecurityAlgorithms.RsaSha256.
    // [OUTPUT / SIDE EFFECT] Signing credentials loaded.
    // [VARIATIONS TO TRY] RsaSsaPssSha256 (PS256).
    private readonly SigningCredentials _signingCredentials;

    // [KYA KARTA HAI] Public key parameters for token verification.
    // [KYU ZAROORI HAI] Resource servers ko bina private key diye tokens verify karne ki capability dena.
    // [KAISE CHALTA HAI] ExportParameters(false) exports only public components (Modulus, Exponent).
    // [OUTPUT / SIDE EFFECT] Public key security key.
    // [VARIATIONS TO TRY] JWKS (JSON Web Key Set) JSON endpoint.
    public RsaSecurityKey PublicKey { get; }

    // [KYA KARTA HAI] OAuth2 token server constructor initialize karta hai.
    // [KYU ZAROORI HAI] Key pair aur credentials initialize karna.
    // [KAISE CHALTA HAI] Creates signing credentials and extracts public key.
    // [OUTPUT / SIDE EFFECT] Token authority ready.
    // [VARIATIONS TO TRY] None.
    public OAuth2TokenServer()
    {
        var privateKey = new RsaSecurityKey(_rsa);
        _signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
        PublicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false));
    }

    // [KYA KARTA HAI] Asymmetric RS256 signed access token issue karta hai.
    // [KYU ZAROORI HAI] Authenticated user ya service ko cryptographically signed bearer token provide karna.
    // [KAISE CHALTA HAI] Builds Claims array, creates JwtSecurityToken with RS256 credentials, serializes to string.
    // [OUTPUT / SIDE EFFECT] Base64 encoded signed JWT string.
    // [VARIATIONS TO TRY] Add custom dark-store context claim.
    public string IssueAccessToken(
        string subject,
        string role,
        string clientAppId,
        IEnumerable<string> scopes,
        TimeSpan? lifetime = null)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(ClaimTypes.Role, role),
            new Claim("client_id", clientAppId)
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            Expires = expires,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    // [KYA KARTA HAI] JWT access token ko public key se cryptographically validate karta hai.
    // [KYU ZAROORI HAI] Tampered, expired, ya unauthorized tokens ko reject karna.
    // [KAISE CHALTA HAI] Validates signature against PublicKey, checks issuer, audience, and expiration.
    // [OUTPUT / SIDE EFFECT] Validated ClaimsPrincipal on success; throws SecurityTokenException on failure.
    // [VARIATIONS TO TRY] ClockSkew tolerance tuning.
    public ClaimsPrincipal ValidateToken(string tokenString)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = PublicKey,
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        var principal = _tokenHandler.ValidateToken(tokenString, validationParameters, out _);
        return principal;
    }
}
