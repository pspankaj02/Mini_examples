namespace KiranaHub.SolidPrinciples.D_DependencyInversion;

// =========================================================================================
// [DIP DEMONSTRATION]:
// Dependency Inversion Principle: High-level modules and low-level modules should depend on abstractions.
// =========================================================================================

// -----------------------------------------------------------------------------------------
// [BAD CODE / VIOLATION]: Tightly coupled
// -----------------------------------------------------------------------------------------
public class LowLevelSqlServer
{
    public void SaveOrderToDisk(string orderId) => Console.WriteLine($"[SQL DB] Wrote {orderId} to disk.");
}

public class BadCheckoutService
{
    // [ANTI-PATTERN TIGHT COUPLING]:
    // 'new' keyword se direct concrete SQL class instantiate ho rahi hai.
    // Is service ko bina actual SQL server ke unit test nahi kiya ja sakta!
    private readonly LowLevelSqlServer _db = new LowLevelSqlServer();

    public void Checkout(string orderId)
    {
        _db.SaveOrderToDisk(orderId);
    }
}

// -----------------------------------------------------------------------------------------
// [GOOD CODE / DIP SOLUTION]: Decoupled via Abstractions
// -----------------------------------------------------------------------------------------

// [ABSTRACTION 1]: Persistence contract
public interface IOrderStorage
{
    void Save(string orderId, decimal amount);
    bool Exists(string orderId);
}

// [ABSTRACTION 2]: Notification contract
public interface INotificationService
{
    void NotifyCustomer(string customerPhone, string message);
}

// [LOW-LEVEL DETAIL IMPLEMENTATION 1]:
public class InMemoryOrderStorage : IOrderStorage
{
    // [IN-MEMORY BACKING STORE]:
    private readonly HashSet<string> _savedOrders = new();

    // [METHOD]:
    public void Save(string orderId, decimal amount)
    {
        // [INSERT STATEMENT]:
        _savedOrders.Add(orderId);
        Console.WriteLine($"[STORAGE] Order {orderId} (Rs. {amount}) persisted successfully.");
    }

    // [LOOKUP STATEMENT]:
    public bool Exists(string orderId) => _savedOrders.Contains(orderId);
}

// [LOW-LEVEL DETAIL IMPLEMENTATION 2]:
public class ConsoleNotificationService : INotificationService
{
    // [METHOD]:
    public void NotifyCustomer(string customerPhone, string message)
    {
        Console.WriteLine($"[NOTIFY] Alert dispatched to {customerPhone}: {message}");
    }
}

// [HIGH-LEVEL BUSINESS SERVICE]: Zero knowledge of SQL, Mongo, or SMS vendors!
public class KiranaCheckoutService
{
    // [ABSTRACTION FIELD 1]:
    private readonly IOrderStorage _storage;

    // [ABSTRACTION FIELD 2]:
    private readonly INotificationService _notifier;

    // [CONSTRUCTOR DEPENDENCY INJECTION]:
    public KiranaCheckoutService(IOrderStorage storage, INotificationService notifier)
    {
        // [GUARD CLAUSE 1]:
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));

        // [GUARD CLAUSE 2]:
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    }

    // [ORCHESTRATION METHOD]:
    public void PlaceOrder(string orderId, string customerPhone, decimal amount)
    {
        // [STEP 1: SAVE VIA ABSTRACTION]:
        _storage.Save(orderId, amount);

        // [STEP 2: NOTIFY VIA ABSTRACTION]:
        _notifier.NotifyCustomer(customerPhone, $"Your KiranaHub order #{orderId} of Rs. {amount} is confirmed!");
    }
}
