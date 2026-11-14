# Project 23: Redis Streams, Geospatial Dark-Store Routing & Redlock Distributed Mutex

> **Quick-Commerce Domain:** KiranaHub High-Throughput In-Memory Engine  
> **Tech Stack:** C# 13, .NET 10, StackExchange.Redis, Redis Geospatial (`GEOADD`, `GEOSEARCH`), Redis Streams (`XADD`, `XREADGROUP`, `XACK`), Redlock Distributed Mutex Algorithm.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Agar aapko Redis ko sirf ek simple key-value store ya cache samajhne ki aadat hai, toh yeh project aapki soch badal dega. Quick-Commerce (10-minute delivery) ke 3 sabse complex real-time problems Redis ke advanced primitives se solve hote hain:

Aapko code iss sequence me padhna hai:
1. **`Geospatial/DarkStoreGeoRouter.cs`**:
   - Sabse pehle dekhein ki customer ke latitude/longitude aane par 1.5 millisecond ke andar 3.5 km radius ke saare operating KiranaHub dark stores kaise discover hote hain using Haversine indexing.
2. **`DistributedLock/RedlockManager.cs`**:
   - Samajhiye ki jab 100 concurrent requests ek hi last pack of Amul Butter (Flash sale) ko ek hi millisecond me order karti hain, toh in-process `lock()` ya SQL row lock kyun fail hota hai, aur Redis distributed lock atomic Lua script ke saath crash-safe locking kaise karta hai.
3. **`Streams/RiderTelemetryStreamEngine.cs`**:
   - Dekhiye ki 10,000 delivery riders ke live GPS coordinates ko bina RabbitMQ overhead ke Redis Stream (`XADD`) me append-only high-throughput log me kaise push kiya jata hai, aur Consumer Groups (`XREADGROUP`) usse horizontally scale karke consume aur acknowledge (`XACK`) kaise karte hain.
4. **`Program.cs`**:
   - Dekhiye kaise yeh teeno engines automated in-memory benchmark harness ke andar end-to-end execute hote hain without needing an external cloud subscription.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: Haversine & Geohash Coordinate System
- Quick-commerce me customer jab app kholta hai, home screen render hone se pehle system ko yeh decide karna hota hai ki customer kis **Dark Store** ke delivery polygon me fall karta hai.
- Agar database par `SELECT * FROM DarkStores WHERE ST_Distance(...)` run karenge 50,000 RPS par, toh PostgreSQL PostGIS CPU 100% explode ho jayega.
- Isliye sabse pehle **`DarkStoreGeoRouter`** banaya gaya jo Geohash integer bit-interleaving technique par kaam karta hai:
  - Latitude aur Longitude ko 52-bit integer me encode karke 2D spatial coordinate ko 1D sorted set (`ZSET`) me map kiya jata hai.
  - Search time complexity $O(N + \log M)$ ho jati hai jahan $N$ is number of items in radius aur $M$ is total dark stores.

