// [KYA KARTA HAI]: Playwright Locator aur Page types import karta hai.
// [KYU ZAROORI HAI]: Cart drawer elements locate aur interact karne ke liye.
// [KAISE CHALTA HAI]: '@playwright/test' package se load hote hain.
// [OUTPUT / SIDE EFFECT]: Page Object types available.
// [VARIATIONS TO TRY]: Custom test fixtures.
import type { Page, Locator } from "@playwright/test";

// [KYA KARTA HAI]: Cart Drawer Page Object Model (POM) class define karta hai.
// [KYU ZAROORI HAI]: Cart items inspection, quantity changes, coupon redemption aur checkout actions encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Locators bind karta hai aur typed asynchronous actions expose karta hai.
// [OUTPUT / SIDE EFFECT]: CartDrawerPage POM class.
// [VARIATIONS TO TRY]: Auto-wait assertion helpers.
export class CartDrawerPage {
  // [KYA KARTA HAI]: Playwright Page instance store karta hai.
  // [KYU ZAROORI HAI]: DOM actions ke liye.
  // [KAISE CHALTA HAI]: Private readonly property.
  // [OUTPUT / SIDE EFFECT]: page property.
  // [VARIATIONS TO TRY]: Sub-frame page support.
  public readonly page: Page;

  // [KYA KARTA HAI]: Promo coupon input box locator.
  // [KYU ZAROORI HAI]: Coupon code enter karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('input[placeholder*="KIRANA10"]').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: getByPlaceholder locator.
  public readonly couponInput: Locator;

  // [KYA KARTA HAI]: Apply coupon button locator.
  // [KYU ZAROORI HAI]: Discount calculation trigger karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('button:has-text("Apply")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: getByRole locator.
  public readonly applyCouponButton: Locator;

  // [KYA KARTA HAI]: Delivery address textarea locator.
  // [KYU ZAROORI HAI]: Customer delivery address bharne ke liye.
  // [KAISE CHALTA HAI]: page.locator('textarea').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: Label-based locator.
  public readonly addressInput: Locator;

  // [KYA KARTA HAI]: Place Order checkout button locator.
  // [KYU ZAROORI HAI]: Order placement finalize karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('button:has-text("Place Order")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: RegExp text match.
  public readonly checkoutButton: Locator;

  // [KYA KARTA HAI]: Constructor jo locators initialize karta hai.
  // [KYU ZAROORI HAI]: Page Object setup ke liye.
  // [KAISE CHALTA HAI]: Properties assign hoti hain.
  // [OUTPUT / SIDE EFFECT]: CartDrawerPage instance.
  // [VARIATIONS TO TRY]: Context injection.
  constructor(page: Page) {
    this.page = page;
    this.couponInput = page.locator('input[placeholder*="KIRANA10"]');
    this.applyCouponButton = page.locator('button:has-text("Apply")');
    this.addressInput = page.locator("textarea");
    this.checkoutButton = page.locator('button:has-text("Place Order")');
  }

  // [KYA KARTA HAI]: Promo coupon apply karta hai.
  // [KYU ZAROORI HAI]: Discount voucher verification ke liye.
  // [KAISE CHALTA HAI]: couponInput.fill() aur applyCouponButton.click() call karta hai.
  // [OUTPUT / SIDE EFFECT]: Coupon applied.
  // [VARIATIONS TO TRY]: Check success message toast.
  public async applyCoupon(code: string): Promise<void> {
    await this.couponInput.fill(code);
    await this.applyCouponButton.click();
  }

  // [KYA KARTA HAI]: Delivery address enter karta hai.
  // [KYU ZAROORI HAI]: Shipping destination set karne ke liye.
  // [KAISE CHALTA HAI]: addressInput.fill() call karta hai.
  // [OUTPUT / SIDE EFFECT]: Address updated.
  // [VARIATIONS TO TRY]: Clear existing text first.
  public async setDeliveryAddress(address: string): Promise<void> {
    await this.addressInput.fill(address);
  }

  // [KYA KARTA HAI]: Checkout button click karta hai.
  // [KYU ZAROORI HAI]: Order submit karne ke liye.
  // [KAISE CHALTA HAI]: checkoutButton.click() call karta hai.
  // [OUTPUT / SIDE EFFECT]: Checkout triggered.
  // [VARIATIONS TO TRY]: Wait for navigation or network request finish.
  public async submitCheckout(): Promise<void> {
    await this.checkoutButton.click();
  }
}

