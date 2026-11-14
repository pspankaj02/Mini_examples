namespace KiranaHub.VirtualActors.Grains;

using KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: Quick-Commerce customer shopping cart ke virtual actor interface ko define karta hai.
// [KYU ZAROORI HAI]: Har live active customer cart ko in-memory isolate karna taaki concurrent clicks aur coupon updates bina DB deadlocks ke execute hon.
// [KAISE CHALTA HAI]: Customer ID ke basis par Silo deterministic cart actor activate karta hai.
// [OUTPUT / SIDE EFFECT]: Stateful cart actor contract.
// [VARIATIONS TO TRY]: Multi-device cart sync ya shared family cart support add kar sakte hain.
public interface ICartGrain : IVirtualGrain
{
    // [KYA KARTA HAI]: Cart me grocery product add ya increment karta hai.
    // [KYU ZAROORI HAI]: Customer basket build karne aur real-time stock reservation ke liye.
    // [KAISE CHALTA HAI]: Actor turn queue me serialize hokar item dictionary me item add/update karta hai.
    // [OUTPUT / SIDE EFFECT]: Cart subtotal and items count updated.
    // [VARIATIONS TO TRY]: Maximum 50 items per order limit validation lagayein.
    Task AddItemAsync(string productId, string name, decimal price, int quantity);

    // [KYA KARTA HAI]: Cart se product remove karta hai.
    // [KYU ZAROORI HAI]: Basket modification aur unreserved stock release ke liye.
    // [KAISE CHALTA HAI]: Product key ke zariye dictionary se remove karta hai.
    // [OUTPUT / SIDE EFFECT]: Product removed from cart.
    // [VARIATIONS TO TRY]: Undo remove item with 10-second grace window support karein.
    Task RemoveItemAsync(string productId);

    // [KYA KARTA HAI]: Discount coupon code apply karta hai.
    // [KYU ZAROORI HAI]: Instant promotional pricing aur cart total recalculation ke liye.
    // [KAISE CHALTA HAI]: Coupon details store karta hai aur total recalculate karta hai.
    // [OUTPUT / SIDE EFFECT]: Discount applied, Net Payable updated.
    // [VARIATIONS TO TRY]: Expiry validation ya minimum cart value requirement check karein.
    Task ApplyCouponAsync(string couponCode, decimal discountPct);

    // [KYA KARTA HAI]: Cart ka latest immutable state snapshot fetch karta hai.
    // [KYU ZAROORI HAI]: Checkout screen, slide-out drawer, aur mobile UI ko accurate calculation dikhana.
    // [KAISE CHALTA HAI]: Turn queue me safe read karke CartState record return karta hai.
    // [OUTPUT / SIDE EFFECT]: CartState snapshot.
    // [VARIATIONS TO TRY]: Free delivery threshold flag attach karein.
    Task<CartState> GetCartAsync();

    // [KYA KARTA HAI]: Cart ko checkout mark karta hai aur items clear karta hai.
    // [KYU ZAROORI HAI]: Successful order placement ke baad cart empty karna.
    // [KAISE CHALTA HAI]: Items list empty karta hai aur IsCheckedOut true set karta hai.
    // [OUTPUT / SIDE EFFECT]: Cart reset to empty state.
    // [VARIATIONS TO TRY]: Abandoned cart reminder trigger cancel karein.
    Task CheckoutAsync();
}

// [KYA KARTA HAI]: Individual cart line item ko represent karne wala immutable record.
// [KYU ZAROORI HAI]: Product ID, unit price, quantity, aur calculated line total encapsulate karna.
// [KAISE CHALTA HAI]: Computed property LineTotal = Price * Quantity calculate karta hai.
// [OUTPUT / SIDE EFFECT]: Line item representation.
// [VARIATIONS TO TRY]: Tax percentage ya perishability category shamil karein.
public record CartLineItem(string ProductId, string Name, decimal Price, int Quantity)
{
    // [KYA KARTA HAI]: Line item ka total amount compute karta hai.
    // [KYU ZAROORI HAI]: Subtotal aggregation ke liye line-level multiplication zaroori hai.
    // [KAISE CHALTA HAI]: Price * Quantity calculation.
    // [OUTPUT / SIDE EFFECT]: Line total decimal amount.
    // [VARIATIONS TO TRY]: Bulk discount tiered pricing apply karein.
    public decimal LineTotal => Price * Quantity;
}

// [KYA KARTA HAI]: Shopping cart ka complete state snapshot represent karne wala record.
// [KYU ZAROORI HAI]: UI aur checkout pipelines ko immutable, calculation-ready cart data deliver karna.
// [KAISE CHALTA HAI]: Subtotal, discount, GST tax (5%), aur final payable amount hold karta hai.
// [OUTPUT / SIDE EFFECT]: Complete financial breakdown of cart.
// [VARIATIONS TO TRY]: Delivery fee ya tip amount fields add karein.
public record CartState(
    string CustomerId,
    IReadOnlyList<CartLineItem> Items,
    decimal Subtotal,
    string? CouponCode,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal NetPayable,
    bool IsCheckedOut,
    DateTime LastUpdatedUtc);