```
Customer Location (28.6280° N, 77.3649° E)
                 │
                 ▼ [GEOSEARCH / Radius = 3.5 km]
┌────────────────────────────────────────────────────────┐
│ Sorted Set (ZSET) with 52-bit Geohash integer buckets   │
└────────────────────────────────────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
DarkStore-01 (0.42 km)  DarkStore-02 (2.1 km)  (Sorted by Distance)
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `DarkStoreGeoRouter.cs` (Geospatial Clustering Engine)
- **Kyu Aaya?** Customer ko order dispatch karne ke liye sabse nazdeek wala dark store chahiye jahan se rider 7 se 10 minute me bike chala sake.
- **Kaise Kaam Karta Hai?**
  - Dark stores ke coordinates register hote hain (`GEOADD`).
  - Search query aane par radial boundary calculate hoti hai aur Haversine formula se distance calculate karke nearest store first return hota hai.
  - Har store ke sath uski computed driving estimate (distance / average speed 20 km/h) compute hoti hai.

### Step 2: `RedlockManager.cs` (Distributed Mutex with Atomic Release)
- **Kyu Aaya?** Jab multiple independent Web API microservices horizontally scale hoti hain Kubernetes me, in-process C# `lock(obj)` ya `SemaphoreSlim` sirf ek pod ke andar kaam karta hai. Do alag pods ek hi time par same product stock ko decrement kar sakte hain, causing negative inventory (overselling).
- **Kaise Kaam Karta Hai?**
  - `SET key lockToken NX PX expiryMilliseconds` — Atomic primitive use hota hai jo tabhi set karega agar key pehle se exist nahi karti (`NX`), aur sath me automatic TTL expiry set karega (`PX`) taaki pod crash hone par dead-lock na bane.
  - Lock release karte waqt race condition hoti hai: *agar lock expire ho gaya aur kisi doosre process ne naya lock le liya, toh purana process doosre ka lock delete na kar de!*
  - Iske liye **Lua script** run ki jaati hai:
    ```lua
    if redis.call('get', KEYS[1]) == ARGV[1] then
        return redis.call('del', KEYS[1])
    else
        return 0
    end
    ```
    Yeh check and delete Redis server par single-threaded atomic instruction me execute hota hai.

### Step 3: `RiderTelemetryStreamEngine.cs` (High-Throughput Append-Only Stream)
- **Kyu Aaya?** Delivery rider har 2 second me apna GPS ping bhejta hai. Agar 10,000 riders hain, toh har second 5,000 pings aate hain. Relational DB ya heavyweight message queues me itne micro-messages push karne se disk I/O chok ho jata hai.
- **Kaise Kaam Karta Hai?**
  - `XADD` command se har telemetry ping (RiderId, Lat, Lon, Speed, Battery, Timestamp) stream me O(1) time me append ho jati hai.
  - Consumer Group (`XREADGROUP`) use hota hai taaki multiple background worker pods workload ko share kar sakein bina message duplicate process kiye.
  - Jab worker dispatch tracking coordinate compute kar leta hai, toh `XACK` bhejta hai, jisse Pending Entries List (PEL) se wo message clear ho jata hai.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Redis Geospatial (`GEOSEARCH`)** | Customer ko orders book karte waqt 15km dur ke warehouse se allocate ho jayenge jahan se 10 minute delivery impossible hogi. SQL queries se table lock honge aur DB CPU spike karke checkout crash ho jayega. |
| **Without Redlock Distributed Mutex** | Flash sale me jab ₹10 wala iPhone/Amul butter live hoga, 10,000 simultaneous users buy click karenge. Database concurrency anomaly ki wajah se 5 available packs ke bajaye 450 orders confirm ho jayenge, company ko heavy financial loss aur legal notices aayenge. |
| **Without Atomic Lua Release in Redlock** | Slow pod apna expired lock delete karte waqt kisi naye pod ka newly acquired lock uda dega, jisse 2 pods critical section me simultaneously ghus jayenge (Split-Brain bug). |
| **Without Redis Streams (`XADD` + `XREADGROUP`)** | Rider telemetry tracking direct DB me write hogi, jisse DB WAL logs full ho jayenge. Network fluctuation me rider pings drop ho jayenge aur customer app par rider ki live position frozen dikhegi. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\23-Redis-Streams-And-Geospatial-Routing\KiranaHub.RedisAdvanced.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 23: Redis Streams, Geospatial & Redlock Engine  
====================================================================

[TEST 1] Testing Geospatial Dark-Store Radial Discovery (GEOSEARCH)...
  -> Nearest Dark-Store found: 'Noida Sector 62 Dark-Store'
     Distance: 0.42 km (Well within 3.5 km 10-minute SLA): PASSED

[TEST 2] Testing Redlock Distributed Mutex (Flash-Sale Overselling Protection)...
  -> Redlock prevented 9 concurrent race condition collisions!
  -> Atomic lease verification & unauthorized release protection: PASSED

[TEST 3] Testing Redis Streams & Consumer Groups (Rider GPS Telemetry)...
  -> Ingested 5 telemetry packets into Redis Stream!
  -> Consumer Group processed and acknowledged all messages. Remaining Lag: 0: PASSED

====================================================================
 [SUCCESS] All Redis Streams, Geospatial & Redlock checks PASSED!   
====================================================================
```
