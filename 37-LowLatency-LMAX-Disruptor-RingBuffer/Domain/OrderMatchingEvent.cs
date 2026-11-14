// [KYA KARTA HAI]: KiranaHub Disruptor Domain namespace define karta hai
// [KYU ZAROORI HAI]: Core low-latency matching engine ke data structures ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project ke domain model directory ko organize karta hai
// [OUTPUT / SIDE EFFECT]: OrderMatchingEvent type domain namespace me available hota hai
// [VARIATIONS TO TRY]: KiranaHub.Disruptor.Events me split kar sakte hain
namespace KiranaHub.Disruptor.Domain;

// [KYA KARTA HAI]: Ring buffer ke pre-allocated slot me rehne wala mutable event class
// [KYU ZAROORI HAI]: Zero-GC allocation runtime achieve karne ke liye; har order par new object create nahi hota
// [KAISE CHALTA HAI]: RingBuffer initialization ke waqt array me pre-instantiate hota hai aur in-place mutate hota hai
// [OUTPUT / SIDE EFFECT]: Garbage Collector par zero pressure banta hai during peak 100,000 orders/sec
// [VARIATIONS TO TRY]: Struct with unsafe memory pointers use karein for unmanaged memory
public class OrderMatchingEvent
{
    // [KYA KARTA HAI]: Unique order identifier track karta hai
    // [KYU ZAROORI HAI]: Har grocery purchase ko uniquely trace karne ke liye
    // [KAISE CHALTA HAI]: Numerical long value store karta hai
    // [OUTPUT / SIDE EFFECT]: Order identity define hoti hai
    // [VARIATIONS TO TRY]: Guid ya 128-bit ULID use karein
    public long OrderId { get; set; }

