namespace KiranaHub.WebApi.Services;

// =========================================================================================
// [TOPIC]: Dependency Injection Lifetimes (Transient, Scoped, Singleton) & Keyed Services
// [PURPOSE]: Educational demonstration of object lifetimes, captive dependency safety, and key-based resolution.
// =========================================================================================

// [KYA KARTA HAI]: Transient service contract define karta hai with unique InstanceId.
// [KYU ZAROORI HAI]: Demonstrate karna ki har request injection par naya instance banta hai.
public interface ITransientService { Guid InstanceId { get; } }

// [KYA KARTA HAI]: Scoped service contract define karta hai.
// [KYU ZAROORI HAI]: Demonstrate karna ki ek hi HTTP request ke andar same instance share hota hai.
public interface IScopedService { Guid InstanceId { get; } }

// [KYA KARTA HAI]: Singleton service contract define karta hai.
// [KYU ZAROORI HAI]: Demonstrate karna ki application startup se shutdown tak single instance rehta hai.
public interface ISingletonService { Guid InstanceId { get; } }

// [KYA KARTA HAI]: ITransientService ka concrete implementation.
// [KAISE CHALTA HAI]: Jab bhi resolve hota hai, Guid.NewGuid() naya identifier generate karta hai.
public class TransientService : ITransientService
{
    // [KYA KARTA HAI]: Instance create hote hi unique GUID generate karke assign karta hai.
    public Guid InstanceId { get; } = Guid.NewGuid();
}

// [KYA KARTA HAI]: IScopedService ka concrete implementation.
// [KAISE CHALTA HAI]: Current HTTP Request ke scope me single instance rehta hai; naye HTTP request me naya instance banta hai.
public class ScopedService : IScopedService
{
    // [KYA KARTA HAI]: Scope creation ke waqt unique GUID assign karta hai.
    public Guid InstanceId { get; } = Guid.NewGuid();
}

// [KYA KARTA HAI]: ISingletonService ka concrete implementation.
// [KAISE CHALTA HAI]: Process lifetime me sirf ek baar instantiate hota hai.
public class SingletonService : ISingletonService
{
    // [KYA KARTA HAI]: Application launch par once-off GUID allocate karta hai.
    public Guid InstanceId { get; } = Guid.NewGuid();
}

// -----------------------------------------------------------------------------------------
// KEYED SERVICES (.NET 8+ Feature)
// -----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Multiple payment implementations ke liye shared interface contract define karta hai.
// [KYU ZAROORI HAI]: Payment providers (UPI, Card, NetBanking) ko polymorphic abstraction dena.
public interface IPaymentGateway
{
    // [KYA KARTA HAI]: Amount accept karke transaction confirmation message string return karta hai.
    string ProcessPayment(decimal amount);
}

// [KYA KARTA HAI]: UPI payment method ka concrete implementation.
// [KYU ZAROORI HAI]: Keyed service registration "upi" ke sath bind hona.
public class UpiPaymentGateway : IPaymentGateway
{
    // [KYA KARTA HAI]: UPI instant charge simulation execute karta hai.
    // [OUTPUT / RETURN]: Fast UPI string with transaction confirmation.
    public string ProcessPayment(decimal amount) => $"[UPI GATEWAY] Rs. {amount} debited via instant VPA intent.";
}

// [KYA KARTA HAI]: Credit/Debit Card payment method ka concrete implementation.
// [KYU ZAROORI HAI]: Keyed service registration "card" ke sath bind hona.
public class CardPaymentGateway : IPaymentGateway
{
    // [KYA KARTA HAI]: Credit/Debit card charge simulation execute karta hai.
    // [OUTPUT / RETURN]: Card network authorization string.
    public string ProcessPayment(decimal amount) => $"[CARD GATEWAY] Rs. {amount} charged on Visa/Mastercard network.";
}
