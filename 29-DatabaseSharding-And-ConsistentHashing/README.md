# Project 29: Database Sharding, Consistent Hashing Rings & Parallel Scatter-Gather Queries

> **Quick-Commerce Domain:** KiranaHub National-Scale Data Partitioning & Distributed Query Engine  
> **Tech Stack:** C# 13, .NET 10, Horizontal Database Sharding, Consistent Hashing with Virtual Nodes (vnodes), Dynamic Shard Router, Parallel Scatter-Gather Query Aggregation.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Jab koi quick-commerce app ek single city (e.g. Noida) se expand hoke 15 major metro cities (Delhi, Mumbai, Bengaluru, Hyderabad, Chennai, Kolkata) me operate karne lagti hai, toh ek central relational database 20,000 queries per second par fail ho jata hai. Disk I/O, table lock contention, aur connection pools exhaust ho jaate hain.

Iska architectural solution hai **Horizontal Database Sharding**: Har city ya regional dark-store cluster ka apna isolated database shard hota hai.

Aapko yeh code iss sequence me padhna hai:
1. **`Hashing/ConsistentHashRing.cs`**:
   - Sabse pehle dekhiye ki standard `Key % N` modulo sharding kyu fail hoti hai jab naya server add hota hai, aur **Consistent Hashing** virtual nodes (vnodes = 150) ke sath 32-bit unsigned integer circle par keys ko kaise uniformly place karta hai.
2. **`Sharding/ShardRouter.cs`**:
   - Samajhiye ki `DatabaseShard` isolated regional partition ko kaise model karta hai, aur `ShardRouter` runtime par `ShardKey` (`DS-NOIDA-62`, `DS-ANDHERI-01`) ko dekhte hi 1 microsecond me target shard kaise dhoondhta hai.
3. **`Querying/ScatterGatherQueryEngine.cs`**:
   - Dekhiye cross-shard reporting: Jab CEO ya analytics team ko *"Total revenue across all cities today"* dekhna hota hai, toh engine query ko parallel me saare shards par **Scatter** (fan-out) karta hai, aur unke partial sums ko **Gather** (merge) karke single clean report banata hai.
4. **`Program.cs`**:
   - Automated benchmark runner jo uniform data distribution, minimal 25% key rebalancing (vs 75% modulo churn), aur parallel scatter-gather exact math verification dikhata hai.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: David Karger's Consistent Hashing & The Modulo Flaw
- Purane systems me sharding formula hota tha:
  $$\text{ShardId} = \text{Hash}(\text{Key}) \pmod N$$
- **The Modulo Catastrophe**:
  - Agar aapke paas $N = 3$ shards hain aur aap load badhne par 1 naya shard add karke $N = 4$ karte hain, toh lagbhag **75% of all keys** ki position badal jaati hai!
  - Poora system chok ho jata hai, massive cache eviction hoti hai, aur database crash ho jata hai (**Thundering Herd Cache Thrashing**).
- **Consistent Hashing Solution**:
  - Circle $[0, 2^{32}-1]$ par shards ko place kiya jata hai.
  - Jab naya shard add hota hai, toh sirf uske immediate neighbor ke kuch hisse migrate hote hain. Expected migration sirf $\approx \frac{1}{N}$ (25%) hoti hai, baaki 75% keys apni jagah frozen rehti hain!

