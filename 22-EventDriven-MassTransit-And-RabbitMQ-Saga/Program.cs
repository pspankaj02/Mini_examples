// [KYA KARTA HAI]: System namespace import karta hai for diagnostics.
// [KYU ZAROORI HAI]: Console logging and assertion checks ke liye.
// [KAISE CHALTA HAI]: .NET core BCL.
// [OUTPUT / SIDE EFFECT]: Basic types.
// [VARIATIONS TO TRY]: Dedicated test framework like xUnit.
using System;

// [KYA KARTA HAI]: Project namespaces import karta hai.
// [KYU ZAROORI HAI]: Events, Outbox, Saga aur Consumers use karne ke liye.
// [KAISE CHALTA HAI]: Using directives.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Global usings in project file.
using KiranaHub.EventDrivenSaga.Messages;
using KiranaHub.EventDrivenSaga.Outbox;
using KiranaHub.EventDrivenSaga.Saga;
using KiranaHub.EventDrivenSaga.Consumers;

Console.WriteLine("====================================================================");
Console.WriteLine("  KiranaHub Project 22: Event-Driven Sagas & Transactional Outbox   ");
Console.WriteLine("====================================================================");

// -------------------------------------------------------------------------
// TEST 1: Transactional Outbox Pattern (Dual-Write Protection)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 1] Testing Transactional Outbox Pattern (Dual-Write Prevention)...");
var outboxRepo = new TransactionalOutboxRepository();

var testOrderEvent = new OrderPlacedEvent(
    "ORD-8801",
    "CUST-101",
    373.00m,
    new List<string> { "item-milk-1", "item-atta-5" },
    DateTime.UtcNow);

// Atomic write simulating single database transaction
outboxRepo.SaveWithOutbox("ORD-8801", testOrderEvent);

var pendingMessages = outboxRepo.GetPendingMessages();
if (pendingMessages.Count != 1)
    throw new Exception($"Expected 1 pending outbox message, but found {pendingMessages.Count}");

var firstMessage = pendingMessages[0];
if (firstMessage.EventType != nameof(OrderPlacedEvent))
    throw new Exception($"Expected event type OrderPlacedEvent, but got {firstMessage.EventType}");

// Simulate Background Outbox Publisher dispatching to message bus
outboxRepo.MarkAsProcessed(firstMessage.Id);
var remainingPending = outboxRepo.GetPendingMessages();
if (remainingPending.Count != 0)
    throw new Exception("Expected 0 pending outbox messages after processing");

Console.WriteLine("  -> Transactional Outbox atomic write & processing: PASSED");

// -------------------------------------------------------------------------
// TEST 2: Saga Happy Path (Order -> Inventory -> Payment -> Completed)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 2] Testing Distributed Saga Happy Path (Success Completion)...");
var sagaStateMachine = new OrderSagaStateMachine();
var inventoryConsumer = new InventoryConsumer();
var paymentConsumer = new PaymentConsumer();

// Step 1: Customer submits order -> OrderPlacedEvent
var order1Placed = new OrderPlacedEvent(
    "ORD-SUCCESS-1",
    "CUST-201",
    450.00m,
    new List<string> { "prod-milk", "prod-bread" },
    DateTime.UtcNow);

var saga1 = sagaStateMachine.Handle(order1Placed);
if (saga1.CurrentState != SagaState.AwaitingInventory)
    throw new Exception($"Expected state AwaitingInventory, but got {saga1.CurrentState}");

// Step 2: Inventory Consumer checks stock and reserves
var inventoryResult1 = inventoryConsumer.Consume(order1Placed);
if (inventoryResult1 is not InventoryReservedEvent reservedEvent)
    throw new Exception("Expected InventoryReservedEvent for happy path");

var saga1AfterInventory = sagaStateMachine.Handle(reservedEvent);
if (saga1AfterInventory.CurrentState != SagaState.AwaitingPayment)
    throw new Exception($"Expected state AwaitingPayment, but got {saga1AfterInventory.CurrentState}");

// Step 3: Payment Consumer captures funds
var paymentResult1 = paymentConsumer.Consume(reservedEvent, saga1.TotalAmount);
var saga1Final = sagaStateMachine.Handle(paymentResult1);
if (saga1Final.CurrentState != SagaState.Completed)
    throw new Exception($"Expected state Completed, but got {saga1Final.CurrentState}");
if (saga1Final.CompletedAtUtc == null)
    throw new Exception("CompletedAtUtc must be set on completed saga");

Console.WriteLine($"  -> Saga Happy Path completed successfully: Order #{saga1Final.CorrelationId} -> State: {saga1Final.CurrentState}: PASSED");

// -------------------------------------------------------------------------
// TEST 3: Saga Compensating Path (Out-of-Stock -> Automatic Rollback / Refund)
// -------------------------------------------------------------------------
Console.WriteLine("\n[TEST 3] Testing Saga Compensating Transaction (Auto-Refund on Out-of-Stock)...");
// Order with an out-of-stock product
var order2Placed = new OrderPlacedEvent(
    "ORD-FAIL-2",
    "CUST-301",
    890.00m,
    new List<string> { "prod-milk", "prod-out-of-stock-avocado" },
    DateTime.UtcNow);

var saga2 = sagaStateMachine.Handle(order2Placed);
if (saga2.CurrentState != SagaState.AwaitingInventory)
    throw new Exception($"Expected state AwaitingInventory, but got {saga2.CurrentState}");

// Inventory consumer fails because product is out of stock
var inventoryResult2 = inventoryConsumer.Consume(order2Placed);
if (inventoryResult2 is not InventoryExhaustedEvent exhaustedEvent)
    throw new Exception("Expected InventoryExhaustedEvent for out-of-stock item");

// State machine triggers COMPENSATING TRANSACTION
var saga2Final = sagaStateMachine.Handle(exhaustedEvent);
if (saga2Final.CurrentState != SagaState.Cancelled)
    throw new Exception($"Expected state Cancelled after compensation, but got {saga2Final.CurrentState}");
if (string.IsNullOrEmpty(saga2Final.FailureReason))
    throw new Exception("FailureReason must be recorded during saga rollback");

Console.WriteLine($"  -> Compensating Transaction Triggered: Order #{saga2Final.CorrelationId} rolled back safely!");
Console.WriteLine($"     Reason: {saga2Final.FailureReason}");
Console.WriteLine("  -> Saga Compensating Transaction: PASSED");

Console.WriteLine("\n====================================================================");
Console.WriteLine(" [SUCCESS] All Event-Driven Sagas & Outbox checks PASSED!");
Console.WriteLine("====================================================================");
