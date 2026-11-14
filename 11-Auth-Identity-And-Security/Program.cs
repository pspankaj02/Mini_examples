using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using KiranaHub.AuthAndSecurity.Authorization;
using KiranaHub.AuthAndSecurity.Models;
using KiranaHub.AuthAndSecurity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

// =========================================================================================
// [TOPIC]: ASP.NET Core Authentication, Identity, RBAC, Rate Limiting & API Versioning
// [PURPOSE]: Enterprise security pipeline configuration with JWT Bearer tokens and policies.
// =========================================================================================

// [KYA KARTA HAI]: WebApplicationBuilder instance allocate karta hai.
var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------------------
// 1. SERVICES & AUTH CONFIGURATION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: PBKDF2 PasswordHasherService ko Singleton register karta hai.
builder.Services.AddSingleton<PasswordHasherService>();

// [KYA KARTA HAI]: JwtTokenService ko Singleton register karta hai.
builder.Services.AddSingleton<JwtTokenService>();

// [KYA KARTA HAI]: Custom authorization requirement handler MinimumLoyaltyPointsHandler ko register karta hai.
builder.Services.AddSingleton<IAuthorizationHandler, MinimumLoyaltyPointsHandler>();

// [KYA KARTA HAI]: Authentication subsystem initialize karta hai with JwtBearer default scheme.
// [KYU ZAROORI HAI]: Incoming HTTP requests me "Authorization: Bearer <TOKEN>" header validate karna.
builder.Services.AddAuthentication(options =>
{
    // [KYA KARTA HAI]: Default authentication scheme ko JwtBearer set karta hai.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // [KYA KARTA HAI]: Default challenge scheme (unauthenticated requests ko 401 challenge bhejna).
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// [KYA KARTA HAI]: JWT Bearer options aur validation parameters configure karta hai.
.AddJwtBearer(options =>
{
    // [KYA KARTA HAI]: Token validation parameters define karta hai.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // [KYA KARTA HAI]: Issuer validation enable karta hai.
        ValidateIssuer = true,
        // [KYA KARTA HAI]: Expected issuer matching string.
        ValidIssuer = JwtTokenService.Issuer,
        // [KYA KARTA HAI]: Audience validation enable karta hai.
        ValidateAudience = true,
        // [KYA KARTA HAI]: Expected audience string.
        ValidAudience = JwtTokenService.Audience,
        // [KYA KARTA HAI]: Expiry lifetime validation enable karta hai.
        ValidateLifetime = true,
        // [KYA KARTA HAI]: Cryptographic digital signature validation enable karta hai.
        ValidateIssuerSigningKey = true,
        // [KYA KARTA HAI]: HMAC SHA-256 symmetric secret key supply karta hai.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTokenService.SecretKey)),
        // [KYA KARTA HAI]: Clock skew zero set karta hai for strict immediate token expiry.
        ClockSkew = TimeSpan.Zero
    };
});

// [KYA KARTA HAI]: Authorization subsystem policies configure karta hai.
builder.Services.AddAuthorization(options =>
{
    // [KYA KARTA HAI]: Role-based policy "AdminOnly" define karta hai (user must have role "Admin").
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // [KYA KARTA HAI]: Policy-based requirement "PrimeCustomerPolicy" define karta hai (100 loyalty points required).
    options.AddPolicy("PrimeCustomerPolicy", policy =>
        policy.Requirements.Add(new MinimumLoyaltyPointsRequirement(requiredPoints: 100)));
});

// [KYA KARTA HAI]: Asp.Versioning.Http API Versioning services add karta hai.
builder.Services.AddApiVersioning(options =>
{
    // [KYA KARTA HAI]: Default API version 1.0 set karta hai.
    options.DefaultApiVersion = new ApiVersion(1, 0);
    // [KYA KARTA HAI]: Agar client version specify na kare toh default 1.0 assume kare.
    options.AssumeDefaultVersionWhenUnspecified = true;
    // [KYA KARTA HAI]: Response headers me "api-supported-versions" report kare.
    options.ReportApiVersions = true;
    // [KYA KARTA HAI]: URL segment (/v1/), header (X-Api-Version), aur query string (api-version) teeno se version read kare.
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version")
    );
});

