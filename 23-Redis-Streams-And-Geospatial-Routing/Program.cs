// [KYA KARTA HAI]: System namespace import karta hai.
// [KYU ZAROORI HAI]: Console logging and assertion checks.
// [KAISE CHALTA HAI]: .NET core BCL.
// [OUTPUT / SIDE EFFECT]: Basic types.
// [VARIATIONS TO TRY]: Unit test runner.
using System;

// [KYA KARTA HAI]: Project namespaces import karta hai.
// [KYU ZAROORI HAI]: Geospatial, Redlock, aur Streams modules use karne ke liye.
// [KAISE CHALTA HAI]: Using directives.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Global usings.
using KiranaHub.RedisAdvanced.Geospatial;
using KiranaHub.RedisAdvanced.DistributedLock;
using KiranaHub.RedisAdvanced.Streams;

Console.WriteLine("====================================================================");
Console.WriteLine("  KiranaHub Project 23: Redis Streams, Geospatial & Redlock Engine  ");
Console.WriteLine("====================================================================");

// -------------------------------------------------------------------------
// TEST 1: Geospatial Dark-Store Radial Routing (GEOADD, GEODIST, GEOSEARCH)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 1] Testing Geospatial Dark-Store Radial Discovery (GEOSEARCH)...");
var geoRouter = new DarkStoreGeoRouter();

// Register 3 Dark-Stores around Delhi-NCR
geoRouter.AddDarkStore("DS-NOIDA-62", "Noida Sector 62 Dark-Store", 28.6280, 77.3650, serviceRadiusKm: 3.5);
geoRouter.AddDarkStore("DS-INDIRAPURAM", "Indirapuram Habitat Center Hub", 28.6410, 77.3710, serviceRadiusKm: 4.0);
geoRouter.AddDarkStore("DS-GREATER-NOIDA", "Greater Noida West Gaur Hub", 28.6010, 77.4280, serviceRadiusKm: 5.0);

// Customer is at Sector 62 Residential Complex (28.6295, 77.3690)
var customerLoc = new GeoLocation(28.6295, 77.3690);
var discoveryResult = geoRouter.FindNearestServiceableDarkStore(customerLoc);

if (discoveryResult == null)
    throw new Exception("Expected customer to be serviceable by at least one dark-store");

var nearestStore = discoveryResult.Value.Store;
var distanceKm = discoveryResult.Value.DistanceKm;

if (nearestStore.Id != "DS-NOIDA-62")
    throw new Exception($"Expected nearest store DS-NOIDA-62, but got {nearestStore.Id}");

if (distanceKm > 1.5)
    throw new Exception($"Expected distance < 1.5 km, but calculated {distanceKm} km");

Console.WriteLine($"  -> Nearest Dark-Store found: '{nearestStore.Name}'");
Console.WriteLine($"     Distance: {distanceKm} km (Well within {nearestStore.ServiceRadiusKm} km 10-minute SLA): PASSED");

// -------------------------------------------------------------------------
// TEST 2: Redlock Distributed Mutex (Flash-Sale Race Condition Prevention)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 2] Testing Redlock Distributed Mutex (Flash-Sale Overselling Protection)...");
var redlock = new RedlockManager();
var flashSaleKey = "lock:flash-sale:onion-1kg-re1";

// Thread 1 acquires lock
var acquired1 = redlock.TryAcquireLock(flashSaleKey, "customer-user-101", TimeSpan.FromSeconds(5));
if (!acquired1)
    throw new Exception("User 101 should have successfully acquired the lock");

// Thread 2 through 10 try to acquire lock concurrently on same key
int rejectedCount = 0;
for (int i = 2; i <= 10; i++)
{
    var acquiredOther = redlock.TryAcquireLock(flashSaleKey, $"customer-user-{i}", TimeSpan.FromSeconds(5));
    if (!acquiredOther)
    {
        rejectedCount++;
    }
}

if (rejectedCount != 9)
    throw new Exception($"Expected 9 rejected lock contentions, but got {rejectedCount}");

// Unauthorized release attempt (User 99 cannot release User 101's lock)
var unauthorizedRelease = redlock.ReleaseLock(flashSaleKey, "malicious-user-99");
if (unauthorizedRelease)
    throw new Exception("Unauthorized user should not be able to release the lock");

// Legitimate owner releases lock
var legitimateRelease = redlock.ReleaseLock(flashSaleKey, "customer-user-101");
if (!legitimateRelease)
    throw new Exception("Legitimate owner should be able to release the lock");

// Now another user can acquire lock
var acquiredNext = redlock.TryAcquireLock(flashSaleKey, "customer-user-202", TimeSpan.FromSeconds(5));
if (!acquiredNext)
    throw new Exception("User 202 should be able to acquire lock after release");

Console.WriteLine("  -> Redlock prevented 9 concurrent race condition collisions!");
Console.WriteLine("  -> Atomic lease verification & unauthorized release protection: PASSED");

// -------------------------------------------------------------------------
// TEST 3: Redis Streams & Consumer Groups (High-Throughput GPS Ingestion)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 3] Testing Redis Streams & Consumer Groups (Rider GPS Telemetry)...");
var streamEngine = new RiderTelemetryStreamEngine();
var streamName = "stream:rider:gps:fleet-noida";

// Ingest 5 GPS packets from delivery rider
for (int i = 1; i <= 5; i++)
{
    streamEngine.XAdd(streamName, new Dictionary<string, string>
    {
        { "riderId", "rider-suresh-10" },
        { "lat", (28.6280 + i * 0.001).ToString("F4") },
        { "lng", (77.3650 + i * 0.001).ToString("F4") },
        { "speed", "28" }
    });
}

// Consumer group reads pending batch
var batch = streamEngine.XReadGroup(streamName, batchSize: 10);
if (batch.Count != 5)
    throw new Exception($"Expected 5 stream messages, but got {batch.Count}");

// Acknowledge all 5 messages
foreach (var msg in batch)
{
    streamEngine.XAck(streamName, msg.MessageId);
}

// Verify consumer lag drops to 0
var pendingLag = streamEngine.GetPendingCount(streamName);
if (pendingLag != 0)
    throw new Exception($"Expected 0 pending messages after XACK, but got {pendingLag}");

Console.WriteLine($"  -> Ingested {batch.Count} telemetry packets into Redis Stream!");
Console.WriteLine($"  -> Consumer Group processed and acknowledged all messages. Remaining Lag: {pendingLag}: PASSED");

Console.WriteLine("\n====================================================================");
Console.WriteLine(" [SUCCESS] All Redis Streams, Geospatial & Redlock checks PASSED!   ");
Console.WriteLine("====================================================================");
