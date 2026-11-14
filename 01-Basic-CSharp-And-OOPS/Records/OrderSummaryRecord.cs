namespace KiranaHub.BasicsAndOops.Records;

// =========================================================================================
// [RECORD DECLARATION]:
// [KYA KARTA HAI]: C# Positional Record jo immutable order summary data transfer object (DTO) create karta hai.
// [KYU ZAROORI HAI]:
// Classes reference equality check karti hain (same memory address).
// Records value-based equality check karte hain (agar fields identical hain toh `==` returns true).
// [KAISE CHALTA HAI]: Compiler behind-the-scenes `Equals()`, `GetHashCode()`, `ToString()`, aur copy constructor generate karta hai.
// =========================================================================================
public record OrderSummaryRecord(
    // [POSITIONAL PARAMETER 1]: Unique Order identifier (e.g. "ORD-1001")
    string OrderId,

    // [POSITIONAL PARAMETER 2]: Customer full name
    string CustomerName,

    // [POSITIONAL PARAMETER 3]: Grand total amount payable
    decimal TotalAmount,

    // [POSITIONAL PARAMETER 4]: Count of individual items in order
    int ItemsCount,

    // [POSITIONAL PARAMETER 5]: Order payment status (e.g. "PENDING", "PAID")
    string PaymentStatus
)
{
    // [COMPUTED PROPERTY]:
    // [KYA KARTA HAI]: Expression-bodied read-only property jo business rule evaluate karti hai.
    // [OUTPUT]: True if TotalAmount > 1000, enabling free gift eligibility.
    public bool IsEligibleForFreeGift => TotalAmount > 1000m;
}
