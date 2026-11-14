// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: MobileProduct type access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/mobile.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Discount badge types.
import type { MobileProduct } from "../types/mobile.js";

// [KYA KARTA HAI]: ProductListItem component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Product information aur stepper actions pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: ProductListItemProps interface.
// [VARIATIONS TO TRY]: OnItemPress for product details modal.
export interface ProductListItemProps {
  readonly product: MobileProduct;
  readonly quantity: number;
  readonly onAdd: (product: MobileProduct) => void;
  readonly onUpdateQuantity: (id: string, qty: number) => void;
}

// [KYA KARTA HAI]: Mobile grocery product row card render karta hai.
// [KYU ZAROORI HAI]: Virtualized FlatList row me clean thumb-friendly product layout dikhane ke liye.
// [KAISE CHALTA HAI]: Card layout with product icon, details aur counter button.
// [OUTPUT / SIDE EFFECT]: Mobile list item card.
// [VARIATIONS TO TRY]: Swipeable row for quick delete.
export const ProductListItem: React.FC<ProductListItemProps> = ({
  product,
  quantity,
  onAdd,
  onUpdateQuantity,
}) => {
  return (
    <div
      style={{
        backgroundColor: "#ffffff",
        borderRadius: "12px",
        padding: "12px",
        marginBottom: "10px",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        border: "1px solid #f3f4f6",
        boxShadow: "0 1px 3px rgba(0,0,0,0.03)",
      }}
    >
      {/* Left: Product Icon & Info */}
      <div style={{ display: "flex", alignItems: "center", gap: "12px", flex: 1 }}>
        <div
          style={{
            width: "50px",
            height: "50px",
            backgroundColor: "#f9fafb",
            borderRadius: "10px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: "24px",
            border: "1px solid #e5e7eb",
          }}
        >
          {product.icon}
        </div>

        <div>
          <h4 style={{ fontSize: "14px", fontWeight: 600, color: "#111827", margin: 0 }}>
            {product.name}
          </h4>
          <span style={{ fontSize: "12px", color: "#6b7280" }}>{product.unit}</span>
          <div style={{ fontSize: "14px", fontWeight: 700, color: "#059669", marginTop: "2px" }}>
            ₹{product.price}
          </div>
        </div>
      </div>

      {/* Right: Stepper or Add Button */}
      <div>
        {quantity === 0 ? (
          <button
            onClick={() => onAdd(product)}
            style={{
              backgroundColor: "#ffffff",
              color: "#059669",
              border: "1.5px solid #059669",
              padding: "6px 14px",
              borderRadius: "8px",
              fontSize: "12px",
              fontWeight: 700,
              cursor: "pointer",
            }}
          >
            ADD
          </button>
        ) : (
          <div
            style={{
              display: "flex",
              alignItems: "center",
              backgroundColor: "#059669",
              borderRadius: "8px",
              overflow: "hidden",
            }}
          >
            <button
              onClick={() => onUpdateQuantity(product.id, quantity - 1)}
              style={{
                backgroundColor: "transparent",
                color: "#ffffff",
                border: "none",
                padding: "6px 8px",
                fontWeight: 700,
                fontSize: "13px",
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
              {quantity}
            </span>
            <button
              onClick={() => onUpdateQuantity(product.id, quantity + 1)}
              style={{
                backgroundColor: "transparent",
                color: "#ffffff",
                border: "none",
                padding: "6px 8px",
                fontWeight: 700,
                fontSize: "13px",
                cursor: "pointer",
              }}
            >
              +
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

