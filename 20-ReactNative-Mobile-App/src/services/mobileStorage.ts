// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: MobileCartItem type verification ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/mobile.js.
// [OUTPUT / SIDE EFFECT]: MobileCartItem symbol.
// [VARIATIONS TO TRY]: Import session user types.
import type { MobileCartItem } from "../types/mobile.js";

// [KYA KARTA HAI]: Storage key constant define karta hai.
// [KYU ZAROORI HAI]: Hardcoded key string typos se bachane ke liye.
// [KAISE CHALTA HAI]: String constant.
// [OUTPUT / SIDE EFFECT]: Storage key "kiranahub_mobile_cart_v1".
// [VARIATIONS TO TRY]: Dynamic user-scoped key.
const MOBILE_CART_KEY = "kiranahub_mobile_cart_v1";

// [KYA KARTA HAI]: In-memory storage table simulate karta hai (jab AsyncStorage ya LocalStorage unavailable ho).
// [KYU ZAROORI HAI]: Unit testing aur cross-platform offline fallback ensure karne ke liye.
// [KAISE CHALTA HAI]: Map<string, string> internal memory dictionary.
// [OUTPUT / SIDE EFFECT]: Simulated device flash storage.
// [VARIATIONS TO TRY]: SQLite mobile database adapter.
const memoryStorage = new Map<string, string>();

// [KYA KARTA HAI]: Cross-platform offline mobile storage adapter class.
// [KYU ZAROORI HAI]: Cart items ko device flash storage me persist karne aur app reopen par reload karne ke liye.
// [KAISE CHALTA HAI]: AsyncStorage / LocalStorage / MemoryStorage me JSON serialize karke asynchronously read/write karta hai.
// [OUTPUT / SIDE EFFECT]: MobileStorageAdapter class.
// [VARIATIONS TO TRY]: EncryptedStorage adapter for sensitive user tokens.
export class MobileStorageAdapter {
  // [KYA KARTA HAI]: Cart items ko device storage me save karta hai.
  // [KYU ZAROORI HAI]: Cart persistence ensure karne ke liye agar customer app kill kar de.
  // [KAISE CHALTA HAI]: JSON.stringify(items) karke key-value store me write karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<void>.
  // [VARIATIONS TO TRY]: Debounced write on rapid user taps.
  public static async saveCart(items: readonly MobileCartItem[]): Promise<void> {
    const serialized = JSON.stringify(items);
    if (typeof localStorage !== "undefined") {
      try {
        localStorage.setItem(MOBILE_CART_KEY, serialized);
        return;
      } catch {
        // Fallback to memory
      }
    }
    memoryStorage.set(MOBILE_CART_KEY, serialized);
  }

  // [KYA KARTA HAI]: Device storage se persisted cart items load karta hai.
  // [KYU ZAROORI HAI]: App restart hone par customer ke items restore karne ke liye.
  // [KAISE CHALTA HAI]: Storage se string fetch karke JSON.parse karta hai, agar na mile toh empty array [] return karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<readonly MobileCartItem[]>.
  // [VARIATIONS TO TRY]: Stale cart expiry check (e.g. expire items older than 24 hours).
  public static async loadCart(): Promise<readonly MobileCartItem[]> {
    let serialized: string | null = null;
    if (typeof localStorage !== "undefined") {
      try {
        serialized = localStorage.getItem(MOBILE_CART_KEY);
      } catch {
        // Fallback to memory
      }
    }
    if (!serialized) {
      serialized = memoryStorage.get(MOBILE_CART_KEY) ?? null;
    }

    if (!serialized) {
      return [];
    }

    try {
      return JSON.parse(serialized) as MobileCartItem[];
    } catch {
      return [];
    }
  }

  // [KYA KARTA HAI]: Persisted cart data ko delete karta hai.
  // [KYU ZAROORI HAI]: Order successfully place hone par storage clean karne ke liye.
  // [KAISE CHALTA HAI]: Storage se key remove karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<void>.
  // [VARIATIONS TO TRY]: Archive placed cart history.
  public static async clearCart(): Promise<void> {
    if (typeof localStorage !== "undefined") {
      try {
        localStorage.removeItem(MOBILE_CART_KEY);
      } catch {
        // Fallback
      }
    }
    memoryStorage.delete(MOBILE_CART_KEY);
  }
}

