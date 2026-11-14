using KiranaHub.BasicsAndOops.Models;

namespace KiranaHub.BasicsAndOops.Services;

// =========================================================================================
// [CLASS PURPOSE]:
// Encapsulates Shopping Cart operations, item aggregation, and bill calculations.
// =========================================================================================
public class Cart
{
    // [PRIVATE DATA STORE]:
    // [KYA KARTA HAI]: Dictionary storing ProductId -> (Product, Quantity) tuple.
    // [KYU ZAROORI HAI]: O(1) instant product lookup and duplicate item aggregation.
    private readonly Dictionary<string, (Product Product, int Quantity)> _items = new();

    // [PUBLIC READ-ONLY VIEW]:
    // [KYA KARTA HAI]: ReadOnlyCollection expose karta hai taaki bahar se list clear na ki ja sake (Data Protection).
    public IReadOnlyCollection<(Product Product, int Quantity)> Items => _items.Values;

    // [METHOD]: Add Item to Cart
    public void AddItem(Product product, int quantity = 1)
    {
        // [DEFENSIVE NULL CHECK]:
        if (product == null) throw new ArgumentNullException(nameof(product));

        // [DEFENSIVE QUANTITY CHECK]:
        if (quantity <= 0) throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        // [LOOKUP CONDITION]: Check if product already exists in cart
        if (_items.ContainsKey(product.Id))
        {
            // [READ EXISTING ENTRY]:
            var existing = _items[product.Id];

            // [MUTATE QUANTITY]: Add to existing quantity instead of creating duplicate line item
            _items[product.Id] = (existing.Product, existing.Quantity + quantity);
        }
        else
        {
            // [NEW ENTRY INSERTION]:
            _items[product.Id] = (product, quantity);
        }
    }

    // [METHOD]: Remove item by ID
    public bool RemoveItem(string productId)
    {
        // Dictionary removal returns true if found and removed
        return _items.Remove(productId);
    }

    // [METHOD]: Calculate Items Subtotal
    public decimal CalculateItemsSubtotal()
    {
        // [ACCUMULATOR INITIALIZATION]:
        decimal total = 0;

        // [FOREACH LOOP]: Iterate over all cart entries
        foreach (var entry in _items.Values)
        {
            // [SUBTOTAL ACCUMULATION]: Multiply price by quantity
            total += entry.Product.Price * entry.Quantity;
        }

        // [RETURN VALUE]:
        return total;
    }

    // [METHOD]: Calculate Polymorphic Delivery Fee
    public decimal CalculateTotalDeliveryFee(int distanceInKm)
    {
        // [ACCUMULATOR INITIALIZATION]:
        decimal totalFee = 0;

        // [FOREACH LOOP]:
        foreach (var entry in _items.Values)
        {
            // [DYNAMIC DISPATCH CALL]:
            // Runtime determines whether entry is Perishable or Packaged product!
            totalFee += entry.Product.CalculateDeliveryFee(distanceInKm);
        }

        // [RETURN TOTAL FEE]:
        return totalFee;
    }

    // [METHOD]: Grand Total
    public decimal CalculateGrandTotal(int distanceInKm)
    {
        // Combine items subtotal + delivery surcharge
        return CalculateItemsSubtotal() + CalculateTotalDeliveryFee(distanceInKm);
    }
}
