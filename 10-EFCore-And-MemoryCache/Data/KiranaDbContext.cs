using System.Text.Json;
using KiranaHub.DataAndCaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KiranaHub.DataAndCaching.Data;

// =========================================================================================
// [TOPIC]: EF Core SaveChangesInterceptor for Automated Auditing
// [PURPOSE]: Automatically inject CreatedAtUtc and LastModifiedAtUtc timestamps into Shadow Properties.
// =========================================================================================

// [KYA KARTA HAI]: Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor se inherit karta hai.
// [KYU ZAROORI HAI]: Database me entity insert ya update hone se pehle change tracker ko intercept karna.
// [KAISE CHALTA HAI]: DbContext.SaveChangesAsync call hone par ye pipeline hook trigger hota hai before SQL execution.
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // [KYA KARTA HAI]: Async save changes hook override karta hai.
    // [KYU ZAROORI HAI]: Database me data commit hone se theek pehle audit properties populate karna.
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // [KYA KARTA HAI]: Guard check: agar context null ho toh base class method execute karke return karta hai.
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        // [KYA KARTA HAI]: Consistent UTC timestamp capture karta hai current save batch ke liye.
        // [KYU ZAROORI HAI]: Timezone discrepancies se bachna.
        var utcNow = DateTime.UtcNow;

        // [KYA KARTA HAI]: ChangeTracker me track ho rahe sabhi entities par loop chalata hai.
        // [KAISE CHALTA HAI]: ChangeTracker har tracked entity ka state (Added, Modified, Unchanged, Deleted) inspect karta hai.
        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            // [KYA KARTA HAI]: Check karta hai ki entity nayi insert ho rahi hai (State == Added).
            if (entry.State == EntityState.Added)
            {
                // [KYA KARTA HAI]: Check karta hai ki entity metadata me "CreatedAtUtc" shadow property exist karti hai ya nahi.
                if (entry.Metadata.FindProperty("CreatedAtUtc") != null)
                {
                    // [KYA KARTA HAI]: Shadow property me current UTC timestamp assign karta hai.
                    // [OUTPUT / SIDE EFFECT]: DB row me CreatedAtUtc column set hota hai.
                    entry.Property("CreatedAtUtc").CurrentValue = utcNow;
                }
                // [KYA KARTA HAI]: Check karta hai ki "LastModifiedAtUtc" shadow property exist karti hai ya nahi.
                if (entry.Metadata.FindProperty("LastModifiedAtUtc") != null)
                {
                    // [KYA KARTA HAI]: Initial modification timestamp assign karta hai.
                    entry.Property("LastModifiedAtUtc").CurrentValue = utcNow;
                }
            }
            // [KYA KARTA HAI]: Check karta hai ki existing entity update ho rahi hai (State == Modified).
            else if (entry.State == EntityState.Modified)
            {
                // [KYA KARTA HAI]: Check karta hai ki "LastModifiedAtUtc" shadow property maujood hai.
                if (entry.Metadata.FindProperty("LastModifiedAtUtc") != null)
                {
                    // [KYA KARTA HAI]: Updated timestamp assign karta hai.
                    entry.Property("LastModifiedAtUtc").CurrentValue = utcNow;
                }
            }
        }

        // [KYA KARTA HAI]: Intercepted state ko base execution flow me return karta hai.
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

// =========================================================================================
// [TOPIC]: KiranaHub EF Core DbContext with Fluent API
// [PURPOSE]: Entity configurations, Shadow Properties, Value Converters, and Compiled Queries.
// =========================================================================================

// [KYA KARTA HAI]: Entity Framework Core DbContext representation for KiranaHub database.
// [KYU ZAROORI HAI]: Database connection, change tracking, and SQL query generation bridge.
public class KiranaDbContext : DbContext
{
    // [KYA KARTA HAI]: Categories table DbSet.
    public DbSet<Category> Categories => Set<Category>();

    // [KYA KARTA HAI]: Products table DbSet.
    public DbSet<Product> Products => Set<Product>();

    // [KYA KARTA HAI]: Customers table DbSet.
    public DbSet<Customer> Customers => Set<Customer>();

    // [KYA KARTA HAI]: CustomerProfiles table DbSet.
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    // [KYA KARTA HAI]: Orders table DbSet.
    public DbSet<Order> Orders => Set<Order>();

