// [KYA KARTA HAI]: React core hooks import karta hai state aur effects manage karne ke liye.
// [KYU ZAROORI HAI]: Reactive cart state, lifecycle sync aur memoized calculations ke liye.
// [KAISE CHALTA HAI]: useState, useEffect, useMemo, useCallback React standard library se load hote hain.
// [OUTPUT / SIDE EFFECT]: React hooks symbols available.
// [VARIATIONS TO TRY]: External state library jaise Zustand ya Redux Toolkit.
import { useState, useEffect, useMemo, useCallback } from "react";

// [KYA KARTA HAI]: Cart domain types import karta hai.
// [KYU ZAROORI HAI]: Type-safety ensure karne ke liye.
// [KAISE CHALTA HAI]: ES module import syntax from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: Product, CartItem, PricingSummary types loaded.
// [VARIATIONS TO TRY]: Import unified types from index.
import type { Product, CartItem, PricingSummary } from "../types/store.js";

// [KYA KARTA HAI]: LocalStorage storage key constant define karta hai.
// [KYU ZAROORI HAI]: Hardcoded key string typos se bachane ke liye.
// [KAISE CHALTA HAI]: Immutable string constant.
// [OUTPUT / SIDE EFFECT]: Storage key "kiranahub_cart_v1".
// [VARIATIONS TO TRY]: User-specific key suffix (e.g. user_id + "_cart").
const CART_STORAGE_KEY = "kiranahub_cart_v1";