// [KYA KARTA HAI]: Fixed-Window Rate Limiting configure karta hai (5 requests per 10 seconds).
// [KYU ZAROORI HAI]: DoS/Brute-force attack prevention.
builder.Services.AddRateLimiter(options =>
{
    // [KYA KARTA HAI]: Rate limit exceed hone par HTTP 429 Too Many Requests status code set karta hai.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // [KYA KARTA HAI]: "StrictLimiter" named policy add karta hai.
    options.AddFixedWindowLimiter("StrictLimiter", limiter =>
    {
        // [KYA KARTA HAI]: 10-second window me maximum 5 requests allow karta hai.
        limiter.PermitLimit = 5;
        // [KYA KARTA HAI]: 10 seconds ka fixed window duration.
        limiter.Window = TimeSpan.FromSeconds(10);
        // [KYA KARTA HAI]: Queue processing order.
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        // [KYA KARTA HAI]: Zero queue limit (burst requests beyond 5 are immediately rejected with 429).
        limiter.QueueLimit = 0;
    });
});

// [KYA KARTA HAI]: WebApplication build karta hai.
var app = builder.Build();

// ----------------------------------------------------------------------------------------
// 2. SECURITY HEADERS MIDDLEWARE
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Custom inline middleware jo HTTP response me standard OWASP security headers inject karta hai.
app.Use(async (context, next) =>
{
    // [KYA KARTA HAI]: MIME type sniffing disable karta hai.
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    // [KYA KARTA HAI]: Clickjacking prevention: iframe embedding forbid karta hai.
    context.Response.Headers["X-Frame-Options"] = "DENY";
    // [KYA KARTA HAI]: Referrer leakage prevention.
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    // [KYA KARTA HAI]: Next middleware ko invoke karta hai.
    await next();
});

// [KYA KARTA HAI]: Rate limiter middleware activate karta hai.
app.UseRateLimiter();

// [KYA KARTA HAI]: Authentication middleware pipeline me attach karta hai (JWT token parser).
app.UseAuthentication();

// [KYA KARTA HAI]: Authorization middleware pipeline me attach karta hai (Role & Policy enforcer).
app.UseAuthorization();

// ----------------------------------------------------------------------------------------
// 3. IN-MEMORY USER STORE (SEEDED WITH ADMIN, PRIME & REGULAR USERS)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Demo user database maintain karne ke liye in-memory list.
var userStore = new List<ApplicationUser>();

// [KYA KARTA HAI]: DI container se PasswordHasherService resolve karta hai.
var hasher = app.Services.GetRequiredService<PasswordHasherService>();

// [KYA KARTA HAI]: DI container se JwtTokenService resolve karta hai.
var tokenService = app.Services.GetRequiredService<JwtTokenService>();

// [KYA KARTA HAI]: Seed Admin User account instantiate karta hai.
var admin = new ApplicationUser
{
    Id = 1,
    Email = "admin@kiranahub.com",
    FullName = "Store SuperAdmin",
    Roles = new() { "Admin", "Customer" },
    LoyaltyPoints = 500,
    IsPrimeMember = true
};
// [KYA KARTA HAI]: Admin password ko PBKDF2 hash karta hai.
admin.PasswordHash = hasher.HashPassword(admin, "Admin@12345");
// [KYA KARTA HAI]: Admin ko user store me add karta hai.
userStore.Add(admin);

// [KYA KARTA HAI]: Seed Prime Customer instantiate karta hai.
var prime = new ApplicationUser
{
    Id = 2,
    Email = "prime@kiranahub.com",
    FullName = "Pankaj Prime Customer",
    Roles = new() { "Customer" },
    LoyaltyPoints = 150,
    IsPrimeMember = true
};
// [KYA KARTA HAI]: Prime user password hash karta hai.
prime.PasswordHash = hasher.HashPassword(prime, "Prime@12345");
// [KYA KARTA HAI]: Store me add karta hai.
userStore.Add(prime);

// [KYA KARTA HAI]: Seed Regular Customer (Low points, Non-prime) instantiate karta hai.
var regular = new ApplicationUser
{
    Id = 3,
    Email = "regular@kiranahub.com",
    FullName = "Regular Shopper",
    Roles = new() { "Customer" },
    LoyaltyPoints = 20,
    IsPrimeMember = false
};
// [KYA KARTA HAI]: Regular password hash karta hai.
regular.PasswordHash = hasher.HashPassword(regular, "Regular@12345");
// [KYA KARTA HAI]: Store me add karta hai.
userStore.Add(regular);

