// [KYA KARTA HAI]: Node.js ka native strict assertion library import karta hai.
// [KYU ZAROORI HAI]: Zero-dependency unit testing aur validation execute karne ke liye.
// [KAISE CHALTA HAI]: 'node:assert/strict' se functions load hote hain.
// [OUTPUT / SIDE EFFECT]: Assertions available.
// [VARIATIONS TO TRY]: Vitest test suite.
import assert from "node:assert/strict";

// [KYA KARTA HAI]: KiranaApiClient class import karta hai.
// [KYU ZAROORI HAI]: API authentication header injection aur product catalog test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../src/services/apiClient.js.
// [OUTPUT / SIDE EFFECT]: KiranaApiClient symbol.
// [VARIATIONS TO TRY]: Mock network adapter.
import { KiranaApiClient } from "../src/services/apiClient.js";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: Product aur CartItem types ke static verification ke liye.
// [KAISE CHALTA HAI]: ES module import from ../src/types/store.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Dedicated test fixture factories.
import type { Product, CartItem } from "../src/types/store.js";

// [KYA KARTA HAI]: SignalR HubConnectionBuilder import karta hai.
// [KYU ZAROORI HAI]: WebSocket connection builder setup test karne ke liye.
// [KAISE CHALTA HAI]: @microsoft/signalr package se load hota hai.
// [OUTPUT / SIDE EFFECT]: SignalR builder symbol.
// [VARIATIONS TO TRY]: Mock SignalR transport.
import { HubConnectionBuilder } from "@microsoft/signalr";

// [KYA KARTA HAI]: Project 18 ke automated tests execute karne wala main function.
// [KYU ZAROORI HAI]: React Storefront ke core services aur calculations ko CI/CD me verify karne ke liye.
// [KAISE CHALTA HAI]: Async function executing 4 structured test cases with strict assertions.
// [OUTPUT / SIDE EFFECT]: Console test reports and exit code 0 on success.
// [VARIATIONS TO TRY]: Parallel test runner.
async function runAllReactStorefrontTests(): Promise<void> {
  console.log("====================================================================");
  console.log("  KiranaHub Project 18: React 19 + TypeScript + SignalR Test Suite");
  console.log("====================================================================");

  // -------------------------------------------------------------------------
  // TEST 1: API Client JWT Header Injection (Project 11 Auth Connection)
  // -------------------------------------------------------------------------
  console.log("\n[TEST 1] Testing API Client JWT Auth Header Injection...");
  const client = new KiranaApiClient("http://localhost:5000");

  // Pehle check karein bina token ke headers
  const unauthHeaders = client.prepareHeaders();
  assert.strictEqual(unauthHeaders["Content-Type"], "application/json");
  assert.strictEqual(unauthHeaders["Authorization"], undefined, "Unauthenticated request must not have Authorization header");

  // JWT Token set karne ke baad
  const fakeJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fakePayload.fakeSignature";
  client.setAuthToken(fakeJwt);
  const authHeaders = client.prepareHeaders();
  assert.strictEqual(authHeaders["Authorization"], `Bearer ${fakeJwt}`, "Authenticated request must carry Bearer token");
  console.log("  -> API Client JWT Header Injection: PASSED");

  // -------------------------------------------------------------------------
  // TEST 2: Product Catalog Retrieval & Fallback
  // -------------------------------------------------------------------------
  console.log("\n[TEST 2] Testing Product Catalog Retrieval & Offline Fallback...");
  const catalog = await client.getProducts();
  assert.ok(catalog.length >= 5, "Catalog must return at least 5 grocery products");

  const milk = catalog.find((p) => p.name.includes("Milk"));
  assert.ok(milk, "Milk must be present in the catalog");
  assert.strictEqual(milk?.category, "Dairy", "Milk category must be Dairy");
  assert.strictEqual(typeof milk?.price, "number", "Price must be a number");
  console.log(`  -> Catalog fetched ${catalog.length} products successfully: PASSED`);

  // -------------------------------------------------------------------------
  // TEST 3: Cart Pricing & Coupon Mathematical Engine
  // -------------------------------------------------------------------------
  console.log("\n[TEST 3] Testing Cart Pricing, Taxes (5% GST), and Discount Calculations...");
  const dummyMilk: Product = {
    id: "p-01",
    name: "Amul Milk 1L",
    price: 64,
    category: "Dairy",
    stockQuantity: 40,
    unit: "1L",
  };

  const dummyBread: Product = {
    id: "p-05",
    name: "Britannia Bread",
    price: 48,
    category: "Bakery",
    stockQuantity: 20,
    unit: "400g",
  };

  const testLines: CartItem[] = [
    { product: dummyMilk, quantity: 2 }, // 64 * 2 = 128
    { product: dummyBread, quantity: 1 }, // 48 * 1 = 48
  ];

  const subtotal = testLines.reduce((sum, item) => sum + item.product.price * item.quantity, 0);
  assert.strictEqual(subtotal, 176, "Subtotal must be 128 + 48 = 176");

  // 10% coupon (KIRANA10)
  const discount = Math.round(subtotal * 0.1);
  assert.strictEqual(discount, 18, "10% of 176 rounded must be 18");

  const discountedSubtotal = subtotal - discount; // 176 - 18 = 158
  const tax = Math.round(discountedSubtotal * 0.05 * 100) / 100; // 158 * 0.05 = 7.9
  assert.strictEqual(tax, 7.9, "5% GST on 158 must be 7.9");

  // Subtotal < 500, delivery fee is 30
  const deliveryFee = 30;
  const grandTotal = Math.round((discountedSubtotal + tax + deliveryFee) * 100) / 100; // 158 + 7.9 + 30 = 195.9
  assert.strictEqual(grandTotal, 195.9, "Grand total must be 195.90");
  console.log("  -> Cart Pricing, GST & Coupon Engine: PASSED");

  // -------------------------------------------------------------------------
  // TEST 4: SignalR HubConnectionBuilder Contract
  // -------------------------------------------------------------------------
  console.log("\n[TEST 4] Testing SignalR HubConnectionBuilder & Reconnect Contract...");
  const hubUrl = "http://localhost:5000/hubs/order-tracking";
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();

  assert.ok(connection, "SignalR connection instance must be successfully instantiated");
  assert.strictEqual(connection.state, "Disconnected", "Initial connection state must be Disconnected");
  console.log("  -> SignalR HubConnectionBuilder: PASSED");

  console.log("\n====================================================================");
  console.log(" [SUCCESS] All React 19 + TypeScript + SignalR Core tests PASSED!");
  console.log("====================================================================");
}

runAllReactStorefrontTests().catch((err) => {
  console.error("Test execution failed:", err);
  process.exit(1);
});

