using KiranaHub.BlazorStore.Models;

namespace KiranaHub.BlazorStore.Services;

// =========================================================================================
// [TOPIC]: Blazor Scoped Reactive State Management
// [PURPOSE]: Central shopping cart state with pub/sub event notifications for Blazor UI components.
// =========================================================================================

// [KYA KARTA HAI]: Cart state service contract.
public interface ICartStateService
{
    // [KYA KARTA HAI]: Read-only view of cart items.
    IReadOnlyList<CartItem> Items { get; }

    // [KYA KARTA HAI]: Total individual items count.
    int TotalItemCount { get; }

    // [KYA KARTA HAI]: Total gross amount before discount.
    decimal Subtotal { get; }

    // [KYA KARTA HAI]: Delivery fee.
    decimal DeliveryFee { get; }

    // [KYA KARTA HAI]: Applied coupon discount.
    decimal DiscountAmount { get; }

    // [KYA KARTA HAI]: Final payable grand total.
    decimal GrandTotal { get; }

    // [KYA KARTA HAI]: Reactive state change event.
    // [KYU ZAROORI HAI]: Blazor components is event ko subscribe karte hain aur state change par StateHasChanged() call karte hain.
    event Action? OnChange;

    // [KYA KARTA HAI]: Product add karne ka method.
    void AddItem(GroceryItem product, int quantity = 1);

    // [KYA KARTA HAI]: Quantity modify karne ka method.
    void UpdateQuantity(int productId, int delta);

    // [KYA KARTA HAI]: Item delete karne ka method.
    void RemoveItem(int productId);

    // [KYA KARTA HAI]: Promo coupon apply karne ka method.
    bool ApplyCoupon(string couponCode);

    // [KYA KARTA HAI]: Cart empty karne ka method.
    void ClearCart();
}

// [KYA KARTA HAI]: ICartStateService ka concrete scoped implementation.
public class CartStateService : ICartStateService
{
    // [KYA KARTA HAI]: Internal mutable items list.
    private readonly List<CartItem> _items = new();

    // [KYA KARTA HAI]: Public read-only collection expose karta hai.
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    // [KYA KARTA HAI]: Total item count compute karta hai.
    public int TotalItemCount => _items.Sum(x => x.Quantity);

    // [KYA KARTA HAI]: Subtotal compute karta hai.
    public decimal Subtotal => _items.Sum(x => x.LineTotal);

    // [KYA KARTA HAI]: Quick-commerce delivery rule: Free above 500 INR, else 40 INR (0 if cart empty).
    public decimal DeliveryFee => Subtotal == 0m || Subtotal >= 500m ? 0m : 40m;

    // [KYA KARTA HAI]: Current discount amount.
    public decimal DiscountAmount { get; private set; }

    // [KYA KARTA HAI]: Grand total: Subtotal + DeliveryFee - DiscountAmount.
    public decimal GrandTotal => Math.Max(0, Subtotal + DeliveryFee - DiscountAmount);

    // [KYA KARTA HAI]: Reactive notification event.
    public event Action? OnChange;

    // [KYA KARTA HAI]: State change notify karne ka helper method.
    private void NotifyStateChanged() => OnChange?.Invoke();

    // [KYA KARTA HAI]: Product cart me add karta hai.
    public void AddItem(GroceryItem product, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new CartItem
            {
                ProductId = product.Id,
                Sku = product.Sku,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }
        NotifyStateChanged();
    }

    // [KYA KARTA HAI]: Quantity increment ya decrement karta hai.
    public void UpdateQuantity(int productId, int delta)
    {
        var existing = _items.FirstOrDefault(x => x.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity += delta;
            if (existing.Quantity <= 0)
            {
                _items.Remove(existing);
            }
            NotifyStateChanged();
        }
    }

    // [KYA KARTA HAI]: Specific product remove karta hai.
    public void RemoveItem(int productId)
    {
        _items.RemoveAll(x => x.ProductId == productId);
        NotifyStateChanged();
    }

    // [KYA KARTA HAI]: Promo coupon apply karta hai (e.g. "KIRANA50" gives Rs. 50 flat discount).
    public bool ApplyCoupon(string couponCode)
    {
        if (couponCode.Equals("KIRANA50", StringComparison.OrdinalIgnoreCase) && Subtotal >= 200m)
        {
            DiscountAmount = 50m;
            NotifyStateChanged();
            return true;
        }
        return false;
    }

    // [KYA KARTA HAI]: Cart empty karta hai.
    public void ClearCart()
    {
        _items.Clear();
        DiscountAmount = 0m;
        NotifyStateChanged();
    }
}

