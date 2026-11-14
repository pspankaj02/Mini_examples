using System;
using System.Diagnostics;
using KiranaHub.BasicsAndOops.Generics;
using KiranaHub.BasicsAndOops.Models;
using KiranaHub.BasicsAndOops.Records;
using KiranaHub.BasicsAndOops.Services;

// [CONSOLE ENCODING CONFIGURATION]:
// [KYA KARTA HAI]: Terminal output ko UTF-8 mode par switch karta hai taaki special symbols aur Rupee text safely render hon.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [BANNER PRINTING]:
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 01: C# BASICS, OOPS, RECORDS & GENERICS DEMONSTRATION      ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. ENCAPSULATION & VALIDATION DEMONSTRATION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] Encapsulation & Validation Check");

// [TRY-CATCH BLOCK]: Expected exception test
try
{
    // [INTENTIONAL INVALID INSTANTIATION]:
    // [KYA KARTA HAI]: Negative price pass karke validation exception trigger karta hai.
    // [KAISE CHALTA HAI]: Property setter 'set' value <= 0 dekhte hi ArgumentException throw karega.
    Product invalid = new PackagedProduct("P-00", "Bad Item", -10m, 500);

    // [FAIL-SAFE LINE]: Agar yahan tak pahuncha toh test fail hona chahiye!
    Debug.Fail("Product with negative price should not have been created!");
}
catch (ArgumentException ex)
{
    // [CATCH BLOCK]:
    // [OUTPUT]: Validation failure caught cleanly.
    Console.WriteLine($"[PASS] Validation caught expected error: {ex.Message}");
}

// ----------------------------------------------------------------------------------------
// 2. INHERITANCE & POLYMORPHISM IN ACTION
// ----------------------------------------------------------------------------------------
Console.WriteLine("\n--> [Step 2] Inheritance & Polymorphism");

// [INSTANTIATION 1 - PERISHABLE PRODUCT]:
// Base type 'Product' reference points to 'PerishableProduct' heap instance.
Product amulMilk = new PerishableProduct(
    id: "MILK-01",
    name: "Amul Taaza Milk 500ml",
    price: 33.00m,
    expiryDate: DateTime.UtcNow.AddDays(2),
    requiresRefrigeration: true
);

// [INSTANTIATION 2 - PACKAGED PRODUCT]:
// Base type 'Product' reference points to 'PackagedProduct' heap instance.
Product aashirvaadAtta = new PackagedProduct(
    id: "ATTA-05",
    name: "Aashirvaad Shudh Chakki Atta 5kg",
    price: 245.00m,
    weightInGrams: 5000
);

// [VARIABLE DECLARATION]: Delivery distance
int distanceKm = 4;

// [POLYMORPHIC INVOCATION 1]:
// [KAISE CHALTA HAI]: vtable resolves to PerishableProduct.CalculateDeliveryFee() -> 4km * 5 + 15 cold chain = 35.
decimal milkDeliveryFee = amulMilk.CalculateDeliveryFee(distanceKm);

// [POLYMORPHIC INVOCATION 2]:
// [KAISE CHALTA HAI]: vtable resolves to PackagedProduct.CalculateDeliveryFee() -> 4km * 4 + 20 heavy = 36.
decimal attaDeliveryFee = aashirvaadAtta.CalculateDeliveryFee(distanceKm);

// [OUTPUT PRINTING]:
Console.WriteLine(amulMilk.GetDisplayBadge());
Console.WriteLine($"   Distance: {distanceKm}km -> Delivery Fee (with Cold Chain): Rs. {milkDeliveryFee}");

Console.WriteLine(aashirvaadAtta.GetDisplayBadge());
Console.WriteLine($"   Distance: {distanceKm}km -> Delivery Fee (with Heavy Weight Surcharge): Rs. {attaDeliveryFee}");

// [DEBUG ASSERTIONS]:
// Self-checking automated verification
Debug.Assert(milkDeliveryFee == 35.0m, "Milk fee calculation mismatch!");
Debug.Assert(attaDeliveryFee == 36.0m, "Atta fee calculation mismatch!");
Console.WriteLine("[PASS] Polymorphic delivery fees verified perfectly.");

// ----------------------------------------------------------------------------------------
// 3. CART SERVICE & COLLECTIONS
// ----------------------------------------------------------------------------------------
Console.WriteLine("\n--> [Step 3] Cart Service & Aggregations");

