using KiranaHub.WebApi.Endpoints;
using KiranaHub.WebApi.Middleware;
using KiranaHub.WebApi.Services;

// =========================================================================================
// [TOPIC]: ASP.NET Core Web API Bootstrap & Pipeline Configuration
// [PURPOSE]: Demonstrates DI Lifetimes, Keyed Services, Custom Middlewares, Controllers, and Minimal APIs.
// =========================================================================================

// [KYA KARTA HAI]: WebApplicationBuilder instance create karta hai hosting configuration ke sath.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: API Controllers ke liye required MVC services container me register karta hai.
builder.Services.AddControllers();

// [KYA KARTA HAI]: Endpoint API Explorer register karta hai for Swagger/OpenAPI metadata discovery.
builder.Services.AddEndpointsApiExplorer();

// [KYA KARTA HAI]: Transient lifetime service register karta hai (new instance per resolution).
builder.Services.AddTransient<ITransientService, TransientService>();

// [KYA KARTA HAI]: Scoped lifetime service register karta hai (single instance per HTTP request).
builder.Services.AddScoped<IScopedService, ScopedService>();

// [KYA KARTA HAI]: Singleton lifetime service register karta hai (single instance for entire app runtime).
builder.Services.AddSingleton<ISingletonService, SingletonService>();

// [KYA KARTA HAI]: Keyed Singleton for UPI payment gateway register karta hai with service key "UPI".
// [KYU ZAROORI HAI]: .NET 8+ feature: multiple implementations of same interface disambiguated by key.
builder.Services.AddKeyedSingleton<IPaymentGateway, UpiPaymentGateway>("UPI");

// [KYA KARTA HAI]: Keyed Singleton for Card payment gateway register karta hai with service key "CARD".
builder.Services.AddKeyedSingleton<IPaymentGateway, CardPaymentGateway>("CARD");

// [KYA KARTA HAI]: WebApplication pipeline object build karta hai.
var app = builder.Build();

// [KYA KARTA HAI]: Outer-most middleware: Global exception handler register karta hai.
// [KYU ZAROORI HAI]: Pipeline ke kisi bhi inner step me crash aane par RFC 7807 ProblemDetails return karna.
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// [KYA KARTA HAI]: Second middleware: Correlation ID middleware register karta hai.
// [KYU ZAROORI HAI]: Request and response headers me X-Correlation-ID stamp karna.
app.UseMiddleware<CorrelationIdMiddleware>();

// [KYA KARTA HAI]: Third middleware: Performance profiling middleware register karta hai.
// [KYU ZAROORI HAI]: Request duration measure karna aur X-Response-Time-Ms header likhna.
app.UseMiddleware<PerformanceProfilingMiddleware>();

// [KYA KARTA HAI]: HTTPS redirection middleware add karta hai.
app.UseHttpsRedirection();

// [KYA KARTA HAI]: Authorization middleware pipeline me integrate karta hai.
app.UseAuthorization();

// [KYA KARTA HAI]: Controller-based endpoints (/api/products) ko route table se map karta hai.
app.MapControllers();

// [KYA KARTA HAI]: Extension method call karke Minimal API quick-cart endpoints map karta hai.
app.MapQuickCartEndpoints();

// [KYA KARTA HAI]: Educational endpoint map karta hai jo Transient, Scoped, Singleton, aur Keyed services demonstrate karta hai.
app.MapGet("/api/di-demo", (
    // [KYA KARTA HAI]: Pehla transient instance inject hota hai.
    ITransientService t1, 
    // [KYA KARTA HAI]: Dusra transient instance inject hota hai (will have DIFFERENT GUID!).
    ITransientService t2,
    // [KYA KARTA HAI]: Pehla scoped instance inject hota hai.
    IScopedService s1, 
    // [KYA KARTA HAI]: Dusra scoped instance inject hota hai (will have SAME GUID within this request!).
    IScopedService s2,
    // [KYA KARTA HAI]: Pehla singleton instance inject hota hai.
    ISingletonService single1, 
    // [KYA KARTA HAI]: Dusra singleton instance inject hota hai (will have SAME GUID forever!).
    ISingletonService single2,
    // [KYA KARTA HAI]: Keyed service "UPI" resolve karta hai.
    [FromKeyedServices("UPI")] IPaymentGateway upiGateway,
    // [KYA KARTA HAI]: Keyed service "CARD" resolve karta hai.
    [FromKeyedServices("CARD")] IPaymentGateway cardGateway) =>
{
    // [KYA KARTA HAI]: Instance comparison result JSON me return karta hai with explanation.
    return TypedResults.Ok(new
    {
        Explanation = "Compare Instance IDs: Transient changes every time, Scoped is identical within same request, Singleton never changes.",
        Transient1 = t1.InstanceId,
        Transient2 = t2.InstanceId,
        AreTransientEqual = t1.InstanceId == t2.InstanceId, // Evaluates to False!
        
        Scoped1 = s1.InstanceId,
        Scoped2 = s2.InstanceId,
        AreScopedEqual = s1.InstanceId == s2.InstanceId, // Evaluates to True!
        
        Singleton1 = single1.InstanceId,
        Singleton2 = single2.InstanceId,
        AreSingletonEqual = single1.InstanceId == single2.InstanceId, // Evaluates to True!

        KeyedUpiTest = upiGateway.ProcessPayment(250m),
        KeyedCardTest = cardGateway.ProcessPayment(1500m)
    });
});

// [KYA KARTA HAI]: Server start karta hai.
app.Run();
