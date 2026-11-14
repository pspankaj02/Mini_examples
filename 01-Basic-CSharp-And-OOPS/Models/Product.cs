namespace KiranaHub.BasicsAndOops.Models;

// =========================================================================================
// [CLASS PURPOSE]:
// Abstract base class jo KiranaHub ke har grocery product ka core blueprint define karti hai.
// Isme OOPS ka Encapsulation (data hiding) aur Abstraction dono implement kiye gaye hain.
// =========================================================================================
public abstract class Product
{
    // [FIELD DECLARATION]:
    // [KYA KARTA HAI]: Private backing field jo product ka price store karta hai.
    // [KYU ZAROORI HAI]: Encapsulation principle - bahar ka koi bhi caller price ko direct corrupt na kar sake.
    // [KAISE CHALTA HAI]: CLR heap par har object instance ke private memory layout me store hota hai.
    private decimal _price;

    // [PROPERTY DECLARATION]:
    // [KYA KARTA HAI]: Unique string identifier (e.g. "MILK-01").
    // [KYU ZAROORI HAI]: Products ko uniquely track karne ke liye.
    // [KAISE CHALTA HAI]: 'init' keyword ensure karta hai ki object creation ke baad yeh property immutable ban jaye.
    public string Id { get; init; }

    // [PROPERTY DECLARATION]:
    // [KYA KARTA HAI]: Product ka readable display name (e.g. "Amul Milk").
    // [KYU ZAROORI HAI]: UI display aur receipts me customer ko naam dikhane ke liye.
    public string Name { get; set; }

    // [ENCAPSULATED PROPERTY WITH VALIDATION]:
    public decimal Price
    {
        // [GETTER LINE]:
        // [KYA KARTA HAI]: Private field `_price` ki value caller ko return karta hai.
        get => _price;

        // [SETTER BLOCK]:
        // [KYA KARTA HAI]: New value set karne se pehle validation rule execute karta hai.
        set
        {
            // [VALIDATION STATEMENT]:
            // [KYU ZAROORI HAI]: Grocery store me koi item 0 ya negative price ka nahi ho sakta.
            if (value <= 0)
            {
                // [EXCEPTION THROW]:
                // [OUTPUT]: ArgumentException trigger hogi agar koi galat price set karega.
                throw new ArgumentException("Product price must be greater than zero!", nameof(value));
            }
            // [ASSIGNMENT LINE]:
            // [KAISE CHALTA HAI]: Valid value private backing field me assign ho jati hai.
            _price = value;
        }
    }

    // [CONSTRUCTOR]:
    // [KYA KARTA HAI]: Object initialization ke waqt mandatory fields set karta hai.
    // [KAISE CHALTA HAI]: 'protected' access modifier ensure karta hai ki sirf derived classes hi ise call kar sakein.
    protected Product(string id, string name, decimal price)
    {
        // [DEFENSIVE CHECK]:
        // [KYA KARTA HAI]: Name empty ya whitespace hone par turant error throw karta hai.
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));

        // [INITIALIZATION LINE 1]:
        Id = id;

        // [INITIALIZATION LINE 2]:
        Name = name;

        // [INITIALIZATION LINE 3]:
        // [NOTE]: Yeh line direct setter call karegi, jisse property setter validation run hogi!
        Price = price;
    }

    // [ABSTRACT METHOD DECLARATION]:
    // [KYA KARTA HAI]: Delivery fee calculation contract define karta hai bina implementation ke.
    // [KYU ZAROORI HAI]: Polymorphism - har category (Perishable vs Heavy Packaged) ka calculation rule alag hota hai.
    // [KAISE CHALTA HAI]: Child class ko vtable me iska concrete override provide karna mandatory hai.
    public abstract decimal CalculateDeliveryFee(int distanceInKm);

    // [VIRTUAL METHOD DECLARATION]:
    // [KYA KARTA HAI]: Default display badge return karta hai jise child class chahe toh override kar sakti hai.
    // [VARIATIONS TO TRY]: Isme currency symbol parameter pass karke multi-currency support add karein.
    public virtual string GetDisplayBadge()
    {
        // [RETURN STATEMENT]: Standard formatted string with string interpolation.
        return $"[STANDARD GROCERY] {Name} - Rs. {Price}";
    }
}
