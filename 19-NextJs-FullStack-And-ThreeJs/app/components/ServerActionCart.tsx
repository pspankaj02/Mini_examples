"use client";

// [KYA KARTA HAI]: Next.js "use client" directive define karta hai.
// [KYU ZAROORI HAI]: User click events, local state aur useTransition hook enable karne ke liye.
// [KAISE CHALTA HAI]: Client component boundary create karta hai.
// [OUTPUT / SIDE EFFECT]: Client bundle component.
// [VARIATIONS TO TRY]: Form action direct attribute syntax.

// [KYA KARTA HAI]: React core hooks import karta hai.
// [KYU ZAROORI HAI]: State management aur non-blocking UI transitions ke liye.
// [KAISE CHALTA HAI]: useState, useTransition standard hooks load hote hain.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React 19 useActionState hook.
import React, { useState, useTransition } from "react";

// [KYA KARTA HAI]: Server Actions import karta hai.
// [KYU ZAROORI HAI]: Client component se direct server code execute karne ke liye without writing REST API controllers.
// [KAISE CHALTA HAI]: RPC binding through Next.js build.
// [OUTPUT / SIDE EFFECT]: Server Action functions available.
// [VARIATIONS TO TRY]: Bound actions via bind().
import { validateAndApplyCoupon, submitOrderAction } from "../actions/cartActions";
import type { ServerPricingResult } from "../actions/cartActions";

