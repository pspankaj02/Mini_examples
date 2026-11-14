// [KYA KARTA HAI]: Playwright Locator aur Page types import karta hai.
// [KYU ZAROORI HAI]: Browser DOM elements interact aur locate karne ke liye.
// [KAISE CHALTA HAI]: '@playwright/test' package se load hote hain.
// [OUTPUT / SIDE EFFECT]: Page Object model types available.
// [VARIATIONS TO TRY]: Custom locator helpers.
import type { Page, Locator } from "@playwright/test";

// [KYA KARTA HAI]: Catalog Page Object Model (POM) class define karta hai.
// [KYU ZAROORI HAI]: Test scripts ko low-level CSS selectors se isolate karne ke liye (maintainability best practice).
// [KAISE CHALTA HAI]: Page instance inject hota hai aur locators encapsulate kiye jate hain.
// [OUTPUT / SIDE EFFECT]: CatalogPage POM class.
// [VARIATIONS TO TRY]: BasePage inheritance.
export class CatalogPage {
  // [KYA KARTA HAI]: Playwright Page instance store karta hai.
  // [KYU ZAROORI HAI]: Browser navigation aur DOM actions perform karne ke liye.
  // [KAISE CHALTA HAI]: Private readonly property.
  // [OUTPUT / SIDE EFFECT]: page property.
  // [VARIATIONS TO TRY]: Context level page sharing.
  public readonly page: Page;

  // [KYA KARTA HAI]: Search input box locator.
  // [KYU ZAROORI HAI]: Search text type karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('input[placeholder*="Search"]').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: getByPlaceholder locator.
  public readonly searchInput: Locator;

  // [KYA KARTA HAI]: Cart open button locator.
  // [KYU ZAROORI HAI]: Cart drawer slide open karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('button:has-text("Cart")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: getByRole('button', { name: 'Cart' }).
  public readonly cartButton: Locator;

  // [KYA KARTA HAI]: Header live tracking indicator button locator.
  // [KYU ZAROORI HAI]: Live order dialog reopen karne ke liye.
  // [KAISE CHALTA HAI]: page.locator('button:has-text("Live Order")').
  // [OUTPUT / SIDE EFFECT]: Locator instance.
  // [VARIATIONS TO TRY]: getByText locator.
  public readonly liveOrderButton: Locator;

  // [KYA KARTA HAI]: Constructor jo page inject karke locators bind karta hai.
  // [KYU ZAROORI HAI]: Page Object initialize karne ke liye.
  // [KAISE CHALTA HAI]: Locators assign hote hain.
  // [OUTPUT / SIDE EFFECT]: CatalogPage instance.
  // [VARIATIONS TO TRY]: Custom baseUrl injection.
  constructor(page: Page) {
    this.page = page;
    this.searchInput = page.locator('input[placeholder*="Search"]');
    this.cartButton = page.locator('button:has-text("Cart")');
    this.liveOrderButton = page.locator('button:has-text("Live Order")');
  }

  // [KYA KARTA HAI]: Storefront home page par navigate karta hai.
  // [KYU ZAROORI HAI]: Test start point par browser navigate karne ke liye.
  // [KAISE CHALTA HAI]: await page.goto('/') execute karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<void>.
  // [VARIATIONS TO TRY]: waitUntil: 'networkidle'.
  public async goto(): Promise<void> {
    await this.page.goto("/");
  }

  // [KYA KARTA HAI]: Search box me grocery query enter karta hai.
  // [KYU ZAROORI HAI]: Live search filter automation ke liye.
  // [KAISE CHALTA HAI]: searchInput.fill(query) execute karta hai.
  // [OUTPUT / SIDE EFFECT]: Input value filled.
  // [VARIATIONS TO TRY]: Slow typing simulation with pressSequentially.
  public async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
  }

  // [KYA KARTA HAI]: Specific category chip par click karta hai.
  // [KYU ZAROORI HAI]: Department filter test karne ke liye.
  // [KAISE CHALTA HAI]: page.locator(`button:has-text("${categoryName}")`).click().
  // [OUTPUT / SIDE EFFECT]: Category selected.
  // [VARIATIONS TO TRY]: Case-insensitive category match.
  public async selectCategory(categoryName: string): Promise<void> {
    await this.page.locator(`button:has-text("${categoryName}")`).click();
  }

  // [KYA KARTA HAI]: Product card par 'ADD' button click karke item cart me dalta hai.
  // [KYU ZAROORI HAI]: Purchase simulation ke liye.
  // [KAISE CHALTA HAI]: Product text ke context me button locate karke click karta hai.
  // [OUTPUT / SIDE EFFECT]: Item added to cart.
  // [VARIATIONS TO TRY]: Quantity parameter to click + multiple times.
  public async addProductToCart(productName: string): Promise<void> {
    const card = this.page.locator(`div:has-text("${productName}")`).last();
    await card.locator('button:has-text("ADD")').click();
  }

  // [KYA KARTA HAI]: Cart drawer open karta hai.
  // [KYU ZAROORI HAI]: Checkout flow proceed karne ke liye.
  // [KAISE CHALTA HAI]: cartButton.click().
  // [OUTPUT / SIDE EFFECT]: Cart drawer opened.
  // [VARIATIONS TO TRY]: Double check drawer open state.
  public async openCart(): Promise<void> {
    await this.cartButton.click();
  }
}

