// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: MobileHeader component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Current location, cart count aur cart tap callback receive karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: MobileHeaderProps interface.
// [VARIATIONS TO TRY]: User avatar tap handler prop.
export interface MobileHeaderProps {
  readonly deliveryLocation: string;
  readonly cartItemCount: number;
  readonly onCartPress: () => void;
  readonly isTrackingActive: boolean;
  readonly onTrackingPress: () => void;
}

// [KYA KARTA HAI]: Mobile Header navigation bar render karta hai.
// [KYU ZAROORI HAI]: Mobile app screen ke top par 10-minute delivery promise, GPS address, aur cart count dikhane ke liye.
// [KAISE CHALTA HAI]: Flexbox header with safe area padding.
// [OUTPUT / SIDE EFFECT]: Mobile header bar.
// [VARIATIONS TO TRY]: Collapsible header on scroll down.
export const MobileHeader: React.FC<MobileHeaderProps> = ({
  deliveryLocation,
  cartItemCount,
  onCartPress,
  isTrackingActive,
  onTrackingPress,
}) => {
  return (
    <div
      style={{
        backgroundColor: "#059669",
        color: "#ffffff",
        padding: "16px 16px 12px",
        borderBottomLeftRadius: "16px",
        borderBottomRightRadius: "16px",
        boxShadow: "0 2px 8px rgba(5,150,105,0.3)",
      }}
    >
      {/* Top Row: Location & Brand */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "8px" }}>
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
            <span style={{ fontSize: "14px" }}>📍</span>
            <span style={{ fontWeight: 700, fontSize: "14px", letterSpacing: "-0.2px" }}>
              {deliveryLocation}
            </span>
            <span style={{ fontSize: "11px", opacity: 0.8 }}>▼</span>
          </div>
          <div style={{ fontSize: "11px", opacity: 0.9, marginTop: "2px" }}>
            ⚡ 10 Mins Instant Delivery
          </div>
        </div>

        {/* Action Buttons */}
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          {isTrackingActive && (
            <button
              onClick={onTrackingPress}
              style={{
                backgroundColor: "#ecfdf5",
                color: "#059669",
                border: "none",
                padding: "6px 10px",
                borderRadius: "12px",
                fontSize: "12px",
                fontWeight: 700,
                cursor: "pointer",
                display: "flex",
                alignItems: "center",
                gap: "4px",
              }}
            >
              <span>🛵 Live</span>
            </button>
          )}

          <button
            onClick={onCartPress}
            style={{
              backgroundColor: "#047857",
              color: "#ffffff",
              border: "1px solid #10b981",
              padding: "6px 12px",
              borderRadius: "12px",
              fontSize: "12px",
              fontWeight: 700,
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              gap: "6px",
            }}
          >
            <span>🛒</span>
            <span>{cartItemCount}</span>
          </button>
        </div>
      </div>
    </div>
  );
};

