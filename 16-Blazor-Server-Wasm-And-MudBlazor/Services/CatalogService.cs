using KiranaHub.BlazorStore.Models;

namespace KiranaHub.BlazorStore.Services;

// =========================================================================================
// [TOPIC]: Blazor Catalog State Service
// [PURPOSE]: Provides thread-safe in-memory grocery inventory for the storefront components.
// =========================================================================================

// [KYA KARTA HAI]: Catalog service contract define karta hai.
public interface ICatalogService
{
    // [KYA KARTA HAI]: Sabhi catalog items retrieve karta hai.
    Task<IReadOnlyList<GroceryItem>> GetProductsAsync();

    // [KYA KARTA HAI]: Unique categories list return karta hai.
    Task<IReadOnlyList<string>> GetCategoriesAsync();

    // [KYA KARTA HAI]: Product stock deduct karta hai on checkout.
    Task<bool> DeductStockAsync(int productId, int quantity);
}

// [KYA KARTA HAI]: ICatalogService ka in-memory concrete implementation.
public class CatalogService : ICatalogService
{
    // [KYA KARTA HAI]: Seed products list maintain karta hai.
    private readonly List<GroceryItem> _products = new()
    {
        new GroceryItem { Id = 1, Sku = "MILK-01", Name = "Amul Fresh Cow Milk 500ml", Category = "Dairy", Price = 33m, Stock = 50, IsPerishable = true, Description = "Farm fresh pasteurized cow milk." },
        new GroceryItem { Id = 2, Sku = "BTR-01", Name = "Amul Salted Table Butter 100g", Category = "Dairy", Price = 58m, Stock = 35, IsPerishable = true, Description = "Delicious creamy salted butter." },
        new GroceryItem { Id = 3, Sku = "ATTA-01", Name = "Aashirvaad Shudh Chakki Atta 5kg", Category = "Grains", Price = 245m, Stock = 20, IsPerishable = false, Description = "100% whole wheat flour." },
        new GroceryItem { Id = 4, Sku = "RICE-01", Name = "India Gate Basmati Rice 1kg", Category = "Grains", Price = 135m, Stock = 25, IsPerishable = false, Description = "Aromatic aged long grain basmati." },
        new GroceryItem { Id = 5, Sku = "SALT-01", Name = "Tata Salt Vacuum Evaporated 1kg", Category = "Pantry", Price = 28m, Stock = 80, IsPerishable = false, Description = "Iodized vacuum evaporated salt." },
        new GroceryItem { Id = 6, Sku = "SNK-01", Name = "Haldiram Aloo Bhujia 400g", Category = "Snacks", Price = 115m, Stock = 40, IsPerishable = false, Description = "Spicy crispy potato mint noodles." },
        new GroceryItem { Id = 7, Sku = "TEA-01", Name = "Tata Tea Premium 500g", Category = "Beverages", Price = 210m, Stock = 30, IsPerishable = false, Description = "Desh ki chai blend of big and small leaves." },
        new GroceryItem { Id = 8, Sku = "COCO-01", Name = "Fresh Tender Coconut", Category = "Beverages", Price = 65m, Stock = 0, IsPerishable = true, Description = "Electrolyte-rich sweet coconut water." }
    };

    // [KYA KARTA HAI]: Catalog items list return karta hai asynchronously.
    public Task<IReadOnlyList<GroceryItem>> GetProductsAsync()
    {
        return Task.FromResult<IReadOnlyList<GroceryItem>>(_products.AsReadOnly());
    }

    // [KYA KARTA HAI]: Distinct categories list return karta hai.
    public Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        var categories = _products.Select(p => p.Category).Distinct().ToList();
        return Task.FromResult<IReadOnlyList<string>>(categories.AsReadOnly());
    }

    // [KYA KARTA HAI]: Stock deduction execute karta hai.
    public Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var product = _products.FirstOrDefault(x => x.Id == productId);
        if (product == null || product.Stock < quantity) return Task.FromResult(false);

        product.Stock -= quantity;
        return Task.FromResult(true);
    }
}

