namespace KiranaHub.SolidPrinciples.I_InterfaceSegregation;

// =========================================================================================
// [ISP DEMONSTRATION]:
// Interface Segregation Principle: Clients should not be forced to depend on interfaces they do not use.
// =========================================================================================

// -----------------------------------------------------------------------------------------
// [BAD CODE / VIOLATION]: "Fat Interface"
// -----------------------------------------------------------------------------------------
public interface IFatPaymentProcessor
{
    void PayViaCreditCard(string cardNumber, string cvv);
    void PayViaUpi(string upiId);
    void CollectCash(decimal amountInHand);
    void RefundToCard(string transactionId);
}

// Cash on Delivery delivery boy ko zabardasti unused methods implement karne padte hain
public class DeliveryBoyCashHandlerViolation : IFatPaymentProcessor
{
    public void CollectCash(decimal amountInHand) => Console.WriteLine($"Collected Rs. {amountInHand}");

    // [DUMMY CRASH IMPLEMENTATIONS]:
    public void PayViaCreditCard(string cardNumber, string cvv) => throw new NotImplementedException();
    public void PayViaUpi(string upiId) => throw new NotImplementedException();
    public void RefundToCard(string transactionId) => throw new NotImplementedException();
}

// -----------------------------------------------------------------------------------------
// [GOOD CODE / ISP SOLUTION]: Role-Specific Granular Interfaces
// -----------------------------------------------------------------------------------------

// [INTERFACE 1]: Dedicated to Cash Handling
public interface ICashCollectable
{
    void CollectCashAtDoorstep(decimal amount);
}

// [INTERFACE 2]: Dedicated to UPI Transactions
public interface IUpiPayable
{
    void PayViaUpiQr(string upiId, decimal amount);
}

// [INTERFACE 3]: Dedicated to Cards & Refunds
public interface ICardProcessor
{
    void ChargeCard(string token, decimal amount);
    void Refund(string transactionId);
}

// [CLEAN IMPLEMENTATION 1: CASH ONLY RIDER]:
public class KiranaCodRider : ICashCollectable
{
    // [METHOD IMPLEMENTATION]: Only implements cash method!
    public void CollectCashAtDoorstep(decimal amount)
    {
        Console.WriteLine($"[CASH RECEIPT] Rider received exact cash: Rs. {amount}");
    }
}

// [CLEAN IMPLEMENTATION 2: ONLINE GATEWAY]: Implements only digital interfaces
public class KiranaOnlineGateway : IUpiPayable, ICardProcessor
{
    public void PayViaUpiQr(string upiId, decimal amount)
    {
        Console.WriteLine($"[UPI SUCCESS] Rs. {amount} debited via VPA {upiId}");
    }

    public void ChargeCard(string token, decimal amount)
    {
        Console.WriteLine($"[CARD SUCCESS] Rs. {amount} charged on secure card token {token}");
    }

    public void Refund(string transactionId)
    {
        Console.WriteLine($"[REFUND PROCESSED] Reversal initiated for txn {transactionId}");
    }
}
