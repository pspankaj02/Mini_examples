namespace KiranaHub.SolidPrinciples.L_LiskovSubstitution;

// =========================================================================================
// [LSP DEMONSTRATION]:
// Liskov Substitution Principle: Subtypes must be substitutable for their base types.
// =========================================================================================

// -----------------------------------------------------------------------------------------
// [BAD CODE / VIOLATION]:
// -----------------------------------------------------------------------------------------
public class BaseDeliveryPartner
{
    public virtual void PickPackage(string orderId) => Console.WriteLine($"Picking {orderId}");
    public virtual void ClimbStairsToDoorstep(int floorNumber) => Console.WriteLine($"Climbing to floor {floorNumber}");
}

public class DroneDeliveryPartnerViolation : BaseDeliveryPartner
{
    // [ANTI-PATTERN OVERRIDE]:
    // [KYU GALAT HAI]: Caller base type assume karke method call karega aur runtime crash face karega!
    public override void ClimbStairsToDoorstep(int floorNumber)
    {
        // [EXCEPTION THROW]: Violates caller expectations
        throw new NotSupportedException("Drones cannot climb apartment stairs!");
    }
}

// -----------------------------------------------------------------------------------------
// [GOOD CODE / LSP SOLUTION]: Proper Contract Hierarchies
// -----------------------------------------------------------------------------------------

// [BASE CAPABILITY CONTRACT]:
public interface IDeliveryPartner
{
    // [PROPERTY]: Partner brand / identification
    string PartnerName { get; }

    // [METHOD]: Package delivery contract that EVERY partner can 100% fulfill
    void DeliverPackage(string orderId, string dropPoint);
}

// [EXTENDED CAPABILITY CONTRACT]:
// Only partners with physical doorstep access implement this sub-interface.
public interface IDoorstepDeliveryPartner : IDeliveryPartner
{
    // [METHOD]: Doorstep handover
    void DeliverToApartmentDoor(string orderId, int floorNumber);
}

// [IMPLEMENTATION 1: BIKE RIDER]: Fulfills both base and doorstep contracts
public class BikeDeliveryRider : IDoorstepDeliveryPartner
{
    // [PROPERTY]:
    public string PartnerName => "Ramesh (Hero Splendor)";

    // [BASE METHOD IMPLEMENTATION]:
    public void DeliverPackage(string orderId, string dropPoint)
    {
        Console.WriteLine($"[RIDER] Arrived at building {dropPoint} with order {orderId}.");
    }

    // [DOORSTEP METHOD IMPLEMENTATION]:
    public void DeliverToApartmentDoor(string orderId, int floorNumber)
    {
        Console.WriteLine($"[RIDER] Took lift/stairs to Floor {floorNumber} and handed over {orderId}.");
    }
}

// [IMPLEMENTATION 2: AUTONOMOUS DRONE]: Fulfills base contract without throwing fake exceptions
public class AutonomousDronePartner : IDeliveryPartner
{
    // [PROPERTY]:
    public string PartnerName => "SkyDart Delivery Drone v2";

    // [BASE METHOD IMPLEMENTATION]:
    public void DeliverPackage(string orderId, string dropPoint)
    {
        Console.WriteLine($"[DRONE] Descending to designated open drop zone at {dropPoint} for order {orderId}.");
    }
}
