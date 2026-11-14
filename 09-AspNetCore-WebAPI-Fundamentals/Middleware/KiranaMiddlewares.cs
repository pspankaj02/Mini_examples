using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.WebApi.Middleware;

// =========================================================================================
// [TOPIC]: Custom ASP.NET Core Middlewares
// [PURPOSE]: Correlation ID tracking, Performance Profiling, and RFC 7807 Global Error Handling.
// =========================================================================================

// -----------------------------------------------------------------------------------------
// [MIDDLEWARE 1]: CorrelationIdMiddleware
// -----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Har request me X-Correlation-ID trace header inject aur verify karta hai.
// [KYU ZAROORI HAI]: Distributed observability: ek customer request ko API Gateway, Services, aur Logs me track karna.
public class CorrelationIdMiddleware
{
    // [KYA KARTA HAI]: Header name constant define karta hai ("X-Correlation-ID").
    private const string CorrelationHeader = "X-Correlation-ID";

    // [KYA KARTA HAI]: Pipeline me agle middleware ko invoke karne ka delegate reference store karta hai.
    private readonly RequestDelegate _next;

    // [KYA KARTA HAI]: Structured logger instance store karta hai.
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    // [KYA KARTA HAI]: Middleware constructor jo RequestDelegate aur Logger receive karta hai.
    // [KAISE CHALTA HAI]: ASP.NET Core pipeline creation time par delegate automatically pass karti hai.
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        // [KYA KARTA HAI]: Pipeline delegate assign karta hai with null check.
        _next = next ?? throw new ArgumentNullException(nameof(next));

        // [KYA KARTA HAI]: Logger assign karta hai with null check.
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // [KYA KARTA HAI]: Middleware execution entrypoint jo per-request execute hota hai.
    // [KYU ZAROORI HAI]: Incoming request header check karna aur correlation context setup karna.
    // [KAISE CHALTA HAI]: ASP.NET Core pipeline convention-based InvokeAsync method call karti hai.
    public async Task InvokeAsync(HttpContext context)
    {
        // [KYA KARTA HAI]: Check karta hai ki client ya API gateway ne already X-Correlation-ID header bheja hai ya nahi.
        // [KYU ZAROORI HAI]: Upstream distributed trace ID ko reuse karna agar already present ho.
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            // [KYA KARTA HAI]: Agar correlation ID missing ho, toh cryptographically fresh 32-character hex GUID generate karta hai.
            // [OUTPUT / SIDE EFFECT]: e.g., "e7c2f81a4b9e4a30b8c2d1e2f3a4b5c6".
            correlationId = Guid.NewGuid().ToString("N");
        }

        // [KYA KARTA HAI]: Correlation ID ko HttpContext.Items dictionary me save karta hai.
        // [KYU ZAROORI HAI]: Downstream controllers aur services is ID ko current request context se read kar sakein.
        context.Items[CorrelationHeader] = correlationId.ToString();

        // [KYA KARTA HAI]: HTTP Response Headers me bhi X-Correlation-ID header set karta hai.
        // [KYU ZAROORI HAI]: Client ko correlation ID wapas mil sake taaki customer support ko share kiya ja sake.
        context.Response.Headers[CorrelationHeader] = correlationId.ToString();

        // [KYA KARTA HAI]: Serilog/ILogger logging scope open karta hai jisme CorrelationId attached rehta hai.
        // [KYU ZAROORI HAI]: Is scope ke andar log hone wale sabhi statements me CorrelationId automatic structured property ban kar aati hai.
        // [KAISE CHALTA HAI]: IDisposable logging scope downstream middleware aur controllers tak active rehta hai.
        using (_logger.BeginScope("{@CorrelationId}", correlationId.ToString()))
        {
            // [KYA KARTA HAI]: Request processing ko pipeline ke agle middleware ko handoff karta hai.
            // [OUTPUT / SIDE EFFECT]: Downstream execution starts.
            await _next(context);
        }
    }
}

// -----------------------------------------------------------------------------------------
// [MIDDLEWARE 2]: PerformanceProfilingMiddleware
// -----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Har incoming HTTP request ka execution time stopwatch se measure karta hai.
// [KYU ZAROORI HAI]: SLA tracking aur slow API queries ko identify karke alert log karna.
public class PerformanceProfilingMiddleware
{
    // [KYA KARTA HAI]: Next middleware delegate.
    private readonly RequestDelegate _next;

    // [KYA KARTA HAI]: Logger reference.
    private readonly ILogger<PerformanceProfilingMiddleware> _logger;

