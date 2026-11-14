# Project 27: Event Sourcing, CQRS Materialized Read Projections & Temporal Time-Travel Debugging

> **Quick-Commerce Domain:** KiranaHub Immutable Event Stream & Temporal Audit Engine  
> **Tech Stack:** C# 13, .NET 10, Pure Event Sourcing (ES), Optimistic Concurrency Control (`ExpectedVersion`), CQRS Materialized Read Projections, Temporal Time-Travel Replay Engine.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Traditional relational databases me hum `UPDATE Orders SET Status = 'Delivered', RiderId = 'RIDER-77'` run karte hain. Isse database ka purana data hamesha ke liye mit jata hai. Quick-commerce me jab customer dispute karta hai: *"Maine cart me ghee add kiya tha par delivery ke waqt gayab tha, aur payment kis time capture hui?"* — traditional CRUD me hum investigation nahi kar sakte kyunki intermediate history delete ho chuki hoti hai.

Event Sourcing isse permanently badal deta hai. Code ko iss order me padhein:
1. **`Events/OrderDomainEvents.cs`**:
   - Dekhiye ki state changes ko mutable columns ke bajaye immutable facts me kaise model kiya jata hai (`OrderCreatedEvent`, `ItemAddedEvent`, `StockAllocatedEvent`, `PaymentCapturedEvent`, `RiderDispatchedEvent`, `OrderDeliveredEvent`).
2. **`Storage/IEventStore.cs` & `Storage/InMemoryEventStore.cs`**:
   - Samajhiye ki append-only log me optimistic concurrency control (`ExpectedVersion`) kaise kaam karta hai aur do simultaneous writers me se stale write ko `OptimisticConcurrencyException` se kaise reject kiya jata hai.
3. **`Aggregates/SourcedOrderAggregate.cs`**:
   - Dekhiye ki ek pure event-sourced aggregate me ek bhi public setter nahi hota. Public methods sirf invariants check karke events raise karte hain, aur internal state sirf `Apply(IDomainEvent)` se mutate hoti hai.
4. **`Projections/OrderSummaryReadModelProjection.cs`**:
   - Samajhiye CQRS Read Side: Event store ke append hone par background subscriber kaise denormalized `OrderSummaryView` update karta hai taaki customer dashboard ko sub-millisecond query performance mile without replaying events every time.
5. **`TimeTravel/TimeTravelDebugger.cs`**:
   - Dekhiye time-travel debugging: kisi bhi historical version (e.g. Version 2) ya timestamp tak stream ko slice karke exact historical state kaise reconstruct hoti hai.
6. **`Program.cs`**:
   - Automated test runner jo Concurrency conflicts, CQRS materialization, aur Time-travel reconstruction ko end-to-end verify karta hai.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: Append-Only Immutable Log & Greg Young's Event Sourcing
- Accountancy (Ledgers) me pichle 500 saal se koi entry erase ya overwrite nahi ki jaati. Naye transaction ko hamesha append kiya jata hai.
- Event Sourcing me hum database me current state store karne ke bajaye **Facts that Happened** store karte hain:
  - Genesis event: `OrderCreated`
  - Mutation 1: `ItemAdded (Butter)`
  - Mutation 2: `ItemAdded (Milk)`
  - Mutation 3: `StockAllocated`
  - Mutation 4: `PaymentCaptured`
- Current state is simply the left-fold aggregation of all past events:
  $$\text{State}_N = \text{Fold}(\text{InitialState}, [\text{Event}_1, \text{Event}_2, \dots, \text{Event}_N])$$

