namespace KiranaHub.SolidPrinciples.O_OpenClosed;

// =========================================================================================
// [OCP DEMONSTRATION]:
// Open/Closed Principle: Open for extension, closed for modification.
// =========================================================================================

// -----------------------------------------------------------------------------------------
// [BAD CODE / VIOLATION]:
// -----------------------------------------------------------------------------------------
public class BadDiscountEngine
{
    // [ANTI-PATTERN METHOD]:
    // [KYU GALAT HAI]: Har naye discount rule ke liye is class ka source code edit karke naya 'if-else' likhna padega.
    public decimal CalculateDiscount(string discountType, decimal cartTotal)
    {
        if (discountType == "FESTIVE")
            return cartTotal * 0.15m;
        else if (discountType == "FIRST_ORDER")
            return 50.0m;
        else if (discountType == "BULK")
            return cartTotal * 0.20m;
        
        return 0;
    }
}

// -----------------------------------------------------------------------------------------
// [GOOD CODE / OCP SOLUTION]: Strategy Pattern
// -----------------------------------------------------------------------------------------

// [STRATEGY CONTRACT INTERFACE]:
// [KYA KARTA HAI]: Abstraction define karta hai jisse koi bhi naya discount rule plug-in kiya ja sake.
public interface IDiscountStrategy
{
    // [PROPERTY]: Human-readable campaign name
    string RuleName { get; }

    // [METHOD]: Condition evaluation check
    bool IsApplicable(decimal cartTotal, int itemCount);

    // [METHOD]: Discount amount computation
    decimal CalculateDiscount(decimal cartTotal);
}

// [EXTENSION 1: DIWALI DISCOUNT]:
public class DiwaliFestiveDiscount : IDiscountStrategy
{
    // [RULE NAME PROPERTY]:
    public string RuleName => "Diwali Festive Dhamaka (10% OFF)";

    // [APPLICABILITY PREDICATE]: Applicable if cart total >= Rs. 500
    public bool IsApplicable(decimal cartTotal, int itemCount) => cartTotal >= 500m;

    // [CALCULATION STATEMENT]: 10% flat off
    public decimal CalculateDiscount(decimal cartTotal) => cartTotal * 0.10m;
}

// [EXTENSION 2: BULK GROCERY DISCOUNT]:
public class BulkGroceryDiscount : IDiscountStrategy
{
    // [RULE NAME PROPERTY]:
    public string RuleName => "Bulk Kirana Saver (Rs. 150 OFF)";

    // [APPLICABILITY PREDICATE]: Applicable if cart total >= Rs. 2000 OR item count >= 10
    public bool IsApplicable(decimal cartTotal, int itemCount) => cartTotal >= 2000m || itemCount >= 10;

    // [CALCULATION STATEMENT]: Rs. 150 flat off
    public decimal CalculateDiscount(decimal cartTotal) => 150m;
}

// [CORE ENGINE: 100% CLOSED FOR MODIFICATION]:
public class KiranaDiscountEngine
{
    // [COLLECTION FIELD]: Injected strategies collection
    private readonly IEnumerable<IDiscountStrategy> _strategies;

    // [CONSTRUCTOR]: Injects all registered strategy implementations
    public KiranaDiscountEngine(IEnumerable<IDiscountStrategy> strategies)
    {
        // [ASSIGNMENT LINE]:
        _strategies = strategies;
    }

    // [EVALUATION METHOD]: Finds and applies the best savings for the customer
    public decimal GetBestDiscount(decimal cartTotal, int itemCount, out string appliedRule)
    {
        // [INITIALIZATION 1]:
        decimal highestDiscount = 0;

        // [INITIALIZATION 2]:
        appliedRule = "None";

        // [FOREACH ENUMERATION]: Iterates over all strategy plug-ins
        foreach (var strategy in _strategies)
        {
            // [CONDITION CHECK]: Is current strategy valid for this cart?
            if (strategy.IsApplicable(cartTotal, itemCount))
            {
                // [COMPUTATION]:
                decimal discount = strategy.CalculateDiscount(cartTotal);

                // [COMPARISON]: Check if this discount beats previous highest
                if (discount > highestDiscount)
                {
                    // [STATE MUTATION 1]:
                    highestDiscount = discount;

                    // [STATE MUTATION 2]:
                    appliedRule = strategy.RuleName;
                }
            }
        }

        // [RETURN HIGHEST SAVING]:
        return highestDiscount;
    }
}
