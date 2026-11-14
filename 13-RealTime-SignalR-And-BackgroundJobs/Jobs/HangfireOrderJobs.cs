using Microsoft.Extensions.Logging;

namespace KiranaHub.RealTimeAndJobs.Jobs;

// =========================================================================================
// [TOPIC]: Hangfire Background Job Processing
// [PURPOSE]: Asynchronous Fire-and-Forget, Delayed, and Recurring background jobs.
// =========================================================================================

// [KYA KARTA HAI]: Hangfire background jobs ka contract define karta hai.
public interface IHangfireOrderJobs
{
    // [KYA KARTA HAI]: Fire-and-Forget job: Invoice PDF generate aur email karta hai.
    Task SendInvoiceEmailAsync(string orderId, string customerEmail);

    // [KYA KARTA HAI]: Delayed job: Delivery ke baad customer feedback SMS bhejta hai.
    Task SendReviewSmsAsync(string orderId, string customerPhone);

    // [KYA KARTA HAI]: Recurring job: Har raat inventory count reconcile karta hai.
    Task DailyInventoryReconciliationAsync();
}

// [KYA KARTA HAI]: IHangfireOrderJobs ka concrete implementation.
public class HangfireOrderJobs : IHangfireOrderJobs
{
    // [KYA KARTA HAI]: Logger reference.
    private readonly ILogger<HangfireOrderJobs> _logger;

    // [KYA KARTA HAI]: Test harness assertions ke liye executed jobs count track karta hai.
    public static int InvoicesSentCount { get; private set; }
    public static int ReviewSmsScheduledCount { get; private set; }
    public static int ReconciliationsCount { get; private set; }

    // [KYA KARTA HAI]: Constructor DI.
    public HangfireOrderJobs(ILogger<HangfireOrderJobs> logger)
    {
        _logger = logger;
    }

    // [KYA KARTA HAI]: Fire-and-Forget invoice generation job method.
    // [KYU ZAROORI HAI]: Customer HTTP checkout request ko fast finish karna; PDF generation background thread pool me chalti hai.
    public async Task SendInvoiceEmailAsync(string orderId, string customerEmail)
    {
        // [KYA KARTA HAI]: Execution start log karta hai.
        _logger.LogInformation("[HANGFIRE FIRE-AND-FORGET] Generating & emailing PDF invoice for Order {OrderId} to {Email}...",
            orderId, customerEmail);

        // [KYA KARTA HAI]: Heavy PDF generation simulation (50ms).
        await Task.Delay(50);

        // [KYA KARTA HAI]: Atomic counter increment for verification.
        InvoicesSentCount++;

        // [KYA KARTA HAI]: Console confirmation print karta hai.
        Console.WriteLine($"   >>> [HANGFIRE JOB COMPLETED] Invoice PDF delivered to {customerEmail} for {orderId}");
    }

    // [KYA KARTA HAI]: Delayed job method for review solicitation.
    public async Task SendReviewSmsAsync(string orderId, string customerPhone)
    {
        // [KYA KARTA HAI]: Delayed execution log karta hai.
        _logger.LogInformation("[HANGFIRE DELAYED JOB] Sending review SMS to {Phone} for completed Order {OrderId}...",
            customerPhone, orderId);

        // [KYA KARTA HAI]: SMS gateway latency simulation.
        await Task.Delay(20);

        // [KYA KARTA HAI]: Counter increment.
        ReviewSmsScheduledCount++;

        // [KYA KARTA HAI]: Console message.
        Console.WriteLine($"   >>> [HANGFIRE JOB COMPLETED] Review link SMS delivered to {customerPhone}");
    }

    // [KYA KARTA HAI]: Recurring daily job method.
    public async Task DailyInventoryReconciliationAsync()
    {
        // [KYA KARTA HAI]: Reconciliation start log karta hai.
        _logger.LogInformation("[HANGFIRE RECURRING CRON] Executing nightly warehouse stock audit against database ledger...");

        // [KYA KARTA HAI]: Audit simulation.
        await Task.Delay(30);

        // [KYA KARTA HAI]: Counter increment.
        ReconciliationsCount++;

        // [KYA KARTA HAI]: Console output.
        Console.WriteLine("   >>> [HANGFIRE CRON COMPLETED] Inventory ledger reconciled with zero discrepancy.");
    }
}

