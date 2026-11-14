namespace KiranaHub.UnitTests.Services;

// =========================================================================================
// [TOPIC]: Order Fulfillment Services & Interfaces
// [PURPOSE]: Business logic encapsulation designed specifically for isolated Unit Testing.
// =========================================================================================

// [KYA KARTA HAI]: Warehouse inventory check aur stock deduction ke liye contract define karta hai.
// [KYU ZAROORI HAI]: Dependency Inversion Principle (DIP) follow karta hai taaki service actual database par depend na kare.
// [KAISE CHALTA HAI]: Unit test me NSubstitute is interface ka dynamic proxy mock generate karta hai.
public interface IInventoryService
{
    // [KYA KARTA HAI]: Check karta hai ki requested SKU aur quantity available hai ya nahi.
    // [OUTPUT / RETURN]: Task<bool> (true agar stock available hai, false otherwise).
    Task<bool> HasSufficientStockAsync(string sku, int requestedQuantity);

    // [KYA KARTA HAI]: Order confirm hone ke baad warehouse se stock deduct karta hai.
    // [SIDE EFFECT]: Actual inventory count decrease hota hai.
    Task DeductStockAsync(string sku, int quantity);
}

// [KYA KARTA HAI]: Third-party payment gateway (Razorpay/PayTM/Stripe) ka contract define karta hai.
// [KYU ZAROORI HAI]: Unit test me real bank ya gateway par HTTP calls rokne ke liye mocking zaroori hai.
public interface IPaymentGateway
{
    // [KYA KARTA HAI]: Customer se total amount charge karta hai.
    // [OUTPUT / RETURN]: Transaction reference ID string return karta hai agar success ho, ya null/empty if failed.
    Task<string> ProcessPaymentAsync(string customerId, decimal amount);
}

// [KYA KARTA HAI]: Customer communication channels (SMS/WhatsApp/Email) ka contract define karta hai.
// [KYU ZAROORI HAI]: Outbound messages ko unit test ke dauran suppress karna aur verify karna ki call trigger hui.
public interface INotificationService
{
    // [KYA KARTA HAI]: Customer ko order confirmation notification bhejta hai.
    // [SIDE EFFECT]: Outbound notification message queue me enqueue hota hai.
    Task SendOrderConfirmationAsync(string customerId, string orderId);
}

// [KYA KARTA HAI]: Order fulfillment operation ka immutable result record encapsulate karta hai.
// [KYU ZAROORI HAI]: Clean return object containing success status, generated orderId, and error message.
// [KAISE CHALTA HAI]: C# 9+ record type auto-generates value-based equality, ToString(), and deconstructors.
public record FulfillmentResult(bool IsSuccess, string? OrderId, string? ErrorMessage);

// [KYA KARTA HAI]: KiranaHub ka core order fulfillment workflow coordinate karta hai (System Under Test - SUT).
// [KYU ZAROORI HAI]: Central business logic class jo inventory, payment, aur notification ko orchestrate karti hai.
public class OrderFulfillmentService
{
    // [KYA KARTA HAI]: Injected inventory service contract ka private readonly reference store karta hai.
    // [KYU ZAROORI HAI]: Immutability ensure karta hai; constructor ke baad reference change nahi ho sakta.
    private readonly IInventoryService _inventory;

    // [KYA KARTA HAI]: Injected payment gateway contract ka private readonly reference store karta hai.
    // [KYU ZAROORI HAI]: Securely process payments via abstraction.
    private readonly IPaymentGateway _payment;

    // [KYA KARTA HAI]: Injected notification service contract ka private readonly reference store karta hai.
    // [KYU ZAROORI HAI]: Send notifications via abstraction.
    private readonly INotificationService _notification;

    // [KYA KARTA HAI]: Constructor Injection ke zariye required dependencies accept karta hai.
    // [KYU ZAROORI HAI]: Loose coupling provide karta hai; unit test me mock instances inject kiye ja sakte hain.
    // [KAISE CHALTA HAI]: IoC container ya test runner mock objects pass karta hai.
    public OrderFulfillmentService(
        IInventoryService inventory,
        IPaymentGateway payment,
        INotificationService notification)
    {
        // [KYA KARTA HAI]: Guard clause: check karta hai ki inventory dependency null toh nahi hai.
        // [KYU ZAROORI HAI]: Fail-fast principle; null injection runtime crash karta hai.
        // [KAISE CHALTA HAI]: Agar inventory null ho toh ArgumentNullException throw karta hai.
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

        // [KYA KARTA HAI]: Guard clause: check karta hai ki payment dependency null toh nahi hai.
        // [KYU ZAROORI HAI]: Payment component missing hone par service functional nahi ho sakti.
        _payment = payment ?? throw new ArgumentNullException(nameof(payment));

        // [KYA KARTA HAI]: Guard clause: check karta hai ki notification dependency null toh nahi hai.
        // [KYU ZAROORI HAI]: NullReferenceException prevent karna at runtime.
        _notification = notification ?? throw new ArgumentNullException(nameof(notification));
    }

