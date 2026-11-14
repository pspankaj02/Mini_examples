using Microsoft.Extensions.Logging;
using Quartz;

namespace KiranaHub.RealTimeAndJobs.Jobs;

// =========================================================================================
// [TOPIC]: Quartz.NET Scheduled Job (IJob)
// [PURPOSE]: Enterprise cron-based precision job scheduling for warehouse expiry audits.
// =========================================================================================

// [KYA KARTA HAI]: Quartz IJob interface implement karta hai jo scheduled trigger par execute hota hai.
// [KYU ZAROORI HAI]: Complex cron expressions (e.g. "0 0/15 * * * ?") aur multi-node cluster locking ke liye Quartz industry benchmark hai.
[DisallowConcurrentExecution]
public class PerishableInventoryAuditJob : IJob
{
    // [KYA KARTA HAI]: Logger reference.
    private readonly ILogger<PerishableInventoryAuditJob> _logger;

    // [KYA KARTA HAI]: Execution count for test assertion.
    public static int AuditExecutionsCount { get; private set; }

    // [KYA KARTA HAI]: Constructor DI.
    public PerishableInventoryAuditJob(ILogger<PerishableInventoryAuditJob> logger)
    {
        _logger = logger;
    }

    // [KYA KARTA HAI]: Quartz scheduler dwara trigger hone par ye method invoke hota hai.
    public Task Execute(IJobExecutionContext context)
    {
        // [KYA KARTA HAI]: Counter increment karta hai.
        AuditExecutionsCount++;

        // [KYA KARTA HAI]: Execution start log karta hai.
        _logger.LogInformation("[QUARTZ.NET CRON TRIGGER] Running perishable goods expiry check (Milk, Bread, Butter)...");

        // [KYA KARTA HAI]: Simulation log message print karta hai.
        Console.WriteLine($"   >>> [QUARTZ.NET AUDIT] Checked 45 cold-chain items. All items within shelf life (Execution #{AuditExecutionsCount})");

        // [KYA KARTA HAI]: Completed task return karta hai.
        return Task.CompletedTask;
    }
}