// [KYA KARTA HAI]: useCart custom hook jo shopping cart ka complete business logic provide karta hai.
// [KYU ZAROORI HAI]: Presentation components ko complex pricing, storage sync, aur item mutations se separate rakhne ke liye.
// [KAISE CHALTA HAI]: State variables maintain karta hai, actions expose karta hai, aur derived pricing memoize karta hai.
// [OUTPUT / SIDE EFFECT]: Custom hook returning cart state, pricing, and action handlers.
// [VARIATIONS TO TRY]: Wrap with React Context Provider for global shared singleton access across components.
export function useCart() {
  // [KYA KARTA HAI]: Cart items array state initialize karta hai from LocalStorage agar available ho.
  // [KYU ZAROORI HAI]: Browser refresh par customer ke items retain karne ke liye.
  // [KAISE CHALTA HAI]: Lazy initializer function evaluate hota hai first render par.
  // [OUTPUT / SIDE EFFECT]: items array state and setItems updater.
  // [VARIATIONS TO TRY]: IndexedDB for larger offline carts.
  const [items, setItems] = useState<CartItem[]>(() => {
    try {
      if (typeof window !== "undefined" && window.localStorage) {
        const stored = localStorage.getItem(CART_STORAGE_KEY);
        if (stored) {
          return JSON.parse(stored) as CartItem[];
        }
      }
    } catch {
      // LocalStorage parsing errors fallback to empty array
    }
    return [];
  });

  // [KYA KARTA HAI]: Applied coupon code string state maintain karta hai.
  // [KYU ZAROORI HAI]: Promo discounts track karne ke liye.
  // [KAISE CHALTA HAI]: useState<string | null>(null) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: couponCode state and setter.
  // [VARIATIONS TO TRY]: Multiple stackable promo coupons array.
  const [couponCode, setCouponCode] = useState<string | null>(null);

  // [KYA KARTA HAI]: LocalStorage sync effect run karta hai whenever items change.
  // [KYU ZAROORI HAI]: Cart mutations ko browser disk storage me persist karne ke liye.
  // [KAISE CHALTA HAI]: useEffect dependency array me [items] watch karta hai.
  // [OUTPUT / SIDE EFFECT]: LocalStorage updated with stringified JSON.
  // [VARIATIONS TO TRY]: Debounced storage sync on rapid quantity updates.
  useEffect(() => {
    try {
      if (typeof window !== "undefined" && window.localStorage) {
        localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
      }
    } catch {
      // Storage quota exceeded or disabled fallback
    }
  }, [items]);

  // [KYA KARTA HAI]: Product ko cart me add karta hai ya existing item ki quantity +1 karta hai.
  // [KYU ZAROORI HAI]: Add to Cart button click handler ke liye.
  // [KAISE CHALTA HAI]: Existing item check karta hai. Agar mil jaye toh quantity increment karta hai, warna new CartItem append karta hai.
  // [OUTPUT / SIDE EFFECT]: Cart state updated.
  // [VARIATIONS TO TRY]: Stock quantity validation before adding.
  const addToCart = useCallback((product: Product) => {
    setItems((prevItems) => {
      const existingIndex = prevItems.findIndex((x) => x.product.id === product.id);
      if (existingIndex >= 0) {
        const updated = [...prevItems];
        const currentItem = updated[existingIndex];
        if (currentItem) {
          updated[existingIndex] = {
            ...currentItem,
            quantity: currentItem.quantity + 1,
          };
        }
        return updated;
      }
      return [...prevItems, { product, quantity: 1 }];
    });
  }, []);

  // [KYA KARTA HAI]: Specific product ki quantity directly update karta hai.
  // [KYU ZAROORI HAI]: Counter stepper (+/-) aur input change ke liye.
  // [KAISE CHALTA HAI]: Quantity <= 0 hone par item ko automatically remove karta hai.
  // [OUTPUT / SIDE EFFECT]: Items list updated.
  // [VARIATIONS TO TRY]: Max purchase limit (e.g. max 10 per user) check karna.
  const updateQuantity = useCallback((productId: string, quantity: number) => {
    setItems((prevItems) => {
      if (quantity <= 0) {
        return prevItems.filter((x) => x.product.id !== productId);
      }
      return prevItems.map((item) =>
        item.product.id === productId ? { ...item, quantity } : item
      );
    });
  }, []);

  // [KYA KARTA HAI]: Item ko cart se delete karta hai.
  // [KYU ZAROORI HAI]: Remove button action ke liye.
  // [KAISE CHALTA HAI]: Array.filter se matching productId ko exclude karta hai.
  // [OUTPUT / SIDE EFFECT]: Filtered items list.
  // [VARIATIONS TO TRY]: Confirmation dialog prompt before deletion.
  const removeFromCart = useCallback((productId: string) => {
    setItems((prevItems) => prevItems.filter((x) => x.product.id !== productId));
  }, []);

  // [KYA KARTA HAI]: Complete cart ko empty karta hai aur coupon reset karta hai.
  // [KYU ZAROORI HAI]: Order successfully place hone ke baad cart flush karne ke liye.
  // [KAISE CHALTA HAI]: Items ko [] aur couponCode ko null set karta hai.
  // [OUTPUT / SIDE EFFECT]: Empty cart state and storage cleared.
  // [VARIATIONS TO TRY]: Undo clear cart snackbar action.
  const clearCart = useCallback(() => {
    setItems([]);
    setCouponCode(null);
    try {
      if (typeof window !== "undefined" && window.localStorage) {
        localStorage.removeItem(CART_STORAGE_KEY);
      }
    } catch {
      // Storage error ignored
    }
  }, []);

  // [KYA KARTA HAI]: Promo coupon apply karta hai.
  // [KYU ZAROORI HAI]: Discount offer claim karne ke liye.
  // [KAISE CHALTA HAI]: Code trim aur uppercase karke validate karta hai.
  // [OUTPUT / SIDE EFFECT]: Returns boolean true if coupon valid, false if invalid.
  // [VARIATIONS TO TRY]: API-based server coupon validation call.
  const applyCoupon = useCallback((code: string): boolean => {
    const cleanCode = code.trim().toUpperCase();
    if (cleanCode === "KIRANA10" || cleanCode === "FIRSTORDER" || cleanCode === "FREEDEL") {
      setCouponCode(cleanCode);
      return true;
    }
    return false;
  }, []);

  // [KYA KARTA HAI]: Applied coupon ko remove karta hai.
  // [KYU ZAROORI HAI]: User coupon change karna chahe toh reset karne ke liye.
  // [KAISE CHALTA HAI]: setCouponCode(null) call karta hai.
  // [OUTPUT / SIDE EFFECT]: Coupon cleared.
  // [VARIATIONS TO TRY]: Log coupon cancellation event.
  const removeCoupon = useCallback(() => {
    setCouponCode(null);
  }, []);

  // [KYA KARTA HAI]: Cart ke items aur coupon ke basis par complete pricing breakdown derive karta hai.
  // [KYU ZAROORI HAI]: Performance optimization: unnecessary re-renders ke bina pricing recalculate karna.
  // [KAISE CHALTA HAI]: useMemo se subtotal, discount, tax aur delivery fee compute karta hai.
  // [OUTPUT / SIDE EFFECT]: Memoized PricingSummary object.
  // [VARIATIONS TO TRY]: Item-wise tax calculations.
  const pricing: PricingSummary = useMemo(() => {
    const subtotal = items.reduce(
      (sum, item) => sum + item.product.price * item.quantity,
      0
    );

    let discount = 0;
    if (couponCode === "KIRANA10") {
      discount = Math.round(subtotal * 0.1); // 10% off
    } else if (couponCode === "FIRSTORDER") {
      discount = Math.min(subtotal, 50); // Flat Rs 50 off
    }

    const discountedSubtotal = Math.max(0, subtotal - discount);
    const tax = Math.round(discountedSubtotal * 0.05 * 100) / 100; // 5% GST
    const deliveryFee = couponCode === "FREEDEL" || subtotal >= 500 || subtotal === 0 ? 0 : 30;
    const grandTotal = Math.round((discountedSubtotal + tax + deliveryFee) * 100) / 100;

    return {
      subtotal,
      tax,
      deliveryFee,
      discount,
      grandTotal,
      couponCode,
    };
  }, [items, couponCode]);

  // [KYA KARTA HAI]: Total unique items count sum karta hai.
  // [KYU ZAROORI HAI]: Header cart icon par red badge dikhane ke liye.
  // [KAISE CHALTA HAI]: items.reduce(sum + quantity) evaluate karta hai.
  // [OUTPUT / SIDE EFFECT]: Total number of products in cart.
  // [VARIATIONS TO TRY]: items.length for line item count instead of quantity.
  const totalItemCount = useMemo(() => {
    return items.reduce((sum, item) => sum + item.quantity, 0);
  }, [items]);

  // [KYA KARTA HAI]: Hook consumer ko states aur actions ka clean object return karta hai.
  // [KYU ZAROORI HAI]: Component consumption ke liye unified API provide karne ke liye.
  // [KAISE CHALTA HAI]: Return statement with readonly properties.
  // [OUTPUT / SIDE EFFECT]: Cart context interface.
  // [VARIATIONS TO TRY]: Tuple return style.
  return {
    items,
    pricing,
    totalItemCount,
    couponCode,
    addToCart,
    updateQuantity,
    removeFromCart,
    clearCart,
    applyCoupon,
    removeCoupon,
  };
}

