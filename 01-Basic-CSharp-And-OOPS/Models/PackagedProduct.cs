using KiranaHub.BasicsAndOops.Interfaces;

namespace KiranaHub.BasicsAndOops.Models;

// =========================================================================================
// [CLASS PURPOSE]:
// Packaged items (Atta, Dal, Biscuit, Oil) ko represent karti hai.
// OOPS Inheritance, Polymorphism, Multiple Interface implementation (`IDeliverable`, `IDiscountable`) demonstrate karti hai.
// =========================================================================================
public class PackagedProduct : Product, IDeliverable, IDiscountable
{
    // [PROPERTY DECLARATION]:
    // [KYA KARTA HAI]: Product ka physical weight grams me store karta hai (e.g. 5000g for 5kg Atta).
    // [KYU ZAROORI HAI]: Heavy weight cargo delivery fees calculate karne ke liye.
    public int WeightInGrams { get; init; }

    // [CONSTRUCTOR]:
    // [KYA KARTA HAI]: Base constructor ko call karta hai aur weight validation execute karta hai.
    public PackagedProduct(string id, string name, decimal price, int weightInGrams)
        : base(id, name, price)
    {
        // [DEFENSIVE VALIDATION]:
        // [KYA KARTA HAI]: Check karta hai ki weight 0 ya negative na ho.
        if (weightInGrams <= 0)
            throw new ArgumentException("Weight must be greater than zero grams.", nameof(weightInGrams));

        // [INITIALIZATION STATEMENT]:
        WeightInGrams = weightInGrams;
    }

    // [POLYMORPHIC OVERRIDE]:
    // [KYA KARTA HAI]: Weight aur distance ke combination par delivery fee compute karta hai.
    public override decimal CalculateDeliveryFee(int distanceInKm)
    {
        // [LINE 1: BASE MILEAGE]:
        // Rs. 4 per kilometer standard charge.
        decimal baseRate = distanceInKm * 4.0m;

        // [LINE 2: HEAVY WEIGHT SURCHARGE CONDITION]:
        // Agar package ka weight 2kg (2000g) se zyada hai toh rider ko lifting bonus lagta hai.
        if (WeightInGrams > 2000)
        {
            // [SURCHARGE ADDITION]:
            baseRate += 20.0m;
        }

        // [RETURN STATEMENT]:
        // Final calculated delivery fee return hoti hai.
        return baseRate;
    }

    // [INTERFACE 1 IMPLEMENTATION]: IDeliverable
    public string GenerateShippingLabel()
    {
        // Barcode label printer ke liye string output emit karta hai.
        return $"STANDARD CARGO: Item {Name} - Weight: {WeightInGrams}g";
    }

    // [INTERFACE 2 IMPLEMENTATION]: IDiscountable
    // [KYA KARTA HAI]: Discount percentage apply karke final discounted price compute karta hai.
    public decimal ApplyDiscount(decimal percentage)
    {
        // [VALIDATION LINE]:
        // Percentage must be between 0 and 100.
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Discount must be between 0 and 100.");

        // [MATHEMATICAL COMPUTATION]:
        decimal discountAmount = (Price * percentage) / 100m;

        // [RETURN DISCOUNTED PRICE]:
        return Price - discountAmount;
    }
}
