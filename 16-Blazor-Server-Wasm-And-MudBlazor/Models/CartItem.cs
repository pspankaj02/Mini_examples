namespace KiranaHub.BlazorStore.Models;

// =========================================================================================
// [TOPIC]: Blazor Shopping Cart Item & Order Summary Models
// [PURPOSE]: Line item calculations and checkout receipts.
// =========================================================================================

// [KYA KARTA HAI]: Shopping cart item model encapsulate karta hai.
public class CartItem
{
    // [KYA KARTA HAI]: Product ID reference.
    public int ProductId { get; set; }

    // [KYA KARTA HAI]: Product SKU barcode.
    public string Sku { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Product Name.
    public string ProductName { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Unit Price.
    public decimal UnitPrice { get; set; }

    // [KYA KARTA HAI]: Cart me chuni gayi quantity.
    public int Quantity { get; set; }

    // [KYA KARTA HAI]: Computed property: UnitPrice * Quantity.
    public decimal LineTotal => UnitPrice * Quantity;
}

// [KYA KARTA HAI]: Final order placement receipt record.
public record OrderReceipt(
    string OrderId,
    string CustomerName,
    string DeliveryAddress,
    string PaymentMode,
    decimal TotalAmount,
    DateTime PlacedAtUtc
);