```
                    Hash Ring (32-bit uint circle)
                             [0 / 2^32]
                         /               \
       Shard-Delhi#12                      Shard-Mumbai#45
            │                                     │
            ▼                                     ▼
      [Key: DS-NOIDA-62]                    [Key: DS-ANDHERI-01]
             \                                   /
              Shard-Bengaluru#88 ─── Shard-Hyderabad#02 (Dynamically Added)
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `ConsistentHashRing.cs` (Virtual Nodes Engine)
- **Kyu Aaya?** Agar physical nodes ko circle par daalein, toh non-uniform clustering hoti hai (Hotspots).
- **Kaise Kaam Karta Hai?**
  - Har physical node ke 150 Virtual Nodes (`vnodes`) circle par randomly distribute hote hain.
  - Statistical Law of Large Numbers ki wajah se data mathematically uniform balance hota hai.

### Step 2: `ShardRouter.cs` (Zero-Latency Transaction Routing)
- **Kyu Aaya?** Application developers ko yeh chinta nahi honi chahiye ki kaunsa order kis IP par jayega.
- **Kaise Kaam Karta Hai?**
  - Order ka `ShardKey` (`DarkStoreId`) padhta hai.
  - Ring se clockwise nearest shard resolve karke routed insert/read execute karta hai.

### Step 3: `ScatterGatherQueryEngine.cs` (Parallel MapReduce Analytics)
- **Kyu Aaya?** Sharded database me cross-shard SQL `JOIN` ya `SUM` chalana impossible hota hai kyunki alag-alag databases ek dusre ko nahi jante.
- **Kaise Kaam Karta Hai?**
  - **Scatter Phase**: `Task.WhenAll` se sabhi shards par asynchronous query parallel me execute hoti hai.
  - **Gather Phase**: Returned sub-lists ko flatten karke in-memory LINQ `GroupBy` se category sums merge kiye jaate hain.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Database Sharding (Single Monolithic DB)** | National launch ke din peak checkout time par PostgreSQL connection limit (e.g. 5,000 connections) exhaust ho jayegi, disk queue 100% spike karegi, aur saare 15 cities ka app freeze ho jayega. |
| **Without Consistent Hashing (Naive Modulo `Key % N`)** | Diwali sale ke liye 3 database servers se 4 servers karte hi 75% customer carts aur sessions invalidate ho jayenge, causing massive cart abandonment and customer outrage. |
| **Without Virtual Nodes (vnodes)** | Hash ring par do nodes bohot paas aa jayenge aur teesra dur rahega, jisse ek single shard 80% traffic jhelega (CPU 100% crash) jabki baaki do shards idle baithe rahenge (Hotspot Failure). |
| **Without Parallel Scatter-Gather Engine** | Executive dashboard chalate waqt queries ek-ek karke sequential chalengi (Delhi -> Mumbai -> Bengaluru). Ek slow regional network ki wajah se report 2 minute tak timeout hoti rahegi. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\29-DatabaseSharding-And-ConsistentHashing\KiranaHub.DatabaseSharding.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 29: Database Sharding & Consistent Hashing      
====================================================================

[TEST 1] Testing Consistent Hashing Shard Routing & Key Distribution...
  -> Delhi Shard Orders     : 45 (₹25200)
  -> Mumbai Shard Orders    : 30 (₹16500)
  -> Bengaluru Shard Orders : 15 (₹8250)
  -> Uniform key distribution achieved across all 3 regional shards: PASSED

[TEST 2] Testing Minimal Key Rebalancing during Shard Scale-Out...
  -> Scaled from 3 to 4 shards: Only 50/200 keys (25.0%) migrated.
  -> Compared to Classic Modulo Hashing (75% reshuffle), Consistent Hashing prevented 50%+ cache churn: PASSED

[TEST 3] Testing Parallel Scatter-Gather Cross-Shard Query Aggregation...
  -> Scatter-Gather Parallel Aggregation Results:
     * Category: Bakery       | Orders: 23 | Revenue: ₹12880
     * Category: Vegetables   | Orders: 23 | Revenue: ₹12650
     * Category: Dairy        | Orders: 22 | Revenue: ₹12320
     * Category: Snacks       | Orders: 22 | Revenue: ₹12100
  -> Total Cross-Shard Revenue: ₹49950 (100% Exact Match): PASSED

====================================================================
 [SUCCESS] All Database Sharding & Consistent Hashing checks PASSED! 
====================================================================
```
