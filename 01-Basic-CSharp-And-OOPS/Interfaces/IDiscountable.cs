namespace KiranaHub.BasicsAndOops.Interfaces;

// =========================================================================================
// [KYA KARTA HAI]:
// Discount eligibility aur calculation ka contract define karta hai.
//
// [KYU ZAROORI HAI]:
// Kuch products par discount lag sakta hai (e.g. Biscuits), jabki government-regulated
// ya loose items par nahi. Interface Segregation ko promote karta hai.
// =========================================================================================
public interface IDiscountable
{
    decimal ApplyDiscount(decimal percentage);
}

