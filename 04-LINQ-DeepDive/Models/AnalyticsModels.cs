namespace KiranaHub.LinqDeepDive.Models;

// =========================================================================================
// [KYA KARTA HAI]:
// KiranaHub analytics aur reporting ke liye data transfer models.
//
// [KYU ZAROORI HAI]:
// Real-world LINQ queries (SelectMany, GroupBy, Join) ko complex nested data
// structures par demonstrate karne ke liye.
// =========================================================================================

public enum Category
{
    Dairy,
    Grains,
    Snacks,
    Beverages,
    PersonalCare
}

public record GroceryItem(string Sku, string Name, Category Category, decimal Price, int StockQuantity);

public record OrderLine(string Sku, string ItemName, int Quantity, decimal UnitPrice);

public record CustomerOrder(
    string OrderId,
    string CustomerId,
    DateTime PlacedAtUtc,
    List<OrderLine> Lines,
    bool IsPaid
);

public record CustomerProfile(string CustomerId, string Name, string City, bool IsPrimeMember);

