using KiranaHub.MvcWeb.Models;

namespace KiranaHub.MvcWeb.ViewModels;

// =========================================================================================
// [TOPIC]: Strongly-Typed ViewModels & DTOs for MVC Views & AJAX Requests
// [PURPOSE]: Decouple Domain Entities from Presentation Layer and prevent Over-Posting attacks.
// =========================================================================================

// [KYA KARTA HAI]: Catalog/Index.cshtml view ko data provide karne wala composite ViewModel.
// [KYU ZAROORI HAI]: Ek hi view ko products list, category filters list, active search query sab ek bundle me chahiye hote hain.
public class CatalogIndexViewModel
{
    // [KYA KARTA HAI]: Filtered grocery products ki list hold karta hai.
    // [KYU ZAROORI HAI]: Razor @foreach loop se grocery product cards render karne ke liye.
    public List<GroceryProduct> Products { get; set; } = new();

    // [KYA KARTA HAI]: Store me available all distinct categories ki list hold karta hai.
    // [KYU ZAROORI HAI]: Category navigation pills dynamically build karne ke liye.
    public List<string> Categories { get; set; } = new();

    // [KYA KARTA HAI]: Currently active category filter string (e.g. "Dairy").
    // [KYU ZAROORI HAI]: Active filter button par Bootstrap active state highlight karne ke liye.
    public string? SelectedCategory { get; set; }

    // [KYA KARTA HAI]: User dwara input kiya gaya search keyword string.
    // [KYU ZAROORI HAI]: Search input box me user ka entered term persist rakhne ke liye after page reload.
    public string? SearchTerm { get; set; }
}

// [KYA KARTA HAI]: Shopping cart ke single line item ka ViewModel encapsulate karta hai.
// [KYU ZAROORI HAI]: Cart table row ya mini-cart item render karna with pricing and quantity.
public class CartItemViewModel
{
    // [KYA KARTA HAI]: Product ka identifier.
    // [KYU ZAROORI HAI]: Item quantity change ya removal actions ke liye ID track karna.
    public int ProductId { get; set; }

    // [KYA KARTA HAI]: Product ka human-readable display name.
    // [KYU ZAROORI HAI]: Cart drawer me product name show karna.
    public string ProductName { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Single unit ka retail price.
    // [KYU ZAROORI HAI]: Per-item price display aur calculations ke liye.
    public decimal UnitPrice { get; set; }

    // [KYA KARTA HAI]: User dwara cart me chuni gayi quantity.
    // [KYU ZAROORI HAI]: Counter widget (+ / -) me count show karna.
    public int Quantity { get; set; }

    // [KYA KARTA HAI]: Computed property: UnitPrice * Quantity calculate karta hai.
    // [KYU ZAROORI HAI]: Har row ka subtotal automatically calculate hona without redundant storage.
    // [KAISE CHALTA HAI]: Getter invocation par real-time arithmetic multiply evaluate hota hai.
    public decimal LineTotal => UnitPrice * Quantity;
}

// [KYA KARTA HAI]: Complete shopping cart aggregate details (all items, subtotal, delivery fee, grand total) encapsulate karta hai.
// [KYU ZAROORI HAI]: Checkout summary, LiveCartSummaryViewComponent, aur AJAX drawer ko complete financial snapshot dena.
public class CartSummaryViewModel
{
    // [KYA KARTA HAI]: Cart me add kiye gaye sabhi items ki list.
    public List<CartItemViewModel> Items { get; set; } = new();

    // [KYA KARTA HAI]: Computed property: Sabhi items ke LineTotal ka sum return karta hai.
    // [KAISE CHALTA HAI]: LINQ Items.Sum(x => x.LineTotal) evaluate hota hai.
    public decimal Subtotal => Items.Sum(x => x.LineTotal);

    // [KYA KARTA HAI]: Quick-commerce delivery fee rule compute karta hai: 500 INR se upar order par Free delivery, warna 40 INR.
    // [KYU ZAROORI HAI]: Dynamic business pricing logic without hardcoded UI hacks.
    public decimal DeliveryFee => Subtotal > 500m ? 0m : 40m;

    // [KYA KARTA HAI]: Subtotal aur DeliveryFee ko add karke final payable Grand Total compute karta hai.
    public decimal GrandTotal => Subtotal + DeliveryFee;

    // [KYA KARTA HAI]: Cart me maujood total individual units count calculate karta hai.
    // [KYU ZAROORI HAI]: Header navigation badge me total item count (e.g. "3 items") display karna.
    public int TotalItemCount => Items.Sum(x => x.Quantity);
}

// [KYA KARTA HAI]: jQuery AJAX "Add to Cart" POST request ka JSON payload bind karne wala DTO.
// [KYU ZAROORI HAI]: Strict typed request contract for AJAX endpoint.
public class AddToCartRequest
{
    // [KYA KARTA HAI]: Cart me add hone wale product ka ID.
    public int ProductId { get; set; }

    // [KYA KARTA HAI]: Kitne units add karne hain (default 1).
    public int Quantity { get; set; } = 1;
}

// [KYA KARTA HAI]: jQuery AJAX "+ / -" quantity change POST request ka DTO.
// [KYU ZAROORI HAI]: Increment/decrement actions ke liye structured payload.
public class UpdateQuantityRequest
{
    // [KYA KARTA HAI]: Target product ka ID.
    public int ProductId { get; set; }

    // [KYA KARTA HAI]: Change delta (+1 for increment, -1 for decrement).
    public int Delta { get; set; }
}
