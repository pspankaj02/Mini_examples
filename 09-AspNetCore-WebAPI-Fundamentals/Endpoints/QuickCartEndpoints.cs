using Microsoft.AspNetCore.Http.HttpResults;

namespace KiranaHub.WebApi.Endpoints;

// =========================================================================================
// [TOPIC]: ASP.NET Core Minimal APIs with TypedResults & Route Groups
// [PURPOSE]: High-performance microservice endpoints with compile-time type safety.
// =========================================================================================

// [KYA KARTA HAI]: Minimal API request/response item record represent karta hai.
// [KYU ZAROORI HAI]: Compact lightweight model without class ceremony.
public record QuickCartItem(string Sku, int Quantity);

// [KYA KARTA HAI]: Static class containing extension method to map Minimal API endpoints.
// [KYU ZAROORI HAI]: Clean architecture: Program.cs ko cluttered hone se bachana.
public static class QuickCartEndpoints
{
    // [KYA KARTA HAI]: In-memory dictionary for quick cart items (SKU -> Quantity).
    private static readonly Dictionary<string, int> QuickCart = new();

    // [KYA KARTA HAI]: Extension method jo endpoint routes ko pipeline builder par register karta hai.
    // [KYU ZAROORI HAI]: Modular routing configuration.
    public static void MapQuickCartEndpoints(this IEndpointRouteBuilder app)
    {
        // [KYA KARTA HAI]: Route Group create karta hai prefix "/api/quick-cart" ke sath aur OpenAPI tag assign karta hai.
        // [KYU ZAROORI HAI]: Group-level middleware, prefixes, aur Swagger categorization apply karna.
        var group = app.MapGroup("/api/quick-cart")
                       .WithTags("Minimal API - Quick Cart");

        // [KYA KARTA HAI]: GET /api/quick-cart endpoint map karta hai using TypedResults.Ok().
        // [KYU ZAROORI HAI]: Compile-time return type safety without boxing into IActionResult.
        // [OUTPUT / RETURN]: 200 OK with dictionary of cart items.
        group.MapGet("/", () => TypedResults.Ok(QuickCart));

        // [KYA KARTA HAI]: POST /api/quick-cart endpoint map karta hai with union return type Results<Created, BadRequest>.
        // [KYU ZAROORI HAI]: Compile-time check: endpoint can only return Created or BadRequest.
        group.MapPost("/", Results<Created<QuickCartItem>, BadRequest<string>> (QuickCartItem item) =>
        {
            // [KYA KARTA HAI]: Input parameters validation check.
            if (string.IsNullOrWhiteSpace(item.Sku) || item.Quantity <= 0)
            {
                // [KYA KARTA HAI]: 400 Bad Request return karta hai with validation message.
                return TypedResults.BadRequest("Valid SKU and positive quantity required.");
            }

            // [KYA KARTA HAI]: Dictionary me quantity add/accumulate karta hai.
            QuickCart[item.Sku] = QuickCart.GetValueOrDefault(item.Sku, 0) + item.Quantity;

            // [KYA KARTA HAI]: HTTP 201 Created return karta hai with resource URI.
            return TypedResults.Created($"/api/quick-cart/{item.Sku}", item);
        });

        // [KYA KARTA HAI]: DELETE /api/quick-cart/{sku} endpoint map karta hai with union return type Results<NoContent, NotFound>.
        group.MapDelete("/{sku}", Results<NoContent, NotFound> (string sku) =>
        {
            // [KYA KARTA HAI]: Dictionary se item remove karne ki koshish karta hai.
            if (QuickCart.Remove(sku))
            {
                // [KYA KARTA HAI]: Agar successfully remove hua toh 204 NoContent return karta hai.
                return TypedResults.NoContent();
            }

            // [KYA KARTA HAI]: Agar SKU missing ho toh 404 NotFound return karta hai.
            return TypedResults.NotFound();
        });
    }
}
