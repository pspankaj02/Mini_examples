namespace KiranaHub.SolidPrinciples.S_SingleResponsibility;

// =========================================================================================
// [RECORD DECLARATION]:
// [KYA KARTA HAI]: Positional record for single grocery line item.
// =========================================================================================
public record OrderItem(string Name, decimal Price, int Quantity);

// -----------------------------------------------------------------------------------------
// [BAD CODE / VIOLATION]: Anti-Pattern
// -----------------------------------------------------------------------------------------
public class OrderProcessorGodClass
{
    // [ANTI-PATTERN METHOD]:
    // [KYA KARTA HAI]: Ek akela method 4 alag alag responsibilities execute karta hai (God Object).
    // [KYU GALAT HAI]: SRP violation - tax change ho, receipt format badle, SMS gateway change ho,
    // ya database schema badle - har baar is ek class ko modify karna padega!
    public void ProcessOrder(string customerPhone, List<OrderItem> items)
    {
        // [RESPONSIBILITY 1: TAX CALCULATION]:
        decimal tax = 0;
        foreach (var item in items) tax += item.Price * item.Quantity * 0.18m;

        // [RESPONSIBILITY 2: RECEIPT FORMATTING]:
        string receipt = $"Receipt for {customerPhone}: Total tax Rs. {tax}";

        // [RESPONSIBILITY 3: SMS NOTIFICATION]:
        Console.WriteLine($"[SMS GATEWAY] Sending to {customerPhone}: {receipt}");

        // [RESPONSIBILITY 4: DATABASE PERSISTENCE]:
        Console.WriteLine("[DATABASE] Saving order to SQL DB...");
    }
}

// -----------------------------------------------------------------------------------------
// [GOOD CODE / SRP SOLUTION]: Segregated single-responsibility classes
// -----------------------------------------------------------------------------------------

// [SRP CLASS 1: TAX CALCULATOR]:
// [SINGLE REASON TO CHANGE]: Sirf aur sirf GST / tax slab rules change hone par!
public class KiranaTaxCalculator
{
    // [CONSTANT DECLARATION]:
    // 5% standard GST on essential groceries.
    private const decimal GstRate = 0.05m;

    // [METHOD DECLARATION]: Pure mathematical calculation over items
    public decimal CalculateTax(IEnumerable<OrderItem> items)
    {
        // [LINE 1: ACCUMULATOR INITIALIZATION]:
        decimal taxableAmount = 0;

        // [LINE 2: FOREACH ENUMERATION]:
        foreach (var item in items)
        {
            // [LINE 3: MULTIPLICATION & ADDITION]:
            taxableAmount += item.Price * item.Quantity;
        }

        // [LINE 4: GST APPLIED RETURN]:
        return taxableAmount * GstRate;
    }
}

// [SRP CLASS 2: RECEIPT FORMATTER]:
// [SINGLE REASON TO CHANGE]: Sirf visual invoice presentation ya text branding change hone par!
public class KiranaReceiptFormatter
{
    // [METHOD DECLARATION]:
    public string GenerateInvoiceText(string orderId, decimal subtotal, decimal tax)
    {
        // [FORMATTED STRING RETURN]:
        // Uses standard C# ':F2' specifier to format amounts to 2 decimal places.
        return $"[INVOICE #{orderId}] Subtotal: Rs. {subtotal:F2} | GST: Rs. {tax:F2} | Total: Rs. {(subtotal + tax):F2}";
    }
}

// [SRP CLASS 3: NOTIFICATION DISPATCHER]:
// [SINGLE REASON TO CHANGE]: Sirf communication provider (Twilio, AWS SNS, WhatsApp API) change hone par!
public class KiranaSmsNotifier
{
    // [METHOD DECLARATION]:
    public void SendOrderConfirmation(string phone, string invoiceText)
    {
        // [DISPATCH SIMULATION LINE]:
        Console.WriteLine($"[SMS to {phone}]: {invoiceText}");
    }
}
