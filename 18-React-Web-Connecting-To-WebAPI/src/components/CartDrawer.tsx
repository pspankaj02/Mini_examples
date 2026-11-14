// [KYA KARTA HAI]: React core and useState hooks import karta hai.
// [KYU ZAROORI HAI]: Local state jaise coupon input aur address manage karne ke liye.
// [KAISE CHALTA HAI]: React standard library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: Formik / React Hook Form for address form validation.
import React, { useState } from "react";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: CartItem aur PricingSummary type checking ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: Store types.
// [VARIATIONS TO TRY]: Dedicated checkout payload types.
import type { CartItem, PricingSummary } from "../types/store.js";

// [KYA KARTA HAI]: CartDrawer component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Cart items, actions, aur checkout callbacks pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: CartDrawerProps interface.
// [VARIATIONS TO TRY]: Delivery slot time selection prop.
export interface CartDrawerProps {
  readonly isOpen: boolean;
  readonly onClose: () => void;
  readonly items: readonly CartItem[];
  readonly pricing: PricingSummary;
  readonly onUpdateQuantity: (productId: string, quantity: number) => void;
  readonly onRemoveFromCart: (productId: string) => void;
  readonly onApplyCoupon: (code: string) => boolean;
  readonly onRemoveCoupon: () => void;
  readonly onCheckout: (address: string) => Promise<void>;
  readonly isCheckingOut: boolean;
}

