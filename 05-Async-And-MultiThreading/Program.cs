using System;
using System.Diagnostics;
using System.Threading.Channels;
using KiranaHub.AsyncAndThreads.Concurrency;
using KiranaHub.AsyncAndThreads.Pipelines;
using KiranaHub.AsyncAndThreads.Services;

// [CONSOLE ENCODING CONFIGURATION]:
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [BANNER]:
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 05: ASYNC PROGRAMMING & MULTI-THREADING IN C#              ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. VALUETASK FAST PATH VS ASYNC TASK
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] ValueTask Fast Path vs Async Network Path");

// [CLIENT INSTANTIATION]:
var paymentClient = new PaymentGatewayClient();

// [BENCHMARK TIMER INITIALIZATION]:
var sw = Stopwatch.StartNew();

// [FAST PATH EXECUTION]: Key "TXN-FAST-01" exists in instant cache
ValueTask<bool> fastPath = paymentClient.VerifyPaymentAsync("TXN-FAST-01");

// [SYNCHRONOUS COMPLETION CHECK]: Returns TRUE immediately without hitting Task thread pool
bool isFastCompletedSync = fastPath.IsCompletedSuccessfully;

// [AWAIT STATEMENT]: Extracts result from stack struct
bool fastResult = await fastPath;
sw.Stop();

Console.WriteLine($"Fast-Path Completed Synchronously: {isFastCompletedSync} in {sw.ElapsedMilliseconds}ms (Verified: {fastResult})");
Debug.Assert(isFastCompletedSync && fastResult);

// [SLOW PATH EXECUTION]: Key "TXN-NEW-999" requires 200ms async network call
sw.Restart();
bool slowResult = await paymentClient.VerifyPaymentAsync("TXN-NEW-999");
sw.Stop();

Console.WriteLine($"Slow-Path Completed Asynchronously in {sw.ElapsedMilliseconds}ms (Verified: {slowResult})");
Debug.Assert(slowResult && sw.ElapsedMilliseconds >= 150);
Console.WriteLine("[PASS] ValueTask fast vs slow paths verified.\n");

// ----------------------------------------------------------------------------------------
// 2. CANCELLATION TOKEN & TIMEOUT HANDLING
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Cooperative Cancellation with CancellationTokenSource");

// [CTS INSTANTIATION IN USING STATEMENT]:
using (var cts = new CancellationTokenSource())
{
    // [TIMEOUT ENFORCEMENT]: Automatically signals cancellation token after 50ms
    cts.CancelAfter(TimeSpan.FromMilliseconds(50));
    try
    {
        // [TARGET CALL]: Takes 200ms, so it MUST be aborted by 50ms token!
        await paymentClient.VerifyPaymentAsync("TXN-CANCEL-TEST", cts.Token);
        Debug.Fail("Should have thrown OperationCanceledException!");
    }
    catch (OperationCanceledException)
    {
        // [CLEAN EXCEPTION CATCH]: Proves cancellation mechanism works cleanly
        Console.WriteLine("[PASS] Operation was cleanly canceled before completion as expected.\n");
    }
}

// ----------------------------------------------------------------------------------------
// 3. THREAD SYNCHRONIZATION WITH LOCK (RACE CONDITION PREVENTION)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] Multi-Threaded Critical Section (lock / Monitor)");

// [MANAGER INSTANTIATION]: Starts with 100 stock
var lockManager = new InventoryLockManager();
int initialStock = lockManager.GetStock();

// [CONCURRENT TASKS LIST]:
var deductTasks = new List<Task>();

// [SPAWN 10 PARALLEL THREADS]:
for (int i = 0; i < 10; i++)
{
    deductTasks.Add(Task.Run(() =>
    {
        // Each thread attempts 10 individual deductions of 1 unit
        for (int j = 0; j < 10; j++)
        {
            lockManager.TryDeductStockSynchronous(1);
        }
    }));
}

// [WAIT FOR ALL THREADS TO COMPLETE]:
await Task.WhenAll(deductTasks);

