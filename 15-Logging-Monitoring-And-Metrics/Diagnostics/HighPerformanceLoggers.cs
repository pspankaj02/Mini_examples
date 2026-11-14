using Microsoft.Extensions.Logging;

namespace KiranaHub.LoggingAndMetrics.Diagnostics;

// =========================================================================================
// [TOPIC]: Zero-Allocation High-Performance Logging (LoggerMessage.Define)
// [PURPOSE]: Eliminates string formatting boxing and GC heap allocations in hot code paths.
// =========================================================================================

// [KYA KARTA HAI]: Pre-compiled cached logging delegates provide karta hai.
// [KYU ZAROORI HAI]: Standard _logger.LogInformation("Order {Id} processed", id) har call par object[] array allocate karta hai; LoggerMessage.Define 0 heap allocation karta hai!
public static class HighPerformanceLoggers
{
    // [KYA KARTA HAI]: Pre-compiled delegate for Order Processing event.
    // [KAISE CHALTA HAI]: EventId 1001, LogLevel.Information, strongly-typed parameters (string orderId, decimal amount).
    private static readonly Action<ILogger, string, decimal, Exception?> _orderProcessed =
        LoggerMessage.Define<string, decimal>(
            LogLevel.Information,
            new EventId(1001, nameof(OrderProcessed)),
            "[PERF LOG] Order {OrderId} placed successfully totaling Rs. {Amount:N2}"
        );

    // [KYA KARTA HAI]: Pre-compiled delegate for Rider Dispatch event.
    private static readonly Action<ILogger, string, string, Exception?> _riderDispatched =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1002, nameof(RiderDispatched)),
            "[PERF LOG] Rider {RiderName} assigned to deliver Order {OrderId}"
        );

    // [KYA KARTA HAI]: Extension method jo zero-allocation order processed log emit karta hai.
    public static void OrderProcessed(this ILogger logger, string orderId, decimal amount)
    {
        // [KYA KARTA HAI]: Pre-compiled delegate invoke karta hai (zero string boxing).
        _orderProcessed(logger, orderId, amount, null);
    }

    // [KYA KARTA HAI]: Extension method jo zero-allocation rider dispatch log emit karta hai.
    public static void RiderDispatched(this ILogger logger, string riderName, string orderId)
    {
        // [KYA KARTA HAI]: Delegate invoke karta hai.
        _riderDispatched(logger, riderName, orderId, null);
    }
}

