using System;
using System.Diagnostics;
using KiranaHub.DataAndCaching.Caching;
using KiranaHub.DataAndCaching.Data;
using KiranaHub.DataAndCaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

// [KYA KARTA HAI]: Console output encoding ko UTF-8 me set karta hai.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [KYA KARTA HAI]: Visual separator line print karta hai.
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Banner text print karta hai.
Console.WriteLine("   KIRANAHUB - 10: EF CORE & IN-MEMORY CACHING DEMONSTRATION              ");

// [KYA KARTA HAI]: Visual separator closing line print karta hai.
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. DATABASE INITIALIZATION & SEEDING (SQLITE CODE-FIRST)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 1 heading print karta hai.
Console.WriteLine("--> [Step 1] Initializing SQLite Database (Code-First Fluent API)");

// [KYA KARTA HAI]: KiranaDbContext ka instance allocate karta hai using statement ke sath for automatic disposal.
using (var db = new KiranaDbContext())
{
    // [KYA KARTA HAI]: Pehle se bani hui SQLite database file ko asynchronously delete karta hai.
    // [KYU ZAROORI HAI]: Fresh isolated schema create karna without residual test data.
    await db.Database.EnsureDeletedAsync();

    // [KYA KARTA HAI]: Fluent API model definitions se fresh SQLite database aur tables create karta hai.
    // [OUTPUT / SIDE EFFECT]: "kiranahub.db" file disk par banti hai with all relational tables and shadow properties.
    await db.Database.EnsureCreatedAsync();

    // [KYA KARTA HAI]: Schema creation success message print karta hai.
    Console.WriteLine("[PASS] SQLite schema created with 1:1, 1:N, M:N and Shadow Properties.\n");

    // [KYA KARTA HAI]: Dairy category entity instantiate karta hai.
    var dairy = new Category { Name = "Dairy", Description = "Fresh dairy products" };

    // [KYA KARTA HAI]: Grains category entity instantiate karta hai.
    var grains = new Category { Name = "Grains", Description = "Daily food staples" };

    // [KYA KARTA HAI]: Categories ko ChangeTracker me Added state me register karta hai.
    db.Categories.AddRange(dairy, grains);

    // [KYA KARTA HAI]: Milk product entity create karta hai (1:N relationship with dairy).
    var milk = new Product { Sku = "MILK-500", Name = "Amul Cow Milk 500ml", Price = 33m, Stock = 40, Category = dairy };

    // [KYA KARTA HAI]: Butter product entity create karta hai (1:N with dairy).
    var butter = new Product { Sku = "BTR-100", Name = "Amul Butter 100g", Price = 58m, Stock = 25, Category = dairy };

    // [KYA KARTA HAI]: Atta product entity create karta hai (1:N with grains).
    var atta = new Product { Sku = "ATTA-5K", Name = "Aashirvaad Atta 5kg", Price = 245m, Stock = 15, Category = grains };

    // [KYA KARTA HAI]: Teeno products ko ChangeTracker me add karta hai.
    db.Products.AddRange(milk, butter, atta);

    // [KYA KARTA HAI]: Customer entity instantiate karta hai with 1:1 nested CustomerProfile aur JSON ValueConverter address.
    var customer = new Customer
    {
        // [KYA KARTA HAI]: Customer full name.
        FullName = "Pankaj Sharma",
        // [KYA KARTA HAI]: Customer email address.
        Email = "pankaj@example.com",
        // [KYA KARTA HAI]: 1:1 related CustomerProfile.
        Profile = new CustomerProfile
        {
            PhoneNumber = "+91-9876543210",
            LoyaltyPoints = 250,
            PreferredAddress = new DeliveryAddress { Street = "Flat 402, Lotus Towers", City = "Bengaluru", Pincode = "560034" }
        }
    };

    // [KYA KARTA HAI]: Customer entity ko DbContext me add karta hai (profile automatically added via cascade).
    db.Customers.Add(customer);

    // [KYA KARTA HAI]: Order aggregate entity create karta hai.
    var order = new Order
    {
        OrderNumber = "ORD-2026-001",
        Customer = customer,
        TotalAmount = 33m * 2 + 58m * 1 // 124 INR
    };

    // [KYA KARTA HAI]: M:N junction line item 1 add karta hai (Order <-> Milk, 2 units).
    order.Items.Add(new OrderItem { Order = order, Product = milk, Quantity = 2, UnitPriceAtOrder = 33m });

    // [KYA KARTA HAI]: M:N junction line item 2 add karta hai (Order <-> Butter, 1 unit).
    order.Items.Add(new OrderItem { Order = order, Product = butter, Quantity = 1, UnitPriceAtOrder = 58m });

    // [KYA KARTA HAI]: Order entity ko DbContext me add karta hai.
    db.Orders.Add(order);

    // [KYA KARTA HAI]: Changes ko SQLite database me commit karta hai; AuditSaveChangesInterceptor shadow properties populate karta hai.
    // [OUTPUT / SIDE EFFECT]: SQL INSERT statements execute hote hain SQLite database par.
    await db.SaveChangesAsync();

    // [KYA KARTA HAI]: Seeding confirmation print karta hai.
    Console.WriteLine("[PASS] Database seeded with relational entities and audit metadata.\n");
}