    // [KYA KARTA HAI]: Order fulfillment pipeline execute karta hai: validation -> stock check -> payment -> deduction -> notification.
    // [KYU ZAROORI HAI]: E-commerce fulfillment ka single primary flow jise test suite se 100% cover kiya jata hai.
    // [KAISE CHALTA HAI]: Async/await state machine sequential async steps run karti hai with zero thread blocking.
    // [OUTPUT / RETURN]: FulfillmentResult with Order ID or friendly Error Message.
    public async Task<FulfillmentResult> FulfillOrderAsync(string customerId, string sku, int quantity, decimal unitPrice)
    {
        // ------------------------------------------------------------------------------------
        // 1. DEFENSIVE VALIDATION
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Customer ID ki presence check karta hai.
        // [KYU ZAROORI HAI]: Bina customer identifier ke order place nahi ho sakta.
        if (string.IsNullOrWhiteSpace(customerId))
        {
            // [KYA KARTA HAI]: ArgumentException throw karta hai clear parameter name ke sath.
            // [OUTPUT / SIDE EFFECT]: Flow halts immediately, exception bubbles up.
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        }

        // [KYA KARTA HAI]: Quantity greater than zero hone ka validation.
        // [KYU ZAROORI HAI]: Negative ya zero items deliver nahi kiye ja sakte.
        if (quantity <= 0)
        {
            // [KYA KARTA HAI]: ArgumentException throw karta hai.
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        // [KYA KARTA HAI]: Unit price positive hone ka validation.
        // [KYU ZAROORI HAI]: Negative pricing fraud ya configuration error hoti hai.
        if (unitPrice <= 0)
        {
            // [KYA KARTA HAI]: ArgumentException throw karta hai.
            throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));
        }

        // ------------------------------------------------------------------------------------
        // 2. CHECK STOCK AVAILABILITY
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Inventory service se asynchronously check karta hai ki required stock maujood hai ya nahi.
        // [KYU ZAROORI HAI]: Agar item out-of-stock hai toh payment process nahi honi chahiye (prevent unnecessary refund).
        // [KAISE CHALTA HAI]: Awaits task; test me mocked return value evaluate hoti hai.
        bool hasStock = await _inventory.HasSufficientStockAsync(sku, quantity);

        // [KYA KARTA HAI]: Agar stock insufficient ho toh early return karta hai failure result.
        // [KYU ZAROORI HAI]: Graceful failure response bina exception throw kiye.
        if (!hasStock)
        {
            // [KYA KARTA HAI]: IsSuccess = false aur error message ke sath result return karta hai.
            // [OUTPUT / RETURN]: FulfillmentResult(false, null, "Insufficient stock...").
            return new FulfillmentResult(false, null, $"Insufficient stock for SKU: {sku}");
        }

        // ------------------------------------------------------------------------------------
        // 3. PROCESS PAYMENT
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Quantity aur unit price ko multiply karke total order amount calculate karta hai.
        // [KYU ZAROORI HAI]: Accurate charge amount computation using high-precision decimal type.
        decimal totalAmount = quantity * unitPrice;

        // [KYA KARTA HAI]: Payment gateway par asynchronously charge request bhejta hai.
        // [KYU ZAROORI HAI]: Customer funds authorize karna before inventory deduction.
        // [KAISE CHALTA HAI]: External gateway HTTP call ya mocked response await hota hai.
        string transactionId = await _payment.ProcessPaymentAsync(customerId, totalAmount);

        // [KYA KARTA HAI]: Check karta hai ki transaction successful raha ya gateway ne empty/null ID return ki.
        // [KYU ZAROORI HAI]: Failed/Declined payments ko handle karna.
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            // [KYA KARTA HAI]: Payment failure result return karta hai without inventory deduction.
            // [OUTPUT / RETURN]: FulfillmentResult(false, null, "Payment authorization failed.").
            return new FulfillmentResult(false, null, "Payment authorization failed.");
        }

        // ------------------------------------------------------------------------------------
        // 4. DEDUCT INVENTORY STOCK
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Payment confirm hone ke baad warehouse se stock count deduct karta hai.
        // [KYU ZAROORI HAI]: Inventory audit accurate rakhna taaki overselling na ho.
        // [KAISE CHALTA HAI]: Awaits inventory deduction call.
        await _inventory.DeductStockAsync(sku, quantity);

        // ------------------------------------------------------------------------------------
        // 5. SEND CUSTOMER CONFIRMATION NOTIFICATION
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Cryptographically unique readable Order ID generate karta hai (e.g. ORD-A1B2C3D4).
        // [KYU ZAROORI HAI]: Tracking identifier for customer, invoice, and rider delivery assignment.
        // [KAISE CHALTA HAI]: Guid.NewGuid() formatted as "N" (no hyphens), sliced first 8 chars, capitalized.
        string orderId = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // [KYA KARTA HAI]: Customer notification service ko trigger karta hai order ID ke sath.
        // [KYU ZAROORI HAI]: Customer delight and real-time order confirmation delivery.
        await _notification.SendOrderConfirmationAsync(customerId, orderId);

        // ------------------------------------------------------------------------------------
        // 6. RETURN SUCCESSFUL RESULT
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: IsSuccess = true, generated orderId, aur null error ke sath successful result return karta hai.
        // [OUTPUT / RETURN]: FulfillmentResult(true, orderId, null).
        return new FulfillmentResult(true, orderId, null);
    }
}
