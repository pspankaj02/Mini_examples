# Project 26: Chaos Engineering, Polly v8 Resilience Pipelines & High-Throughput Load Testing

> **Quick-Commerce Domain:** KiranaHub Zero-Downtime Reliability & Stress Testing Engine  
> **Tech Stack:** C# 13, .NET 10, Polly v8 (`Polly.Core`), Circuit Breaker Pattern, Exponential Backoff with Jitter, Graceful Fallback Strategies, Chaos Fault Injection, High-Throughput Concurrency Benchmarking (P50/P95/P99).

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Jab system local machine par akele test hota hai, toh sab kuch smooth lagta hai. Lekin quick-commerce production me do cheezein hoti hain:
1. **Third-party failures**: Payment gateway (Razorpay/Paytm) achanak 503 error throw karne lagti hai ya 15 second tak socket hang kar deti hai.
2. **Flash-sale surges**: Ek hi second me 5,000 customers "Pay & Order" button click karte hain.

Agar aapke pass resilience pipeline nahi hai, toh ek slow gateway poore microservice system ke thread pool ko chok kar dega (Cascading Failure).

Aapko yeh code iss sequence me padhna hai:
1. **`Resilience/PollyPipelines.cs`**:
   - Dekhiye Polly v8 ka unified pipeline:
     - **Layer 1 (Fallback)**: Gateway down hone par customer ko 500 crash dikhane ke bajaye order ko Cash-on-Delivery (COD) mode me accept karna.
     - **Layer 2 (Circuit Breaker)**: 50% failure rate aate hi circuit ko `Open` trip karke downstream service ko rest dena.
     - **Layer 3 (Retry with Jitter)**: Transient network hiccups par exponential backoff (`50ms * 2^attempt + jitter`) lagana taaki thundering herd na bane.
     - **Layer 4 (Timeout)**: 800ms se slow calls ko hard terminate karna.
2. **`Chaos/ChaosFaultInjector.cs`**:
   - Samajhiye ki controlled Chaos Engineering kya hai: kaise synthetic latency (10ms - 80ms) aur configurable failure rates (15% to 100%) inject karke real-world payment failures simulate kiye jaate hain.
3. **`LoadSimulation/CheckoutLoadTestEngine.cs`**:
   - Dekhiye ki 30 parallel virtual users kaise simultaneous checkout requests generate karte hain, `SemaphoreSlim` se concurrency throttle kaise hoti hai, aur P50, P95, P99 tail latencies mathematically kaise compute hoti hain.
4. **`Program.cs`**:
   - Automated stress runner jo Circuit Breaker ke Open hone, Cooldown wait karne, Half-Open me probe bhejne aur Closed me reset hone ka complete lifecycle verify karta hai.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: Cascading Failures & Michael Nygard's Circuit Breaker
- In distributed systems, failure of a single downstream dependency can bring down the entire fleet:
  - Ek API call freeze hoti hai -> Kestrel web server ka 1 thread block hota hai.
  - 500 concurrent users aate hain -> Saare 500 thread pool workers block ho jaate hain.
  - Server health checks fail hote hain -> Kubernetes pod ko terminate kar deta hai -> Load doosre pod par shift hota hai -> Wo bhi crash ho jata hai (**Cascading Collapse**).
- Isliye sabse pehle **`PollyPipelines`** banaya gaya:
  - Circuit Breaker downstream health monitor karta hai.
  - Jab circuit **Open** hota hai, toh calls direct execute hi nahi hoti; turant fallback return ho jata hai (Fast Fail).

