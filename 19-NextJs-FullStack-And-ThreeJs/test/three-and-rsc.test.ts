// [KYA KARTA HAI]: Node.js ka native strict assertion library import karta hai.
// [KYU ZAROORI HAI]: Zero external test runner overhead ke bina verification execute karne ke liye.
// [KAISE CHALTA HAI]: 'node:assert/strict' se functions load hote hain.
// [OUTPUT / SIDE EFFECT]: Assertions available.
// [VARIATIONS TO TRY]: Vitest / Jest test runner.
import assert from "node:assert/strict";

// [KYA KARTA HAI]: Three.js library import karta hai.
// [KYU ZAROORI HAI]: Headless 3D math, scene tree aur mesh building test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from 'three'.
// [OUTPUT / SIDE EFFECT]: THREE symbols available.
// [VARIATIONS TO TRY]: Headless WebGL testing with gl package.
import * as THREE from "three";

// [KYA KARTA HAI]: Server Actions import karta hai.
// [KYU ZAROORI HAI]: Server actions logic (coupon, tax, order placement) unit test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../app/actions/cartActions.js.
// [OUTPUT / SIDE EFFECT]: validateAndApplyCoupon, submitOrderAction symbols.
// [VARIATIONS TO TRY]: Mock FormData injection.
import { validateAndApplyCoupon, submitOrderAction } from "../app/actions/cartActions";

// [KYA KARTA HAI]: Route Handler import karta hai.
// [KYU ZAROORI HAI]: Next.js Route Handler GET response test karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../app/api/catalog/route.
// [OUTPUT / SIDE EFFECT]: GET function.
// [VARIATIONS TO TRY]: Pass NextRequest object.
import { GET as getCatalogRoute } from "../app/api/catalog/route";

// [KYA KARTA HAI]: Main test execution runner function.
// [KYU ZAROORI HAI]: Next.js Full-Stack aur Three.js 3D engine ko systematically test karne ke liye.
// [KAISE CHALTA HAI]: Async function executing 4 structured test cases.
// [OUTPUT / SIDE EFFECT]: Console test reports and exit code 0 on success.
// [VARIATIONS TO TRY]: Automated CI test runner.
async function runAllNextJsAndThreeJsTests(): Promise<void> {
  console.log("====================================================================");
  console.log("  KiranaHub Project 19: Next.js 15 + Three.js 3D Test Suite");
  console.log("====================================================================");

  // -------------------------------------------------------------------------
  // TEST 1: Three.js 3D Scene, Geometry, PBR Materials & Tree Structure
  // -------------------------------------------------------------------------
  console.log("\n[TEST 1] Testing Three.js 3D Scene Graph, Camera & PBR Material...");
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(45, 1.2, 0.1, 100);
  camera.position.set(0, 1.2, 3.8);

  const boxGeometry = new THREE.BoxGeometry(1.6, 1.4, 1.6);
  const standardMaterial = new THREE.MeshStandardMaterial({
    roughness: 0.45,
    metalness: 0.1,
  });

  const crateMesh = new THREE.Mesh(boxGeometry, standardMaterial);
  scene.add(crateMesh);

  // Verify scene tree
  assert.strictEqual(scene.children.length, 1, "Scene must hold exactly 1 mesh");
  assert.strictEqual(scene.children[0], crateMesh, "Child must be the crate mesh");
  assert.strictEqual(crateMesh.geometry.type, "BoxGeometry", "Geometry must be BoxGeometry");
  assert.strictEqual(crateMesh.material.type, "MeshStandardMaterial", "Material must be PBR standard");

  // Verify 3D rotation math
  crateMesh.rotation.y += Math.PI / 2; // 90 degree rotation
  assert.ok(Math.abs(crateMesh.rotation.y - Math.PI / 2) < 0.0001, "Rotation on Y-axis must be 90 degrees");
  console.log("  -> Three.js 3D Scene Graph & PBR Materials: PASSED");

  // -------------------------------------------------------------------------
  // TEST 2: Server Action: validateAndApplyCoupon
  // -------------------------------------------------------------------------
  console.log("\n[TEST 2] Testing Next.js Server Action: validateAndApplyCoupon...");
  // Case A: 10% coupon (KIRANA10)
  const result10 = await validateAndApplyCoupon("KIRANA10", 400);
  assert.strictEqual(result10.success, true);
  assert.strictEqual(result10.discount, 40, "10% of 400 must be 40");
  assert.strictEqual(result10.deliveryFee, 30, "Subtotal < 500 must trigger Rs 30 delivery fee");
  // Taxable = 400 - 40 = 360. Tax = 360 * 0.05 = 18
  assert.strictEqual(result10.tax, 18);
  // Grand total = 360 + 18 + 30 = 408
  assert.strictEqual(result10.grandTotal, 408);

  // Case B: High-value order (Subtotal >= 500) with FREE delivery
  const resultHigh = await validateAndApplyCoupon("KIRANA10", 600);
  assert.strictEqual(resultHigh.deliveryFee, 0, "Orders >= 500 must receive FREE delivery (0 fee)");

  // Case C: Invalid coupon code
  const resultInvalid = await validateAndApplyCoupon("BOGUS_CODE", 200);
  assert.strictEqual(resultInvalid.success, false);
  assert.strictEqual(resultInvalid.discount, 0);
  console.log("  -> Server Action validateAndApplyCoupon: PASSED");

  // -------------------------------------------------------------------------
  // TEST 3: Server Action: submitOrderAction with FormData
  // -------------------------------------------------------------------------
  console.log("\n[TEST 3] Testing Next.js Server Action: submitOrderAction with FormData...");
  const validFormData = new FormData();
  validFormData.set("deliveryAddress", "B-201, Express Park View, Greater Noida");
  validFormData.set("itemsJson", JSON.stringify([{ id: "nx-01", qty: 2 }]));
  validFormData.set("couponCode", "KIRANA10");

  const orderResult = await submitOrderAction(validFormData);
  assert.strictEqual(orderResult.success, true);
  assert.ok(orderResult.orderId?.startsWith("KRN-"), "Order ID must start with 'KRN-' prefix");

  // Missing address check
  const invalidFormData = new FormData();
  const failedResult = await submitOrderAction(invalidFormData);
  assert.strictEqual(failedResult.success, false);
  assert.strictEqual(failedResult.error, "Delivery address is required.");
  console.log("  -> Server Action submitOrderAction: PASSED");

  // -------------------------------------------------------------------------
  // TEST 4: Next.js Route Handler: /api/catalog
  // -------------------------------------------------------------------------
  console.log("\n[TEST 4] Testing Next.js Route Handler GET /api/catalog...");
  const response = await getCatalogRoute();
  assert.strictEqual(response.status, 200, "Route Handler must return HTTP 200 OK");
  assert.strictEqual(response.headers.get("X-Powered-By"), "KiranaHub Next.js App Router");

  const body = (await response.json()) as { success: boolean; data: any[]; totalCount: number };
  assert.strictEqual(body.success, true);
  assert.ok(body.data.length >= 6, "Must return at least 6 products");
  console.log(`  -> Route Handler returned ${body.totalCount} products: PASSED`);

  console.log("\n====================================================================");
  console.log(" [SUCCESS] All Next.js 15 + Three.js 3D Core tests PASSED!");
  console.log("====================================================================");
}

runAllNextJsAndThreeJsTests().catch((err) => {
  console.error("Test execution failed:", err);
  process.exit(1);
});
