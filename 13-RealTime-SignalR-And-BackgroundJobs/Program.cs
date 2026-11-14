using System;
using System.Diagnostics;
using Hangfire;
using Hangfire.MemoryStorage;
using KiranaHub.RealTimeAndJobs.Hubs;
using KiranaHub.RealTimeAndJobs.Jobs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Quartz;

// =========================================================================================
// [TOPIC]: Real-Time SignalR, Hangfire & Quartz.NET Bootstrap & Verification Harness
// [PURPOSE]: Configures WebSockets, in-memory job queues, precision cron schedulers, and end-to-end self-checks.
// =========================================================================================

// [KYA KARTA HAI]: WebApplicationBuilder instance initialize karta hai.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: Console logging configure karta hai with Information level.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ----------------------------------------------------------------------------------------
// 1. SIGNALR CONFIGURATION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: SignalR server subsystem IoC container me register karta hai.
// [KYU ZAROORI HAI]: Full-duplex WebSocket hub pipelines enable karna.
builder.Services.AddSignalR();

// ----------------------------------------------------------------------------------------
// 2. HANGFIRE CONFIGURATION (IN-MEMORY STORAGE)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Background job service dependency register karta hai.
builder.Services.AddTransient<IHangfireOrderJobs, HangfireOrderJobs>();

// [KYA KARTA HAI]: Hangfire services register karta hai using MemoryStorage (no SQL server needed).
builder.Services.AddHangfire(configuration =>
{
    // [KYA KARTA HAI]: Serialization options configure karta hai.
    configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                 // [KYA KARTA HAI]: Standard JSON type serializer use karta hai.
                 .UseSimpleAssemblyNameTypeSerializer()
                 .UseRecommendedSerializerSettings()
                 // [KYA KARTA HAI]: In-memory storage allocate karta hai background job queues ke liye.
                 .UseMemoryStorage();
});

// [KYA KARTA HAI]: Hangfire Server worker pool background service start karta hai jo queued jobs execute karega.
builder.Services.AddHangfireServer(options =>
{
    // [KYA KARTA HAI]: Worker thread count set karta hai.
    options.WorkerCount = 2;
});

// ----------------------------------------------------------------------------------------
// 3. QUARTZ.NET CONFIGURATION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Quartz.NET scheduler services register karta hai.
builder.Services.AddQuartz(q =>
{
    // [KYA KARTA HAI]: PerishableInventoryAuditJob ke liye unique JobKey define karta hai.
    var jobKey = new JobKey("PerishableAuditJob", "InventoryGroup");

    // [KYA KARTA HAI]: Job define karta hai with configured key.
    q.AddJob<PerishableInventoryAuditJob>(opts => opts.WithIdentity(jobKey));

    // [KYA KARTA HAI]: Trigger define karta hai jo har 5 minutes me execute hoga (test harness me manually fire karenge).
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("PerishableAuditTrigger", "InventoryGroup")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));
});

// [KYA KARTA HAI]: Quartz Hosted Service background worker enable karta hai jo app ke sath scheduler start/stop karega.
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = false);

// [KYA KARTA HAI]: Fixed localhost HTTP port configure karta hai for test harness client communication.
builder.WebHost.UseUrls("http://127.0.0.1:5115");

// [KYA KARTA HAI]: WebApplication pipeline object build karta hai.
var app = builder.Build();

// ----------------------------------------------------------------------------------------
// 4. MAP HUBS & DASHBOARDS
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: SignalR OrderTrackingHub ko endpoint route "/hubs/order-tracking" par map karta hai.
app.MapHub<OrderTrackingHub>("/hubs/order-tracking");

// [KYA KARTA HAI]: Hangfire visual monitoring dashboard ko endpoint "/hangfire" par map karta hai.
app.UseHangfireDashboard("/hangfire");

// [KYA KARTA HAI]: Background job creation Minimal API endpoint for demonstration.
app.MapPost("/api/jobs/checkout-invoice", (string orderId, string email, IBackgroundJobClient jobClient) =>
{
    // [KYA KARTA HAI]: Fire-and-forget job enqueue karta hai via Hangfire.
    string jobId = jobClient.Enqueue<IHangfireOrderJobs>(x => x.SendInvoiceEmailAsync(orderId, email));
    return Results.Ok(new { Message = "Invoice job queued", JobId = jobId });
});

// [KYA KARTA HAI]: Web application ko asynchronous task ke roop me start karta hai (non-blocking).
await app.StartAsync();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 13: REAL-TIME SIGNALR & BACKGROUND JOBS DEMONSTRATION      ");
Console.WriteLine("==========================================================================");

// ----------------------------------------------------------------------------------------
// 5. TEST HARNESS: SIGNALR CLIENT CONNECTION & REAL-TIME WEBSOCKET VERIFICATION
// ----------------------------------------------------------------------------------------
Console.WriteLine("\n--> [Step 1] Connecting to SignalR OrderTrackingHub as real WebSocket client");

// [KYA KARTA HAI]: Client-side HubConnection build karta hai pointing to server URL.
var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://127.0.0.1:5115/hubs/order-tracking")
    .WithAutomaticReconnect()
    .Build();

// [KYA KARTA HAI]: Status update event received tracking flags.
string? receivedStatus = null;
double? receivedLat = null;
double? receivedLon = null;

// [KYA KARTA HAI]: Client event listener register karta hai for ReceiveOrderStatusUpdate.
hubConnection.On<string, string>("ReceiveOrderStatusUpdate", (orderId, status) =>
{
    receivedStatus = status;
    Console.WriteLine($"   [CLIENT EVENT RECEIVED] Order {orderId} status changed to: '{status}'");
});

