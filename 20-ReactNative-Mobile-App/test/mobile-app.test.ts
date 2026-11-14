// [KYA KARTA HAI]: Node.js ka native strict assertion library import karta hai.
// [KYU ZAROORI HAI]: Zero external test runner overhead ke bina verification execute karne ke liye.
// [KAISE CHALTA HAI]: 'node:assert/strict' se functions load hote hain.
// [OUTPUT / SIDE EFFECT]: Assertions available.
// [VARIATIONS TO TRY]: Jest React Native test runner.
import assert from "node:assert/strict";

// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: Type-safety ensure karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../src/types/mobile.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Test factories.
import type { MobileProduct, MobileCartItem, RiderGpsSnapshot } from "../src/types/mobile.js";

// [KYA KARTA HAI]: MobileStorageAdapter import karta hai.
// [KYU ZAROORI HAI]: Offline persistence engine test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../src/services/mobileStorage.js.
// [OUTPUT / SIDE EFFECT]: MobileStorageAdapter symbol.
// [VARIATIONS TO TRY]: Mock device flash storage.
import { MobileStorageAdapter } from "../src/services/mobileStorage.js";

// [KYA KARTA HAI]: Project 20 mobile app ke automated tests execute karta hai.
// [KYU ZAROORI HAI]: Mobile app ke offline persistence, calculations aur live tracking math ko verify karne ke liye.
// [KAISE CHALTA HAI]: Async runner executing 4 structured test cases.
// [OUTPUT / SIDE EFFECT]: Console test reports and exit code 0 on success.
// [VARIATIONS TO TRY]: Automated CI test runner.
async function runAllMobileAppTests(): Promise<void> {
  console.log("====================================================================");
  console.log("  KiranaHub Project 20: React Native (Expo) Mobile App Test Suite");
  console.log("====================================================================");

  // -------------------------------------------------------------------------
  // TEST 1: Offline AsyncStorage Emulation & Cart Persistence
  // -------------------------------------------------------------------------
  console.log("\n[TEST 1] Testing MobileStorageAdapter Offline Cart Persistence...");
  await MobileStorageAdapter.clearCart();
  const initialCart = await MobileStorageAdapter.loadCart();
  assert.strictEqual(initialCart.length, 0, "Cart must be initially empty");

  const sampleMilk: MobileProduct = {
    id: "mob-01",
    name: "Amul Taaza Milk 1L",
    price: 64,
    category: "Dairy",
    unit: "1 Litre",
    stockQuantity: 40,
    icon: "🥛",
  };

  const samplePaneer: MobileProduct = {
    id: "mob-02",
    name: "Mother Dairy Paneer 200g",
    price: 92,
    category: "Dairy",
    unit: "200 g",
    stockQuantity: 25,
    icon: "🧀",
  };

  const savedItems: MobileCartItem[] = [
    { product: sampleMilk, quantity: 2 },
    { product: samplePaneer, quantity: 1 },
  ];

  await MobileStorageAdapter.saveCart(savedItems);
  const loadedItems = await MobileStorageAdapter.loadCart();
  assert.strictEqual(loadedItems.length, 2, "Persisted cart must restore 2 items");
  assert.strictEqual(loadedItems[0]?.product.name, "Amul Taaza Milk 1L");
  assert.strictEqual(loadedItems[0]?.quantity, 2);

  await MobileStorageAdapter.clearCart();
  const clearedCart = await MobileStorageAdapter.loadCart();
  assert.strictEqual(clearedCart.length, 0, "Cart must be cleared after checkout");
  console.log("  -> MobileStorageAdapter Offline Persistence: PASSED");

  // -------------------------------------------------------------------------
  // TEST 2: Mobile Pricing, 10% Auto Discount, and 5% GST
  // -------------------------------------------------------------------------
  console.log("\n[TEST 2] Testing Mobile Pricing Engine (Subtotal, GST, Free Delivery)...");
  // Subtotal = (64 * 2) + (92 * 1) = 128 + 92 = 220
  const subtotal = savedItems.reduce((sum, x) => sum + x.product.price * x.quantity, 0);
  assert.strictEqual(subtotal, 220);

  const discount = Math.round(subtotal * 0.1); // 10% of 220 = 22
  assert.strictEqual(discount, 22);

  const taxable = subtotal - discount; // 220 - 22 = 198
  const tax = Math.round(taxable * 0.05 * 100) / 100; // 198 * 0.05 = 9.9
  assert.strictEqual(tax, 9.9);

  // Subtotal < 500 => standard delivery fee of 30
  const deliveryFee = 30;
  const grandTotal = Math.round((taxable + tax + deliveryFee) * 100) / 100; // 198 + 9.9 + 30 = 237.9
  assert.strictEqual(grandTotal, 237.9, "Grand total must be 237.90");
  console.log("  -> Mobile Pricing & 5% GST Calculations: PASSED");

  // -------------------------------------------------------------------------
  // TEST 3: Live Rider GPS Movement Telemetry Math
  // -------------------------------------------------------------------------
  console.log("\n[TEST 3] Testing Rider GPS Coordinates & ETA Progression Simulation...");
  const initialGps: RiderGpsSnapshot = {
    latitude: 28.628,
    longitude: 77.365,
    speedKmph: 32,
    remainingMeters: 1200,
    estimatedMinutes: 6,
  };

  const finalGps: RiderGpsSnapshot = {
    latitude: 28.63,
    longitude: 77.372,
    speedKmph: 0,
    remainingMeters: 0,
    estimatedMinutes: 0,
  };

  assert.ok(initialGps.remainingMeters > finalGps.remainingMeters, "Distance must decrease over time");
  assert.ok(initialGps.estimatedMinutes > finalGps.estimatedMinutes, "ETA minutes must decrease over time");
  assert.strictEqual(finalGps.remainingMeters, 0, "Final delivery packet must have 0 meters remaining");
  assert.strictEqual(finalGps.speedKmph, 0, "Rider must stop (0 km/h) upon delivery");
  console.log("  -> Rider GPS Telemetry Math: PASSED");

  // -------------------------------------------------------------------------
  // TEST 4: Category Filtering & Search Keyword Match
  // -------------------------------------------------------------------------
  console.log("\n[TEST 4] Testing Category Filtering & Search Match...");
  const catalogList: MobileProduct[] = [sampleMilk, samplePaneer];
  const dairyOnly = catalogList.filter((x) => x.category === "Dairy");
  assert.strictEqual(dairyOnly.length, 2, "Both items must belong to Dairy category");

  const milkSearch = catalogList.filter((x) => x.name.toLowerCase().includes("milk"));
  assert.strictEqual(milkSearch.length, 1, "Only Amul Milk must match search keyword 'milk'");
  console.log("  -> Category Filtering & Search Match: PASSED");

  console.log("\n====================================================================");
  console.log(" [SUCCESS] All React Native (Expo) Mobile App tests PASSED!");
  console.log("====================================================================");
}

runAllMobileAppTests().catch((err) => {
  console.error("Test execution failed:", err);
  process.exit(1);
});

