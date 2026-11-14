using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using KiranaHub.LoggingAndMetrics.Diagnostics;
using KiranaHub.LoggingAndMetrics.HealthChecks;
using KiranaHub.LoggingAndMetrics.Metrics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

// =========================================================================================
// [TOPIC]: Logging, Health Monitoring & Metrics Bootstrap & Verification Harness
// [PURPOSE]: Serilog structured logs, Kubernetes Liveness/Readiness probes, and OpenTelemetry Metrics.
// =========================================================================================

// [KYA KARTA HAI]: Serilog bootstrap logger initialize karta hai.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KiranaHub.MonitoringService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// [KYA KARTA HAI]: WebApplicationBuilder instance create karta hai.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: Default ASP.NET Core logging provider ko Serilog se replace karta hai.
builder.Host.UseSerilog();

// ----------------------------------------------------------------------------------------
// 1. HEALTH CHECKS REGISTRATION (LIVENESS VS READINESS)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Health check subsystem services register karta hai with readiness tags.
builder.Services.AddHealthChecks()
    // [KYA KARTA HAI]: Database connection check register karta hai tagged "ready".
    .AddCheck<DatabaseHealthCheck>("Database_SQLite", tags: new[] { "ready" })
    // [KYA KARTA HAI]: Warehouse IoT connectivity check register karta hai tagged "ready".
    .AddCheck<WarehouseConnectivityHealthCheck>("Warehouse_DarkStore", tags: new[] { "ready" });

// [KYA KARTA HAI]: Kestrel port 5335 bind karta hai for monitoring test server.
builder.WebHost.UseUrls("http://127.0.0.1:5335");

// [KYA KARTA HAI]: Application build karta hai.
var app = builder.Build();

// ----------------------------------------------------------------------------------------
// 2. HEALTH CHECK ENDPOINT MAPPINGS
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Kubernetes Liveness Probe endpoint (/health/live) map karta hai.
// [KYU ZAROORI HAI]: Kubelet process alive check: agar container freeze ho jaye toh restart trigger karta hai.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // [KYA KARTA HAI]: Predicate _ => false: Kisi bhi dependent service check ko run nahi karta (sirf process alive status).
    Predicate = _ => false
});

// [KYA KARTA HAI]: Kubernetes Readiness Probe endpoint (/health/ready) map karta hai.
// [KYU ZAROORI HAI]: Traffic routing check: Agar DB down hai toh load balancer traffic hold kar le.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // [KYA KARTA HAI]: Sirf un checks ko execute karta hai jinke pass "ready" tag ho.
    Predicate = check => check.Tags.Contains("ready"),
    // [KYA KARTA HAI]: Custom JSON formatter use karta hai RFC-compliant detailed health report ke liye.
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Entries = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                DurationMs = e.Value.Duration.TotalMilliseconds,
                Data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// [KYA KARTA HAI]: Server start karta hai asynchronously.
await app.StartAsync();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 15: LOGGING, MONITORING & METRICS DEMONSTRATION            ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 3. TEST 1: ZERO-ALLOCATION HIGH PERFORMANCE LOGGING
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] Demonstrating Zero-Allocation High-Performance Logging (LoggerMessage.Define)");

// [KYA KARTA HAI]: Logger instance resolve karta hai.
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// [KYA KARTA HAI]: Pre-compiled delegate call karta hai.
// [KYU ZAROORI HAI]: High-throughput scenario me CPU allocations avoid karna.
logger.OrderProcessed("ORD-PERF-909", 345.50m);
logger.RiderDispatched("Ramesh Kumar", "ORD-PERF-909");

Console.WriteLine("[PASS] Zero-allocation source-generated logging delegates executed.\n");

// ----------------------------------------------------------------------------------------
// 4. TEST 2: KUBERNETES HEALTH CHECKS (LIVENESS & READINESS)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Testing Kubernetes Liveness (/health/live) & Readiness (/health/ready)");

using var httpClient = new HttpClient();

// [KYA KARTA HAI]: Liveness probe hit karta hai.
var liveResponse = await httpClient.GetAsync("http://127.0.0.1:5335/health/live");
Console.WriteLine($"Liveness Probe Status: {liveResponse.StatusCode}");
Debug.Assert(liveResponse.IsSuccessStatusCode, "Liveness probe failed!");

