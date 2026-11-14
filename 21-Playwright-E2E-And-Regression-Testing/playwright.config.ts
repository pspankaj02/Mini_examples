// [KYA KARTA HAI]: Playwright configuration helper import karta hai.
// [KYU ZAROORI HAI]: Type-safe Playwright test runner settings configure karne ke liye.
// [KAISE CHALTA HAI]: defineConfig function typed options accept karta hai.
// [OUTPUT / SIDE EFFECT]: Playwright test config object.
// [VARIATIONS TO TRY]: Custom testDir or timeout.
import { defineConfig, devices } from "@playwright/test";

// [KYA KARTA HAI]: Playwright test framework configuration define karta hai.
// [KYU ZAROORI HAI]: Test timeouts, retries, reporters, aur cross-browser matrices setup karne ke liye.
// [KAISE CHALTA HAI]: Static configuration object export.
// [OUTPUT / SIDE EFFECT]: Exported config for Playwright CLI.
// [VARIATIONS TO TRY]: Video recording on failure: 'retain-on-failure'.
export default defineConfig({
  testDir: "./tests",
  timeout: 30000,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "list",
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] },
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
    },
  ],
});

