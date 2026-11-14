"use server";

// [KYA KARTA HAI]: Next.js "use server" directive define karta hai.
// [KYU ZAROORI HAI]: Functions ko Server Actions banata hai jo client bundle me expose nahi hote aur direct server execution karte hain.
// [KAISE CHALTA HAI]: Next.js compiler RPC endpoint generate karta hai.
// [OUTPUT / SIDE EFFECT]: Secure server-only code execution boundary.
// [VARIATIONS TO TRY]: Server Actions inside Server Components vs standalone actions file.

// [KYA KARTA HAI]: Server-side pricing calculation breakdown interface define karta hai.
// [KYU ZAROORI HAI]: Server Action return payload ko strongly-typed rakhne ke liye.
// [KAISE CHALTA HAI]: Numeric values aur string properties encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: ServerPricingResult contract.
// [VARIATIONS TO TRY]: Discount breakdown details list.
export interface ServerPricingResult {
  readonly success: boolean;
  readonly message: string;
  readonly subtotal: number;
  readonly discount: number;
  readonly tax: number;
  readonly deliveryFee: number;
  readonly grandTotal: number;
  readonly couponCode: string | null;
}

// [KYA KARTA HAI]: Server Action jo coupon code aur subtotal ko server-side validate aur calculate karta hai.
// [KYU ZAROORI HAI]: Client-side tampering se bachane ke liye (users cannot hack promo discounts in browser memory).
// [KAISE CHALTA HAI]: Server par coupon rule match karta hai, tax aur delivery fee compute karta hai, aur result return karta hai.
// [OUTPUT / SIDE EFFECT]: Promise<ServerPricingResult>.
// [VARIATIONS TO TRY]: Database lookup in coupon table with usage limits check.
export async function validateAndApplyCoupon(
  code: string,
  subtotal: number
): Promise<ServerPricingResult> {
  // [KYA KARTA HAI]: Subtotal non-negative verification check karta hai.
  // [KYU ZAROORI HAI]: Malicious negative subtotal values ko reject karne ke liye.
  // [KAISE CHALTA HAI]: if (subtotal < 0) condition check.
  // [OUTPUT / SIDE EFFECT]: Validation error or continues.
  // [VARIATIONS TO TRY]: Minimum order value requirement check.
  if (subtotal < 0) {
    return {
      success: false,
      message: "Invalid subtotal amount.",
      subtotal: 0,
      discount: 0,
      tax: 0,
      deliveryFee: 0,
      grandTotal: 0,
      couponCode: null,
    };
  }

  const cleanCode = code.trim().toUpperCase();
  let discount = 0;
  let isValid = false;
  let message = "";

  // [KYA KARTA HAI]: Server-side coupon verification logic.
  // [KYU ZAROORI HAI]: Business discount policies strictly enforce karne ke liye.
  // [KAISE CHALTA HAI]: Switch statement matching promo codes.
  // [OUTPUT / SIDE EFFECT]: Calculated discount amount.
  // [VARIATIONS TO TRY]: Dynamic promo rules from MongoDB or PostgreSQL.
  switch (cleanCode) {
    case "KIRANA10":
      discount = Math.round(subtotal * 0.1); // 10% off
      isValid = true;
      message = "🎉 10% discount applied via KIRANA10!";
      break;

    case "FIRSTORDER":
      discount = Math.min(subtotal, 50); // Flat Rs 50 off
      isValid = true;
      message = "🎁 Welcome gift: Flat ₹50 off applied!";
      break;

    case "FREEDEL":
      discount = 0;
      isValid = true;
      message = "🚚 Free delivery coupon applied!";
      break;

    default:
      isValid = false;
      message = `❌ Invalid coupon '${code}'. Valid codes: KIRANA10, FIRSTORDER, FREEDEL.`;
      break;
  }

  const effectiveDiscount = isValid ? discount : 0;
  const discountedSubtotal = Math.max(0, subtotal - effectiveDiscount);
  const tax = Math.round(discountedSubtotal * 0.05 * 100) / 100; // 5% GST
  const deliveryFee = cleanCode === "FREEDEL" || subtotal >= 500 || subtotal === 0 ? 0 : 30;
  const grandTotal = Math.round((discountedSubtotal + tax + deliveryFee) * 100) / 100;

  return {
    success: isValid,
    message,
    subtotal,
    discount: effectiveDiscount,
    tax,
    deliveryFee,
    grandTotal,
    couponCode: isValid ? cleanCode : null,
  };
}

// [KYA KARTA HAI]: Server Action jo HTML Form submission se direct order place karta hai.
// [KYU ZAROORI HAI]: Zero JavaScript form submission aur progressive enhancement support karne ke liye.
// [KAISE CHALTA HAI]: FormData se address aur cart JSON extract karta hai, order ID generate karta hai, aur return karta hai.
// [OUTPUT / SIDE EFFECT]: Order result payload.
// [VARIATIONS TO TRY]: Next.js revalidatePath('/') call to purge stale cache.
export async function submitOrderAction(formData: FormData): Promise<{
  readonly success: boolean;
  readonly orderId?: string;
  readonly error?: string;
}> {
  const address = formData.get("deliveryAddress")?.toString();
  const itemsJson = formData.get("itemsJson")?.toString();

  if (!address || address.trim().length === 0) {
    return { success: false, error: "Delivery address is required." };
  }

  if (!itemsJson) {
    return { success: false, error: "Cart is empty." };
  }

  const orderId = `KRN-${Math.floor(100000 + Math.random() * 900000)}`;
  return { success: true, orderId };
}