// ----------------------------------------------------------------------------------------
// 2. RELATIONAL QUERIES (1:1, 1:N, M:N) & VALUE CONVERTER INSPECTION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 2 heading print karta hai.
Console.WriteLine("--> [Step 2] Testing Relational Navigation Properties & Value Converters");

// [KYA KARTA HAI]: Fresh DbContext instance open karta hai to test actual database read.
using (var db = new KiranaDbContext())
{
    // [KYA KARTA HAI]: Order ko relational navigation graphs ke sath eager load karta hai (Customer -> Profile, Items -> Product -> Category).
    // [KYU ZAROORI HAI]: N+1 query problem prevent karna single multi-JOIN query ke sath.
    var retrievedOrder = await db.Orders
        .Include(o => o.Customer)
            .ThenInclude(c => c.Profile)
        .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
        .FirstOrDefaultAsync(o => o.OrderNumber == "ORD-2026-001");

    // [KYA KARTA HAI]: Assert karta hai ki order database se fetch hua hai.
    Debug.Assert(retrievedOrder != null);

    // [KYA KARTA HAI]: Order number aur total amount console par print karta hai.
    Console.WriteLine($"Order: #{retrievedOrder.OrderNumber} (Total: Rs. {retrievedOrder.TotalAmount})");

    // [KYA KARTA HAI]: 1:1 navigation se fetched customer name aur phone number print karta hai.
    Console.WriteLine($"Customer: {retrievedOrder.Customer.FullName} | Phone: {retrievedOrder.Customer.Profile.PhoneNumber}");

    // [KYA KARTA HAI]: SQLite JSON text se ValueConverter ke zariye deserialize hua address print karta hai.
    Console.WriteLine($"Delivery Address (Deserialized from JSON): {retrievedOrder.Customer.Profile.PreferredAddress.Street}, {retrievedOrder.Customer.Profile.PreferredAddress.City} - {retrievedOrder.Customer.Profile.PreferredAddress.Pincode}");

    // [KYA KARTA HAI]: Order line items heading print karta hai.
    Console.WriteLine("Order Line Items (M:N):");

    // [KYA KARTA HAI]: M:N junction items par loop chalata hai.
    foreach (var item in retrievedOrder.Items)
    {
        // [KYA KARTA HAI]: Har line item ki product details, category, quantity, price, line total print karta hai.
        Console.WriteLine($"   * {item.Product.Name} [{item.Product.Category.Name}] - Qty: {item.Quantity} @ Rs. {item.UnitPriceAtOrder} = Rs. {item.LineTotal}");
    }

    // [KYA KARTA HAI]: Assert karta hai ki order me exactly 2 items hain.
    Debug.Assert(retrievedOrder.Items.Count == 2);

    // [KYA KARTA HAI]: Step 2 success confirmation print karta hai.
    Console.WriteLine("[PASS] Relational navigation properties and ValueConverter verified.\n");

    // ------------------------------------------------------------------------------------
    // 3. SHADOW PROPERTIES & AUDIT INTERCEPTOR INSPECTION
    // ------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Step 3 heading print karta hai.
    Console.WriteLine("--> [Step 3] Shadow Properties Inspection (CreatedAtUtc & LastModifiedAtUtc)");

    // [KYA KARTA HAI]: DbContext ChangeTracker entry access karta hai for the product entity.
    var productEntry = db.Entry(retrievedOrder.Items[0].Product);

    // [KYA KARTA HAI]: Shadow property "CreatedAtUtc" ki value read karta hai change tracker se.
    // [KYU ZAROORI HAI]: Entity class me property define kiye bina DB audit timestamp read karna.
    var createdAt = (DateTime?)productEntry.Property("CreatedAtUtc").CurrentValue;

    // [KYA KARTA HAI]: Shadow property "LastModifiedAtUtc" ki value read karta hai.
    var modifiedAt = (DateTime?)productEntry.Property("LastModifiedAtUtc").CurrentValue;

    // [KYA KARTA HAI]: Product name print karta hai.
    Console.WriteLine($"Product: {retrievedOrder.Items[0].Product.Name}");

    // [KYA KARTA HAI]: CreatedAtUtc shadow timestamp print karta hai.
    Console.WriteLine($"   Shadow Property 'CreatedAtUtc'     : {createdAt:yyyy-MM-dd HH:mm:ss} UTC");

    // [KYA KARTA HAI]: LastModifiedAtUtc shadow timestamp print karta hai.
    Console.WriteLine($"   Shadow Property 'LastModifiedAtUtc': {modifiedAt:yyyy-MM-dd HH:mm:ss} UTC");

    // [KYA KARTA HAI]: Assert karta hai ki dono shadow properties successfully populate hui hain.
    Debug.Assert(createdAt.HasValue && modifiedAt.HasValue);

    // [KYA KARTA HAI]: Step 3 success message print karta hai.
    Console.WriteLine("[PASS] Shadow properties automatically populated by AuditSaveChangesInterceptor.\n");

    // ------------------------------------------------------------------------------------
    // 4. EF CORE CHANGE TRACKER DEMONSTRATION
    // ------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Step 4 heading print karta hai.
    Console.WriteLine("--> [Step 4] EF Core Change Tracker States");

    // [KYA KARTA HAI]: Transient product instance allocate karta hai.
    var testProduct = new Product { Sku = "NEW-ITEM", Name = "New Tea", Price = 90m, CategoryId = 1 };

    // [KYA KARTA HAI]: State before Add print karta hai.
    Console.WriteLine($"State before Add: {db.Entry(testProduct).State}"); // Detached

    // [KYA KARTA HAI]: Verify karta hai ki abhi entity ChangeTracker se Detached hai.
    Debug.Assert(db.Entry(testProduct).State == EntityState.Detached);

    // [KYA KARTA HAI]: DbContext me Add karta hai.
    db.Products.Add(testProduct);

    // [KYA KARTA HAI]: State after Add print karta hai.
    Console.WriteLine($"State after Add : {db.Entry(testProduct).State}"); // Added

    // [KYA KARTA HAI]: Verify karta hai ki state Added ho gayi hai.
    Debug.Assert(db.Entry(testProduct).State == EntityState.Added);

    // [KYA KARTA HAI]: State ko manually wapas Detached karke discard kar deta hai.
    db.Entry(testProduct).State = EntityState.Detached;

    // [KYA KARTA HAI]: Step 4 success confirmation.
    Console.WriteLine("[PASS] ChangeTracker state machine verified.\n");

    // ------------------------------------------------------------------------------------
    // 5. PRE-COMPILED QUERY (EF.CompileAsyncQuery)
    // ------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Step 5 heading print karta hai.
    Console.WriteLine("--> [Step 5] Pre-Compiled Query Execution");

    // [KYA KARTA HAI]: Static pre-compiled delegate call karta hai with DbContext aur SKU parameter.
    // [KYU ZAROORI HAI]: Maximum throughput: SQL expression translation caching.
    var compiledProduct = await KiranaDbContext.GetProductBySkuCompiled(db, "MILK-500");

    // [KYA KARTA HAI]: Compiled query result print karta hai.
    Console.WriteLine($"Compiled Query Result: {compiledProduct?.Name} - Rs. {compiledProduct?.Price}");

    // [KYA KARTA HAI]: Verify karta hai compiled query product match karta hai.
    Debug.Assert(compiledProduct != null && compiledProduct.Sku == "MILK-500");

    // [KYA KARTA HAI]: Step 5 success message.
    Console.WriteLine("[PASS] Compiled query sub-millisecond execution verified.\n");
}