// [KYA KARTA HAI]: Client event listener register karta hai for ReceiveRiderLocation.
hubConnection.On<string, double, double>("ReceiveRiderLocation", (orderId, lat, lon) =>
{
    receivedLat = lat;
    receivedLon = lon;
    Console.WriteLine($"   [CLIENT EVENT RECEIVED] Rider GPS coordinate: ({lat:F4}, {lon:F4}) for Order {orderId}");
});

// [KYA KARTA HAI]: Client connection start karta hai.
await hubConnection.StartAsync();
Console.WriteLine("[PASS] SignalR client connected successfully to Hub.");

// [KYA KARTA HAI]: Order tracking group join karta hai.
string testOrderId = "ORD-REALTIME-777";
await hubConnection.InvokeAsync("JoinOrderGroup", testOrderId);

// [KYA KARTA HAI]: Server-side Hub context resolve karta hai to simulate dispatch.
var hubContext = app.Services.GetRequiredService<IHubContext<OrderTrackingHub, IOrderTrackingClient>>();

// [KYA KARTA HAI]: Server se live status update broadcast karta hai order group me.
await hubContext.Clients.Group($"order-{testOrderId}").ReceiveOrderStatusUpdate(testOrderId, "Out For Delivery");

// [KYA KARTA HAI]: Server se live rider coordinates broadcast karta hai.
await hubContext.Clients.Group($"order-{testOrderId}").ReceiveRiderLocation(testOrderId, 28.6139, 77.2090);

// [KYA KARTA HAI]: Short delay deta hai WebSocket packet transit ke liye.
await Task.Delay(150);

// [KYA KARTA HAI]: Assert karta hai ki client ne real-time me dono events receive kiye!
Debug.Assert(receivedStatus == "Out For Delivery", "SignalR client did not receive status update!");
Debug.Assert(receivedLat.HasValue && receivedLon.HasValue, "SignalR client did not receive GPS coordinates!");
Console.WriteLine("[PASS] Real-time bi-directional SignalR WebSocket communication verified!\n");

// [KYA KARTA HAI]: Hub connection cleanly disconnect karta hai.
await hubConnection.StopAsync();

// ----------------------------------------------------------------------------------------
// 6. TEST HARNESS: HANGFIRE BACKGROUND JOBS VERIFICATION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Executing Hangfire Fire-and-Forget, Delayed, and Recurring Jobs");

// [KYA KARTA HAI]: Hangfire IBackgroundJobClient resolve karta hai.
var hangfireClient = app.Services.GetRequiredService<IBackgroundJobClient>();

// [KYA KARTA HAI]: Fire-and-Forget background job enqueue karta hai (Invoice PDF).
string invoiceJobId = hangfireClient.Enqueue<IHangfireOrderJobs>(x =>
    x.SendInvoiceEmailAsync(testOrderId, "pankaj@kiranahub.com"));
Console.WriteLine($"Enqueued Fire-and-Forget Job ID: {invoiceJobId}");

// [KYA KARTA HAI]: Delayed background job schedule karta hai (Review SMS after delay).
string delayedJobId = hangfireClient.Schedule<IHangfireOrderJobs>(x =>
    x.SendReviewSmsAsync(testOrderId, "+91-9876543210"), TimeSpan.FromSeconds(1));
Console.WriteLine($"Scheduled Delayed Job ID: {delayedJobId}");

// [KYA KARTA HAI]: Recurring daily job register karta hai.
RecurringJob.AddOrUpdate<IHangfireOrderJobs>("nightly-stock-reconciliation",
    x => x.DailyInventoryReconciliationAsync(), Cron.Daily);
Console.WriteLine("Registered Recurring Daily Job 'nightly-stock-reconciliation'");

// [KYA KARTA HAI]: Hangfire server ko jobs pick aur execute karne ka thoda time deta hai.
await Task.Delay(500);

// [KYA KARTA HAI]: Assert karta hai ki Hangfire job worker ne invoice generate kar di.
Debug.Assert(HangfireOrderJobs.InvoicesSentCount >= 1, "Hangfire worker did not process invoice job!");
Console.WriteLine("[PASS] Hangfire background job queue and worker execution verified!\n");

// ----------------------------------------------------------------------------------------
// 7. TEST HARNESS: QUARTZ.NET PRECISION CRON SCHEDULER VERIFICATION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] Quartz.NET Cron-Scheduled Job Triggering");

// [KYA KARTA HAI]: Quartz ISchedulerFactory resolve karta hai.
var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();

// [KYA KARTA HAI]: PerishableInventoryAuditJob ko manually trigger karta hai immediate execution ke liye.
var quartzJobKey = new JobKey("PerishableAuditJob", "InventoryGroup");
await scheduler.TriggerJob(quartzJobKey);

// [KYA KARTA HAI]: Quartz worker execution wait.
await Task.Delay(200);

// [KYA KARTA HAI]: Assert karta hai ki Quartz job ne warehouse inventory audit execute kiya.
Debug.Assert(PerishableInventoryAuditJob.AuditExecutionsCount >= 1, "Quartz job was not executed!");
Console.WriteLine("[PASS] Quartz.NET cron job trigger and execution verified!\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 13-REALTIME-SIGNALR-AND-BACKGROUNDJOBS TESTS PASSED!     ");
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Host stop karta hai for clean process termination.
await app.StopAsync();