```
Incoming Checkout Request
           │
           ▼ [Polly v8 Resilience Pipeline]
┌────────────────────────────────────────────────────────┐
│ 1. Fallback Policy (Intercepts Exceptions / Fast-Fail) │
│ 2. Circuit Breaker (Closed -> Open -> HalfOpen)        │
│ 3. Retry Strategy (Exponential Backoff + Jitter)       │
│ 4. Timeout Strategy (800ms SLA Cap)                    │
└────────────────────────────────────────────────────────┘
           │
           ▼
[Downstream Payment Gateway / DB (Chaos Fault Injected)]
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `PollyPipelines.cs` (Zero-Allocation Polly v8 Pipeline)
- **Kyu Aaya?** Legacy Polly v7 me `Policy.Wrap()` heap par multiple objects allocate karta tha. Polly v8 completely redesigned hai aur zero memory allocation ke sath 100,000+ RPS handle karta hai.
- **Kaise Kaam Karta Hai?**
  - Builder pattern se multiple reactive aur proactive strategies compose hoti hain.
  - Circuit state transition callbacks (`OnOpened`, `OnHalfOpened`, `OnClosed`) audit telemetry emit karte hain.

### Step 2: `ChaosFaultInjector.cs` (Controlled Chaos Testing)
- **Kyu Aaya?** Testing in production is dangerous. Local development me actual bank APIs ko bar-bar crash nahi karaya ja sakta.
- **Kaise Kaam Karta Hai?**
  - Thread-safe `Random.Shared` se failure probability calculate karta hai.
  - `Task.Delay` se artificial network transmission jitter inject karta hai.
  - Probability match hone par `PaymentGatewayUnavailableException` throw karta hai.

### Step 3: `CheckoutLoadTestEngine.cs` (High-Throughput Percentile Profiler)
- **Kyu Aaya?** "Average latency" ek misleading metric hai (Flaw of Averages). Agar 99 customers ko 20ms lag raha hai aur 1 VIP customer ko 15 second lag raha hai, toh average ~35ms dikhega, jabki 1 customer ka experience terrible tha.
- **Kaise Kaam Karta Hai?**
  - High-resolution `Stopwatch` se har individual request ka time record hota hai.
  - Latencies sort karke 50th percentile (P50), 95th percentile (P95), aur 99th percentile (P99) tail latencies compute hoti hain.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Circuit Breaker** | Payment gateway slow hone par Kestrel server ke saare thread pool threads block ho jayenge. Checkout ke alawa browsing aur search bhi freeze ho jayegi (Total site-wide outage). |
| **Without Exponential Backoff & Jitter** | Jab gateway recover hone ki koshish karega, 10,000 retry requests ek hi second par hit karengi (Thundering Herd attack), jisse recovering gateway dubara crash ho jayega. |
| **Without Graceful Fallback Strategy** | Online payment fail hone par customer ko generic "500 Internal Server Error" dikhega aur cart abandon ho jayegi. Fallback hone se order COD mode me convert ho kar book ho jata hai. |
| **Without Timeout Strategy** | Dead TCP connection 15 minute tak socket open rakhega, jisse cloud firewall aur load balancer port exhaust ho jayenge (SNAT Port Exhaustion). |
| **Without P95/P99 Percentile Profiling** | Engineering team average 30ms latency dekh kar khush hoti rahegi, jabki top 5% high-value customers 2-second lag face karke app uninstall kar rahe honge. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\26-ChaosEngineering-And-LoadTesting\KiranaHub.ChaosAndLoadTesting.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 26: Chaos Engineering & Load Testing Suite      
====================================================================

[TEST 1] Verifying Polly v8 Circuit Breaker Trip & Fallback Pipeline...
  [POLLY ALERT] Circuit Breaker TRIPPED to OPEN! Break Duration: 1500ms. Reason: [CHAOS FAULT] Downstream Payment Gateway timed out or returned HTTP 503 Service Unavailable (Roll: 0.82 < 1.00)
  -> Circuit Breaker tripped 1 time(s) to OPEN state: PASSED
  -> All 6 failed calls were gracefully diverted to Fallback (COD mode): PASSED
  -> Waiting 1.6s for Circuit Breaker break duration to expire...
  [POLLY PROBE] Circuit Breaker entered HALF-OPEN state. Testing trial requests...
  [POLLY RECOVERY] Circuit Breaker RESET to CLOSED! Downstream health fully restored.
  -> Circuit Breaker probe passed and state RESET to CLOSED: PASSED

[TEST 2] Running High-Throughput Concurrency & Tail Latency Benchmark...
  -> Executed 300 concurrent checkout transactions in 0.6s
  -> Throughput: 502.2 Requests/Second (RPS)
  -> Primary Gateway Authorizations: 299 (99.7%)
  -> Graceful COD Fallbacks: 1 (0.3%)
  -> Unhandled 500 Crashes: 0 (0.0%): PASSED
  -> Latency Percentiles:
     * Min Latency : 14.17 ms
     * P50 (Median): 31.8 ms
     * P95 Tail    : 141.56 ms
     * P99 Tail    : 190.21 ms
     * Max Latency : 252.22 ms: PASSED

====================================================================
 [SUCCESS] All Polly v8 Chaos & Load Testing checks PASSED!         
====================================================================
```