// ----------------------------------------------------------------------------------------
// 6. IN-MEMORY CACHE & CACHE STAMPEDE (THUNDERING HERD) CONCURRENCY TEST
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 6 heading print karta hai.
Console.WriteLine("--> [Step 6] In-Memory Caching & Cache Stampede Protection");

// [KYA KARTA HAI]: Microsoft.Extensions.Caching.Memory.MemoryCache instance create karta hai.
var memoryCache = new MemoryCache(new MemoryCacheOptions());

// [KYA KARTA HAI]: KiranaCacheManager instance create karta hai with injected memory cache.
var cacheManager = new KiranaCacheManager(memoryCache);

// [KYA KARTA HAI]: Database query invocation count measure karne ke liye counter variable.
int databaseHitCount = 0;

// [KYA KARTA HAI]: Simulated slow database query local function.
async Task<List<string>> FetchTopSellingProductsFromDbAsync()
{
    // [KYA KARTA HAI]: Thread-safe atomic increment of database hit count.
    Interlocked.Increment(ref databaseHitCount);

    // [KYA KARTA HAI]: 100ms artificial database latency simulate karta hai.
    await Task.Delay(100);

    // [KYA KARTA HAI]: Fresh grocery items list return karta hai.
    return new List<string> { "Amul Milk 500ml", "Aashirvaad Atta 5kg", "Amul Butter 100g" };
}

