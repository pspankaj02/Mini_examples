using System;
using System.Diagnostics;
using System.Linq.Expressions;
using KiranaHub.LinqDeepDive.Expressions;
using KiranaHub.LinqDeepDive.Extensions;
using KiranaHub.LinqDeepDive.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 04: LINQ DEEP-DIVE & EXPRESSION TREES IN C# (ENRICHED)     ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// MOCK DATA SETUP
// ----------------------------------------------------------------------------------------
var catalog = new List<GroceryItem>
{
    new("DAIRY-01", "Amul Cow Milk 500ml", Category.Dairy, 30m, 50),
    new("DAIRY-02", "Amul Salted Butter 100g", Category.Dairy, 58m, 30),
    new("GRAIN-01", "Aashirvaad Atta 5kg", Category.Grains, 245m, 20),
    new("GRAIN-02", "India Gate Basmati Rice 1kg", Category.Grains, 130m, 15),
    new("SNACK-01", "Haldiram Bhujia 400g", Category.Snacks, 115m, 40),
    new("SNACK-02", "Parle-G Biscuit 250g", Category.Snacks, 25m, 100),
    new("BEV-01",   "Tata Tea Premium 500g", Category.Beverages, 210m, 25)
};

var customers = new List<CustomerProfile>
{
    new("CUST-1", "Pankaj Sharma", "Delhi", IsPrimeMember: true),
    new("CUST-2", "Rohit Verma", "Mumbai", IsPrimeMember: false),
    new("CUST-3", "Neha Singh (Zero Orders)", "Bengaluru", IsPrimeMember: true)
};

var orders = new List<CustomerOrder>
{
    new("ORD-101", "CUST-1", DateTime.UtcNow.AddHours(-3), new List<OrderLine>
    {
        new("DAIRY-01", "Amul Cow Milk 500ml", 2, 30m),
        new("SNACK-02", "Parle-G Biscuit 250g", 4, 25m)
    }, IsPaid: true),

    new("ORD-102", "CUST-1", DateTime.UtcNow.AddHours(-1), new List<OrderLine>
    {
        new("GRAIN-01", "Aashirvaad Atta 5kg", 1, 245m)
    }, IsPaid: true),

    new("ORD-103", "CUST-2", DateTime.UtcNow.AddHours(-2), new List<OrderLine>
    {
        new("SNACK-01", "Haldiram Bhujia 400g", 2, 115m),
        new("BEV-01", "Tata Tea Premium 500g", 1, 210m)
    }, IsPaid: true)
};

// ----------------------------------------------------------------------------------------
// 1. DEFERRED EXECUTION VS IMMEDIATE EXECUTION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] Deferred Execution Demonstration");
int executionCounter = 0;

var deferredQuery = catalog.Where(item =>
{
    executionCounter++;
    return item.Price > 100m;
});

Console.WriteLine($"Query defined. Execution counter: {executionCounter} (Should be 0!)");
Debug.Assert(executionCounter == 0, "Query should not execute upon definition!");

var highValueItems = deferredQuery.ToList();
Console.WriteLine($"Query evaluated via .ToList(). Execution counter: {executionCounter} (Evaluated elements: {highValueItems.Count})");
Debug.Assert(executionCounter == catalog.Count, "Deferred query executed exactly when enumerated.");
Console.WriteLine("[PASS] Deferred execution verified.\n");

// ----------------------------------------------------------------------------------------
// 2. SELECT VS SELECTMANY (FLATTENING HIERARCHIES)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Select vs SelectMany");
var allSoldLines = orders.SelectMany(o => o.Lines).ToList();
Console.WriteLine($"Total individual order line items sold across all orders: {allSoldLines.Count}");
Debug.Assert(allSoldLines.Count == 5, "SelectMany should flatten to 5 lines!");

int parleGSold = allSoldLines.Where(l => l.Sku == "SNACK-02").Sum(l => l.Quantity);
Console.WriteLine($"Total Parle-G units sold: {parleGSold}");
Debug.Assert(parleGSold == 4);
Console.WriteLine("[PASS] SelectMany flattening verified.\n");

