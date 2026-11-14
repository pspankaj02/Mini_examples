using KiranaHub.MvcWeb.Services;

// =========================================================================================
// [TOPIC]: ASP.NET Core MVC Application Bootstrap & Pipeline Configuration
// [PURPOSE]: Configures IoC container services, Session middleware, MVC routing, and security.
// =========================================================================================

// [KYA KARTA HAI]: WebApplicationBuilder instance create karta hai pre-configured defaults (Kestrel, appsettings.json, logging) ke sath.
// [KYU ZAROORI HAI]: Modern ASP.NET Core minimal hosting model ka entrypoint jaha services register hoti hain.
// [KAISE CHALTA HAI]: Host.CreateDefaultBuilder() aur WebHostBuilder internal combine hokar dependency injection container setup karte hain.
// [OUTPUT / SIDE EFFECT]: builder object ready for service registration.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: MVC Controllers aur Razor Views ke liye required framework services IoC container me register karta hai.
// [KYU ZAROORI HAI]: Razor view engine, TagHelpers, ViewComponents, aur Model Binders enable karne ke liye zaroori hai.
// [KAISE CHALTA HAI]: Registers IControllerFactory, IViewEngine, ITempDataDictionaryFactory, etc. in container.
builder.Services.AddControllersWithViews();

// [KYA KARTA HAI]: IHttpContextAccessor service ko Singleton ke roop me register karta hai.
// [KYU ZAROORI HAI]: Non-controller classes jaise CartSessionService ke andar current HTTP request ka Session read/write karne ke liye.
// [KAISE CHALTA HAI]: AsyncLocal<HttpContext> ambient context pattern use karke thread-safe context provide karta hai.
builder.Services.AddHttpContextAccessor();

// [KYA KARTA HAI]: In-memory IDistributedCache implementation register karta hai.
// [KYU ZAROORI HAI]: ASP.NET Core Session middleware ko state serialize karke store karne ke liye backing cache chahiye hota hai.
// [KAISE CHALTA HAI]: Managed memory me volatile cache allocate karta hai.
builder.Services.AddDistributedMemoryCache();

// [KYA KARTA HAI]: ASP.NET Core Session state services configure karta hai with security options.
// [KYU ZAROORI HAI]: User ka shopping cart server-side state me preserve rakhna across page requests.
builder.Services.AddSession(options =>
{
    // [KYA KARTA HAI]: Session expiry idle timeout 30 minutes set karta hai.
    // [KYU ZAROORI HAI]: Inactive sessions ko memory se automatically garbage collect karna.
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // [KYA KARTA HAI]: Session cookie par HttpOnly flag enable karta hai.
    // [KYU ZAROORI HAI]: Security hardening: JavaScript document.cookie se session ID read nahi kar sakti (XSS protection).
    options.Cookie.HttpOnly = true;

    // [KYA KARTA HAI]: Cookie ko essential mark karta hai.
    // [KYU ZAROORI HAI]: GDPR cookie consent ban hone par bhi shopping cart session operate kar sake.
    options.Cookie.IsEssential = true;

    // [KYA KARTA HAI]: Session cookie ka custom secure name set karta hai.
    // [KYU ZAROORI HAI]: Default framework names mask karke fingerprinting prevent karna.
    options.Cookie.Name = ".KiranaHub.Session";
});

// [KYA KARTA HAI]: ICartSessionService ko Scoped lifetime ke sath DI container me register karta hai.
// [KYU ZAROORI HAI]: Har HTTP request ke dauran single CartSessionService instance banega aur request end hone par dispose ho jayega.
// [KAISE CHALTA HAI]: Container request scope ke andar single instance provide karta hai across controllers and view components.
builder.Services.AddScoped<ICartSessionService, CartSessionService>();

// [KYA KARTA HAI]: Service collection freeze karke WebApplication pipeline instance build karta hai.
// [KYU ZAROORI HAI]: Pipeline middleware registration phase shuru karne ke liye.
// [KAISE CHALTA HAI]: ServiceProvider build hota hai aur DI container immutable ban jata hai.
var app = builder.Build();

// [KYA KARTA HAI]: Environment check karta hai ki application Production/Staging me run ho rahi hai ya Development me.
// [KYU ZAROORI HAI]: Production me raw developer stack traces hide karna taaki sensitive information leak na ho.
if (!app.Environment.IsDevelopment())
{
    // [KYA KARTA HAI]: Global unhandled exceptions ko friendly custom error page (/Home/Error) par redirect karta hai.
    app.UseExceptionHandler("/Home/Error");

    // [KYA KARTA HAI]: HTTP Strict Transport Security (HSTS) response header enable karta hai.
    // [KYU ZAROORI HAI]: Browsers ko force karta hai ki sirf secure HTTPS protocol se communicate karein (downgrade attack prevention).
    app.UseHsts();
}

// [KYA KARTA HAI]: Incoming HTTP requests ko automatic HTTPS secure port par 301/307 redirect karta hai.
// [KYU ZAROORI HAI]: Network sniffing aur plaintext communication prevent karna.
app.UseHttpsRedirection();

// [KYA KARTA HAI]: Routing middleware ko pipeline me introduce karta hai jo URL ko endpoints se match karta hai.
// [KYU ZAROORI HAI]: Endpoint routing decision evaluate karna before session and authorization.
app.UseRouting();

// [KYA KARTA HAI]: Session middleware activate karta hai pipeline me.
// [KYU ZAROORI HAI]: Request me se session cookie read karna aur HttpContext.Session ko populate karna.
// [CRITICAL RULE]: Session middleware MUST be placed between UseRouting() and MapControllerRoute().
app.UseSession();

// [KYA KARTA HAI]: Authorization middleware activate karta hai.
// [KYU ZAROORI HAI]: Protected endpoints par user access policies enforce karna.
app.UseAuthorization();

// [KYA KARTA HAI]: Conventional MVC route template define karta hai with defaults.
// [KYU ZAROORI HAI]: Root URL "/" hit hone par CatalogController ka Index action automatically invoke ho.
// [KAISE CHALTA HAI]: Pattern "{controller=Catalog}/{action=Index}/{id?}" token matching karta hai.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// [KYA KARTA HAI]: Kestrel web server start karta hai aur incoming HTTP requests listen karna shuru karta hai.
// [KYU ZAROORI HAI]: Blocking call jo application ko background me running state me rakhti hai.
// [OUTPUT / SIDE EFFECT]: Application starts listening on configured port (e.g. http://localhost:5000).
app.Run();