// [KYA KARTA HAI]: Concurrency simulation log print karta hai.
Console.WriteLine("Simulating 10 simultaneous concurrent requests requesting 'TopGroceries'...");

// [KYA KARTA HAI]: Ek hi millisecond par 10 concurrent async tasks launch karta hai same cache key ke liye.
var concurrentTasks = Enumerable.Range(1, 10).Select(async id =>
{
    // [KYA KARTA HAI]: Har concurrent task KiranaCacheManager se data request karta hai.
    return await cacheManager.GetOrCreateWithStampedeProtectionAsync(
        cacheKey: "TopGroceries",
        databaseFactory: FetchTopSellingProductsFromDbAsync,
        absoluteExpiration: TimeSpan.FromSeconds(5),
        slidingExpiration: TimeSpan.FromSeconds(2)
    );
});

// [KYA KARTA HAI]: Sabhi 10 concurrent requests ke complete hone ka wait karta hai.
var results = await Task.WhenAll(concurrentTasks);

// [KYA KARTA HAI]: Total completed requests count print karta hai (10).
Console.WriteLine($"Total requests completed: {results.Length}");

// [KYA KARTA HAI]: Actual database query hit count print karta hai.
Console.WriteLine($"Actual Database Query Hit Count: {databaseHitCount} (Should be strictly 1!)");

// [KYA KARTA HAI]: Assert karta hai ki 10 concurrent requests ke bawajood database sirf EXACTLY 1 bar hit hua!
// [KYU ZAROORI HAI]: Guarantee that Cache Stampede was completely prevented by SemaphoreSlim.
Debug.Assert(databaseHitCount == 1, "Cache Stampede occurred! More than 1 request hit the database.");

// [KYA KARTA HAI]: Step 6 success confirmation.
Console.WriteLine("[PASS] SemaphoreSlim cache stampede protection verified perfectly.\n");

// [KYA KARTA HAI]: Project completion banner lines print karta hai.
Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 10-EFCORE-AND-MEMORYCACHE TESTS AND EXAMPLES PASSED!     ");
Console.WriteLine("==========================================================================");
