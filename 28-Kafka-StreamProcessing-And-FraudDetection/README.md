# Project 28: Apache Kafka Event Streaming, Sliding-Window Analytics & Real-Time Fraud Detection

> **Quick-Commerce Domain:** KiranaHub High-Throughput Stream Processing & Financial Security Engine  
> **Tech Stack:** C# 13, .NET 10, Distributed Commit Log Architecture, Topic Partition Hashing, Consumer Groups & Offset Management, Sliding Window Analytics (Surge Multipliers), Real-Time Heuristic Fraud Engine (Velocity Spikes, Impossible Geo Travel, Value Anomalies).

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Jab millions of customers simultaneously grocery orders karte hain, traditional relational databases ya simple queues (RabbitMQ) analytics aur fraud detection ke liye choke ho jaati hain. Apache Kafka ka append-only partitioned log model horizontal scaling allow karta hai: ek topic ko 3, 10 ya 50 partitions me split kiya jata hai, aur har partition independent consumer pod dwara in-order process hota hai.

Aapko yeh code iss sequence me padhna hai:
1. **`Streaming/KafkaTopicEngine.cs`**:
   - Dekhiye ki Kafka ka commit log kaise kaam karta hai:
     - Key Hashing (`abs(hash) % partitionCount`) se same customer ke saare orders hamesha ek hi partition me strictly in-order route hote hain.
     - Consumer Groups (`analytics-worker-group`) committed offsets track karte hain, aur consumer lag compute hota hai.
2. **`Windowing/SlidingWindowAggregator.cs`**:
   - Samajhiye stream processing windowing:
     - 60-second sliding time window me checkout events record hote hain.
     - Window duration se purane events automatically prune hote hain (zero memory leak).
     - Dark-store demand velocity ke hisaab se dynamic surge pricing multipliers (1.0x -> 1.25x -> 1.50x) compute hote hain.
3. **`Fraud/FraudDetectionEngine.cs`**:
   - Dekhiye sub-5ms inline fraud detection rules:
     - **Card Velocity**: $\ge 3$ checkouts within 10 seconds.
     - **Impossible Geo Travel**: Haversine formula se Noida aur Bengaluru ke beech 1,740 km travel ko 3 minute me detect karke block karna (> 800 km/h flight speed limit).
     - **Value Anomaly**: Historical median se $> 10\times$ spike detect karna.
4. **`Program.cs`**:
   - Automated benchmark runner jo multi-partition distribution, consumer offset commitment, sliding-window surge rates, aur 3 fraud scenarios ko verify karta hai.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: Jay Kreps' Distributed Commit Log Architecture
- Traditional Message Queues (RabbitMQ) me message consume hone ke baad queue se delete ho jata hai. Isliye 5 alag services (Analytics, Fraud, Warehouse, Invoicing, ML) ek hi stream ko independent speeds par read nahi kar sakti thin.
- Apache Kafka ne isse badal diya:
  - **Immutable Append-Only Log**: Messages delete nahi hote; retain rehte hain.
  - **Independent Offsets**: Har consumer group apna pointer (offset) alag manage karta hai.
  - **Partitioning**: Key hash se continuous streams parallel pipelines me bifurcate hoti hain.

