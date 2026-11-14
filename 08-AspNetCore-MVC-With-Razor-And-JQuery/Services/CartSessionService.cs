using System.Text.Json;
using KiranaHub.MvcWeb.Models;
using KiranaHub.MvcWeb.ViewModels;

namespace KiranaHub.MvcWeb.Services;

// =========================================================================================
// [TOPIC]: Session-Backed Shopping Cart Service
// [PURPOSE]: Manages shopping cart state across HTTP requests using ASP.NET Core ISession.
// =========================================================================================

// [KYA KARTA HAI]: Shopping Cart operations (Get, Add, Update, Remove, Clear) ke liye contract define karta hai.
// [KYU ZAROORI HAI]: Controllers ko session storage implementation details se decouple rakhna.
// [KAISE CHALTA HAI]: Dependency Injection me Scoped service ke roop me register hota hai.
public interface ICartSessionService
{
    // [KYA KARTA HAI]: Current user ke active cart ka CartSummaryViewModel return karta hai.
    CartSummaryViewModel GetCart();

    // [KYA KARTA HAI]: GroceryProduct ko specified quantity ke sath cart me add karta hai.
    void AddItem(GroceryProduct product, int quantity);

    // [KYA KARTA HAI]: Cart item ki quantity ko positive ya negative delta se adjust karta hai.
    void UpdateQuantity(int productId, int delta);

    // [KYA KARTA HAI]: Specified product ID ke item ko cart se completely delete karta hai.
    void RemoveItem(int productId);

    // [KYA KARTA HAI]: User ka entire cart session clear/empty kar deta hai.
    void ClearCart();
}

// [KYA KARTA HAI]: ICartSessionService ka concrete implementation jo ASP.NET Core session store use karta hai.
// [KYU ZAROORI HAI]: Stateless HTTP protocol ke upar per-user state maintain karna.
public class CartSessionService : ICartSessionService
{
    // [KYA KARTA HAI]: Session dictionary me cart JSON store karne ke liye unique cache key constant define karta hai.
    // [KYU ZAROORI HAI]: Magic strings se bachna aur key consistency ensure karna across methods.
    private const string CartSessionKey = "KIRANA_CART_ITEMS";

    // [KYA KARTA HAI]: ASP.NET Core IHttpContextAccessor ka reference hold karta hai.
    // [KYU ZAROORI HAI]: Non-controller classes me current request ke HttpContext aur Session ko access karne ka standard mechanism.
    private readonly IHttpContextAccessor _httpContextAccessor;