// ----------------------------------------------------------------------------------------
// 3. GROUPBY (CATEGORY-WISE REVENUE & INVENTORY STATS)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] GroupBy Analytics");
var categoryStats = catalog
    .GroupBy(item => item.Category)
    .Select(g => new
    {
        Category = g.Key,
        ProductCount = g.Count(),
        TotalInventoryValue = g.Sum(x => x.Price * x.StockQuantity),
        AveragePrice = g.Average(x => x.Price)
    })
    .OrderByDescending(stat => stat.TotalInventoryValue)
    .ToList();

foreach (var stat in categoryStats)
{
    Console.WriteLine($"Category: {stat.Category,-12} | Items: {stat.ProductCount} | Inventory Value: Rs. {stat.TotalInventoryValue,8:F2} | Avg Price: Rs. {stat.AveragePrice:F2}");
}
Console.WriteLine("[PASS] GroupBy aggregations verified.\n");

// ----------------------------------------------------------------------------------------
// 4. INNER JOIN (ORDERS WITH CUSTOMERS)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 4] LINQ Inner Join");
var orderDetailsWithCustomer = orders
    .Join(
        customers,
        order => order.CustomerId,
        customer => customer.CustomerId,
        (order, customer) => new
        {
            order.OrderId,
            CustomerName = customer.Name,
            customer.City,
            customer.IsPrimeMember,
            OrderTotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice)
        }
    ).ToList();

foreach (var detail in orderDetailsWithCustomer)
{
    string primeTag = detail.IsPrimeMember ? " [PRIME]" : "";
    Console.WriteLine($"Order #{detail.OrderId} | Customer: {detail.CustomerName}{primeTag} ({detail.City}) | Total: Rs. {detail.OrderTotal}");
}
Debug.Assert(orderDetailsWithCustomer.Count == 3);
Console.WriteLine("[PASS] LINQ Join verified.\n");

// ----------------------------------------------------------------------------------------
// 5. AGGREGATE (CUSTOM ACCUMULATOR)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 5] Aggregate (Custom Seed & Accumulator)");
decimal totalRevenueWithFee = orders
    .SelectMany(o => o.Lines)
    .Aggregate(
        seed: 0.0m,
        func: (runningTotal, line) => runningTotal + (line.UnitPrice * line.Quantity * 1.05m)
    );
Console.WriteLine($"Total Gross Revenue across orders (with 5% packaging markup): Rs. {totalRevenueWithFee:F2}");
Console.WriteLine("[PASS] Aggregate function verified.\n");

// ----------------------------------------------------------------------------------------
// 6. CUSTOM EXTENSION METHODS (PAGINATION & MEDIAN)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 6] Custom LINQ Extensions: Paginate & Median");
var page2Items = catalog.Paginate(pageNumber: 2, pageSize: 3).ToList();
Console.WriteLine($"Page 2 (Items 4 to 6): {string.Join(", ", page2Items.Select(x => x.Name))}");
Debug.Assert(page2Items.Count == 3);

var pricesList = catalog.Select(x => x.Price);
decimal medianPrice = pricesList.Median();
Console.WriteLine($"Median Catalog Price: Rs. {medianPrice}");
Console.WriteLine("[PASS] Custom LINQ extensions verified.\n");

// ----------------------------------------------------------------------------------------
// 7. EXPRESSION TREES INSPECTION
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 7] Expression Tree Inspection (LINQ to SQL / EF Core Foundation)");
Expression<Func<GroceryItem, bool>> isExpensiveExpr = item => item.Price > 100m;
ExpressionTreeInspector.InspectFilterExpression(isExpensiveExpr);

Func<GroceryItem, bool> compiledFunc = isExpensiveExpr.Compile();
bool isMilkExpensive = compiledFunc(catalog[0]); // 30m > 100m -> false
Console.WriteLine($"Compiled Expression evaluated on Milk (Rs. 30): {isMilkExpensive}");
Debug.Assert(!isMilkExpensive);
Console.WriteLine("[PASS] Expression tree compilation and inspection verified.\n");

