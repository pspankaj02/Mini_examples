using KiranaHub.MvcWeb.Services;
using KiranaHub.MvcWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.MvcWeb.Controllers;

// =========================================================================================
// [CLASS PURPOSE]:
// Shopping Cart Controller jo customer ke cart interactions ko manage karta hai.
// Yeh controller traditional Razor Views aur background jQuery AJAX JSON requests dono ko serve karta hai.
// =========================================================================================
public class CartController : Controller
{
    // [FIELD DECLARATION]:
    // [KYA KARTA HAI]: Private readonly field jo session-backed cart service ka reference store karta hai.
    // [KYU ZAROORI HAI]: Dependency Inversion Principle (DIP) - direct session manipulation ke bajaye abstraction use karna.
    // [KAISE CHALTA HAI]: 'readonly' ensure karta hai ki constructor ke baad iska reference koi change na kar sake.
    private readonly ICartSessionService _cartService;

    // [CONSTRUCTOR INJECTION]:
    // [KYA KARTA HAI]: ASP.NET Core built-in IoC container se 'ICartSessionService' ka registered instance inject karwata hai.
    // [KYU ZAROORI HAI]: Loose coupling - controller ko yeh janne ki zaroorat nahi ki session memory me hai ya Redis me.
    // [KAISE CHALTA HAI]: Request aane par Dependency Injection container constructor parameters resolve karta hai.
    public CartController(ICartSessionService cartService)
    {
        // [ASSIGNMENT LINE]:
        // Injected service ko private field me assign kiya gaya taaki baaki action methods use kar sakein.
        _cartService = cartService;
    }

    // =====================================================================================
    // ACTION 1: Traditional Razor View (Cart Page)
    // =====================================================================================

    // [ATTRIBUTE]:
    // [KYA KARTA HAI]: Specify karta hai ki yeh method sirf HTTP GET requests handle karega (e.g. browser URL navigation).
    [HttpGet]
    public IActionResult Index()
    {
        // [SERVICE CALL]:
        // [KYA KARTA HAI]: Current user ke session cookie se cart items, subtotal aur delivery fees retrieve karta hai.
        // [OUTPUT]: Ek populated 'CartSummaryViewModel' object return hota hai.
        var cart = _cartService.GetCart();

        // [VIEW RETURN]:
        // [KYA KARTA HAI]: 'Views/Cart/Index.cshtml' view ko compile karke HTML generate karta hai aur cart model pass karta hai.
        // [VARIATIONS TO TRY]: Agar cart khali ho toh seedha Catalog/Index par redirect karwa ke dekhein: 'if(cart.Items.Count==0) return RedirectToAction("Index", "Catalog");'
        return View(cart);
    }

    // =====================================================================================
    // ACTION 2: jQuery AJAX Endpoint - Add to Cart
    // =====================================================================================

    // [ATTRIBUTE 1]:
    // [KYA KARTA HAI]: Sirf HTTP POST requests allow karta hai.
    // [KYU ZAROORI HAI]: Server state me cart item modify ho raha hai, jo REST standards me POST hona chahiye.
    [HttpPost]

    // [ATTRIBUTE 2]:
    // [KYA KARTA HAI]: Cross-Site Request Forgery (CSRF) validation token check karta hai.
    // [KYU ZAROORI HAI]: Agar user kisi unauthorized tab par hai, toh koi hacker background me uske cart me order add na kar sake.
    // [KAISE CHALTA HAI]: Request header 'RequestVerificationToken' ko decrypt karke antiforgery cookie se match karta hai.
    [ValidateAntiForgeryToken]

