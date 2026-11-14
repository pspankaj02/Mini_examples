using System;
using System.Diagnostics;
using KiranaHub.SolidPrinciples.D_DependencyInversion;
using KiranaHub.SolidPrinciples.I_InterfaceSegregation;
using KiranaHub.SolidPrinciples.L_LiskovSubstitution;
using KiranaHub.SolidPrinciples.O_OpenClosed;
using KiranaHub.SolidPrinciples.S_SingleResponsibility;

// [CONSOLE ENCODING SETUP]:
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [BANNER]:
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 02: SOLID PRINCIPLES IN ACTION (C# ARCHITECTURE)          ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// [S] SINGLE RESPONSIBILITY PRINCIPLE (SRP)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [S - SRP]: Testing Single Responsibility Classes");

// [ORDER ITEMS INITIALIZATION]:
var items = new List<OrderItem>
{
    new("Tata Salt 1kg", 28m, 2),
    new("Fortune Sunflower Oil 1L", 160m, 1)
}; // Subtotal = 28*2 + 160 = 216

// [CLASS 1 INVOCATION]: Tax calculation responsibility
var taxCalc = new KiranaTaxCalculator();
decimal tax = taxCalc.CalculateTax(items); // 5% of 216 = 10.80

// [CLASS 2 INVOCATION]: Receipt formatting responsibility
var receiptFormatter = new KiranaReceiptFormatter();
string receipt = receiptFormatter.GenerateInvoiceText("ORD-501", 216m, tax);

// [CLASS 3 INVOCATION]: Notification dispatch responsibility
var notifier = new KiranaSmsNotifier();
notifier.SendOrderConfirmation("+91-9876543210", receipt);

// [ASSERTION]:
Debug.Assert(tax == 10.80m, "SRP Tax calculation mismatch!");
Console.WriteLine("[PASS] S - SRP: Segregated services work cohesively without God class.\n");

// ----------------------------------------------------------------------------------------
// [O] OPEN/CLOSED PRINCIPLE (OCP)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [O - OCP]: Testing Extensible Discount Strategies");

// [STRATEGY LIST REGISTRATION]: Open for extension
var discountStrategies = new List<IDiscountStrategy>
{
    new DiwaliFestiveDiscount(), // 10% on >= 500
    new BulkGroceryDiscount()     // Rs. 150 on >= 2000
};

// [ENGINE INITIALIZATION]: Closed for modification
var discountEngine = new KiranaDiscountEngine(discountStrategies);

// [CASE 1 TEST]: Order of Rs. 800 (Diwali applicable: 10% = 80)
decimal disc1 = discountEngine.GetBestDiscount(800m, 3, out string rule1);
Console.WriteLine($"Order Rs. 800 -> Discount: Rs. {disc1} (Applied Rule: {rule1})");
Debug.Assert(disc1 == 80m && rule1.Contains("Diwali"));

// [CASE 2 TEST]: Order of Rs. 2500 (10% of 2500 = 250 > 150 bulk, so Diwali wins!)
decimal disc2 = discountEngine.GetBestDiscount(2500m, 5, out string rule2);
Console.WriteLine($"Order Rs. 2500 -> Discount: Rs. {disc2} (Applied Rule: {rule2})");
Debug.Assert(disc2 == 250m);
Console.WriteLine("[PASS] O - OCP: Discount engine extensible with zero changes to existing classes.\n");

// ----------------------------------------------------------------------------------------
// [L] LISKOV SUBSTITUTION PRINCIPLE (LSP)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [L - LSP]: Subtypes honor contracts without breaking clients");

// [SUBTYPE SUBSTITUTION 1]: Bike rider as IDeliveryPartner
IDeliveryPartner bikeRider = new BikeDeliveryRider();

// [SUBTYPE SUBSTITUTION 2]: Drone as IDeliveryPartner
IDeliveryPartner drone = new AutonomousDronePartner();

// [BASE INVOCATIONS]: Both execute cleanly without throwing NotImplementedException
bikeRider.DeliverPackage("ORD-501", "Tower A, Flat 402");
drone.DeliverPackage("ORD-502", "Villa 12 Open Lawn");

// [PATTERN MATCHING CHECK FOR EXTENDED CAPABILITY]:
if (bikeRider is IDoorstepDeliveryPartner doorstep)
{
    doorstep.DeliverToApartmentDoor("ORD-501", 4);
}
Console.WriteLine("[PASS] L - LSP: Subtypes cleanly substituted based on exact capabilities.\n");

// ----------------------------------------------------------------------------------------
// [I] INTERFACE SEGREGATION PRINCIPLE (ISP)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [I - ISP]: Segregated interfaces prevent unnecessary implementation bloat");

// [CLIENT 1]: Rider only deals with Cash interface
ICashCollectable codRider = new KiranaCodRider();
codRider.CollectCashAtDoorstep(350m);

// [CLIENT 2]: Gateway only deals with UPI interface
IUpiPayable upiGateway = new KiranaOnlineGateway();
upiGateway.PayViaUpiQr("customer@okhdfcbank", 350m);
Console.WriteLine("[PASS] I - ISP: Clients only depend on methods they actually need.\n");

// ----------------------------------------------------------------------------------------
// [D] DEPENDENCY INVERSION PRINCIPLE (DIP)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [D - DIP]: High-level checkout decoupled from concrete DB/Notification");

// [ABSTRACTION INSTANCES]:
IOrderStorage storage = new InMemoryOrderStorage();
INotificationService notifySvc = new ConsoleNotificationService();

// [DEPENDENCY INJECTION]: Passed into high-level orchestrator
var checkoutService = new KiranaCheckoutService(storage, notifySvc);
checkoutService.PlaceOrder("ORD-999", "+91-9988776655", 1450m);

// [VERIFICATION]:
Debug.Assert(storage.Exists("ORD-999"), "Order must exist in storage after checkout!");
Console.WriteLine("[PASS] D - DIP: High-level business logic is 100% decoupled and unit-testable.\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 02-SOLID-PRINCIPLES-IN-ACTION DEMOS AND TESTS PASSED!    ");
Console.WriteLine("==========================================================================");