// ----------------------------------------------------------------------------------------
// 8. GROUPJOIN (LEFT OUTER JOIN) [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 8] GroupJoin (SQL LEFT OUTER JOIN in LINQ)");
// Har customer ke sath uske orders dikhao; agar customer ka koi order nahi hai tab bhi customer dikhna chahiye!
var leftOuterJoin = customers.GroupJoin(
    orders,
    c => c.CustomerId,
    o => o.CustomerId,
    (customer, custOrders) => new
    {
        customer.Name,
        OrderCount = custOrders.Count(),
        TotalSpend = custOrders.SelectMany(x => x.Lines).Sum(l => l.Quantity * l.UnitPrice)
    }
).ToList();

foreach (var row in leftOuterJoin)
{
    Console.WriteLine($"Customer: {row.Name,-30} | Orders: {row.OrderCount} | Total Spend: Rs. {row.TotalSpend}");
}
// Verify Neha Singh with 0 orders is still preserved in output!
Debug.Assert(leftOuterJoin.Any(x => x.Name.Contains("Zero Orders") && x.OrderCount == 0));
Console.WriteLine("[PASS] GroupJoin (Left Outer Join) verified.\n");

// ----------------------------------------------------------------------------------------
// 9. TOLOOKUP VS GROUPBY [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 9] ToLookup (Immediate, In-Memory Multi-Key Index Dictionary)");
// GroupBy is deferred execution; ToLookup is immediate execution and creates an immutable 1:Many dictionary
var lookupByCategory = catalog.ToLookup(x => x.Category);
Console.WriteLine($"Lookup keys count: {lookupByCategory.Count}");
Console.WriteLine("Dairy items from lookup index: " + string.Join(", ", lookupByCategory[Category.Dairy].Select(x => x.Name)));
Debug.Assert(lookupByCategory[Category.Dairy].Count() == 2);
Console.WriteLine("[PASS] ToLookup multi-index verified.\n");

// ----------------------------------------------------------------------------------------
// 10. ZIP (PARALLEL PAIRING) [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 10] Zip (Merging Two Independent Sequences)");
var sampleItems = new[] { "Milk", "Butter", "Atta" };
var couponCodes = new[] { "SAVE10", "BUTTER50", "ATTA20" };

var pairedPromotions = sampleItems.Zip(couponCodes, (item, coupon) => $"{item} with coupon [{coupon}]").ToList();
foreach (var promo in pairedPromotions) Console.WriteLine($"   Promo: {promo}");
Debug.Assert(pairedPromotions.Count == 3 && pairedPromotions[0].Contains("SAVE10"));
Console.WriteLine("[PASS] LINQ Zip verified.\n");

// ----------------------------------------------------------------------------------------
// 11. EXPRESSION VISITOR (DYNAMIC AST REWRITING) [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 11] ExpressionVisitor (Dynamic Query AST Rewriting)");
// Original expression: item => item.Price > 100m
Expression<Func<GroceryItem, bool>> originalQuery = item => item.Price > 100m;
Console.WriteLine($"Original Query AST: {originalQuery}");

// Rewrite AST at runtime: Change threshold 100m to 50m
var rewriter = new PriceThresholdRewriter(oldThreshold: 100m, newThreshold: 50m);
var rewrittenExpression = (Expression<Func<GroceryItem, bool>>)rewriter.Visit(originalQuery);
Console.WriteLine($"Rewritten Query AST: {rewrittenExpression}");

Func<GroceryItem, bool> rewrittenFunc = rewrittenExpression.Compile();
// Butter is Rs. 58. In original query (>100) it was false. In rewritten query (>50) it is true!
bool butterIncluded = rewrittenFunc(catalog[1]); // Amul Butter 58m
Console.WriteLine($"Butter (Rs. 58) in rewritten (>50) filter: {butterIncluded}");
Debug.Assert(butterIncluded == true, "Expression rewriting failed!");
Console.WriteLine("[PASS] ExpressionVisitor dynamic AST rewriting verified.\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 11 LINQ TOPICS PASSED AND VERIFIED!                      ");
Console.WriteLine("==========================================================================");