    // [KYA KARTA HAI]: OrderItems junction table DbSet.
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // [KYA KARTA HAI]: DbContext configuration hook override karta hai.
    // [KYU ZAROORI HAI]: SQLite database provider aur audit interceptor wire-up karna.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // [KYA KARTA HAI]: Check karta hai options already configured hain ya nahi.
        if (!optionsBuilder.IsConfigured)
        {
            // [KYA KARTA HAI]: SQLite provider configure karta hai file "kiranahub.db" ke sath.
            optionsBuilder.UseSqlite("Data Source=kiranahub.db");

            // [KYA KARTA HAI]: AuditSaveChangesInterceptor ko DbContext interceptors pipeline me attach karta hai.
            // [OUTPUT / SIDE EFFECT]: Har SaveChanges par audit interceptor automatically run hoga.
            optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor());
        }
    }

    // [KYA KARTA HAI]: Fluent API model configuration hook override karta hai.
    // [KYU ZAROORI HAI]: Table mappings, foreign keys, delete behaviors, composite keys, and value converters define karna.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // [KYA KARTA HAI]: Base framework model configurations call karta hai.
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------------------------
        // 1. SHADOW PROPERTIES CONFIGURATION
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Model ke sabhi entities par loop chalata hai.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // [KYA KARTA HAI]: OrderItem junction table ko chhod kar sabhi main entities par shadow properties lagata hai.
            if (entityType.ClrType != typeof(OrderItem))
            {
                // [KYA KARTA HAI]: Shadow property "CreatedAtUtc" define karta hai on the table schema.
                // [KYU ZAROORI HAI]: Database me column hoga par C# class me property nahi hogi (clean domain models).
                entityType.AddProperty("CreatedAtUtc", typeof(DateTime));

                // [KYA KARTA HAI]: Shadow property "LastModifiedAtUtc" define karta hai.
                entityType.AddProperty("LastModifiedAtUtc", typeof(DateTime));
            }
        }

        // ---------------------------------------------------------------------------------
        // 2. ONE-TO-MANY (1:N): Category has many Products
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Category entity ke liye Fluent API configuration shuru karta hai.
        modelBuilder.Entity<Category>(entity =>
        {
            // [KYA KARTA HAI]: Primary Key set karta hai (Id).
            entity.HasKey(c => c.Id);

            // [KYA KARTA HAI]: Name column required aur max length 100 enforce karta hai.
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);

            // [KYA KARTA HAI]: 1:N relationship configure karta hai: One Category has many Products.
            entity.HasMany(c => c.Products)
                  // [KYA KARTA HAI]: Each Product points back to one Category.
                  .WithOne(p => p.Category)
                  // [KYA KARTA HAI]: Foreign Key column CategoryId map karta hai.
                  .HasForeignKey(p => p.CategoryId)
                  // [KYA KARTA HAI]: Restrict delete behavior configure karta hai (agar category me products hain toh category delete nahi ho sakti).
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------------------------
        // 3. ONE-TO-ONE (1:1): Customer has one CustomerProfile
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Customer entity ke liye Fluent API block.
        modelBuilder.Entity<Customer>(entity =>
        {
            // [KYA KARTA HAI]: Primary Key set karta hai.
            entity.HasKey(c => c.Id);

            // [KYA KARTA HAI]: Email required aur max length 150 characters.
            entity.Property(c => c.Email).IsRequired().HasMaxLength(150);

            // [KYA KARTA HAI]: Unique database index create karta hai Email column par.
            // [KYU ZAROORI HAI]: Duplicate email registration database level par prevent karna.
            entity.HasIndex(c => c.Email).IsUnique();

            // [KYA KARTA HAI]: 1:1 relationship configure karta hai with CustomerProfile.
            entity.HasOne(c => c.Profile)
                  // [KYA KARTA HAI]: Profile points back to single Customer.
                  .WithOne(p => p.Customer)
                  // [KYA KARTA HAI]: Foreign key CustomerId CustomerProfile table me store hogi.
                  .HasForeignKey<CustomerProfile>(p => p.CustomerId)
                  // [KYA KARTA HAI]: Cascade delete behavior (Customer delete hone par profile automatically delete ho jaye).
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------------------------
        // 4. VALUE CONVERTER: DeliveryAddress record <-> JSON string column
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: CustomerProfile entity configuration.
        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            // [KYA KARTA HAI]: Primary Key set karta hai.
            entity.HasKey(cp => cp.Id);

            // [KYA KARTA HAI]: PreferredAddress complex record par EF Core ValueConverter lagata hai.
            // [KYU ZAROORI HAI]: C# record object ko DB me JSON text column me store karna aur read par wapas deserialize karna.
            entity.Property(cp => cp.PreferredAddress)
                  .HasConversion(
                      // [KYA KARTA HAI]: Save conversion: DeliveryAddress object ko JSON text me serialize karta hai.
                      addr => JsonSerializer.Serialize(addr, (JsonSerializerOptions?)null),
                      // [KYA KARTA HAI]: Read conversion: Database ke JSON text ko wapas C# DeliveryAddress record me parse karta hai.
                      json => JsonSerializer.Deserialize<DeliveryAddress>(json, (JsonSerializerOptions?)null) ?? new DeliveryAddress()
                  );
        });

        // ---------------------------------------------------------------------------------
        // 5. MANY-TO-MANY (M:N): Order <-> Product via OrderItem junction
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: OrderItem junction entity configuration.
        modelBuilder.Entity<OrderItem>(entity =>
        {
            // [KYA KARTA HAI]: Composite Primary Key define karta hai (OrderId + ProductId).
            // [KYU ZAROORI HAI]: Single order me same product ek hi bar junction row banaye.
            entity.HasKey(oi => new { oi.OrderId, oi.ProductId });

            // [KYA KARTA HAI]: Many OrderItems point to one Order.
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId);

            // [KYA KARTA HAI]: Many OrderItems point to one Product.
            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId);
        });
    }

    // =====================================================================================
    // PRE-COMPILED QUERY (EF.CompileAsyncQuery)
    // =====================================================================================

    // [KYA KARTA HAI]: Expression tree ko SQL me ek bar compile karke memory delegate me cache karta hai.
    // [KYU ZAROORI HAI]: High-concurrency throughput me har request par LINQ to SQL parsing CPU overhead eliminate karna.
    // [KAISE CHALTA HAI]: EF.CompileAsyncQuery delegate banata hai jo direct pre-generated SQL execute karta hai.
    public static readonly Func<KiranaDbContext, string, Task<Product?>> GetProductBySkuCompiled =
        EF.CompileAsyncQuery((KiranaDbContext ctx, string sku) =>
            ctx.Products
               .Include(p => p.Category)
               .AsNoTracking()
               .FirstOrDefault(p => p.Sku == sku));
}
