// [KYA KARTA HAI]: Playwright Locator aur Page types import karta hai.
// [KYU ZAROORI HAI]: Tracking modal elements interact aur verify karne ke liye.
// [KAISE CHALTA HAI]: '@playwright/test' package se load hote hain.
// [OUTPUT / SIDE EFFECT]: Page Object types available.
// [VARIATIONS TO TRY]: Custom expectation matchers.
import type { Page, Locator } from "@playwright/test";

// [KYA KARTA HAI]: Order Tracking Page Object Model (POM) class define karta hai.
// [KYU ZAROORI HAI]: Real-time status badges, rider coordinates aur stepper milestones assert karne ke liye.
// [KAISE CHALTA HAI]: Locators bind karta hai aur status assertion methods provide karta hai.
// [OUTPUT / SIDE EFFECT]: OrderTrackingPage POM class.
// [VARIATIONS TO TRY]: Screenshot visual comparison method.
export class OrderTrackingPage {
  // [KYA KARTA HAI]: Playwright Page instance store karta hai.
  // [KYU ZAROORI HAI]: Modal DOM queries execute karne ke liye.
  // [KAISE CHALTA HAI]: Private readonly property.
  // [OUTPUT / SIDE EFFECT]: page property.
  // [VARIATIONS TO TRY]: Active browser context sharing.
  public readonly page: Page;

  // [KYA KARTA HAI]: Modal title locator.
  // [KYU ZAROORI HAI]: Order ID presentation verify karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('h3:has-text("Live Order")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: Exact regex match.
  public readonly modalTitle: Locator;

  // [KYA KARTA HAI]: Current status banner locator.
  // [KYU ZAROORI HAI]: 'Order Received', 'Packed', 'Out for Delivery' text verify karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('div:has-text("CURRENT STATUS")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: Sibling element lookup.
  public readonly statusBanner: Locator;

  // [KYA KARTA HAI]: Modal close button locator.
  // [KYU ZAROORI HAI]: Tracking modal dismiss karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('button:has-text("Close Tracking")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: Top right '✕' button locator.
  public readonly closeButton: Locator;

  // [KYA KARTA HAI]: Constructor jo tracking locators bind karta hai.
  // [KYU ZAROORI HAI]: Page Object setup ke liye.
  // [KAISE CHALTA HAI]: Properties assign hoti hain.
  // [OUTPUT / SIDE EFFECT]: OrderTrackingPage instance.
  // [VARIATIONS TO TRY]: Custom polling timeout.
  constructor(page: Page) {
    this.page = page;
    this.modalTitle = page.locator('h3:has-text("Live Order")');
    this.statusBanner = page.locator('div:has-text("CURRENT STATUS")');
    this.closeButton = page.locator('button:has-text("Close Tracking")');
  }

  // [KYA KARTA HAI]: Tracking modal dismiss karta hai.
  // [KYU ZAROORI HAI]: Main storefront view par return hone ke liye.
  // [KAISE CHALTA HAI]: closeButton.click() call karta hai.
  // [OUTPUT / SIDE EFFECT]: Modal dismissed.
  // [VARIATIONS TO TRY]: Escape key press simulation.
  public async closeTracking(): Promise<void> {
    await this.closeButton.click();
  }
}

