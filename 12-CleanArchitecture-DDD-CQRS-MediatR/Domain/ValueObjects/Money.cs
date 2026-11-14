namespace KiranaHub.CleanArchitecture.Domain.ValueObjects;

// =========================================================================================
// [TOPIC]: DDD Value Object (Money)
// [PURPOSE]: Encapsulates currency and amount with structural equality and domain invariant validations.
// =========================================================================================

// [KYA KARTA HAI]: Money value object encapsulate karta hai (Amount + Currency).
// [KYU ZAROORI HAI]: Primitive Obsession anti-pattern se bachata hai; decimal price ke sath currency tightly coupled rehni chahiye.
// [KAISE CHALTA HAI]: C# record type auto-generates value-based equality (two Money objects with same amount & currency are equal).
public record Money
{
    // [KYA KARTA HAI]: Monetary amount store karta hai.
    public decimal Amount { get; init; }

    // [KYA KARTA HAI]: ISO Currency code store karta hai (e.g. "INR", "USD").
    public string Currency { get; init; }

    // [KYA KARTA HAI]: Private parameterless constructor for serialization engines.
    private Money()
    {
        // [KYA KARTA HAI]: Default currency "INR" set karta hai.
        Currency = "INR";
    }

    // [KYA KARTA HAI]: Primary constructor with strict invariant validations.
    public Money(decimal amount, string currency = "INR")
    {
        // [KYA KARTA HAI]: Invariant Check 1: Negative money check.
        // [KYU ZAROORI HAI]: E-commerce domain me negative money exist nahi kar sakti.
        if (amount < 0)
        {
            // [KYA KARTA HAI]: ArgumentOutOfRangeException throw karta hai.
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        // [KYA KARTA HAI]: Invariant Check 2: Currency code validation.
        if (string.IsNullOrWhiteSpace(currency))
        {
            // [KYA KARTA HAI]: ArgumentException throw karta hai blank currency par.
            throw new ArgumentException("Currency code cannot be empty.", nameof(currency));
        }

        // [KYA KARTA HAI]: Amount property assign karta hai.
        Amount = amount;

        // [KYA KARTA HAI]: Currency uppercase format me assign karta hai.
        Currency = currency.ToUpperInvariant();
    }

    // [KYA KARTA HAI]: Static factory method zero INR create karne ke liye.
    // [OUTPUT / RETURN]: Money(0, "INR").
    public static Money Zero(string currency = "INR") => new(0m, currency);

    // [KYA KARTA HAI]: Operator overload (+) do Money objects ko add karta hai.
    // [KYU ZAROORI HAI]: Domain expressions readable banane ke liye (e.g. total = item1 + item2).
    public static Money operator +(Money left, Money right)
    {
        // [KYA KARTA HAI]: Invariant check: Currency match check.
        // [KYU ZAROORI HAI]: Rupees aur Dollars ko direct bina conversion add karna financial bug hota hai!
        if (left.Currency != right.Currency)
        {
            // [KYA KARTA HAI]: InvalidOperationException throw karta hai.
            throw new InvalidOperationException($"Cannot add money of different currencies: '{left.Currency}' and '{right.Currency}'.");
        }

        // [KYA KARTA HAI]: Amounts add karke naya immutable Money return karta hai.
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    // [KYA KARTA HAI]: Operator overload (*) Money ko integer quantity se multiply karta hai.
    public static Money operator *(Money money, int quantity)
    {
        // [KYA KARTA HAI]: Quantity negative check.
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

        // [KYA KARTA HAI]: Multiplied amount return karta hai.
        return new Money(money.Amount * quantity, money.Currency);
    }

    // [KYA KARTA HAI]: ToString representation format karta hai (e.g. "INR 150.00").
    public override string ToString() => $"{Currency} {Amount:N2}";
}

