using KiranaHub.MvcWeb.Models;
using KiranaHub.MvcWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.MvcWeb.Controllers;

// =========================================================================================
// [TOPIC]: ASP.NET Core MVC Catalog Controller
// [PURPOSE]: Handles web requests for grocery catalog browsing, category filtering, and search.
// =========================================================================================

// [KYA KARTA HAI]: Microsoft.AspNetCore.Mvc.Controller se inherit karne wala MVC Controller jo HTML Views render karta hai.
// [KYU ZAROORI HAI]: Razor Views, ViewBag, TempData, aur View() helper methods access karne ke liye Controller base class zaroori hai.
// [KAISE CHALTA HAI]: ASP.NET Core routing engine URL matching se action method resolve karta hai (e.g. /Catalog/Index).
public class CatalogController : Controller
{
    // [KYA KARTA HAI]: In-memory grocery products ka static thread-safe read list maintain karta hai.
    // [KYU ZAROORI HAI]: Demo and educational purpose ke liye database setup ke bina instant catalog data provide karna.
    // [KAISE CHALTA HAI]: Application startup par heap memory me single static list allocate hoti hai jo all requests share karti hain.
    // [OUTPUT / SIDE EFFECT]: Memory me 8 sample grocery items ready rehte hain.
    private static readonly List<GroceryProduct> Products = new()
    {
        // [KYA KARTA HAI]: Amul Milk grocery product define karta hai (Perishable = true).
        new GroceryProduct { Id = 1, Name = "Amul Fresh Cow Milk 500ml", Category = "Dairy", Price = 33m, Stock = 40, IsPerishable = true },
        // [KYA KARTA HAI]: Amul Butter define karta hai.
        new GroceryProduct { Id = 2, Name = "Amul Salted Table Butter 100g", Category = "Dairy", Price = 58m, Stock = 25, IsPerishable = true },
        // [KYA KARTA HAI]: Aashirvaad Atta grocery product define karta hai (Perishable = false).
        new GroceryProduct { Id = 3, Name = "Aashirvaad Shudh Chakki Atta 5kg", Category = "Grains", Price = 245m, Stock = 15, IsPerishable = false },
        // [KYA KARTA HAI]: India Gate Basmati Rice item define karta hai.
        new GroceryProduct { Id = 4, Name = "India Gate Basmati Rice 1kg", Category = "Grains", Price = 130m, Stock = 20, IsPerishable = false },
        // [KYA KARTA HAI]: Tata Salt define karta hai with high stock.
        new GroceryProduct { Id = 5, Name = "Tata Salt Vacuum Evaporated 1kg", Category = "Pantry", Price = 28m, Stock = 100, IsPerishable = false },
        // [KYA KARTA HAI]: Haldiram Aloo Bhujia snacks define karta hai.
        new GroceryProduct { Id = 6, Name = "Haldiram Nagpur Aloo Bhujia 400g", Category = "Snacks", Price = 115m, Stock = 35, IsPerishable = false },
        // [KYA KARTA HAI]: Tata Tea Premium beverages item define karta hai.
        new GroceryProduct { Id = 7, Name = "Tata Tea Premium 500g", Category = "Beverages", Price = 210m, Stock = 18, IsPerishable = false },
        // [KYA KARTA HAI]: Tender Coconut define karta hai with zero stock (Out of stock testing scenario).
        new GroceryProduct { Id = 8, Name = "Fresh Tender Coconut", Category = "Beverages", Price = 65m, Stock = 0, IsPerishable = true }
    };

    // [KYA KARTA HAI]: Product ID ke basis par catalog me item lookup karta hai.
    // [KYU ZAROORI HAI]: CartController aur dusre components ko product details fetch karne ke liye helper provide karna.
    // [KAISE CHALTA HAI]: FirstOrDefault LINQ expression list ko iterate karta hai; matching item ya null return karta hai.
    // [OUTPUT / RETURN]: GroceryProduct instance ya null agar ID match na ho.
    public static GroceryProduct? FindById(int id) => Products.FirstOrDefault(x => x.Id == id);

    // [KYA KARTA HAI]: Catalog page ka main action method jo category filter aur search query accept karta hai.
    // [KYU ZAROORI HAI]: User jab URL hit kare (e.g. /Catalog?category=Dairy&search=milk), filtered list render ho sake.
    // [KAISE CHALTA HAI]: Model binding query string se parameters bind karta hai; LINQ filtration chalti hai aur Razor view return hota hai.
    // [OUTPUT / RETURN]: ViewResult containing strongly-typed CatalogIndexViewModel.
    public IActionResult Index(string? category, string? search)
    {
        // [KYA KARTA HAI]: Static list ko IEnumerable query me wrap karta hai for deferred execution.
        // [KYU ZAROORI HAI]: Step-by-step conditional filtering apply karne ke liye.
        var filtered = Products.AsEnumerable();

        // [KYA KARTA HAI]: Check karta hai ki category parameter provided aur non-empty hai ya nahi.
        // [KYU ZAROORI HAI]: Optional filter check karna before LINQ evaluation.
        if (!string.IsNullOrWhiteSpace(category))
        {
            // [KYA KARTA HAI]: Category match filter apply karta hai case-insensitively.
            // [KYU ZAROORI HAI]: "dairy", "DAIRY", "Dairy" teeno formats match ho sakein.
            // [KAISE CHALTA HAI]: StringComparison.OrdinalIgnoreCase culture-agnostic fast string comparison karta hai.
            filtered = filtered.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        // [KYA KARTA HAI]: Check karta hai ki search box me user ne koi text enter kiya hai ya nahi.
        // [KYU ZAROORI HAI]: Substring keyword filtering tabhi chale jab search term ho.
        if (!string.IsNullOrWhiteSpace(search))
        {
            // [KYA KARTA HAI]: Product Name me search keyword ka case-insensitive contains match karta hai.
            // [KYU ZAROORI HAI]: Substring search enable karna (e.g. "milk" matches "Amul Fresh Cow Milk 500ml").
            filtered = filtered.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // [KYA KARTA HAI]: Strongly-typed ViewModel instantiate karta hai UI rendering ke liye.
        // [KYU ZAROORI HAI]: Separation of Concerns: Domain models ko directly View me expose karne se bachte hain; ViewModel me display-specific data bundle karte hain.
        var viewModel = new CatalogIndexViewModel
        {
            // [KYA KARTA HAI]: Filtered query ko evaluate karke List<GroceryProduct> me materialize karta hai.
            // [KYU ZAROORI HAI]: Razor view me repeated iterations fast ho sakein.
            Products = filtered.ToList(),

            // [KYA KARTA HAI]: Catalog ke sabhi unique categories extract karta hai filter dropdown ke liye.
            // [KYU ZAROORI HAI]: Category pill buttons dynamically render ho sakein.
            Categories = Products.Select(p => p.Category).Distinct().ToList(),

            // [KYA KARTA HAI]: Current selected category ko ViewModel me preserve karta hai.
            // [KYU ZAROORI HAI]: UI me active tab highlight karne ke liye.
            SelectedCategory = category,

            // [KYA KARTA HAI]: Current search keyword ko ViewModel me preserve karta hai.
            // [KYU ZAROORI HAI]: Search input field me user ka query text retain karne ke liye.
            SearchTerm = search
        };

        // [KYA KARTA HAI]: Views/Catalog/Index.cshtml view ko ViewModel pass karke HTML response render karta hai.
        // [KYU ZAROORI HAI]: MVC pipeline view engine ko trigger karke HTML response stream generate kare.
        // [OUTPUT / RETURN]: ViewResult with 200 OK status code.
        return View(viewModel);
    }
}
