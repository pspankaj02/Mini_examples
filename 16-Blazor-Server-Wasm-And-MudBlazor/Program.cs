using System;
using System.Diagnostics;
using KiranaHub.BlazorStore;
using KiranaHub.BlazorStore.Models;
using KiranaHub.BlazorStore.Services;
using MudBlazor.Services;

// =========================================================================================
// [TOPIC]: Blazor Full-Stack Web App Bootstrap & Verification Harness
// [PURPOSE]: Configures Interactive Server Components, MudBlazor UI library, and reactive state.
// =========================================================================================

// [KYA KARTA HAI]: WebApplicationBuilder instance allocate karta hai.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: Razor components aur interactive server components IoC container me add karta hai.
// [KYU ZAROORI HAI]: Modern unified Blazor Web App model me Server-side interactive rendering enable karna.
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// [KYA KARTA HAI]: MudBlazor UI component services (DialogService, Snackbar, Popover, ResizeObserver) register karta hai.
builder.Services.AddMudServices();

// [KYA KARTA HAI]: In-memory grocery catalog service register karta hai as Singleton.
builder.Services.AddSingleton<ICatalogService, CatalogService>();

// [KYA KARTA HAI]: Reactive shopping cart state service register karta hai as Scoped (per user session).
builder.Services.AddScoped<ICartStateService, CartStateService>();

// [KYA KARTA HAI]: Kestrel port 5445 bind karta hai.
builder.WebHost.UseUrls("http://127.0.0.1:5445");

// [KYA KARTA HAI]: WebApplication build karta hai.
var app = builder.Build();

// [KYA KARTA HAI]: Static files (CSS, JS, images, fonts) serve karne ka middleware pipeline me add karta hai.
app.UseStaticFiles();

// [KYA KARTA HAI]: Anti-forgery CSRF protection token validation middleware enable karta hai.
app.UseAntiforgery();

// [KYA KARTA HAI]: Root App component ko map karta hai with InteractiveServer render mode support.
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 16: BLAZOR SERVER, WASM & MUDBLAZOR DEMONSTRATION          ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. SELF-VERIFICATION TEST: REACTIVE CART STATE CALCULATIONS
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] Verifying Reactive Cart State Service (Subtotal, Discounts, Delivery Fee)");

var cart = new CartStateService();
bool eventFired = false;
cart.OnChange += () => eventFired = true;

// Add 2x Milk (33 each = 66)
var milk = new GroceryItem { Id = 1, Sku = "MILK-01", Name = "Amul Milk 500ml", Price = 33m, Stock = 50 };
cart.AddItem(milk, 2);

Debug.Assert(eventFired, "Cart OnChange event was not triggered on AddItem!");
Debug.Assert(cart.TotalItemCount == 2);
Debug.Assert(cart.Subtotal == 66m);
Debug.Assert(cart.DeliveryFee == 40m, "Orders below Rs. 500 must have Rs. 40 delivery fee!");
Debug.Assert(cart.GrandTotal == 106m);
Console.WriteLine($"   Cart Subtotal: Rs. {cart.Subtotal} | Delivery Fee: Rs. {cart.DeliveryFee} | Grand Total: Rs. {cart.GrandTotal}");

// Add 2x Atta (245 each = 490) -> Subtotal becomes 556 (> 500 -> Free delivery!)
var atta = new GroceryItem { Id = 2, Sku = "ATTA-01", Name = "Aashirvaad Atta 5kg", Price = 245m, Stock = 20 };
cart.AddItem(atta, 2);

Debug.Assert(cart.Subtotal == 556m);
Debug.Assert(cart.DeliveryFee == 0m, "Orders above Rs. 500 must receive FREE delivery!");
Debug.Assert(cart.GrandTotal == 556m);
Console.WriteLine($"   Free Delivery Triggered: Subtotal Rs. {cart.Subtotal} -> Delivery Fee: Rs. {cart.DeliveryFee}");

// Apply Promo Coupon "KIRANA50" -> Flat Rs. 50 discount
bool couponApplied = cart.ApplyCoupon("KIRANA50");
Debug.Assert(couponApplied);
Debug.Assert(cart.DiscountAmount == 50m);
Debug.Assert(cart.GrandTotal == 506m);
Console.WriteLine($"   Coupon 'KIRANA50' Applied: Discount Rs. {cart.DiscountAmount} -> Final Payable: Rs. {cart.GrandTotal}");

Console.WriteLine("[PASS] Scoped reactive cart business logic verified successfully!\n");

// ----------------------------------------------------------------------------------------
// 2. SELF-VERIFICATION TEST: CATALOG INVENTORY SERVICE
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Verifying Catalog Inventory Service & Stock Deduction");

var catalog = new CatalogService();
var products = await catalog.GetProductsAsync();
Debug.Assert(products.Count >= 8, "Catalog should have at least 8 grocery items seeded!");

var categories = await catalog.GetCategoriesAsync();
Debug.Assert(categories.Contains("Dairy") && categories.Contains("Grains"));

// Deduct 5 units of Milk
bool deductSuccess = await catalog.DeductStockAsync(1, 5);
Debug.Assert(deductSuccess);
var updatedMilk = (await catalog.GetProductsAsync()).First(x => x.Id == 1);
Debug.Assert(updatedMilk.Stock == 45);
Console.WriteLine($"   Deducted 5 units of Milk. Stock updated from 50 to: {updatedMilk.Stock}");

Console.WriteLine("[PASS] Catalog inventory service verified successfully!\n");

// ----------------------------------------------------------------------------------------
// 3. START BLAZOR HOST (ASYNC VERIFICATION)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] Booting Kestrel Blazor Server Host on http://127.0.0.1:5445");
await app.StartAsync();
Console.WriteLine("[PASS] Blazor Server circuit listener started successfully!");

using var httpClient = new HttpClient();
var indexResponse = await httpClient.GetAsync("http://127.0.0.1:5445/");
Debug.Assert(indexResponse.IsSuccessStatusCode, "Blazor host failed to respond to root request!");
Console.WriteLine($"   Root HTTP Status: {indexResponse.StatusCode} (MudBlazor UI ready for user browser)");

Console.WriteLine("\n==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 16-BLAZOR-SERVER-WASM-AND-MUDBLAZOR TESTS PASSED!        ");
Console.WriteLine("==========================================================================");

await app.StopAsync();

