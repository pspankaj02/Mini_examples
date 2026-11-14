namespace KiranaHub.BlazorStore.Models;

// =========================================================================================
// [TOPIC]: Blazor Storefront Domain Model
// [PURPOSE]: Represents a grocery product in the MudBlazor catalog.
// =========================================================================================

// [KYA KARTA HAI]: Grocery item ka model class define karta hai.
// [KYU ZAROORI HAI]: MudDataGrid, MudTable, aur Product cards me strongly-typed binding ke liye.
public class GroceryItem
{
    // [KYA KARTA HAI]: Item ka unique integer identifier.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Inventory barcode SKU string (e.g. "MILK-01").
    public string Sku { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Grocery item display name.
    public string Name { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Grocery category (e.g. "Dairy", "Grains", "Snacks").
    public string Category { get; set; } = "General";

    // [KYA KARTA HAI]: Unit retail price in Indian Rupees.
    public decimal Price { get; set; }

    // [KYA KARTA HAI]: Available inventory stock count.
    public int Stock { get; set; }

    // [KYA KARTA HAI]: Boolean flag mark karta hai cold-chain item (e.g. Milk, Butter).
    public bool IsPerishable { get; set; }

    // [KYA KARTA HAI]: Product image URL path.
    public string ImageUrl { get; set; } = "/images/default-grocery.png";

    // [KYA KARTA HAI]: Product description text.
    public string Description { get; set; } = string.Empty;
}