// [KYA KARTA HAI]: Slide-out cart drawer component render karta hai.
// [KYU ZAROORI HAI]: Cart review, quantity change, coupon apply, address entry aur checkout trigger karne ke liye.
// [KAISE CHALTA HAI]: Fixed right-side panel with dark overlay backdrop.
// [OUTPUT / SIDE EFFECT]: Rendered slide-out drawer.
// [VARIATIONS TO TRY]: Slide-in CSS transition animations.
export const CartDrawer: React.FC<CartDrawerProps> = ({
  isOpen,
  onClose,
  items,
  pricing,
  onUpdateQuantity,
  onRemoveFromCart,
  onApplyCoupon,
  onRemoveCoupon,
  onCheckout,
  isCheckingOut,
}) => {
  const [couponInput, setCouponInput] = useState<string>("");
  const [couponMessage, setCouponMessage] = useState<string | null>(null);
  const [address, setAddress] = useState<string>("B-402, Green Valley Apts, Sector 62, Noida");

  if (!isOpen) {
    return null;
  }

  const handleApplyCoupon = (e: React.FormEvent) => {
    e.preventDefault();
    if (!couponInput.trim()) return;
    const success = onApplyCoupon(couponInput);
    if (success) {
      setCouponMessage(`🎉 Coupon '${couponInput.toUpperCase()}' applied successfully!`);
      setCouponInput("");
    } else {
      setCouponMessage("❌ Invalid coupon code. Try 'KIRANA10' or 'FIRSTORDER'.");
    }
  };

  const handleCheckoutSubmit = async () => {
    if (items.length === 0) return;
    if (!address.trim()) {
      alert("Please enter a valid delivery address.");
      return;
    }
    await onCheckout(address);
  };

  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 100,
        display: "flex",
        justifyContent: "flex-end",
      }}
    >
      {/* Backdrop Overlay */}
      <div
        onClick={onClose}
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: "rgba(0, 0, 0, 0.4)",
          backdropFilter: "blur(2px)",
        }}
      />

      {/* Drawer Panel */}
      <div
        style={{
          position: "relative",
          width: "100%",
          maxWidth: "420px",
          height: "100%",
          backgroundColor: "#ffffff",
          display: "flex",
          flexDirection: "column",
          boxShadow: "-4px 0 20px rgba(0,0,0,0.15)",
          zIndex: 101,
        }}
      >
        {/* Drawer Header */}
        <div
          style={{
            padding: "16px 20px",
            borderBottom: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
            <span style={{ fontSize: "20px" }}>🛍️</span>
            <h2 style={{ fontSize: "18px", fontWeight: 700, color: "#111827" }}>
              My Cart ({items.length})
            </h2>
          </div>
          <button
            onClick={onClose}
            style={{
              background: "none",
              border: "none",
              fontSize: "20px",
              cursor: "pointer",
              color: "#6b7280",
            }}
          >
            ✕
          </button>
        </div>

        {/* Drawer Body: Items List */}
        <div style={{ flex: 1, overflowY: "auto", padding: "16px 20px" }}>
          {items.length === 0 ? (
            <div style={{ textAlign: "center", padding: "60px 20px", color: "#6b7280" }}>
              <div style={{ fontSize: "48px", marginBottom: "16px" }}>🛒</div>
              <h3 style={{ fontSize: "16px", fontWeight: 600, color: "#374151", marginBottom: "8px" }}>
                Your cart is empty!
              </h3>
              <p style={{ fontSize: "14px" }}>Add groceries from the catalog to see them here.</p>
            </div>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
              {items.map((line) => (
                <div
                  key={line.product.id}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    padding: "10px",
                    backgroundColor: "#f9fafb",
                    borderRadius: "8px",
                    border: "1px solid #e5e7eb",
                  }}
                >
                  <div style={{ flex: 1, paddingRight: "10px" }}>
                    <div style={{ fontWeight: 600, fontSize: "14px", color: "#111827" }}>
                      {line.product.name}
                    </div>
                    <div style={{ fontSize: "12px", color: "#6b7280" }}>
                      ₹{line.product.price} × {line.quantity} = ₹{line.product.price * line.quantity}
                    </div>
                  </div>

                  {/* Quantity Stepper */}
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      backgroundColor: "#059669",
                      borderRadius: "6px",
                      overflow: "hidden",
                    }}
                  >
                    <button
                      onClick={() => onUpdateQuantity(line.product.id, line.quantity - 1)}
                      style={{
                        backgroundColor: "transparent",
                        color: "#ffffff",
                        border: "none",
                        padding: "4px 8px",
                        fontWeight: 700,
                        cursor: "pointer",
                      }}
                    >
                      −
                    </button>
                    <span
                      style={{
                        color: "#ffffff",
                        fontWeight: 700,
                        fontSize: "12px",
                        padding: "0 6px",
                      }}
                    >
                      {line.quantity}
                    </span>
                    <button
                      onClick={() => onUpdateQuantity(line.product.id, line.quantity + 1)}
                      style={{
                        backgroundColor: "transparent",
                        color: "#ffffff",
                        border: "none",
                        padding: "4px 8px",
                        fontWeight: 700,
                        cursor: "pointer",
                      }}
                    >
                      +
                    </button>
                  </div>

                  {/* Remove Button */}
                  <button
                    onClick={() => onRemoveFromCart(line.product.id)}
                    style={{
                      background: "none",
                      border: "none",
                      color: "#9ca3af",
                      cursor: "pointer",
                      fontSize: "14px",
                      marginLeft: "8px",
                      padding: "4px",
                    }}
                    title="Remove item"
                  >
                    🗑️
                  </button>
                </div>
              ))}

              {/* Coupon Code Section */}
              <div
                style={{
                  marginTop: "16px",
                  padding: "12px",
                  backgroundColor: "#f0fdf4",
                  borderRadius: "8px",
                  border: "1px dashed #86efac",
                }}
              >
                <div style={{ fontSize: "12px", fontWeight: 600, color: "#166534", marginBottom: "8px" }}>
                  🏷️ Have a Promo Coupon?
                </div>
                {pricing.couponCode ? (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "space-between",
                      backgroundColor: "#ffffff",
                      padding: "6px 12px",
                      borderRadius: "6px",
                      border: "1px solid #86efac",
                    }}
                  >
                    <span style={{ fontWeight: 700, fontSize: "13px", color: "#059669" }}>
                      {pricing.couponCode} (-₹{pricing.discount})
                    </span>
                    <button
                      onClick={onRemoveCoupon}
                      style={{
                        backgroundColor: "transparent",
                        border: "none",
                        color: "#dc2626",
                        cursor: "pointer",
                        fontSize: "12px",
                        fontWeight: 600,
                      }}
                    >
                      Remove
                    </button>
                  </div>
                ) : (
                  <form onSubmit={handleApplyCoupon} style={{ display: "flex", gap: "8px" }}>
                    <input
                      type="text"
                      placeholder="Enter KIRANA10"
                      value={couponInput}
                      onChange={(e) => setCouponInput(e.target.value)}
                      style={{
                        flex: 1,
                        padding: "6px 10px",
                        borderRadius: "6px",
                        border: "1px solid #d1d5db",
                        fontSize: "13px",
                      }}
                    />
                    <button
                      type="submit"
                      style={{
                        backgroundColor: "#059669",
                        color: "#ffffff",
                        border: "none",
                        padding: "6px 12px",
                        borderRadius: "6px",
                        fontSize: "13px",
                        fontWeight: 600,
                        cursor: "pointer",
                      }}
                    >
                      Apply
                    </button>
                  </form>
                )}
                {couponMessage && (
                  <div style={{ fontSize: "12px", marginTop: "6px", color: "#374151" }}>
                    {couponMessage}
                  </div>
                )}
              </div>

              {/* Delivery Address Input */}
              <div style={{ marginTop: "12px" }}>
                <label style={{ display: "block", fontSize: "12px", fontWeight: 600, color: "#374151", marginBottom: "4px" }}>
                  📍 Delivery Address:
                </label>
                <textarea
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  rows={2}
                  style={{
                    width: "100%",
                    padding: "8px",
                    borderRadius: "6px",
                    border: "1px solid #d1d5db",
                    fontSize: "12px",
                    resize: "none",
                  }}
                />
              </div>
            </div>
          )}
        </div>

        {/* Drawer Footer: Pricing Breakdown & Checkout */}
        {items.length > 0 && (
          <div
            style={{
              padding: "16px 20px",
              borderTop: "1px solid #e5e7eb",
              backgroundColor: "#f9fafb",
            }}
          >
            <div style={{ display: "flex", flexDirection: "column", gap: "6px", marginBottom: "16px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#6b7280" }}>
                <span>Subtotal:</span>
                <span>₹{pricing.subtotal}</span>
              </div>
              {pricing.discount > 0 && (
                <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#059669" }}>
                  <span>Discount:</span>
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
                <span>To Pay:</span>
                <span>₹{pricing.grandTotal}</span>
              </div>
            </div>

            <button
              onClick={handleCheckoutSubmit}
              disabled={isCheckingOut}
              style={{
                width: "100%",
                padding: "12px",
                backgroundColor: isCheckingOut ? "#9ca3af" : "#059669",
                color: "#ffffff",
                border: "none",
                borderRadius: "8px",
                fontSize: "15px",
                fontWeight: 700,
                cursor: isCheckingOut ? "not-allowed" : "pointer",
                boxShadow: "0 2px 6px rgba(5,150,105,0.3)",
              }}
            >
              {isCheckingOut ? "Placing Order..." : `Place Order (₹${pricing.grandTotal})`}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