// [KYA KARTA HAI]: ServerActionCart component render karta hai.
// [KYU ZAROORI HAI]: Next.js Server Actions ka live, interactive demonstration provide karne ke liye.
// [KAISE CHALTA HAI]: Local cart lines maintain karta hai, server action se coupon compute karta hai, aur form submit karta hai.
// [OUTPUT / SIDE EFFECT]: Rendered Server Action cart widget.
// [VARIATIONS TO TRY]: Optimistic UI updates with useOptimistic().
export const ServerActionCart: React.FC = () => {
  const [couponInput, setCouponInput] = useState<string>("KIRANA10");
  const [address, setAddress] = useState<string>("Flat 102, Palm Heights, Indirapuram, Ghaziabad");
  const [isPending, startTransition] = useTransition();

  const [pricing, setPricing] = useState<ServerPricingResult>({
    success: false,
    message: "No coupon applied yet",
    subtotal: 340,
    discount: 0,
    tax: 17,
    deliveryFee: 30,
    grandTotal: 387,
    couponCode: null,
  });

  const [orderConfirmation, setOrderConfirmation] = useState<string | null>(null);

  // [KYA KARTA HAI]: Server Action invoke karta hai non-blocking transition me.
  // [KYU ZAROORI HAI]: UI freeze kiye bina server calculation run karne ke liye.
  // [KAISE CHALTA HAI]: startTransition(() => { validateAndApplyCoupon(...) }) execute karta hai.
  // [OUTPUT / SIDE EFFECT]: pricing state updated with server calculated values.
  // [VARIATIONS TO TRY]: Pass abort signal.
  const handleApplyCouponAction = () => {
    startTransition(async () => {
      const result = await validateAndApplyCoupon(couponInput, pricing.subtotal);
      setPricing(result);
    });
  };

  // [KYA KARTA HAI]: Order submit form handler jo Server Action call karta hai.
  // [KYU ZAROORI HAI]: Server-side order generation ke liye.
  // [KAISE CHALTA HAI]: FormData build karke submitOrderAction(formData) execute karta hai.
  // [OUTPUT / SIDE EFFECT]: orderConfirmation state updated.
  // [VARIATIONS TO TRY]: Redirect to /order/[id] page.
  const handleOrderSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    startTransition(async () => {
      const formData = new FormData();
      formData.set("deliveryAddress", address);
      formData.set("itemsJson", JSON.stringify([{ id: "nx-01", qty: 2 }, { id: "nx-03", qty: 1 }]));
      formData.set("couponCode", pricing.couponCode ?? "");

      const result = await submitOrderAction(formData);
      if (result.success && result.orderId) {
        setOrderConfirmation(`🎉 Order placed successfully! Order ID: #${result.orderId}`);
      } else {
        alert(result.error ?? "Failed to place order.");
      }
    });
  };

  return (
    <div
      style={{
        backgroundColor: "#ffffff",
        border: "1px solid #e5e7eb",
        borderRadius: "16px",
        padding: "20px",
        boxShadow: "0 4px 6px -1px rgba(0,0,0,0.05)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: "8px", marginBottom: "12px" }}>
        <span style={{ fontSize: "20px" }}>⚡</span>
        <h2 style={{ fontSize: "16px", fontWeight: 700, color: "#111827" }}>
          Next.js Server Actions Checkout Engine
        </h2>
      </div>

      <div style={{ fontSize: "13px", color: "#6b7280", marginBottom: "16px" }}>
        Direct server execution with <code>"use server"</code> without separate REST controllers.
      </div>

      {/* Cart Summary */}
      <div
        style={{
          backgroundColor: "#f9fafb",
          padding: "12px",
          borderRadius: "8px",
          marginBottom: "16px",
          border: "1px solid #e5e7eb",
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
          <span>Amul Buffalo Milk 1L (×2):</span>
          <span>₹136</span>
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
          <span>Fortune Sunflower Oil 1L (×1):</span>
          <span>₹135</span>
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
          <span>English Oven Multigrain Bread (×1):</span>
          <span>₹55</span>
        </div>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            fontSize: "14px",
            fontWeight: 700,
            borderTop: "1px dashed #d1d5db",
            paddingTop: "6px",
            marginTop: "6px",
          }}
        >
          <span>Subtotal:</span>
          <span>₹{pricing.subtotal}</span>
        </div>
      </div>

      {/* Server Action: Coupon Code Form */}
      <div style={{ marginBottom: "16px" }}>
        <label style={{ display: "block", fontSize: "12px", fontWeight: 600, color: "#374151", marginBottom: "4px" }}>
          🏷️ Server-Side Promo Coupon:
        </label>
        <div style={{ display: "flex", gap: "8px" }}>
          <input
            type="text"
            value={couponInput}
            onChange={(e) => setCouponInput(e.target.value)}
            placeholder="Try KIRANA10 or FIRSTORDER"
            style={{
              flex: 1,
              padding: "8px 12px",
              borderRadius: "6px",
              border: "1px solid #d1d5db",
              fontSize: "13px",
            }}
          />
          <button
            onClick={handleApplyCouponAction}
            disabled={isPending}
            style={{
              backgroundColor: isPending ? "#9ca3af" : "#059669",
              color: "#ffffff",
              border: "none",
              padding: "8px 16px",
              borderRadius: "6px",
              fontSize: "13px",
              fontWeight: 600,
              cursor: isPending ? "not-allowed" : "pointer",
            }}
          >
            {isPending ? "Validating..." : "Apply Action"}
          </button>
        </div>
        <div style={{ fontSize: "12px", color: pricing.success ? "#059669" : "#dc2626", marginTop: "6px" }}>
          {pricing.message}
        </div>
      </div>

      {/* Calculated Breakdown */}
      <div
        style={{
          borderTop: "1px solid #e5e7eb",
          paddingTop: "12px",
          marginBottom: "16px",
          display: "flex",
          flexDirection: "column",
          gap: "6px",
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#6b7280" }}>
          <span>Subtotal:</span>
          <span>₹{pricing.subtotal}</span>
        </div>
        {pricing.discount > 0 && (
          <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#059669" }}>
            <span>Discount ({pricing.couponCode}):</span>
            <span>-₹{pricing.discount}</span>
          </div>
        )}
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#6b7280" }}>
          <span>GST Tax (5%):</span>
          <span>₹{pricing.tax}</span>
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#6b7280" }}>
          <span>Delivery Fee:</span>
          <span>{pricing.deliveryFee === 0 ? "FREE" : `₹${pricing.deliveryFee}`}</span>
        </div>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            fontSize: "16px",
            fontWeight: 700,
            color: "#111827",
            borderTop: "1px solid #e5e7eb",
            paddingTop: "8px",
          }}
        >
          <span>Grand Total:</span>
          <span>₹{pricing.grandTotal}</span>
        </div>
      </div>

      {/* Checkout Submission Form */}
      <form onSubmit={handleOrderSubmit}>
        <label style={{ display: "block", fontSize: "12px", fontWeight: 600, color: "#374151", marginBottom: "4px" }}>
          📍 Delivery Address:
        </label>
        <input
          type="text"
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          required
          style={{
            width: "100%",
            padding: "8px 12px",
            borderRadius: "6px",
            border: "1px solid #d1d5db",
            fontSize: "13px",
            marginBottom: "12px",
          }}
        />

        <button
          type="submit"
          disabled={isPending}
          style={{
            width: "100%",
            padding: "12px",
            backgroundColor: isPending ? "#9ca3af" : "#059669",
            color: "#ffffff",
            border: "none",
            borderRadius: "8px",
            fontSize: "14px",
            fontWeight: 700,
            cursor: isPending ? "not-allowed" : "pointer",
          }}
        >
          {isPending ? "Submitting Server Action..." : `Place Order (₹${pricing.grandTotal})`}
        </button>
      </form>

      {/* Order Confirmation Banner */}
      {orderConfirmation && (
        <div
          style={{
            marginTop: "16px",
            padding: "12px",
            backgroundColor: "#ecfdf5",
            border: "1px solid #86efac",
            borderRadius: "8px",
            fontSize: "13px",
            fontWeight: 600,
            color: "#065f46",
            textAlign: "center",
          }}
        >
          {orderConfirmation}
        </div>
      )}
    </div>
  );
};