```
Incoming Checkout Events (100k RPS)
                 │
                 ▼ [RouteToPartition(CustomerId)]
┌────────────────────────────────────────────────────────┐
│ Partition 0: [Off 0] -> [Off 1] -> [Off 2] ...         │
│ Partition 1: [Off 0] -> [Off 1] -> [Off 2] ...         │
│ Partition 2: [Off 0] -> [Off 1] -> [Off 2] ...         │
└────────────────────────────────────────────────────────┘
        │                                 │
        ▼                                 ▼
[SlidingWindowAggregator]        [FraudDetectionEngine]
60s Sliding Horizon:             1. Velocity Spike (>=3 in 10s)
Velocity: 21 orders/min          2. Impossible Travel (>800 km/h)
Surge Multiplier: 1.50x          3. Value Anomaly (>10x Median)
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `KafkaTopicEngine.cs` (Multi-Partition Topic Engine)
- **Kyu Aaya?** Monolithic queues me single-thread bottleneck hota hai. Microservices ko parallel me work distribute karne ke liye partitions chahiye.
- **Kaise Kaam Karta Hai?**
  - Messages `Key` hash ke through deterministic partition me jaate hain.
  - Consumer groups batch me read karti hain aur processed offset commit karti hain.
  - Consumer Lag calculate hota hai (`head - committed`).

### Step 2: `SlidingWindowAggregator.cs` (Dynamic Demand Velocity Engine)
- **Kyu Aaya?** Rainstorms ya evening dinner rush me dark stores par achanak orders spike karte hain. Agar surge pricing na lage toh warehouse queue 40 minute delay ho jayegi (10-minute SLA breach).
- **Kaise Kaam Karta Hai?**
  - Sliding 60-second window me order velocity calculate hoti hai.
  - Automatic surge pricing multiplier apply hota hai jo customer demand aur available rider supply ko balance karta hai.

### Step 3: `FraudDetectionEngine.cs` (Inline Financial Security)
- **Kyu Aaya?** Cybercriminals stolen credit cards se quick-commerce apps par bot script chala kar hazaron rupaye ke dry fruits aur electronics order karte hain (Carding Attack).
- **Kaise Kaam Karta Hai?**
  - Velocity checks carding attacks ko block karti hain.
  - Haversine geo checks account takeovers (ek user do alag shehron me 5 minute me login) ko block karti hain.
  - Value checks suspicious credit card balance drain attempts ko flag karti hain.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Partitioned Commit Log** | 50,000 orders/sec par single message queue chok ho jayegi. Analytics team ke slow query chalane par live checkout processing halt ho jayegi. |
| **Without Partition Offset Tracking & Lag Monitoring** | Consumer worker pod crash hone par uncommitted messages duplicate process ho jayenge ya skip ho jayenge, jisse customer ko do baar grocery deliver ho sakti hai. |
| **Without Sliding-Window Surge Pricing** | Barish ke time dark store par 100 orders ek sath aayenge lekin delivery boy sirf 5 honge. Saare orders 2 ghante late honge aur brand reputation barbad ho jayegi. |
| **Without Card Velocity Fraud Detection** | Fraudster bot script chala kar 10 second me 50 orders place kar dega. Bank chargeback fine lagayega aur merchant account block ho jayega. |
| **Without Impossible Geo Travel Detection** | Compromised user account se hacker doosri state me order place karke customer wallet balance chura lega without legitimate user's knowledge. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\28-Kafka-StreamProcessing-And-FraudDetection\KiranaHub.KafkaStreaming.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 28: Kafka Streaming & Fraud Detection Suite     
====================================================================

[TEST 1] Testing Kafka Multi-Partition Broker & Consumer Group Offsets...
  -> Produced 60 messages across 3 partitions: P0=20, P1=20, P2=20: PASSED
  -> Consumer Group 'analytics-worker-group' consumed all 60 messages. Remaining Lag = 0: PASSED

[TEST 2] Testing Sliding-Window Velocity & Dynamic Surge Multipliers...
  -> Normal Velocity: 3 orders/min -> Surge Multiplier: 1x: PASSED
  -> Peak Velocity: 21 orders/min -> Surge Multiplier: 1.5x: PASSED

[TEST 3] Testing Real-Time Streaming Fraud Detection Engine...
  -> Velocity Anomaly Detected: Risk='CRITICAL', Blocked=True: PASSED
  -> Impossible Travel Speed Detected: [FRAUD_IMPOSSIBLE_TRAVEL] User 'CUST-001' moved 1741.1 km in 3.0 minutes (Implied Speed: 35214 km/h > 800 km/h limit)!: PASSED
  -> Value Anomaly Detected: [FRAUD_VALUE_ANOMALY] Order amount ₹15000 is > 10x user's historical median of ₹250!: PASSED

====================================================================
 [SUCCESS] All Kafka Streaming, Windowing & Fraud checks PASSED!    
====================================================================
```
