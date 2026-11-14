// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: OrderStatus aur RiderLocation types access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Import custom animation types.
import type { OrderStatus, RiderLocation } from "../types/store.js";

// [KYA KARTA HAI]: LiveOrderTracking component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Active order ID, current status, rider location aur close modal callback pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: LiveOrderTrackingProps interface.
// [VARIATIONS TO TRY]: Call rider phone callback prop.
export interface LiveOrderTrackingProps {
  readonly isOpen: boolean;
  readonly onClose: () => void;
  readonly orderId: string | null;
  readonly status: OrderStatus;
  readonly riderLocation: RiderLocation | null;
  readonly isConnected: boolean;
}

// [KYA KARTA HAI]: Visual steps configuration array define karta hai.
// [KYU ZAROORI HAI]: Progress stepper me ordered milestones map karne ke liye.
// [KAISE CHALTA HAI]: Array of step objects with id, label, icon.
// [OUTPUT / SIDE EFFECT]: Stepper steps array.
// [VARIATIONS TO TRY]: Dynamic estimated time per step.
const TRACKING_STEPS: readonly { id: OrderStatus; label: string; icon: string }[] = [
  { id: "Placed", label: "Order Placed", icon: "📝" },
  { id: "Packed", label: "Packed at Store", icon: "📦" },
  { id: "Dispatched", label: "Out for Delivery", icon: "🛵" },
  { id: "Delivered", label: "Delivered", icon: "🎉" },
];

// [KYA KARTA HAI]: Status ka numerical progression index calculate karta hai.
// [KYU ZAROORI HAI]: Progress bar percentage aur completed steps highlight karne ke liye.
// [KAISE CHALTA HAI]: Switch case mapping.
// [OUTPUT / SIDE EFFECT]: Integer index (0 to 4).
// [VARIATIONS TO TRY]: Enum ordinal value.
function getStepIndex(status: OrderStatus): number {
  switch (status) {
    case "Placed": return 1;
    case "Packed": return 2;
    case "Dispatched": return 3;
    case "Delivered": return 4;
    default: return 0;
  }
}

// [KYA KARTA HAI]: Live SignalR WebSocket Order Tracking modal render karta hai.
// [KYU ZAROORI HAI]: Customer ko real-time order progression, rider telemetry, aur live status dikhane ke liye.
// [KAISE CHALTA HAI]: Centered modal dialog with live pulsating green pulse and stepper steps.
// [OUTPUT / SIDE EFFECT]: Rendered tracking dialog.
// [VARIATIONS TO TRY]: Mapbox / Leaflet live interactive map canvas integration.
export const LiveOrderTracking: React.FC<LiveOrderTrackingProps> = ({
  isOpen,
  onClose,
  orderId,
  status,
  riderLocation,
  isConnected,
}) => {
  if (!isOpen || !orderId) {
    return null;
  }

  const currentStep = getStepIndex(status);

  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 110,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: "16px",
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
          backgroundColor: "rgba(0,0,0,0.5)",
          backdropFilter: "blur(3px)",
        }}
      />

      {/* Modal Card */}
      <div
        style={{
          position: "relative",
          width: "100%",
          maxWidth: "480px",
          backgroundColor: "#ffffff",
          borderRadius: "16px",
          padding: "24px",
          boxShadow: "0 20px 25px -5px rgba(0,0,0,0.1), 0 10px 10px -5px rgba(0,0,0,0.04)",
          zIndex: 111,
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: "16px",
            borderBottom: "1px solid #f3f4f6",
            paddingBottom: "12px",
          }}
        >
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <span style={{ fontSize: "20px" }}>⚡</span>
              <h3 style={{ fontSize: "18px", fontWeight: 700, color: "#111827" }}>
                Live Order #{orderId}
              </h3>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "6px", marginTop: "4px" }}>
              <span
                style={{
                  width: "8px",
                  height: "8px",
                  borderRadius: "50%",
                  backgroundColor: isConnected ? "#10b981" : "#f59e0b",
                  display: "inline-block",
                }}
              />
              <span style={{ fontSize: "12px", color: "#6b7280" }}>
                {isConnected ? "SignalR WebSocket Connected" : "Connecting..."}
              </span>
            </div>
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

        {/* ETA & Status Banner */}
        <div
          style={{
            backgroundColor: status === "Delivered" ? "#ecfdf5" : "#eff6ff",
            border: `1px solid ${status === "Delivered" ? "#86efac" : "#bfdbfe"}`,
            borderRadius: "10px",
            padding: "14px",
            marginBottom: "20px",
            textAlign: "center",
          }}
        >
          <div style={{ fontSize: "13px", fontWeight: 600, color: "#1e40af", marginBottom: "4px" }}>
            CURRENT STATUS
          </div>
          <div style={{ fontSize: "20px", fontWeight: 700, color: status === "Delivered" ? "#047857" : "#1d4ed8" }}>
            {status === "Placed" && "📝 Order Received"}
            {status === "Packed" && "📦 Dark-Store Packing Done"}
            {status === "Dispatched" && `🛵 Out for Delivery (${riderLocation?.estimatedMinutes ?? 8} mins)`}
            {status === "Delivered" && "🎉 Delivered Successfully!"}
          </div>
          {riderLocation && (
            <div style={{ fontSize: "12px", color: "#4b5563", marginTop: "4px" }}>
              Rider GPS: {riderLocation.latitude}, {riderLocation.longitude} • Speed: 28 km/h
            </div>
          )}
        </div>

        {/* 4-Step Visual Stepper */}
        <div style={{ display: "flex", flexDirection: "column", gap: "16px", marginBottom: "20px" }}>
          {TRACKING_STEPS.map((step, idx) => {
            const stepNum = idx + 1;
            const isCompleted = currentStep >= stepNum;
            const isCurrent = currentStep === stepNum;

            return (
              <div
                key={step.id}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "14px",
                  position: "relative",
                }}
              >
                {/* Step Circle */}
                <div
                  style={{
                    width: "36px",
                    height: "36px",
                    borderRadius: "50%",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: "16px",
                    backgroundColor: isCompleted ? "#059669" : "#f3f4f6",
                    color: isCompleted ? "#ffffff" : "#9ca3af",
                    border: isCurrent ? "2px solid #059669" : "none",
                    fontWeight: 700,
                    transition: "all 0.2s ease",
                  }}
                >
                  {isCompleted ? "✓" : step.icon}
                </div>

                {/* Step Text */}
                <div style={{ flex: 1 }}>
                  <div
                    style={{
                      fontSize: "14px",
                      fontWeight: isCurrent ? 700 : isCompleted ? 600 : 500,
                      color: isCompleted ? "#111827" : "#9ca3af",
                    }}
                  >
                    {step.label}
                  </div>
                  <div style={{ fontSize: "12px", color: "#6b7280" }}>
                    {step.id === "Placed" && "Payment confirmed via UPI"}
                    {step.id === "Packed" && "Cold-chain checked & bagged"}
                    {step.id === "Dispatched" && "Rider Suresh Kumar assigned"}
                    {step.id === "Delivered" && "Handed to customer with OTP"}
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Close Button */}
        <button
          onClick={onClose}
          style={{
            width: "100%",
            padding: "10px",
            backgroundColor: "#f3f4f6",
            color: "#374151",
            border: "none",
            borderRadius: "8px",
            fontSize: "14px",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          Close Tracking
        </button>
      </div>
    </div>
  );
};

