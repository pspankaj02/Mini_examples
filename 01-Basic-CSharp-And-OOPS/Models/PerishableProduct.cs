using KiranaHub.BasicsAndOops.Interfaces;

namespace KiranaHub.BasicsAndOops.Models;

// =========================================================================================
// [CLASS PURPOSE]:
// Derived class inheriting from `Product` base class and implementing `IDeliverable` interface.
// Milk, curd, paneer aur vegetables jaise perishable items ko represent karti hai.
// =========================================================================================
public class PerishableProduct : Product, IDeliverable
{
    // [PROPERTY DECLARATION]:
    // [KYA KARTA HAI]: Expiry date track karta hai.
    // [KYU ZAROORI HAI]: Perishable inventory control aur customer safety ke liye.
    // [KAISE CHALTA HAI]: 'init' property object bante waqt assign hoti hai aur baad me immutable rehti hai.
    public DateTime ExpiryDate { get; init; }

    // [PROPERTY DECLARATION]:
    // [KYA KARTA HAI]: Flag jo batata hai ki item ko ice box / cold chain delivery chahiye ya nahi.
    public bool RequiresRefrigeration { get; init; }

    // [CONSTRUCTOR]:
    // [KYA KARTA HAI]: Child class constructor jo base class constructor ko 'base(...)' ke zariye call karta hai.
    // [KAISE CHALTA HAI]: Pehle base class ke fields initialize hote hain, fir child class ki body execute hoti hai.
    public PerishableProduct(string id, string name, decimal price, DateTime expiryDate, bool requiresRefrigeration)
        : base(id, name, price)
    {
        // [INITIALIZATION 1]:
        ExpiryDate = expiryDate;

        // [INITIALIZATION 2]:
        RequiresRefrigeration = requiresRefrigeration;
    }

    // [POLYMORPHIC METHOD OVERRIDE]:
    // [KYA KARTA HAI]: Base class ke abstract `CalculateDeliveryFee` method ko concrete business logic deta hai.
    // [KAISE CHALTA HAI]: Vtable dispatch runtime par base pointer `Product p` hone par bhi is overridden method ko call karta hai.
    public override decimal CalculateDeliveryFee(int distanceInKm)
    {
        // [CALCULATION LINE 1]:
        // [KYA KARTA HAI]: Base rate calculate karta hai: Rs. 5 per kilometer.
        decimal baseFee = distanceInKm * 5.0m;

        // [CONDITIONAL CHECK]:
        // [KYA KARTA HAI]: Check karta hai ki refrigeration cold chain required hai ya nahi.
        if (RequiresRefrigeration)
        {
            // [RETURN WITH SURCHARGE]:
            // [KYU ZAROORI HAI]: Cold storage van / dry ice packaging ka extra Rs. 15 surcharge add karna.
            return baseFee + 15.0m;
        }

        // [DEFAULT RETURN]:
        // Normal perishable delivery bina extra cold charge ke return karta hai.
        return baseFee;
    }

    // [VIRTUAL METHOD OVERRIDE]:
    // [KYA KARTA HAI]: Display badge string customize karta hai with expiry date and cold tag.
    public override string GetDisplayBadge()
    {
        // [TERNARY EXPRESSION]:
        // [KYA KARTA HAI]: Tag string create karta hai conditional logic se.
        string coldTag = RequiresRefrigeration ? " [COLD CHAIN]" : "";

        // [INTERPOLATED STRING RETURN]:
        // [FORMAT SPECIFIER]: `dd-MMM-yyyy` format date ko human-readable banata hai (e.g. 15-Nov-2026).
        return $"[PERISHABLE]{coldTag} {Name} (Expires: {ExpiryDate:dd-MMM-yyyy}) - Rs. {Price}";
    }

    // [INTERFACE IMPLEMENTATION]:
    // [KYA KARTA HAI]: `IDeliverable` interface ka `GenerateShippingLabel` contract fulfill karta hai.
    public string GenerateShippingLabel()
    {
        // [LABEL STRING RETURN]:
        // Warehouse dispatch team ke liye priority label text generate karta hai.
        return $"RUSH DISPATCH: Item {Name} (Id: {Id}) - Perishable Good";
    }
}
