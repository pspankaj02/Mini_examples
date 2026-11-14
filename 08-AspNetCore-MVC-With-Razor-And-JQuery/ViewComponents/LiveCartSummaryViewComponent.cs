using KiranaHub.MvcWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.MvcWeb.ViewComponents;

// =========================================================================================
// [TOPIC]: ASP.NET Core ViewComponent
// [PURPOSE]: Self-contained reusable UI widget with full Dependency Injection support.
// =========================================================================================

// [KYA KARTA HAI]: Microsoft.AspNetCore.Mvc.ViewComponent se inherit karta hai jo navigation bar me live cart widget render karta hai.
// [KYU ZAROORI HAI]: Partial Views ke contrast me, ViewComponent ke paas apna independent backend class aur DI injection hota hai; controller ko global cart data query karne ki zaroorat nahi padti.
// [KAISE CHALTA HAI]: Razor layout me `@await Component.InvokeAsync("LiveCartSummary")` call par ASP.NET Core DI is class ko instantiate karti hai aur `Invoke()` method execute hota hai.
public class LiveCartSummaryViewComponent : ViewComponent
{
    // [KYA KARTA HAI]: Injected cart session service ka private readonly reference store karta hai.
    // [KYU ZAROORI HAI]: Active user ke cart items aur subtotal fetch karne ke liye.
    private readonly ICartSessionService _cartService;

    // [KYA KARTA HAI]: Constructor Injection ke zariye ICartSessionService dependency receive karta hai.
    // [KYU ZAROORI HAI]: Loose coupling aur testability ensure karna.
    // [KAISE CHALTA HAI]: ASP.NET Core IoC container active HTTP request scope se service provide karta hai.
    public LiveCartSummaryViewComponent(ICartSessionService cartService)
    {
        // [KYA KARTA HAI]: Guard clause: check karta hai ki dependency null toh nahi hai.
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
    }

    // [KYA KARTA HAI]: ViewComponent lifecycle ka execution entrypoint method.
    // [KYU ZAROORI HAI]: Data fetch karke associated Razor component view ko bind karna.
    // [KAISE CHALTA HAI]: Synchronous ya async dono forms me implement kiya ja sakta hai; View(model) return karta hai.
    // [OUTPUT / RETURN]: IViewComponentResult (ViewResult) pointing to Views/Shared/Components/LiveCartSummary/Default.cshtml.
    public IViewComponentResult Invoke()
    {
        // [KYA KARTA HAI]: Session storage se current user ka cart data retrieve karta hai.
        // [OUTPUT / SIDE EFFECT]: CartSummaryViewModel with items count and total price.
        var cart = _cartService.GetCart();

        // [KYA KARTA HAI]: Cart model ko default view me pass karke HTML fragment render karta hai.
        // [KYU ZAROORI HAI]: Navbar me live badge update ho sake bina poor page refresh kiye.
        return View(cart);
    }
}