    // [METHOD SIGNATURE]:
    // [KYA KARTA HAI]: '[FromBody]' request body stream se JSON payload ko 'AddToCartRequest' DTO me deserialize karta hai.
    public IActionResult AddToCartAjax([FromBody] AddToCartRequest request)
    {
        // [LINE 1: DEFENSIVE VALIDATION]:
        // [KYA KARTA HAI]: Request null hone ya non-positive ProductId/Quantity par check lagata hai.
        // [KYU ZAROORI HAI]: Corrupt data ya malicious payload se server crash hone ya invalid cart create hone se rokna.
        if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
        {
            // [HTTP 400 RESPONSE]:
            // [KYA KARTA HAI]: Client ko HTTP Status 400 Bad Request aur JSON error message return karta hai.
            return BadRequest(new { success = false, message = "Invalid product or quantity." });
        }

        // [LINE 2: CATALOG DATA LOOKUP]:
        // [KYA KARTA HAI]: In-memory catalog me check karta hai ki ProductId exist karti hai ya nahi.
        // [KAISE CHALTA HAI]: Product memory list par LINQ search run karta hai.
        var product = CatalogController.FindById(request.ProductId);

        // [LINE 3: NULL SAFETY CHECK]:
        // [KYA KARTA HAI]: Verify karta hai ki product database me mila ya nahi.
        if (product == null)
        {
            // [HTTP 404 RESPONSE]:
            // [KYA KARTA HAI]: Agar product ID exist nahi karti toh standard 404 Not Found error return karta hai.
            return NotFound(new { success = false, message = "Product not found in catalog." });
        }

        // [LINE 4: BUSINESS INVENTORY CHECK]:
        // [KYA KARTA HAI]: Check karta hai ki requested quantity warehouse stock se zyada toh nahi hai.
        // [KYU ZAROORI HAI]: Out-of-stock items ko add hone se rokna.
        // [VARIATIONS TO TRY]: Isme check karein ki cart me already pade items + requested quantity milakar stock cross na ho!
        if (product.Stock < request.Quantity)
        {
            // [HTTP 400 RESPONSE]:
            // Client ko helpful dynamic stock balance batata hai.
            return BadRequest(new { success = false, message = $"Sorry, only {product.Stock} items available in stock." });
        }

        // [LINE 5: SESSION MUTATION]:
        // [KYA KARTA HAI]: Service ke zariye current user ke encrypted session cookie me item add/increment karta hai.
        // [KAISE CHALTA HAI]: JSON serialize hokar session store me write hota hai.
        _cartService.AddItem(product, request.Quantity);

        // [LINE 6: FRESH RE-FETCH]:
        // [KYA KARTA HAI]: Recalculated total, updated subtotal aur delivery fee fetch karta hai.
        var updatedCart = _cartService.GetCart();

        // [LINE 7: JSON SUCCESS RESPONSE]:
        // [KYA KARTA HAI]: Browser ke jQuery AJAX callback ko clean JSON payload return karta hai taaki DOM update ho sake.
        // [OUTPUT EXPECTATION]: jQuery header badge me 'cartCount' aur total amount update kar dega without page reload.
        return Json(new
        {
            success = true,
            message = $"{product.Name} added to cart!",
            cartCount = updatedCart.TotalItemCount,
            cartTotal = updatedCart.GrandTotal
        });
    }

    // =====================================================================================
    // ACTION 3: jQuery AJAX Endpoint - Update Quantity (+1 / -1)
    // =====================================================================================

    // [ATTRIBUTE]: Sirf HTTP POST allow karta hai.
    [HttpPost]

    // [ATTRIBUTE]: CSRF token match karta hai.
    [ValidateAntiForgeryToken]
    public IActionResult UpdateQuantityAjax([FromBody] UpdateQuantityRequest request)
    {
        // [LINE 1: SERVICE DELEGATION]:
        // [KYA KARTA HAI]: Given productId par delta (+1 ya -1) apply karta hai. Agar quantity 0 ho jati hai toh item remove ho jata hai.
        _cartService.UpdateQuantity(request.ProductId, request.Delta);

        // [LINE 2: CART RE-EVALUATION]:
        // [KYA KARTA HAI]: Updated calculations (subtotal, delivery surcharge) read karta hai.
        var updatedCart = _cartService.GetCart();

        // [LINE 3: FIND SPECIFIC ITEM]:
        // [KYA KARTA HAI]: Current item ki nayi individual quantity aur line total extract karta hai.
        var item = updatedCart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);

        // [LINE 4: RETURN LIVE STATS]:
        // [KYA KARTA HAI]: Client-side jQuery table row ko update karne ke liye granular numbers return karta hai.
        // [OUTPUT EXPECTATION]: Table row ka subtotal aur grand total dynamically change ho jata hai.
        return Json(new
        {
            success = true,
            newQuantity = item?.Quantity ?? 0,
            newLineTotal = item?.LineTotal ?? 0m,
            subtotal = updatedCart.Subtotal,
            deliveryFee = updatedCart.DeliveryFee,
            grandTotal = updatedCart.GrandTotal,
            totalItems = updatedCart.TotalItemCount
        });
    }

    // =====================================================================================
    // ACTION 4: jQuery AJAX Endpoint - Remove Entire Item
    // =====================================================================================

    // [ATTRIBUTE]: Sirf HTTP POST allow karta hai.
    [HttpPost]

    // [ATTRIBUTE]: CSRF protection.
    [ValidateAntiForgeryToken]
    public IActionResult RemoveItemAjax([FromBody] int productId)
    {
        // [LINE 1: REMOVE FROM SESSION]:
        // [KYA KARTA HAI]: Given ProductId ko session cart list se delete karta hai.
        _cartService.RemoveItem(productId);

        // [LINE 2: READ UPDATED TOTALS]:
        // [KYA KARTA HAI]: Baki bache items ka recalculated grand total nikalta hai.
        var updatedCart = _cartService.GetCart();

        // [LINE 3: JSON CONFIRMATION]:
        // [KYA KARTA HAI]: Success status aur revised cart totals return karta hai taaki client table row ko fadeOut() kar sake.
        return Json(new
        {
            success = true,
            subtotal = updatedCart.Subtotal,
            deliveryFee = updatedCart.DeliveryFee,
            grandTotal = updatedCart.GrandTotal,
            totalItems = updatedCart.TotalItemCount
        });
    }
}
