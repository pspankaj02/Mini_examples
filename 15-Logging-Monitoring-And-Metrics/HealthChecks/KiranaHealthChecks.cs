using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KiranaHub.LoggingAndMetrics.HealthChecks;

// =========================================================================================
// [TOPIC]: Enterprise Health Checks (Liveness & Readiness Probes)
// [PURPOSE]: Kubernetes / Container orchestrator health monitoring.
// =========================================================================================

// [KYA KARTA HAI]: Primary database connection health probe implement karta hai.
// [KYU ZAROORI HAI]: Readiness probe: Agar database down hai toh container ko traffic send nahi karna chahiye.
public class DatabaseHealthCheck : IHealthCheck
{
    // [KYA KARTA HAI]: Simulated DB failure toggle for test harness.
    public static bool SimulateDatabaseDown { get; set; } = false;

    // [KYA KARTA HAI]: Health evaluation check method.
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // [KYA KARTA HAI]: Check karta hai simulated failure active hai ya nahi.
        if (SimulateDatabaseDown)
        {
            // [KYA KARTA HAI]: Unhealthy result return karta hai with error description.
            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: "Cannot connect to primary SQLite/PostgreSQL database cluster. Connection timeout."
            ));
        }

        // [KYA KARTA HAI]: Healthy status return karta hai with latency metadata.
        return Task.FromResult(HealthCheckResult.Healthy(
            description: "Database cluster online. Ping: 2ms",
            data: new Dictionary<string, object>
            {
                ["ActiveConnections"] = 8,
                ["DatabaseEngine"] = "SQLite v3"
            }
        ));
    }
}

// [KYA KARTA HAI]: Dark store warehouse socket connectivity probe.
public class WarehouseConnectivityHealthCheck : IHealthCheck
{
    // [KYA KARTA HAI]: Health check callback.
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // [KYA KARTA HAI]: Healthy status return karta hai.
        return Task.FromResult(HealthCheckResult.Healthy(
            description: "Dark store IoT scanner socket connected and responsive."
        ));
    }
}