// [VERIFY EXACT STOCK COUNT]:
int remainingStock = lockManager.GetStock();
Console.WriteLine($"Initial Stock: {initialStock} | Final Stock after 100 deductions: {remainingStock}");
Debug.Assert(remainingStock == 0, "Race condition detected! Stock did not reach 0 safely.");
Console.WriteLine("[PASS] Thread synchronization via lock verified.\n");

// ----------------------------------------------------------------------------------------
// 4. ASYNCHRONOUS THROTTLING VIA SEMAPHORESLIM
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 4] Asynchronous Throttling via SemaphoreSlim");

// [METRIC TRACKERS]:
int concurrentCalls = 0;
int maxConcurrentSeen = 0;

// [SEMAPHORE INSTANTIATION]: Max 3 simultaneous permits
var throttler = new SemaphoreSlim(3, 3);

// [10 CONCURRENT TASKS ATTEMPTING ACCESS SIMULTANEOUSLY]:
var throttleTasks = Enumerable.Range(1, 10).Select(async id =>
{
    // Asynchronously wait for permit
    await throttler.WaitAsync();
    try
    {
        // Atomic increment of live concurrent counter
        int current = Interlocked.Increment(ref concurrentCalls);
        lock (lockManager)
        {
            if (current > maxConcurrentSeen) maxConcurrentSeen = current;
        }
        await Task.Delay(30); // Simulate work
    }
    finally
    {
        // Atomic decrement and permit release
        Interlocked.Decrement(ref concurrentCalls);
        throttler.Release();
    }
});

await Task.WhenAll(throttleTasks);
Console.WriteLine($"Max concurrent executions observed: {maxConcurrentSeen} (Max permitted: 3)");
Debug.Assert(maxConcurrentSeen <= 3, "Throttler allowed more than 3 concurrent tasks!");
Console.WriteLine("[PASS] SemaphoreSlim async throttling verified.\n");

// ----------------------------------------------------------------------------------------
// 5. HIGH-THROUGHPUT PRODUCER-CONSUMER VIA CHANNELS
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 5] Producer-Consumer Pipeline with System.Threading.Channels");

// [CHANNEL PIPELINE CREATION]: Capacity of 20 items
var pipeline = new OrderProcessingChannel(capacity: 20);
int ordersProcessedByConsumer = 0;

// [BACKGROUND CONSUMER TASK]:
var consumerTask = Task.Run(async () =>
{
    // Reads stream of incoming orders until Writer.Complete() is called
    await foreach (var order in pipeline.Reader.ReadAllAsync())
    {
        Interlocked.Increment(ref ordersProcessedByConsumer);
    }
});

// [PRODUCER LOOP]: Publishes 50 orders into the channel
for (int i = 1; i <= 50; i++)
{
    await pipeline.PublishOrderAsync(new IncomingOrder($"ORD-FL-{i}", $"Customer {i}", 199m));
}
pipeline.CompletePublishing(); // Signals EOF

// [WAIT FOR CONSUMER TO DRAIN BUFFER]:
await consumerTask;
Console.WriteLine($"Total orders consumed and fulfilled by background worker: {ordersProcessedByConsumer}");
Debug.Assert(ordersProcessedByConsumer == 50, "Channel consumer did not process all published orders!");
Console.WriteLine("[PASS] System.Threading.Channels pipeline verified.\n");

// ----------------------------------------------------------------------------------------
// 6. PARALLEL.FOREACHASYNC
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 6] Parallel.ForEachAsync Batch Processing");
var testBatch = Enumerable.Range(1, 20).ToList();
int processedBatchCount = 0;

// Executes async iterations across multiple CPU cores with concurrency bound of 4
await Parallel.ForEachAsync(testBatch, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (item, token) =>
{
    await Task.Delay(10, token);
    Interlocked.Increment(ref processedBatchCount);
});

Console.WriteLine($"Parallel.ForEachAsync processed {processedBatchCount} items across 4 cores.");
Debug.Assert(processedBatchCount == 20);
Console.WriteLine("[PASS] Parallel.ForEachAsync verified.\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 05-ASYNC-AND-MULTITHREADING TESTS AND EXAMPLES PASSED!   ");
Console.WriteLine("==========================================================================");
