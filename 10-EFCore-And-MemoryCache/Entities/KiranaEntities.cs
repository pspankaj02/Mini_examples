namespace KiranaHub.DataAndCaching.Entities;

// =========================================================================================
// [TOPIC]: Entity Framework Core Relational Domain Entities
// [PURPOSE]: 1:1, 1:N, M:N relationships, Value Objects, Junction Payload, and Navigation Properties.
// =========================================================================================

// [KYA KARTA HAI]: Grocery Category table entity (1:N parent entity).
// [KYU ZAROORI HAI]: Products ko categorize karne ke liye relational table (e.g. Dairy, Grains, Snacks).
public class Category
{
    // [KYA KARTA HAI]: Primary Key for Category entity.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Category display name (e.g., "Dairy").
    public string Name { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Category description text.
    public string Description { get; set; } = string.Empty;

    // [KYA KARTA HAI]: 1:N Navigation collection to child Products.
    // [KYU ZAROORI HAI]: EF Core me category.Products query ya cascade load karne ke liye.
    // [KAISE CHALTA HAI]: Relational JOIN ya separate SELECT ke zariye EF Core lazy/eager load karta hai.
    public List<Product> Products { get; set; } = new();
}

// [KYA KARTA HAI]: Product table entity (1:N child, M:N participant).
// [KYU ZAROORI HAI]: Central grocery product record in database.
public class Product
{
    // [KYA KARTA HAI]: Primary key.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Unique SKU string.
    public string Sku { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Product name.
    public string Name { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Unit price stored as high-precision decimal.
    public decimal Price { get; set; }

    // [KYA KARTA HAI]: Available stock count.
    public int Stock { get; set; }

    // [KYA KARTA HAI]: Foreign Key referencing Category table.
    // [KYU ZAROORI HAI]: Relational integrity: har product kisi category se belong karta hai.
    public int CategoryId { get; set; }

    // [KYA KARTA HAI]: Navigation property pointing to parent Category object.
    public Category Category { get; set; } = null!;

    // [KYA KARTA HAI]: M:N navigation collection to OrderItem junction entities.
    // [KYU ZAROORI HAI]: Ek product multiple orders me appear ho sakta hai.
    public List<OrderItem> OrderItems { get; set; } = new();
}

// [KYA KARTA HAI]: Customer table entity (1:1 and 1:N parent).
// [KYU ZAROORI HAI]: Customer account information maintain karna.
public class Customer
{
    // [KYA KARTA HAI]: Primary Key.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Unique Email address.
    public string Email { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Customer full name.
    public string FullName { get; set; } = string.Empty;

    // [KYA KARTA HAI]: 1:1 Navigation property to CustomerProfile.
    // [KYU ZAROORI HAI]: Har customer ka exactly ek detailed profile hota hai.
    public CustomerProfile Profile { get; set; } = null!;

    // [KYA KARTA HAI]: 1:N Navigation property to Customer's Orders list.
    public List<Order> Orders { get; set; } = new();
}

// [KYA KARTA HAI]: CustomerProfile table entity (1:1 dependent entity).
// [KYU ZAROORI HAI]: Extended customer details (phone, loyalty, address) ko cleanly segregate karna.
public class CustomerProfile
{
    // [KYA KARTA HAI]: Primary Key.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Phone number.
    public string PhoneNumber { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Customer reward points integer.
    public int LoyaltyPoints { get; set; }

    // [KYA KARTA HAI]: Structured DeliveryAddress value object (stored as JSON text via EF Core ValueConverter).
    // [KYU ZAROORI HAI]: Relational table me nested complex object store karna without creating separate table.
    public DeliveryAddress PreferredAddress { get; set; } = new();

    // [KYA KARTA HAI]: 1:1 Foreign Key pointing back to Customer table.
    public int CustomerId { get; set; }

    // [KYA KARTA HAI]: Navigation property to principal Customer entity.
    public Customer Customer { get; set; } = null!;
}

// [KYA KARTA HAI]: Immutable Value Object representing delivery street, city, pincode.
// [KYU ZAROORI HAI]: Value equality semantics (address with same street/city/pincode is identical).
public record DeliveryAddress
{
    // [KYA KARTA HAI]: Street line.
    public string Street { get; set; } = "104 MG Road";

    // [KYA KARTA HAI]: City name.
    public string City { get; set; } = "Bengaluru";

    // [KYA KARTA HAI]: Postal code.
    public string Pincode { get; set; } = "560001";
}

// [KYA KARTA HAI]: Order table entity.
// [KYU ZAROORI HAI]: Completed grocery order header record.
public class Order
{
    // [KYA KARTA HAI]: Primary Key.
    public int Id { get; set; }

    // [KYA KARTA HAI]: Human readable Order number (e.g. ORD-20261114-001).
    public string OrderNumber { get; set; } = string.Empty;

    // [KYA KARTA HAI]: UTC timestamp of order placement.
    public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;

    // [KYA KARTA HAI]: Total financial value of order.
    public decimal TotalAmount { get; set; }

    // [KYA KARTA HAI]: Foreign Key to Customer.
    public int CustomerId { get; set; }

    // [KYA KARTA HAI]: Navigation property to Customer.
    public Customer Customer { get; set; } = null!;

    // [KYA KARTA HAI]: Navigation collection of order line items (M:N junction).
    public List<OrderItem> Items { get; set; } = new();
}

// [KYA KARTA HAI]: Explicit Junction Table Entity for Many-to-Many relationship (Order <-> Product).
// [KYU ZAROORI HAI]: Relationship attributes (Quantity, UnitPriceAtOrder) store karne ke liye explicit junction entity zaroori hai.
public class OrderItem
{
    // [KYA KARTA HAI]: Composite Primary Key component 1 & Foreign Key to Order.
    public int OrderId { get; set; }

    // [KYA KARTA HAI]: Navigation property to Order.
    public Order Order { get; set; } = null!;

    // [KYA KARTA HAI]: Composite Primary Key component 2 & Foreign Key to Product.
    public int ProductId { get; set; }

    // [KYA KARTA HAI]: Navigation property to Product.
    public Product Product { get; set; } = null!;

    // [KYA KARTA HAI]: Ordered item quantity.
    public int Quantity { get; set; }

    // [KYA KARTA HAI]: Snapshot of product unit price at the time order was placed.
    // [KYU ZAROORI HAI]: Future product price changes should never modify past order receipts!
    public decimal UnitPriceAtOrder { get; set; }

    // [KYA KARTA HAI]: Computed property: Quantity * UnitPriceAtOrder.
    public decimal LineTotal => Quantity * UnitPriceAtOrder;
}
