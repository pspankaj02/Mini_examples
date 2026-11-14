using System.ComponentModel.DataAnnotations;

namespace KiranaHub.MvcWeb.Models;

// =========================================================================================
// [TOPIC]: MVC Domain Model with DataAnnotations Validation
// [PURPOSE]: Represents a grocery product entity with validation rules for Model Binding.
// =========================================================================================

// [KYA KARTA HAI]: Grocery Product ka domain model define karta hai.
// [KYU ZAROORI HAI]: Catalog items ki properties, validation rules, aur data structure encapsulate karna.
// [KAISE CHALTA HAI]: ASP.NET Core Model Binder HTML form data ya JSON ko is class me bind karta hai.
public class GroceryProduct
{
    // [KYA KARTA HAI]: Product ka unique identifier property.
    // [KYU ZAROORI HAI]: Primary key jo specific item ko uniquely distinguish karti hai.
    public int Id { get; set; }

    // [KYA KARTA HAI]: DataAnnotation jo specify karta hai ki product name blank nahi ho sakta.
    // [KYU ZAROORI HAI]: Server-side validation; form submit par empty name reject ho sake.
    // [OUTPUT / ERROR]: Agar user name blank chhod de toh ModelState.IsValid false hota hai.
    [Required(ErrorMessage = "Product name is required.")]
    // [KYA KARTA HAI]: String length limit enforce karta hai (3 to 100 characters).
    // [KYU ZAROORI HAI]: Database field overflow aur junk single-character entries prevent karna.
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Category property mandatory banata hai with default value "General".
    // [KYU ZAROORI HAI]: Category-based filtering aur catalog grouping ke liye zaroori hai.
    [Required]
    public string Category { get; set; } = "General";

    // [KYA KARTA HAI]: Numeric range validation: Price 1 se 10,000 INR ke darmiyan hona chahiye.
    // [KYU ZAROORI HAI]: Zero ya negative pricing aur unrealistic numbers se protect karna.
    [Range(1.0, 10000.0, ErrorMessage = "Price must be between Rs. 1 and Rs. 10,000.")]
    public decimal Price { get; set; }

    // [KYA KARTA HAI]: Stock range validation: 0 se 500 units allowed.
    // [KYU ZAROORI HAI]: Negative inventory quantities rokkna.
    [Range(0, 500, ErrorMessage = "Stock quantity cannot be negative.")]
    public int Stock { get; set; }

    // [KYA KARTA HAI]: Boolean flag jo mark karta hai ki product jaldi kharab hone wala hai ya nahi (e.g. Milk, Butter).
    // [KYU ZAROORI HAI]: Cold-chain delivery logic aur TagHelper styling ke liye decision parameter.
    public bool IsPerishable { get; set; }

    // [KYA KARTA HAI]: Product image path property with fallback default image.
    // [KYU ZAROORI HAI]: UI card me grocery product ka visual thumbnail render karna.
    public string ImageUrl { get; set; } = "/images/default-grocery.png";
}
