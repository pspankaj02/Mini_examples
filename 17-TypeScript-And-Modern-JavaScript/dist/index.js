// [KYA KARTA HAI]: Node.js ka native strict assertion module import karta hai.
// [KYU ZAROORI HAI]: Zero 3rd-party test framework overhead ke bina unit tests aur verification execute karne ke liye.
// [KAISE CHALTA HAI]: 'node:assert/strict' se deepStrictEqual, strictEqual, throws functions import karta hai.
// [OUTPUT / SIDE EFFECT]: Test assertions available ho jati hain.
// [VARIATIONS TO TRY]: Vitest ya Jest assertions me replace kiya ja sakta hai.
import assert from "node:assert/strict";
import { isPerishableItem, assertValidRiderPhone } from "./types/domain.js";
// [KYA KARTA HAI]: PricingEngine aur DiscountRule types import karta hai.
// [KYU ZAROORI HAI]: Cart calculations test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from PricingEngine.js.
// [OUTPUT / SIDE EFFECT]: PricingEngine loaded.
// [VARIATIONS TO TRY]: Dynamic discount strategy injection.
import { PricingEngine } from "./services/PricingEngine.js";
// [KYA KARTA HAI]: OrderStateMachine aur assertNever helper import karta hai.
// [KYU ZAROORI HAI]: State transitions aur exhaustive pattern matching verify karne ke liye.
// [KAISE CHALTA HAI]: ES module import from OrderStateMachine.js.
// [OUTPUT / SIDE EFFECT]: OrderStateMachine loaded.
// [VARIATIONS TO TRY]: Workflow event listeners.
import { OrderStateMachine } from "./services/OrderStateMachine.js";
// [KYA KARTA HAI]: Modern JS showcase methods import karta hai.
// [KYU ZAROORI HAI]: ES2024 features test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ModernJsShowcase.js.
// [OUTPUT / SIDE EFFECT]: ModernJsShowcase loaded.
// [VARIATIONS TO TRY]: Custom reactive streams.
import { ModernJsShowcase } from "./modern-features/ModernJsShowcase.js";
// [KYA KARTA HAI]: In-Memory Generic Repository implementation define karta hai jo IRepository<T> contract follow karta hai.
// [KYU ZAROORI HAI]: TypeScript ke Generic Types aur Constraints (T extends Entity) ko real-world code me demonstrate karne ke liye.
// [KAISE CHALTA HAI]: Private Map<string, T> me items store karta hai aur Promise-based async methods return karta hai.
// [OUTPUT / SIDE EFFECT]: Generic Repository class.
// [VARIATIONS TO TRY]: IndexedDB ya LocalStorage adapter implement kar sakte hain.
class InMemoryRepository {
    // [KYA KARTA HAI]: Internal storage ke liye Map banata hai.
    // [KYU ZAROORI HAI]: O(1) id-based lookup aur deletion ke liye.
    // [KAISE CHALTA HAI]: new Map<string, T>() initialize hota hai.
    // [OUTPUT / SIDE EFFECT]: In-memory storage table.
    // [VARIATIONS TO TRY]: Deep clone on save to prevent external mutation.
    storage = new Map();
    // [KYA KARTA HAI]: Entity ko ID se fetch karta hai asynchronously.
    // [KYU ZAROORI HAI]: Read operation provide karne ke liye.
    // [KAISE CHALTA HAI]: Map.get(id) call karta hai aur undefined hone par null return karta hai.
    // [OUTPUT / SIDE EFFECT]: Promise<T | null>.
    // [VARIATIONS TO TRY]: Cache TTL validation.
    async getById(id) {
        return this.storage.get(id) ?? null;
    }
    // [KYA KARTA HAI]: Saare entities ko readonly array me return karta hai.
    // [KYU ZAROORI HAI]: Catalog listing ke liye.
    // [KAISE CHALTA HAI]: Array.from(this.storage.values()) return karta hai.
    // [OUTPUT / SIDE EFFECT]: Promise<readonly T[]>.
    // [VARIATIONS TO TRY]: Pagination pagination slice return karna.
    async getAll() {
        return Array.from(this.storage.values());
    }
    // [KYA KARTA HAI]: Entity ko save ya update karta hai.
    // [KYU ZAROORI HAI]: Write / Upsert operation ke liye.
    // [KAISE CHALTA HAI]: Map.set(entity.id, entity) store karta hai.
    // [OUTPUT / SIDE EFFECT]: Promise<void>.
    // [VARIATIONS TO TRY]: Optimistic concurrency version check.
    async save(entity) {
        this.storage.set(entity.id, entity);
    }
    // [KYA KARTA HAI]: Entity ko ID se delete karta hai.
    // [KYU ZAROORI HAI]: Removal operation ke liye.
    // [KAISE CHALTA HAI]: Map.delete(id) boolean return karta hai.
    // [OUTPUT / SIDE EFFECT]: Promise<boolean>.
    // [VARIATIONS TO TRY]: Soft-delete flag set karna.
    async delete(id) {
        return this.storage.delete(id);
    }
}
// [KYA KARTA HAI]: Main execution function jo sabhi TypeScript aur Modern JS concepts ko end-to-end execute aur assert karta hai.
// [KYU ZAROORI HAI]: Automated verification ensure karne ke liye taaki zero broken code rahe.
// [KAISE CHALTA HAI]: Async self-invoking function jo 6 structured test sections execute karta hai.
// [OUTPUT / SIDE EFFECT]: Test logs aur exit code 0 on success.
// [VARIATIONS TO TRY]: CI/CD pipeline integration.
async function runAllKiranaHubTypeScriptTests() {
    console.log("====================================================================");
    console.log("  KiranaHub Project 17: TypeScript & Modern JS Core Engine Test Suite");
    console.log("====================================================================");
    // -------------------------------------------------------------------------
    // TEST 1: Generic Repository & Entity Constraints
    // -------------------------------------------------------------------------
    console.log("\n[TEST 1] Testing Generic Repository<T extends Entity>...");
    const groceryRepo = new InMemoryRepository();
    const milkItem = {
        id: "item-101",
        name: "Amul Taaza Milk 1L",
        price: 64,
        category: "Dairy",
        stockQuantity: 50,
        isPerishable: true,
        expiryDays: 3,
    };
    const attaItem = {
        id: "item-102",
        name: "Aashirvaad Shudh Chakki Atta 5kg",
        price: 245,
        category: "Produce",
        stockQuantity: 30,
        isPerishable: false,
    };
    await groceryRepo.save(milkItem);
    await groceryRepo.save(attaItem);
    const fetchedMilk = await groceryRepo.getById("item-101");
    assert.strictEqual(fetchedMilk?.name, "Amul Taaza Milk 1L", "Milk name must match repository record");
    assert.strictEqual(fetchedMilk?.price, 64, "Milk price must be Rs 64");
    const allItems = await groceryRepo.getAll();
    assert.strictEqual(allItems.length, 2, "Repository must hold 2 items");
    console.log("  -> Generic Repository CRUD & constraints: PASSED");
    // -------------------------------------------------------------------------
    // TEST 2: Custom Type Guards & Assertion Functions
    // -------------------------------------------------------------------------
    console.log("\n[TEST 2] Testing Type Guards (isPerishableItem) & Assertion Functions...");
    assert.strictEqual(isPerishableItem(milkItem), true, "Milk must be recognized as perishable");
    assert.strictEqual(isPerishableItem(attaItem), false, "Atta must NOT be recognized as perishable");
    if (isPerishableItem(milkItem)) {
        // Narrowed type: compiler guarantees expiryDays is number
        const days = milkItem.expiryDays;
        assert.strictEqual(days, 3, "Narrowed expiry days must be 3");
    }
    // Assertion function testing
    assertValidRiderPhone("9876543210"); // Valid Indian 10-digit number
    assert.throws(() => assertValidRiderPhone("12345"), /Invalid rider phone number/, "Short phone numbers must be rejected by assertion function");
    console.log("  -> Type Guards & Assertion Functions: PASSED");
    // -------------------------------------------------------------------------
    // TEST 3: Discriminated Unions & Order State Machine
    // -------------------------------------------------------------------------
    console.log("\n[TEST 3] Testing Discriminated Unions & Exhaustive State Transitions...");
    const initialLines = [
        { item: milkItem, quantity: 2 },
        { item: attaItem, quantity: 1 },
    ];
    const placedOrder = {
        status: "Placed",
        orderId: "ord-8801",
        items: initialLines,
        totalAmount: 373,
        placedAtUtc: new Date(),
    };
    // State 1: Placed -> Packed
    const packedOrder = OrderStateMachine.packOrder(placedOrder, "picker-rajesh");
    assert.strictEqual(packedOrder.status, "Packed");
    assert.strictEqual(packedOrder.pickerId, "picker-rajesh");
    // State 2: Packed -> Dispatched
    const dispatchedOrder = OrderStateMachine.dispatchOrder(packedOrder, "Suresh Kumar", "9876501234", 12);
    assert.strictEqual(dispatchedOrder.status, "Dispatched");
    assert.strictEqual(dispatchedOrder.estimatedMinutes, 12);
    // State 3: Dispatched -> Delivered
    const deliveredOrder = OrderStateMachine.deliverOrder(dispatchedOrder, "Ananya Verma");
    assert.strictEqual(deliveredOrder.status, "Delivered");
    assert.strictEqual(deliveredOrder.receivedBy, "Ananya Verma");
    // State summary exhaustive pattern matching check
    const placedSummary = OrderStateMachine.getStatusSummary(placedOrder);
    const deliveredSummary = OrderStateMachine.getStatusSummary(deliveredOrder);
    assert.ok(placedSummary.includes("[Placed]"), "Summary must reflect Placed state");
    assert.ok(deliveredSummary.includes("[Delivered]"), "Summary must reflect Delivered state");
    // State rule enforcement: Delivered order cannot be cancelled
    assert.throws(() => OrderStateMachine.cancelOrder(deliveredOrder, "Changed mind"), /already been delivered/, "Delivered order cancellation must be rejected");
    // Cancellation of unfulfilled order
    const cancelledOrder = OrderStateMachine.cancelOrder(placedOrder, "Customer out of town");
    assert.strictEqual(cancelledOrder.status, "Cancelled");
    assert.strictEqual(cancelledOrder.refundInitiated, true);
    console.log("  -> Discriminated Unions & State Transitions: PASSED");
    // -------------------------------------------------------------------------
    // TEST 4: PricingEngine Calculations & Discount Rules
    // -------------------------------------------------------------------------
    console.log("\n[TEST 4] Testing PricingEngine (Subtotal, GST Tax, Free Delivery, Loyalty)...");
    // Subtotal = (64 * 2) + (245 * 1) = 128 + 245 = 373
    // Subtotal < 500, so standard delivery fee of Rs 30 applies.
    // 10% coupon rule:
    const couponRule = (subtotal) => Math.round(subtotal * 0.1);
    const breakdownWithCoupon = PricingEngine.calculate(initialLines, couponRule);
    assert.strictEqual(breakdownWithCoupon.subtotal, 373, "Subtotal must be Rs 373");
    assert.strictEqual(breakdownWithCoupon.discountAmount, 37, "10% discount on 373 must be 37");
    assert.strictEqual(breakdownWithCoupon.deliveryFee, 30, "Subtotal < 500 must trigger Rs 30 delivery fee");
    // Taxable = 373 - 37 = 336. Tax = 336 * 0.05 = 16.8
    assert.strictEqual(breakdownWithCoupon.taxAmount, 16.8, "5% GST on 336 must be 16.80");
    // Grand total = 336 + 16.8 + 30 = 382.8
    assert.strictEqual(breakdownWithCoupon.grandTotal, 382.8, "Grand total must be 382.80");
    assert.strictEqual(breakdownWithCoupon.earnedLoyaltyPoints, 3, "Rs 382.8 must earn 3 points");
    // High-value order eligible for FREE delivery (Subtotal >= 500)
    const highValueLines = [
        { item: attaItem, quantity: 3 }, // 245 * 3 = 735
    ];
    const breakdownFreeDelivery = PricingEngine.calculate(highValueLines);
    assert.strictEqual(breakdownFreeDelivery.subtotal, 735);
    assert.strictEqual(breakdownFreeDelivery.deliveryFee, 0, "Subtotal >= 500 must get FREE delivery (0 fee)");
    console.log("  -> PricingEngine & Discount Calculations: PASSED");
    // -------------------------------------------------------------------------
    // TEST 5: Utility Types Showcase (Partial, Omit, Readonly, Record)
    // -------------------------------------------------------------------------
    console.log("\n[TEST 5] Testing TypeScript Utility Types (Partial, Omit, Record)...");
    // Update DTO with partial properties excluding 'id'
    const patchDto = {
        price: 68,
        stockQuantity: 45,
    };
    assert.strictEqual(patchDto.price, 68);
    assert.strictEqual(patchDto.stockQuantity, 45);
    // Category Stock Record mapping
    const categoryInventory = {
        Dairy: 50,
        Produce: 30,
        Bakery: 0,
        Snacks: 15,
        Beverages: 25,
    };
    assert.strictEqual(categoryInventory.Dairy, 50);
    assert.strictEqual(categoryInventory.Produce, 30);
    console.log("  -> TypeScript Utility Types: PASSED");
    // -------------------------------------------------------------------------
    // TEST 6: Modern JavaScript Showcase (Object.groupBy, structuredClone, Async Iterators, Map/Set)
    // -------------------------------------------------------------------------
    console.log("\n[TEST 6] Testing Modern JS (Object.groupBy, structuredClone, Async Iterators, Map/Set)...");
    const catalogList = [
        milkItem,
        attaItem,
        {
            id: "item-103",
            name: "Britannia Brown Bread",
            price: 50,
            category: "Bakery",
            stockQuantity: 20,
            isPerishable: true,
            expiryDays: 5,
        },
    ];
    // 1. Object.groupBy
    const grouped = ModernJsShowcase.groupProductsByCategory(catalogList);
    assert.strictEqual(grouped.Dairy?.length, 1);
    assert.strictEqual(grouped.Produce?.length, 1);
    assert.strictEqual(grouped.Bakery?.length, 1);
    // 2. structuredClone deep cloning
    const originalCart = {
        orderId: "ord-test",
        timestamp: new Date("2026-11-14T10:00:00Z"),
        tags: new Set(["quick-delivery", "fragile"]),
    };
    const clonedCart = ModernJsShowcase.deepCloneCartState(originalCart);
    assert.deepStrictEqual(clonedCart, originalCart, "Cloned cart data must match original");
    assert.notStrictEqual(clonedCart, originalCart, "Cloned cart must have different memory reference");
    assert.notStrictEqual(clonedCart.tags, originalCart.tags, "Cloned Set must have different reference");
    // 3. Fast Search Index (Set deduplication & Map lookup)
    const searchIndex = ModernJsShowcase.buildFastSearchIndex(catalogList);
    assert.strictEqual(searchIndex.uniqueCategories.length, 3, "Must have 3 unique categories");
    assert.strictEqual(searchIndex.priceLookupMap.get("item-101"), 64, "O(1) price lookup for item-101 must be 64");
    // 4. Async Generator live telemetry streaming with 'for await...of'
    const telemetryPoints = [];
    for await (const packet of ModernJsShowcase.streamRiderTelemetry(28.6139, 77.209, 28.62, 77.215, 3)) {
        telemetryPoints.push(packet.remainingMeters);
    }
    assert.strictEqual(telemetryPoints.length, 3, "Stream must yield exactly 3 packets");
    assert.strictEqual(telemetryPoints[2], 0, "Final telemetry packet must reach destination (0 remaining meters)");
    console.log("  -> Modern JS Showcase & Async Streams: PASSED");
    console.log("\n====================================================================");
    console.log(" [SUCCESS] All TypeScript & Modern JS Core Engine checks PASSED!");
    console.log("====================================================================");
}
// Execute the test runner
runAllKiranaHubTypeScriptTests().catch((err) => {
    console.error("Test execution failed:", err);
    process.exit(1);
});
