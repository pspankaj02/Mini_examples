using System.ComponentModel.DataAnnotations;

namespace KiranaHub.WebApi.Models;

// =========================================================================================
// [TOPIC]: Web API Data Transfer Objects (DTOs) with Strict Validation
// [PURPOSE]: Decouple internal persistence models from external API contracts and prevent over-posting.
// =========================================================================================

// [KYA KARTA HAI]: API output response contract represent karta hai (immutable record).
// [KYU ZAROORI HAI]: Client ko sirf wahi properties return karna jo publicly safe aur zaroori hain.
// [KAISE CHALTA HAI]: C# positional record lightweight immutable schema provide karta hai.
public record ProductReadDto(int Id, string Sku, string Name, string Category, decimal Price, int Stock);

// [KYA KARTA HAI]: POST /api/products create request ka request body payload DTO.
// [KYU ZAROORI HAI]: Incoming JSON payload par strict schema validation rules enforce karna.
public class ProductCreateDto
{
    // [KYA KARTA HAI]: SKU property ko mandatory declare karta hai.
    // [OUTPUT / ERROR]: Agar SKU missing ho toh 400 Bad Request error return hota hai.
    [Required(ErrorMessage = "SKU barcode is required.")]
    // [KYA KARTA HAI]: Regex format enforce karta hai: 4 se 15 uppercase alphanumeric characters with hyphens (e.g. "MILK-01").
    // [KYU ZAROORI HAI]: Barcode format consistency ensure karna.
    [RegularExpression(@"^[A-Z0-9\-]{4,15}$", ErrorMessage = "SKU must be uppercase alphanumeric (4-15 chars).")]
    public string Sku { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Product name non-empty enforce karta hai.
    [Required(ErrorMessage = "Product name cannot be empty.")]
    // [KYA KARTA HAI]: Name length 2 se 100 characters ke beech restrict karta hai.
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Category property mandatory banata hai with default "General".
    [Required]
    public string Category { get; set; } = "General";

    // [KYA KARTA HAI]: Price positive decimal (0.50 se 50,000 INR) enforce karta hai.
    // [KYU ZAROORI HAI]: Zero ya negative pricing API inputs ko reject karna.
    [Range(0.50, 50000.0, ErrorMessage = "Price must be positive.")]
    public decimal Price { get; set; }

    // [KYA KARTA HAI]: Stock count range validation (0 to 1,000 units).
    // [KYU ZAROORI HAI]: Negative inventory quantities rokkna.
    [Range(0, 1000, ErrorMessage = "Stock must be between 0 and 1000.")]
    public int Stock { get; set; }
}