    // [KYA KARTA HAI]: Constructor DI injection.
    public PerformanceProfilingMiddleware(RequestDelegate next, ILogger<PerformanceProfilingMiddleware> logger)
    {
        // [KYA KARTA HAI]: Pipeline delegate store karta hai.
        _next = next ?? throw new ArgumentNullException(nameof(next));

        // [KYA KARTA HAI]: Logger store karta hai.
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // [KYA KARTA HAI]: Request lifecycle latency measure karta hai.
    public async Task InvokeAsync(HttpContext context)
    {
        // [KYA KARTA HAI]: High-resolution Stopwatch timer start karta hai.
        // [KYU ZAROORI HAI]: Microsecond accuracy time tracking.
        var timer = Stopwatch.StartNew();

        // [KYA KARTA HAI]: Response headers write hone se pehle hook register karta hai.
        // [KYU ZAROORI HAI]: Response stream start hone se pehle custom timing header inject karna.
        // [KAISE CHALTA HAI]: Kestrel response flush karne se just pehle is callback ko invoke karta hai.
        context.Response.OnStarting(() =>
        {
            // [KYA KARTA HAI]: Stopwatch stop karta hai.
            timer.Stop();

            // [KYA KARTA HAI]: X-Response-Time-Ms header me elapsed milliseconds value set karta hai.
            // [OUTPUT / SIDE EFFECT]: Client ko response header me e.g. "X-Response-Time-Ms: 14" milta hai.
            context.Response.Headers["X-Response-Time-Ms"] = timer.ElapsedMilliseconds.ToString();

            // [KYA KARTA HAI]: Completed task return karta hai callback contract fulfill karne ke liye.
            return Task.CompletedTask;
        });

        // [KYA KARTA HAI]: Pipeline ke agle middleware ko call karta hai.
        await _next(context);

        // [KYA KARTA HAI]: Check karta hai ki request ka execution time 500ms se zyada toh nahi laga.
        // [KYU ZAROORI HAI]: Performance bottleneck detection (SLA violation alert).
        if (timer.ElapsedMilliseconds > 500)
        {
            // [KYA KARTA HAI]: Warning level log record karta hai with HTTP method, URL path, aur elapsed time.
            _logger.LogWarning("SLOW REQUEST: {Method} {Path} took {Elapsed}ms",
                context.Request.Method, context.Request.Path, timer.ElapsedMilliseconds);
        }
    }
}

// -----------------------------------------------------------------------------------------
// [MIDDLEWARE 3]: GlobalExceptionHandlerMiddleware
// -----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Unhandled runtime exceptions ko catch karke standard RFC 7807 ProblemDetails JSON me convert karta hai.
// [KYU ZAROORI HAI]: Production crashes me raw stack trace hide karna aur client ko machine-readable uniform error contract dena.
public class GlobalExceptionHandlerMiddleware
{
    // [KYA KARTA HAI]: Next delegate.
    private readonly RequestDelegate _next;

    // [KYA KARTA HAI]: Logger.
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    // [KYA KARTA HAI]: Constructor DI.
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        // [KYA KARTA HAI]: Next delegate assign karta hai.
        _next = next ?? throw new ArgumentNullException(nameof(next));

        // [KYA KARTA HAI]: Logger assign karta hai.
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // [KYA KARTA HAI]: Pipeline invocation ko try-catch block me wrap karta hai.
    public async Task InvokeAsync(HttpContext context)
    {
        // [KYA KARTA HAI]: Try block start karta hai downstream middleware ko protect karne ke liye.
        try
        {
            // [KYA KARTA HAI]: Downstream execution continue karta hai.
            await _next(context);
        }
        // [KYA KARTA HAI]: Kisi bhi unhandled exception ko catch karta hai.
        catch (Exception ex)
        {
            // [KYA KARTA HAI]: Critical error log karta hai with full exception stack trace.
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

            // [KYA KARTA HAI]: Response content type ko RFC 7807 standard MIME type "application/problem+json" set karta hai.
            context.Response.ContentType = "application/problem+json";

            // [KYA KARTA HAI]: HTTP status code ko 500 Internal Server Error set karta hai.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // [KYA KARTA HAI]: Correlation ID extract karta hai error context me trace link karne ke liye.
            string correlationId = context.Items.TryGetValue("X-Correlation-ID", out var cId) 
                ? cId?.ToString() ?? "N/A" : "N/A";

            // [KYA KARTA HAI]: RFC 7807 ProblemDetails object instantiate karta hai structured error details ke sath.
            var problem = new ProblemDetails
            {
                // [KYA KARTA HAI]: Error category URI identifier.
                Type = "https://kiranahub.com/errors/internal-server-error",
                // [KYA KARTA HAI]: User-friendly high level title.
                Title = "An internal server error occurred while processing your grocery order.",
                // [KYA KARTA HAI]: HTTP Status integer.
                Status = StatusCodes.Status500InternalServerError,
                // [KYA KARTA HAI]: Exception detail message.
                Detail = ex.Message,
                // [KYA KARTA HAI]: URL path jaha crash hua.
                Instance = context.Request.Path
            };

            // [KYA KARTA HAI]: CorrelationId extension property me add karta hai support debugging ke liye.
            problem.Extensions["correlationId"] = correlationId;

            // [KYA KARTA HAI]: ProblemDetails object ko JSON string me serialize karta hai.
            var json = JsonSerializer.Serialize(problem);

            // [KYA KARTA HAI]: Serialized JSON ko HTTP response body me write karta hai.
            // [OUTPUT / RETURN]: Clean JSON response with 500 status code.
            await context.Response.WriteAsync(json);
        }
    }
}