    // [KYA KARTA HAI]: Ordering customer ki identity track karta hai
    // [KYU ZAROORI HAI]: Customer wallet balance aur flash sale quota limit check karne ke liye
    // [KAISE CHALTA HAI]: Customer code string hold karta hai
    // [OUTPUT / SIDE EFFECT]: Customer relationship establish hoti hai
    // [VARIATIONS TO TRY]: Primitive int customer ID use karein to eliminate reference types
    public string CustomerId { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Kirana grocery item SKU code store karta hai (e.g. "SKU-MANGO-ALPHANSO")
    // [KYU ZAROORI HAI]: Specific dark-store warehouse inventory match karne ke liye
    // [KAISE CHALTA HAI]: Item code string store karta hai
    // [OUTPUT / SIDE EFFECT]: Inventory reservation ke liye target product specify hota hai
    // [VARIATIONS TO TRY]: 64-bit SKU hash value use karein for faster lookups
    public string ItemSku { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Unit price of the grocery item store karta hai
    // [KYU ZAROORI HAI]: Financial calculation aur wallet deduction verify karne ke liye
    // [KAISE CHALTA HAI]: Decimal monetary precision value hold karta hai
    // [OUTPUT / SIDE EFFECT]: Total order cost compute karne me use hota hai
    // [VARIATIONS TO TRY]: Long representing cents/paise for microsecond integer arithmetic
    public decimal UnitPrice { get; set; }

    // [KYA KARTA HAI]: Ordered quantity units count karta hai
    // [KYU ZAROORI HAI]: Inventory stock deduction amount decide karne ke liye
    // [KAISE CHALTA HAI]: Integer unit count store karta hai
    // [OUTPUT / SIDE EFFECT]: Stock decrement count define hota hai
    // [VARIATIONS TO TRY]: Short/byte use karein agar max quantity bounded ho
    public int Quantity { get; set; }

    // [KYA KARTA HAI]: Stage 1 inventory validation status flag hold karta hai
    // [KYU ZAROORI HAI]: Out-of-stock orders ko aage wallet charge hone se rokne ke liye
    // [KAISE CHALTA HAI]: Boolean flag pipeline consumer 1 dwara set hota hai
    // [OUTPUT / SIDE EFFECT]: Downstream consumers is flag ko check karte hain
    // [VARIATIONS TO TRY]: Enum with detailed rejection reasons (OutOfStock, Expired, Reserved)
    public bool IsInventoryReserved { get; set; }

    // [KYA KARTA HAI]: Stage 2 customer wallet/payment confirmation flag hold karta hai
    // [KYU ZAROORI HAI]: Dispatch se pehle payment settlement confirm karne ke liye
    // [KAISE CHALTA HAI]: Pipeline consumer 2 wallet balance deduct karke flag true karta hai
    // [OUTPUT / SIDE EFFECT]: Dispatch pipeline ko green signal milta hai
    // [VARIATIONS TO TRY]: Payment transaction reference hash store karein
    public bool IsPaymentApproved { get; set; }

    // [KYA KARTA HAI]: Stage 3 dark-store packing and dispatch clearance flag
    // [KYU ZAROORI HAI]: Warehouse delivery rider picking queue me entry verify karne ke liye
    // [KAISE CHALTA HAI]: Pipeline consumer 3 order match complete mark karta hai
    // [OUTPUT / SIDE EFFECT]: Order lifecycle successfully complete hoti hai
    // [VARIATIONS TO TRY]: Automated AGV robot dispatch queue ID store karein
    public bool IsDispatched { get; set; }

    // [KYA KARTA HAI]: Microsecond precision entry timestamp store karta hai
    // [KYU ZAROORI HAI]: Sub-microsecond processing latency benchmark karne ke liye
    // [KAISE CHALTA HAI]: Stopwatch.GetTimestamp() ya DateTime.UtcNow.Ticks store karta hai
    // [OUTPUT / SIDE EFFECT]: End-to-end processing duration calculate karne me use hota hai
    // [VARIATIONS TO TRY]: High-resolution CPU TSC (Time Stamp Counter) ticks
    public long IngestTimestampTicks { get; set; }

    // [KYA KARTA HAI]: Slot event ko new incoming order parameters ke saath in-place reuse karta hai
    // [KYU ZAROORI HAI]: Pre-allocated slot me new object allocate kiye bina data populate karta hai
    // [KAISE CHALTA HAI]: Sabhi properties ko new values assign karta hai without 'new' keyword
    // [OUTPUT / SIDE EFFECT]: Existing heap memory re-used; zero GC pressure
    // [VARIATIONS TO TRY]: Object pool pattern ke sath integrate karein
    public void Populate(long orderId, string customerId, string itemSku, decimal unitPrice, int quantity, long timestamp)
    {
        // [KYA KARTA HAI]: OrderId property set karta hai
        // [KYU ZAROORI HAI]: In-place overwrite for new order
        // [KAISE CHALTA HAI]: Assigns incoming orderId
        // [OUTPUT / SIDE EFFECT]: Order ID updated
        // [VARIATIONS TO TRY]: Sequential generator se pass karein
        OrderId = orderId;

        // [KYA KARTA HAI]: CustomerId property set karta hai
        // [KYU ZAROORI HAI]: In-place overwrite for customer
        // [KAISE CHALTA HAI]: Assigns incoming customerId
        // [OUTPUT / SIDE EFFECT]: Customer ID updated
        // [VARIATIONS TO TRY]: Cached string reference pass karein
        CustomerId = customerId;

        // [KYA KARTA HAI]: ItemSku property set karta hai
        // [KYU ZAROORI HAI]: In-place overwrite for SKU
        // [KAISE CHALTA HAI]: Assigns incoming SKU string
        // [OUTPUT / SIDE EFFECT]: SKU updated
        // [VARIATIONS TO TRY]: Interned string pool use karein
        ItemSku = itemSku;

        // [KYA KARTA HAI]: UnitPrice property set karta hai
        // [KYU ZAROORI HAI]: In-place overwrite for price
        // [KAISE CHALTA HAI]: Assigns unit price
        // [OUTPUT / SIDE EFFECT]: Price updated
        // [VARIATIONS TO TRY]: Discounted rate formula apply karein
        UnitPrice = unitPrice;

        // [KYA KARTA HAI]: Quantity property set karta hai
        // [KYU ZAROORI HAI]: In-place overwrite for quantity
        // [KAISE CHALTA HAI]: Assigns quantity count
        // [OUTPUT / SIDE EFFECT]: Quantity updated
        // [VARIATIONS TO TRY]: Max batch limit check karein
        Quantity = quantity;

        // [KYA KARTA HAI]: Inventory reservation state reset karta hai
        // [KYU ZAROORI HAI]: Previous order ka stale state naye order me leak na ho
        // [KAISE CHALTA HAI]: Sets IsInventoryReserved to false
        // [OUTPUT / SIDE EFFECT]: Clean unreserved state
        // [VARIATIONS TO TRY]: Rejection reason code reset karein
        IsInventoryReserved = false;

        // [KYA KARTA HAI]: Payment approval state reset karta hai
        // [KYU ZAROORI HAI]: Security ke liye payment unapproved state me start honi chahiye
        // [KAISE CHALTA HAI]: Sets IsPaymentApproved to false
        // [OUTPUT / SIDE EFFECT]: Clean unapproved payment state
        // [VARIATIONS TO TRY]: Escrow lock acquire karein
        IsPaymentApproved = false;

        // [KYA KARTA HAI]: Dispatch clearance state reset karta hai
        // [KYU ZAROORI HAI]: Har naye order ko fresh processing pipeline se guzarna hota hai
        // [KAISE CHALTA HAI]: Sets IsDispatched to false
        // [OUTPUT / SIDE EFFECT]: Clean undispatched state
        // [VARIATIONS TO TRY]: Tracking status string reset karein
        IsDispatched = false;

        // [KYA KARTA HAI]: Ingestion timestamp tick record karta hai
        // [KYU ZAROORI HAI]: Sub-microsecond latency measurement benchmark ke liye
        // [KAISE CHALTA HAI]: Assigns incoming high-resolution tick
        // [OUTPUT / SIDE EFFECT]: Timestamp recorded
        // [VARIATIONS TO TRY]: Hardware cycle count rdtsc use karein
        IngestTimestampTicks = timestamp;
    }
}