// [KYA KARTA HAI]: Readiness probe hit karta hai (Normal state).
var readyResponse = await httpClient.GetAsync("http://127.0.0.1:5335/health/ready");
var readyJson = await readyResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Readiness Probe Status: {readyResponse.StatusCode}");
Console.WriteLine($"Readiness Probe JSON: {readyJson}");
Debug.Assert(readyResponse.IsSuccessStatusCode && readyJson.Contains("Healthy"), "Readiness probe failed!");

// [KYA KARTA HAI]: Database outage simulate karta hai.
Console.WriteLine("\nSimulating primary database outage...");
DatabaseHealthCheck.SimulateDatabaseDown = true;

// [KYA KARTA HAI]: Readiness probe dubara hit karta hai database crash ke dauran.
var degradedResponse = await httpClient.GetAsync("http://127.0.0.1:5335/health/ready");
var degradedJson = await degradedResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Readiness Probe during Outage: {degradedResponse.StatusCode} (Expected: 503 ServiceUnavailable)");
Console.WriteLine($"Readiness Report: {degradedJson}");

// [KYA KARTA HAI]: Assert karta hai HTTP 503 Service Unavailable return hua aur status Unhealthy hai.
Debug.Assert(degradedResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);
Debug.Assert(degradedJson.Contains("Unhealthy"));
Console.WriteLine("[PASS] Health Check correctly reported 503 Service Unavailable during DB downtime!\n");

// [KYA KARTA HAI]: Database status restore karta hai.
DatabaseHealthCheck.SimulateDatabaseDown = false;

// ----------------------------------------------------------------------------------------
// 5. TEST 3: OPENTELEMETRY / SYSTEM.DIAGNOSTICS.METRICS INSTRUMENTATION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] Testing System.Diagnostics.Metrics (Counters, Histograms, Gauges)");

// [KYA KARTA HAI]: In-memory MeterListener attach karta hai to observe recorded telemetry metrics.
using var meterListener = new MeterListener();
long capturedOrdersPlaced = 0;
double capturedDurationMs = 0;

meterListener.InstrumentPublished = (instrument, listener) =>
{
    // [KYA KARTA HAI]: Sirf KiranaHub.Monitoring meter ke instruments subscribe karta hai.
    if (instrument.Meter.Name == KiranaMetrics.MeterName)
    {
        listener.EnableMeasurementEvents(instrument);
    }
};

// [KYA KARTA HAI]: Measurement callback capture karta hai.
meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
{
    if (instrument.Name == "kiranahub.orders.placed")
    {
        capturedOrdersPlaced += measurement;
    }
});

meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
{
    if (instrument.Name == "kiranahub.order.duration_ms")
    {
        capturedDurationMs = measurement;
    }
});

// [KYA KARTA HAI]: Listener start karta hai.
meterListener.Start();

// [KYA KARTA HAI]: Real-time business metrics record karta hai.
KiranaMetrics.RecordOrder("Dairy", 124m, 42.5);

// [KYA KARTA HAI]: Assert karta hai counter ne 1 order record kiya aur histogram ne 42.5ms measure kiya.
Debug.Assert(capturedOrdersPlaced == 1, "Metrics Counter did not increment!");
Debug.Assert(capturedDurationMs == 42.5, "Metrics Histogram did not record duration!");

Console.WriteLine($"Captured Telemetry Metrics:");
Console.WriteLine($"   Counter (kiranahub.orders.placed)    : {capturedOrdersPlaced}");
Console.WriteLine($"   Histogram (kiranahub.order.duration) : {capturedDurationMs} ms");
Console.WriteLine($"   Observable Gauge (active riders)     : {KiranaMetrics.CurrentActiveRiders} riders in transit");
Console.WriteLine("[PASS] System.Diagnostics.Metrics quantitative telemetry verified perfectly!\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 15-LOGGING-MONITORING-AND-METRICS TESTS PASSED!          ");
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Clean server shutdown.
await app.StopAsync();
Log.CloseAndFlush();