```
Customer Actions
      │
      ▼ [Business Command Method]
SourcedOrderAggregate (Invariants Checked)
      │
      ▼ [RaiseEvent()]
Append-Only Event Store (Optimistic Concurrency: ExpectedVersion = ActualVersion)
      │
      ├──────────────────────────────┐
      ▼                              ▼
[Event Stream Log]            [CQRS Read Projection]
Event 1: OrderCreated         OrderSummaryView (Denormalized Cache)
Event 2: ItemAdded            Status: "Delivered", Items: 3, Total: ₹618
Event 3: StockAllocated       (Sub-millisecond Dashboard Reads)
Event 4: PaymentCaptured
Event 5: RiderDispatched
Event 6: OrderDelivered
      │
      ▼ [Time-Travel Debugger]
ReplayToVersion(2) ──> Reconstructed State at V2: Status="Created", Items=1, Total=₹275
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `OrderDomainEvents.cs` (Immutable Facts)
- **Kyu Aaya?** Business me "kya hua" usko audit proof contract me capture karna.
- **Kaise Kaam Karta Hai?** C# 13 immutable records with UTC timestamps aur monotonic versions. `WithVersion` method ensure karta hai ki event freeze rahe.

### Step 2: `IEventStore.cs` & `InMemoryEventStore.cs` (Optimistic Concurrency Engine)
- **Kyu Aaya?** Agar do workers simultaneously same order me items add karein, toh ek ka write doosre ko bina jaane overwrite na kar de (Lost Update bug).
- **Kaise Kaam Karta Hai?**
  - Caller batata hai `expectedVersion`.
  - Agar store me stream ka version already aage nikal chuka hai (`actualVersion != expectedVersion`), toh store write reject karke `OptimisticConcurrencyException` throw karta hai.

### Step 3: `SourcedOrderAggregate.cs` (Pure Event-Sourced Aggregate)
- **Kyu Aaya?** Domain logic aur state mutation ko separate karna.
- **Kaise Kaam Karta Hai?**
  - Aggregate ko rebuild karne ke liye `RehydrateFromHistory(events)` call hota hai.
  - Har historical event ke upar `Apply(evt)` execute hota hai, jisse database table columns ke bina live in-memory object reconstruct ho jata hai.

### Step 4: `OrderSummaryReadModelProjection.cs` (CQRS Asynchronous Read Model)
- **Kyu Aaya?** Agar har query par 1,000 events replay karenge, toh customer app slow ho jayegi.
- **Kaise Kaam Karta Hai?**
  - Event store me event commit hote hi projection automatically invoke hoti hai.
  - Denormalized view memory/cache me update ho jata hai jisse dashboard reads O(1) time complexity me ho jaati hain.

### Step 5: `TimeTravelDebugger.cs` (Temporal State Reconstruction)
- **Kyu Aaya?** Legal disputes aur customer support auditing.
- **Kaise Kaam Karta Hai?**
  - `ReplayToVersionAsync(orderId, version)` stream ko us index par slice karta hai aur aggregate reconstruct karta hai.
  - Developer dekh sakta hai ki theek 10 minute pehle order me kya kya items the.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Event Sourcing (Destructive SQL UPDATE only)** | Customer claim karega ki usne 500g butter order kiya tha, delivery boy ne 100g diya. Database me sirf current value 100g dikhegi. Company ke paas koi proof nahi hoga ki customer ne kab kya change kiya tha. |
| **Without Optimistic Concurrency Control** | Do customer support agents ek hi waqt order update karenge (ek item add karega, dusra discount coupon lagayega). Ek agent ka update doosre ko silent overwrite kar dega (Silent Data Loss). |
| **Without CQRS Read Projections** | Customer app par "My Orders" kholte hi backend har order ke 50 events ko DB se fetch karke in-memory replay karega. App 5-10 second lag karegi aur DB CPU 100% choke ho jayega. |
| **Without Time-Travel Debugging** | Production me achanak negative total amount ya invalid status transition bug aane par developers reproduce nahi kar payenge kyunki unhe nahi pata hoga ki order us state me kis sequence se pahuncha. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\27-EventSourcing-And-CQRS-ReadProjections\KiranaHub.EventSourcing.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 27: Event Sourcing & CQRS Projections Engine    
====================================================================

[TEST 1] Testing Event Stream Append & Optimistic Concurrency Protection...
  -> Successfully committed Genesis events (OrderCreated + 2 ItemAdded): PASSED
  -> Caught expected Concurrency Collision: [CONCURRENCY CONFLICT] Stream '6b6de1c1-205a-452a-8b15-89b2da64ad4c' expected version 0, but store currently at version 3!: PASSED

[TEST 2] Testing Lifecycle Progression & CQRS Materialized Read Model...
  -> Materialized View Status: 'Delivered'
  -> Materialized Total Amount: ₹618
  -> Materialized Item Count: 3
  -> Materialized Rider: Vikram Singh
  -> CQRS Materialized Read Model fully synchronized: PASSED

[TEST 3] Testing Temporal Time-Travel Debugging (Replay to Historical Version)...
  -> Reconstructed State at Version 2: Status='Created', Items=1, Amount=₹550
  -> Rider Assigned at V2: None (Correct)
  -> Temporal Time-Travel State Reconstruction: PASSED

====================================================================
 [SUCCESS] All Event Sourcing, CQRS & Time-Travel checks PASSED!    
====================================================================
```
