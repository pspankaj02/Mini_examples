using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KiranaHub.CleanArchitecture.Application.Common.Behaviors;

// =========================================================================================
// [TOPIC]: MediatR Pipeline Behavior - Logging & Timing
// [PURPOSE]: Cross-cutting concern for logging every command and query entering the system.
// =========================================================================================

// [KYA KARTA HAI]: MediatR pipeline behavior jo har request ke execution time ko measure aur log karta hai.
// [KYU ZAROORI HAI]: AOP (Aspect-Oriented Programming): Handlers ke andar bar-baar logging code likhne ki zaroorat nahi padti.
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // [KYA KARTA HAI]: Logger dependency.
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    // [KYA KARTA HAI]: Constructor DI.
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    // [KYA KARTA HAI]: Pipeline interception method.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // [KYA KARTA HAI]: Request type name capture karta hai.
        string requestName = typeof(TRequest).Name;

        // [KYA KARTA HAI]: Request entry log karta hai.
        _logger.LogInformation("[MEDIATR PIPELINE] Handling {RequestName}", requestName);

        // [KYA KARTA HAI]: Stopwatch start karta hai.
        var stopwatch = Stopwatch.StartNew();

        // [KYA KARTA HAI]: Pipeline ke agle behavior ya actual request handler ko invoke karta hai.
        var response = await next();

        // [KYA KARTA HAI]: Stopwatch stop karta hai.
        stopwatch.Stop();

        // [KYA KARTA HAI]: Request completion log karta hai with elapsed milliseconds.
        _logger.LogInformation("[MEDIATR PIPELINE] Handled {RequestName} in {ElapsedMs}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        // [KYA KARTA HAI]: Response return karta hai caller ko.
        return response;
    }
}

