// [KYA KARTA HAI]: React core component rendering import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React runtime symbols available.
// [VARIATIONS TO TRY]: Named React.memo wrapper.
import React from "react";

// [KYA KARTA HAI]: Header component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Parent component se search string, callback aur cart count receive karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties contract specify karta hai.
// [OUTPUT / SIDE EFFECT]: HeaderProps interface.
// [VARIATIONS TO TRY]: Add user profile picture prop.
export interface HeaderProps {
  readonly searchTerm: string;
  readonly onSearchChange: (value: string) => void;
  readonly cartItemCount: number;
  readonly onOpenCart: () => void;
  readonly isTrackingActive: boolean;
  readonly onOpenTracking: () => void;
}

// [KYA KARTA HAI]: KiranaHub ka top header navigation bar render karta hai.
// [KYU ZAROORI HAI]: Brand identity, 10-minute delivery promise, search input, live tracking indicator aur cart drawer button display karne ke liye.
// [KAISE CHALTA HAI]: Functional component jo clean, responsive header HTML return karta hai.
// [OUTPUT / SIDE EFFECT]: Rendered header bar.
// [VARIATIONS TO TRY]: Sticky blur backdrop filter on scroll.
export const Header: React.FC<HeaderProps> = ({
  searchTerm,
  onSearchChange,
  cartItemCount,
  onOpenCart,
  isTrackingActive,
  onOpenTracking,
}) => {
  return (
    <header
      style={{
        backgroundColor: "#ffffff",
        borderBottom: "1px solid #e5e7eb",
        padding: "12px 24px",
        position: "sticky",
        top: 0,
        zIndex: 50,
        boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
      }}
    >
      <div
        style={{
          maxWidth: "1200px",
          margin: "0 auto",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          gap: "16px",
          flexWrap: "wrap",
        }}
      >
        {/* Brand Logo & Tagline */}
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <div
            style={{
              backgroundColor: "#059669",
              color: "#ffffff",
              padding: "6px 12px",
              borderRadius: "8px",
              fontWeight: 700,
              fontSize: "18px",
              letterSpacing: "-0.5px",
            }}
          >
            🛒 KiranaHub
          </div>
          <span
            style={{
              backgroundColor: "#fef3c7",
              color: "#92400e",
              fontSize: "12px",
              fontWeight: 600,
              padding: "4px 8px",
              borderRadius: "16px",
            }}
          >
            ⚡ 10 Mins Delivery
          </span>
        </div>

        {/* Live Search Input */}
        <div style={{ flex: "1 1 280px", maxWidth: "450px", position: "relative" }}>
          <input
            type="text"
            placeholder="Search groceries (e.g. Milk, Atta, Bread)..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            style={{
              width: "100%",
              padding: "10px 14px",
              borderRadius: "8px",
              border: "1px solid #d1d5db",
              fontSize: "14px",
              outline: "none",
              backgroundColor: "#f9fafb",
            }}
          />
          {searchTerm && (
            <button
              onClick={() => onSearchChange("")}
              style={{
                position: "absolute",
                right: "10px",
                top: "50%",
                transform: "translateY(-50%)",
                background: "none",
                border: "none",
                cursor: "pointer",
                color: "#6b7280",
                fontSize: "16px",
              }}
              title="Clear search"
            >
              ×
            </button>
          )}
        </div>

        {/* Action Buttons: Tracking & Cart */}
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          {isTrackingActive && (
            <button
              onClick={onOpenTracking}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "6px",
                backgroundColor: "#ecfdf5",
                color: "#059669",
                border: "1px solid #10b981",
                padding: "8px 14px",
                borderRadius: "8px",
                fontSize: "14px",
                fontWeight: 600,
                cursor: "pointer",
              }}
            >
              <span
                style={{
                  width: "8px",
                  height: "8px",
                  borderRadius: "50%",
                  backgroundColor: "#10b981",
                  display: "inline-block",
                }}
              />
              Live Order
            </button>
          )}

          <button
            onClick={onOpenCart}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              backgroundColor: "#059669",
              color: "#ffffff",
              border: "none",
              padding: "9px 18px",
              borderRadius: "8px",
              fontSize: "14px",
              fontWeight: 600,
              cursor: "pointer",
              boxShadow: "0 2px 4px rgba(5,150,105,0.2)",
            }}
          >
            <span>Cart</span>
            {cartItemCount > 0 && (
              <span
                style={{
                  backgroundColor: "#ffffff",
                  color: "#059669",
                  padding: "2px 7px",
                  borderRadius: "12px",
                  fontSize: "12px",
                  fontWeight: 700,
                }}
              >
                {cartItemCount}
              </span>
            )}
          </button>
        </div>
      </div>
    </header>
  );
};

