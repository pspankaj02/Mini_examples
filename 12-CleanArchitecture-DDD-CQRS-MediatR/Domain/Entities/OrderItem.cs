using KiranaHub.CleanArchitecture.Domain.ValueObjects;

namespace KiranaHub.CleanArchitecture.Domain.Entities;

// =========================================================================================
// [TOPIC]: DDD OrderItem Entity
// [PURPOSE]: Line item within the Order Aggregate boundary.
// =========================================================================================

// [KYA KARTA HAI]: Order ke single line item ka domain entity encapsulate karta hai.
// [KYU ZAROORI HAI]: Aggregate Root (Order) ke internal children entities ko protect karna.
public class OrderItem
{
    // [KYA KARTA HAI]: Product SKU identifier.
    public string Sku { get; }

    // [KYA KARTA HAI]: Product display name.
    public string ProductName { get; }

    // [KYA KARTA HAI]: Unit price stored as Money Value Object.
    public Money UnitPrice { get; }

    // [KYA KARTA HAI]: Ordered quantity.
    public int Quantity { get; }

    // [KYA KARTA HAI]: Computed property: UnitPrice * Quantity.
    public Money LineTotal => UnitPrice * Quantity;

    // [KYA KARTA HAI]: Constructor jo domain invariants enforce karta hai.
    public OrderItem(string sku, string productName, Money unitPrice, int quantity)
    {
        // [KYA KARTA HAI]: SKU validation check.
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU cannot be empty.", nameof(sku));

        // [KYA KARTA HAI]: ProductName validation.
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("Product name cannot be empty.", nameof(productName));

        // [KYA KARTA HAI]: UnitPrice null check.
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));

        // [KYA KARTA HAI]: Quantity greater than zero check.
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");

        // [KYA KARTA HAI]: Fields assign karta hai.
        Sku = sku;
        ProductName = productName;
        Quantity = quantity;
    }
}

