using System;
using System.Diagnostics;
using KiranaHub.CleanArchitecture.Application.Common.Behaviors;
using KiranaHub.CleanArchitecture.Application.Orders.Commands;
using KiranaHub.CleanArchitecture.Application.Orders.EventHandlers;
using KiranaHub.CleanArchitecture.Application.Orders.Queries;
using KiranaHub.CleanArchitecture.Domain.Repositories;
using KiranaHub.CleanArchitecture.Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// [KYA KARTA HAI]: Console output encoding ko UTF-8 me set karta hai.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [KYA KARTA HAI]: Visual separator line print karta hai.
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Banner text print karta hai.
Console.WriteLine("   KIRANAHUB - 12: CLEAN ARCHITECTURE, DDD, CQRS & MEDIATR               ");

// [KYA KARTA HAI]: Visual separator closing line print karta hai.
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. DEPENDENCY INJECTION & MEDIATR PIPELINE CONFIGURATION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: DI service collection instantiate karta hai.
var services = new ServiceCollection();

// [KYA KARTA HAI]: Console logging services configure karta hai.
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// [KYA KARTA HAI]: MediatR register karta hai scanning current assembly for handlers.
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    // [KYA KARTA HAI]: LoggingBehavior ko open-generic pipeline behavior ke roop me attach karta hai.
    // [KYU ZAROORI HAI]: Har incoming command aur query automatic logging pipeline se pass hogi.
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

// [KYA KARTA HAI]: Infrastructure repository register karta hai.
services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// [KYA KARTA HAI]: Service provider build karta hai.
var serviceProvider = services.BuildServiceProvider();

// [KYA KARTA HAI]: MediatR IMediator instance resolve karta hai.
var mediator = serviceProvider.GetRequiredService<IMediator>();

// ----------------------------------------------------------------------------------------
// 2. CQRS COMMAND: PLACE ORDER (WRITE PIPELINE)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 2 heading print karta hai.
Console.WriteLine("--> [Step 1] Executing CQRS Command: PlaceOrderCommand via MediatR");

// [KYA KARTA HAI]: PlaceOrder command payload construct karta hai.
var placeOrderCmd = new PlaceOrderCommand(
    CustomerId: "CUST-PANKAJ-99",
    Items: new List<PlaceOrderItemDto>
    {
        new("MILK-01", "Amul Cow Milk 500ml", 33.00m, 2),
        new("BTR-01", "Amul Butter 100g", 58.00m, 1)
    }
);

// [KYA KARTA HAI]: MediatR command dispatch karta hai through pipeline behaviors to PlaceOrderCommandHandler.
var placeResult = await mediator.Send(placeOrderCmd);

// [KYA KARTA HAI]: Command execution result verify karta hai.
Debug.Assert(placeResult.IsSuccess, "PlaceOrderCommand failed!");
string orderId = placeResult.Value!;
Console.WriteLine($"[PASS] Order successfully created with ID: {orderId}");

// [KYA KARTA HAI]: Verify karta hai ki Domain Event (OrderPlacedDomainEvent) trigger hua aur notification handler ne receive kiya.
Debug.Assert(OrderPlacedNotificationHandler.DispatchedEventCount >= 1, "Domain event was not dispatched!");
Console.WriteLine("[PASS] Domain Event 'OrderPlacedDomainEvent' was published and handled asynchronously.\n");

// ----------------------------------------------------------------------------------------
// 3. CQRS QUERY: GET ORDER BY ID (READ PIPELINE)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 3 heading print karta hai.
Console.WriteLine("--> [Step 2] Executing CQRS Query: GetOrderByIdQuery via MediatR");

// [KYA KARTA HAI]: Read query send karta hai using order ID.
var query = new GetOrderByIdQuery(orderId);
var orderDto = await mediator.Send(query);

// [KYA KARTA HAI]: Assert karta hai ki query result non-null hai.
Debug.Assert(orderDto != null, "Query returned null order!");
Console.WriteLine($"Fetched Order Details:");
Console.WriteLine($"   Order ID    : {orderDto.OrderId}");
Console.WriteLine($"   Customer ID : {orderDto.CustomerId}");
Console.WriteLine($"   Status      : {orderDto.Status}");
Console.WriteLine($"   Total Amount: {orderDto.Currency} {orderDto.TotalAmount:N2}");
Console.WriteLine($"   Line Items  : {orderDto.Items.Count}");

// [KYA KARTA HAI]: Assert karta hai total amount 33*2 + 58 = 124 INR hai.
Debug.Assert(orderDto.TotalAmount == 124.00m);
Debug.Assert(orderDto.Items.Count == 2);
Console.WriteLine("[PASS] CQRS Query projection returned accurate read model.\n");

// ----------------------------------------------------------------------------------------
// 4. DOMAIN INVARIANTS: ORDER CANCELLATION & PROTECTION RULES
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 4 heading print karta hai.
Console.WriteLine("--> [Step 3] Testing Aggregate Invariants: Order Cancellation");

// [KYA KARTA HAI]: Valid cancellation command dispatch karta hai.
var cancelCmd = new CancelOrderCommand(orderId, "Customer decided to change delivery address.");
var cancelResult = await mediator.Send(cancelCmd);

// [KYA KARTA HAI]: Verify karta hai cancellation successful rahi.
Debug.Assert(cancelResult.IsSuccess);
Console.WriteLine("[PASS] Order successfully transitioned to 'Cancelled' state.");

// [KYA KARTA HAI]: Invariant protection test: Dubara cancel karne ki koshish karta hai.
Console.WriteLine("Attempting to cancel the already cancelled order again (should be rejected by Domain Invariants)...");
var duplicateCancelResult = await mediator.Send(cancelCmd);

// [KYA KARTA HAI]: Assert karta hai ki second cancellation fail hui with friendly error message.
Debug.Assert(duplicateCancelResult.IsFailure);
Console.WriteLine($"[PASS] Domain rejected invalid transition: '{duplicateCancelResult.Error}'\n");

// ----------------------------------------------------------------------------------------
// 5. COMMAND VALIDATION: EMPTY ITEMS REJECTION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 5 heading print karta hai.
Console.WriteLine("--> [Step 4] Testing Command Validation: Zero Items Order Rejection");

// [KYA KARTA HAI]: Empty items list wala invalid command banata hai.
var invalidCmd = new PlaceOrderCommand("CUST-INVALID", new List<PlaceOrderItemDto>());
var invalidResult = await mediator.Send(invalidCmd);

// [KYA KARTA HAI]: Assert karta hai ki command handler ne input reject kar diya.
Debug.Assert(invalidResult.IsFailure);
Console.WriteLine($"[PASS] Validation caught empty items: '{invalidResult.Error}'\n");

// [KYA KARTA HAI]: Final success banner lines print karta hai.
Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 12-CLEAN-ARCHITECTURE-DDD-CQRS TESTS PASSED!             ");
Console.WriteLine("==========================================================================");

