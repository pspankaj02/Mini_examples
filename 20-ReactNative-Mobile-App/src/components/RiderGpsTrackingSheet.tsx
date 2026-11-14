// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: MobileOrderStatus aur RiderGpsSnapshot types access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/mobile.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Animation duration prop.
import type { MobileOrderStatus, RiderGpsSnapshot } from "../types/mobile.js";

// [KYA KARTA HAI]: RiderGpsTrackingSheet component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Order ID, status, live GPS snapshot aur close callback pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: RiderGpsTrackingSheetProps interface.
// [VARIATIONS TO TRY]: Rider rating callback prop.
export interface RiderGpsTrackingSheetProps {
  readonly isVisible: boolean;
  readonly onClose: () => void;
  readonly orderId: string | null;
  readonly status: MobileOrderStatus;
  readonly gpsSnapshot: RiderGpsSnapshot | null;
}

// [KYA KARTA HAI]: Mobile bottom sheet tracking modal render karta hai.
// [KYU ZAROORI HAI]: Mobile screen ke bottom se slide-up hokar live GPS delivery status aur rider movement dikhane ke liye.
// [KAISE CHALTA HAI]: Fixed bottom sheet container with backdrop overlay.
// [OUTPUT / SIDE EFFECT]: Rendered mobile bottom sheet.
// [VARIATIONS TO TRY]: PanResponder gesture for dragging down sheet to dismiss.
export const RiderGpsTrackingSheet: React.FC<RiderGpsTrackingSheetProps> = ({
  isVisible,
  onClose,
  orderId,
  status,
  gpsSnapshot,
}) => {
  if (!isVisible || !orderId) return null;

  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 200,
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
      }}
    >
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: "rgba(0,0,0,0.4)",
        }}
      />

      {/* Sheet Content */}
      <div
        style={{
          position: "relative",
          backgroundColor: "#ffffff",
          borderTopLeftRadius: "24px",
          borderTopRightRadius: "24px",
          padding: "20px 20px 32px",
          boxShadow: "0 -4px 16px rgba(0,0,0,0.15)",
          zIndex: 201,
        }}
      >
        {/* Handle Bar */}
        <div
          style={{
            width: "40px",
            height: "4px",
            backgroundColor: "#d1d5db",
            borderRadius: "2px",
            margin: "0 auto 16px",
          }}
        />

        {/* Title & ETA */}
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "16px" }}>
          <div>
            <span style={{ fontSize: "12px", color: "#059669", fontWeight: 700, textTransform: "uppercase" }}>
              {status === "Delivered" ? "Order Completed" : "⚡ Delivering in 10 mins"}
            </span>
            <h3 style={{ fontSize: "18px", fontWeight: 700, color: "#111827", margin: "2px 0 0" }}>
              Order #{orderId}
            </h3>
          </div>
          <button
            onClick={onClose}
            style={{
              background: "none",
              border: "none",
              fontSize: "20px",
              cursor: "pointer",
              color: "#9ca3af",
            }}
          >
            ✕
          </button>
        </div>

        {/* Live Rider GPS Telemetry Card */}
        {gpsSnapshot && (
          <div
            style={{
              backgroundColor: "#f0fdf4",
              border: "1px solid #86efac",
              borderRadius: "12px",
              padding: "12px",
              marginBottom: "16px",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "6px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
                <span style={{ fontSize: "16px" }}>🛵</span>
                <span style={{ fontWeight: 700, fontSize: "13px", color: "#166534" }}>
                  Rider: Suresh Kumar
                </span>
              </div>
              <span style={{ fontSize: "12px", fontWeight: 700, color: "#059669" }}>
                Speed: {gpsSnapshot.speedKmph} km/h
              </span>
            </div>
            <div style={{ fontSize: "12px", color: "#374151" }}>
              Coordinates: {gpsSnapshot.latitude.toFixed(4)}, {gpsSnapshot.longitude.toFixed(4)}
            </div>
            <div style={{ fontSize: "12px", color: "#4b5563", marginTop: "2px" }}>
              Distance: {gpsSnapshot.remainingMeters} meters away • ETA: ~{gpsSnapshot.estimatedMinutes} mins
            </div>
          </div>
        )}

        {/* Status Stepper Progression */}
        <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "20px", position: "relative" }}>
          {["Placed", "Packed", "Dispatched", "Delivered"].map((step, idx) => {
            const stepOrder = ["Placed", "Packed", "Dispatched", "Delivered"];
            const currentIdx = stepOrder.indexOf(status);
            const isDone = currentIdx >= idx;

            return (
              <div key={step} style={{ display: "flex", flexDirection: "column", alignItems: "center", flex: 1 }}>
                <div
                  style={{
                    width: "28px",
                    height: "28px",
                    borderRadius: "50%",
                    backgroundColor: isDone ? "#059669" : "#e5e7eb",
                    color: isDone ? "#ffffff" : "#9ca3af",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: "12px",
                    fontWeight: 700,
                    marginBottom: "4px",
                  }}
                >
                  {isDone ? "✓" : idx + 1}
                </div>
                <span
                  style={{
                    fontSize: "11px",
                    color: isDone ? "#111827" : "#9ca3af",
                    fontWeight: isDone ? 700 : 500,
                  }}
                >
                  {step}
                </span>
              </div>
            );
          })}
        </div>

        {/* Dismiss Button */}
        <button
          onClick={onClose}
          style={{
            width: "100%",
            padding: "12px",
            backgroundColor: "#059669",
            color: "#ffffff",
            border: "none",
            borderRadius: "10px",
            fontSize: "14px",
            fontWeight: 700,
            cursor: "pointer",
          }}
        >
          Got It
        </button>
      </div>
    </div>
  );
};