// [SERVICE INSTANTIATION]:
var cart = new Cart();

// [ITEM ADDITION 1]: 2 packets of Milk (33 * 2 = 66)
cart.AddItem(amulMilk, quantity: 2);

// [ITEM ADDITION 2]: 1 bag of Atta (245 * 1 = 245)
cart.AddItem(aashirvaadAtta, quantity: 1);

// [SUBTOTAL CALCULATION]: Sum of items price (66 + 245 = 311)
decimal subtotal = cart.CalculateItemsSubtotal();

// [TOTAL DELIVERY CALCULATION]: Sum of item delivery surcharges
decimal totalDelivery = cart.CalculateTotalDeliveryFee(distanceKm);

// [GRAND TOTAL CALCULATION]: Subtotal + Delivery
decimal grandTotal = cart.CalculateGrandTotal(distanceKm);

Console.WriteLine($"Items Subtotal: Rs. {subtotal}");
Console.WriteLine($"Total Delivery Surcharge: Rs. {totalDelivery}");
Console.WriteLine($"Grand Total Payable: Rs. {grandTotal}");

// [ASSERTIONS]:
Debug.Assert(subtotal == 311.0m, "Cart subtotal assertion failed!");
Debug.Assert(grandTotal == subtotal + totalDelivery, "Grand total assertion failed!");
Console.WriteLine("[PASS] Cart calculations verified.");

// ----------------------------------------------------------------------------------------
// 4. C# RECORDS & IMMUTABILITY ('with' EXPRESSION)
// ----------------------------------------------------------------------------------------
Console.WriteLine("\n--> [Step 4] C# Records & Value-based Equality");

// [RECORD INSTANCE 1]:
var orderSummary1 = new OrderSummaryRecord("ORD-1001", "Pankaj Sharma", grandTotal, 3, "PENDING");

// [RECORD INSTANCE 2]: Identical values in a completely separate object reference
var orderSummary2 = new OrderSummaryRecord("ORD-1001", "Pankaj Sharma", grandTotal, 3, "PENDING");

// [VALUE EQUALITY EVALUATION]:
// [KAISE CHALTA HAI]: Records override '==' to compare values of every positional property instead of reference addresses.
bool areEqual = (orderSummary1 == orderSummary2);
Console.WriteLine($"Order 1: {orderSummary1}");
Console.WriteLine($"Order 2: {orderSummary2}");
Console.WriteLine($"Are both record instances equal? {areEqual} (Value Equality)");
Debug.Assert(areEqual, "Records must be equal by value!");

// ['with' NON-DESTRUCTIVE MUTATION]:
// [KAISE CHALTA HAI]: Creates a new cloned copy with ONLY the specified property changed, leaving the original intact.
var paidOrder = orderSummary1 with { PaymentStatus = "PAID" };
Console.WriteLine($"Updated Record using 'with': {paidOrder}");
Debug.Assert(paidOrder.PaymentStatus == "PAID" && paidOrder.OrderId == "ORD-1001");
Debug.Assert(orderSummary1.PaymentStatus == "PENDING", "Original record should remain immutable!");
Console.WriteLine("[PASS] Record immutability and with-expressions verified.");

// ----------------------------------------------------------------------------------------
// 5. C# GENERICS (ApiResponse<T>)
// ----------------------------------------------------------------------------------------
Console.WriteLine("\n--> [Step 5] C# Generics with ApiResponse<T>");

// [GENERIC WRAPPER CREATION]:
// ApiResponse is typed to OrderSummaryRecord
var successResponse = ApiResponse<OrderSummaryRecord>.Success(paidOrder, "Order placed successfully at KiranaHub!");

Console.WriteLine($"API Response -> Status: {(successResponse.IsSuccess ? "200 OK" : "Failed")}");
Console.WriteLine($"Message: {successResponse.Message}");
Console.WriteLine($"Payload Customer: {successResponse.Data?.CustomerName}");

// [GENERIC TYPE ASSERTION]:
Debug.Assert(successResponse.IsSuccess && successResponse.Data != null);
Console.WriteLine("[PASS] Generics type-safety verified.");

Console.WriteLine("\n==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 01-BASIC-CSHARP-AND-OOPS TESTS AND EXAMPLES PASSED!      ");
Console.WriteLine("==========================================================================");