// ----------------------------------------------------------------------------------------
// 4. AUTHENTICATION ENDPOINTS
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: POST /api/auth/login endpoint map karta hai.
app.MapPost("/api/auth/login", (LoginRequest req) =>
{
    // [KYA KARTA HAI]: Email se matching user lookup karta hai.
    var user = userStore.FirstOrDefault(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase));

    // [KYA KARTA HAI]: Check karta hai ki user mila aur password hash match hua ya nahi.
    if (user == null || !hasher.VerifyPassword(user, user.PasswordHash, req.Password))
    {
        // [KYA KARTA HAI]: 401 Unauthorized status code return karta hai invalid credentials par.
        return Results.Json(new AuthResponse(false, null, null, null, null, "Invalid email or password."), statusCode: 401);
    }

    // [KYA KARTA HAI]: Authenticated user ke liye signed JWT Bearer token generate karta hai.
    string token = tokenService.GenerateToken(user);

    // [KYA KARTA HAI]: 200 OK ke sath AuthResponse return karta hai containing JWT token.
    return Results.Ok(new AuthResponse(true, token, user.Email, user.FullName, user.Roles, "Login successful."));
});

// ----------------------------------------------------------------------------------------
// 5. ROLE-BASED & POLICY-BASED PROTECTED ENDPOINTS
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: GET /api/admin/dashboard endpoint map karta hai protected by [Authorize(Policy = "AdminOnly")].
// [KYU ZAROORI HAI]: Role-based authorization demonstration (Sirf Admin role user access kar sakta hai).
app.MapGet("/api/admin/dashboard", [Authorize(Policy = "AdminOnly")] () =>
{
    // [KYA KARTA HAI]: Secret dashboard metrics return karta hai.
    return Results.Ok(new
    {
        Message = "Welcome to the Secret Store Admin Dashboard!",
        TotalRevenue = "Rs. 1,45,000",
        ActiveRiders = 12
    });
});

// [KYA KARTA HAI]: GET /api/exclusive/prime-deals endpoint map karta hai protected by [Authorize(Policy = "PrimeCustomerPolicy")].
// [KYU ZAROORI HAI]: Dynamic requirement demonstration (LoyaltyPoints >= 100 or IsPrimeMember == true).
app.MapGet("/api/exclusive/prime-deals", [Authorize(Policy = "PrimeCustomerPolicy")] () =>
{
    // [KYA KARTA HAI]: Prime exclusive deals array return karta hai.
    return Results.Ok(new
    {
        Message = "Unlocked Exclusive Kirana Prime Discounts!",
        Deals = new[] { "50% OFF on Extra Virgin Olive Oil", "Free 1kg Sugar with Basmati Rice" }
    });
});

// ----------------------------------------------------------------------------------------
// 6. RATE-LIMITED ENDPOINT
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Strict rate limiter endpoint map karta hai.
// [OUTPUT / BEHAVIOR]: 5 calls allowed per 10s, 6th call returns 429 Too Many Requests.
app.MapGet("/api/test/rate-limited", () =>
{
    // [KYA KARTA HAI]: Normal success message return karta hai within limit.
    return Results.Ok(new { Message = "Request allowed within rate limit threshold." });
}).RequireRateLimiting("StrictLimiter");

// ----------------------------------------------------------------------------------------
// 7. API VERSIONING (v1 vs v2)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: ApiVersionSet build karta hai declaring version 1.0 and 2.0.
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// [KYA KARTA HAI]: Version 1 endpoint map karta hai (/api/v1/orders).
app.MapGet("/api/v{version:apiVersion}/orders", () =>
{
    // [KYA KARTA HAI]: Version 1 response schema return karta hai (basic order status).
    return Results.Ok(new
    {
        Version = "1.0",
        Orders = new[]
        {
            new { OrderId = "ORD-001", Status = "Delivered", ItemsCount = 2 }
        }
    });
})
// [KYA KARTA HAI]: Version set assign karta hai.
.WithApiVersionSet(apiVersionSet)
// [KYA KARTA HAI]: Version 1.0 se map karta hai.
.MapToApiVersion(new ApiVersion(1, 0));

// [KYA KARTA HAI]: Version 2 endpoint map karta hai (/api/v2/orders).
app.MapGet("/api/v{version:apiVersion}/orders", () =>
{
    // [KYA KARTA HAI]: Version 2 response schema return karta hai (enhanced with real-time GPS coordinates and ETA).
    return Results.Ok(new
    {
        Version = "2.0 (Enhanced Live Tracking)",
        Orders = new[]
        {
            new
            {
                OrderId = "ORD-001",
                Status = "Out For Delivery",
                ItemsCount = 2,
                EstimatedMinutesArrival = 6,
                RiderLocation = new { Latitude = 28.6139, Longitude = 77.2090 }
            }
        }
    });
})
// [KYA KARTA HAI]: Version set attach karta hai.
.WithApiVersionSet(apiVersionSet)
// [KYA KARTA HAI]: Version 2.0 se map karta hai.
.MapToApiVersion(new ApiVersion(2, 0));

// [KYA KARTA HAI]: Kestrel server start karta hai.
app.Run();