    // [KYA KARTA HAI]: Constructor Injection ke zariye IHttpContextAccessor receive karta hai.
    // [KYU ZAROORI HAI]: ASP.NET Core runtime se active request context inject karwana.
    public CartSessionService(IHttpContextAccessor httpContextAccessor)
    {
        // [KYA KARTA HAI]: Injected accessor field me store karta hai with null check.
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    // [KYA KARTA HAI]: Current HTTP request ke ISession object ko safely resolve karta hai.
    // [KYU ZAROORI HAI]: Bar-baar _httpContextAccessor.HttpContext?.Session null check code likhne se bachana.
    // [KAISE CHALTA HAI]: Expression-bodied property jo null hone par descriptive InvalidOperationException throw karti hai.
    // [OUTPUT / RETURN]: Active ISession instance for current client session.
    private ISession Session => _httpContextAccessor.HttpContext?.Session 
        ?? throw new InvalidOperationException("HttpContext or Session is not available. Ensure app.UseSession() is registered in Program.cs.");

    // [KYA KARTA HAI]: Session se cart JSON read karta hai aur deserialize karke CartSummaryViewModel banata hai.
    // [KYU ZAROORI HAI]: Cart page rendering, AJAX counter update, aur checkout calculations ke liye current cart state read karna.
    // [KAISE CHALTA HAI]: Session.GetString() cookie-associated in-memory session cache se data nikalta hai.
    // [OUTPUT / RETURN]: CartSummaryViewModel with populated items list or empty list.
    public CartSummaryViewModel GetCart()
    {
        // [KYA KARTA HAI]: Session storage se CartSessionKey ke against saved raw JSON string read karta hai.
        // [KAISE CHALTA HAI]: Session provider byte array ko string me decode karta hai.
        string? json = Session.GetString(CartSessionKey);

        // [KYA KARTA HAI]: Check karta hai ki session me cart already exist karta hai ya fresh user hai.
        // [KYU ZAROORI HAI]: Fresh session me null deserialization exception se bachna.
        if (string.IsNullOrEmpty(json))
        {
            // [KYA KARTA HAI]: Empty CartSummaryViewModel return karta hai (empty items list).
            return new CartSummaryViewModel();
        }

        // [KYA KARTA HAI]: JSON string ko List<CartItemViewModel> me deserialize karta hai.
        // [KAISE CHALTA HAI]: System.Text.Json high-performance UTF-8 parser chala kar managed objects banata hai.
        var items = JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new List<CartItemViewModel>();

        // [KYA KARTA HAI]: Deserialized items ko CartSummaryViewModel me pack karke return karta hai.
        return new CartSummaryViewModel { Items = items };
    }

    // [KYA KARTA HAI]: Cart me naya item add karta hai ya existing item ki quantity increment karta hai.
    // [KYU ZAROORI HAI]: E-commerce catalog se 'Add to Cart' button click handle karna.
    // [KAISE CHALTA HAI]: Existing list read karta hai, mutate karta hai, aur JSON wapas session me persist karta hai.
    public void AddItem(GroceryProduct product, int quantity)
    {
        // [KYA KARTA HAI]: Current cart state fetch karta hai session se.
        var cart = GetCart();

        // [KYA KARTA HAI]: Check karta hai ki ye product ID already cart me maujood hai ya nahi.
        // [KAISE CHALTA HAI]: FirstOrDefault matching CartItemViewModel search karta hai.
        var existing = cart.Items.FirstOrDefault(x => x.ProductId == product.Id);

        // [KYA KARTA HAI]: Agar product pehle se cart me hai.
        if (existing != null)
        {
            // [KYA KARTA HAI]: Existing item ki quantity ko requested quantity se increment karta hai.
            // [OUTPUT / SIDE EFFECT]: e.g., 1 milk + 2 milk = 3 milk.
            existing.Quantity += quantity;
        }
        else
        {
            // [KYA KARTA HAI]: Agar product cart me naya hai, toh fresh CartItemViewModel add karta hai.
            // [OUTPUT / SIDE EFFECT]: Items list me naya element insert hota hai.
            cart.Items.Add(new CartItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        // [KYA KARTA HAI]: Updated list ko JSON serialize karke session me persist karta hai.
        SaveCart(cart.Items);
    }

    // [KYA KARTA HAI]: Specific product ki quantity modify karta hai (+1 ya -1 click par).
    // [KYU ZAROORI HAI]: Cart drawer ya checkout page par user item count adjust karta hai.
    public void UpdateQuantity(int productId, int delta)
    {
        // [KYA KARTA HAI]: Current cart state retrieve karta hai.
        var cart = GetCart();

        // [KYA KARTA HAI]: Matching product item lookup karta hai.
        var existing = cart.Items.FirstOrDefault(x => x.ProductId == productId);

        // [KYA KARTA HAI]: Agar item cart me present ho.
        if (existing != null)
        {
            // [KYA KARTA HAI]: Quantity me delta add karta hai (e.g. +1 ya -1).
            existing.Quantity += delta;

            // [KYA KARTA HAI]: Agar quantity decrement hokar 0 ya negative ho gayi ho.
            // [KYU ZAROORI HAI]: Zero quantity item cart me nahi rehna chahiye; remove ho jana chahiye.
            if (existing.Quantity <= 0)
            {
                // [KYA KARTA HAI]: Item ko cart list se nikal deta hai.
                cart.Items.Remove(existing);
            }

            // [KYA KARTA HAI]: Modified cart ko wapas session me save karta hai.
            SaveCart(cart.Items);
        }
    }

    // [KYA KARTA HAI]: Specified product ID ko cart se remove karta hai.
    // [KYU ZAROORI HAI]: 'Delete / Remove' icon click par item ko cart se hata sakein.
    public void RemoveItem(int productId)
    {
        // [KYA KARTA HAI]: Cart load karta hai.
        var cart = GetCart();

        // [KYA KARTA HAI]: Matching ProductId wale sabhi items ko list se remove karta hai.
        // [KAISE CHALTA HAI]: List<T>.RemoveAll predicate matching elements ko O(n) me shift karke delete karta hai.
        cart.Items.RemoveAll(x => x.ProductId == productId);

        // [KYA KARTA HAI]: Updated state ko session me save karta hai.
        SaveCart(cart.Items);
    }

    // [KYA KARTA HAI]: CartSessionKey ko session se delete kar deta hai.
    // [KYU ZAROORI HAI]: Order successfully place hone par ya 'Empty Cart' click karne par cart clear karna.
    public void ClearCart()
    {
        // [KYA KARTA HAI]: Session storage se key remove karta hai.
        // [OUTPUT / SIDE EFFECT]: Next GetCart() call will return 0 items.
        Session.Remove(CartSessionKey);
    }

    // [KYA KARTA HAI]: Items list ko JSON string me serialize karke session store me save karta hai.
    // [KYU ZAROORI HAI]: In-memory mutation ko persistent session cookie state me commit karna.
    // [KAISE CHALTA HAI]: JsonSerializer.Serialize() executes reflection/source generation to create JSON string; Session.SetString() commits to cache.
    private void SaveCart(List<CartItemViewModel> items)
    {
        // [KYA KARTA HAI]: Cart items list ko JSON text me serialize karta hai.
        string json = JsonSerializer.Serialize(items);

        // [KYA KARTA HAI]: JSON text ko current user ke session dictionary me store karta hai.
        Session.SetString(CartSessionKey, json);
    }
}
