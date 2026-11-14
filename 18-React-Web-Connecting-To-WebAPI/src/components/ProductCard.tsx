// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping for performance.
import React from "react";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: Product type access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: Product type.
// [VARIATIONS TO TRY]: Import custom product badge types.
import type { Product } from "../types/store.js";

// [KYA KARTA HAI]: ProductCard component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Product details, current cart count aur action callbacks receive karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: ProductCardProps interface.
// [VARIATIONS TO TRY]: Wishlist toggle callback prop.
export interface ProductCardProps {
  readonly product: Product;
  readonly cartQuantity: number;
  readonly onAddToCart: (product: Product) => void;
  readonly onUpdateQuantity: (productId: string, quantity: number) => void;
}

// [KYA KARTA HAI]: Single grocery product card render karta hai.
// [KYU ZAROORI HAI]: Catalog grid me har item ki pricing, unit, image aur smart Add/Quantity stepper dikhane ke liye.
// [KAISE CHALTA HAI]: Functional component jo clean card layout return karta hai.
// [OUTPUT / SIDE EFFECT]: Rendered product card.
// [VARIATIONS TO TRY]: Hover zoom animation on product image.
export const ProductCard: React.FC<ProductCardProps> = ({
  product,
  cartQuantity,
  onAddToCart,
  onUpdateQuantity,
}) => {
  return (
    <div
      style={{
        backgroundColor: "#ffffff",
        border: "1px solid #e5e7eb",
        borderRadius: "12px",
        padding: "16px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        transition: "box-shadow 0.2s ease",
        position: "relative",
      }}
    >
      {/* Category Pill Tag */}
      <div style={{ marginBottom: "8px" }}>
        <span
          style={{
            fontSize: "11px",
            fontWeight: 600,
            color: "#059669",
            backgroundColor: "#ecfdf5",
            padding: "2px 8px",
            borderRadius: "12px",
            textTransform: "uppercase",
            letterSpacing: "0.5px",
          }}
        >
          {product.category}
        </span>
      </div>

      {/* Product Image / Placeholder Icon */}
      <div
        style={{
          height: "120px",
          backgroundColor: "#f3f4f6",
          borderRadius: "8px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "40px",
          marginBottom: "12px",
        }}
      >
        {product.category === "Dairy" ? "🥛" : product.category === "Produce" ? "🥦" : product.category === "Bakery" ? "🍞" : product.category === "Snacks" ? "🍿" : "🧃"}
      </div>

      {/* Product Name & Unit */}
      <div style={{ flex: 1, marginBottom: "12px" }}>
        <h3
          style={{
            fontSize: "15px",
            fontWeight: 600,
            color: "#111827",
            marginBottom: "4px",
            lineHeight: "1.3",
          }}
        >
          {product.name}
        </h3>
        <p style={{ fontSize: "13px", color: "#6b7280" }}>{product.unit}</p>
      </div>

      {/* Price & Action Button */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          paddingTop: "8px",
          borderTop: "1px dashed #e5e7eb",
        }}
      >
        <div>
          <span style={{ fontSize: "16px", fontWeight: 700, color: "#111827" }}>
            ₹{product.price}
          </span>
        </div>

        {cartQuantity === 0 ? (
          <button
            onClick={() => onAddToCart(product)}
            style={{
              backgroundColor: "#ffffff",
              color: "#059669",
              border: "1.5px solid #059669",
              padding: "6px 16px",
              borderRadius: "6px",
              fontWeight: 700,
              fontSize: "13px",
              cursor: "pointer",
              transition: "all 0.15s ease",
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
              borderRadius: "6px",
              overflow: "hidden",
            }}
          >
            <button
              onClick={() => onUpdateQuantity(product.id, cartQuantity - 1)}
              style={{
                backgroundColor: "transparent",
                color: "#ffffff",
                border: "none",
                padding: "6px 10px",
                fontSize: "14px",
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
                fontSize: "13px",
                padding: "0 6px",
              }}
            >
              {cartQuantity}
            </span>
            <button
              onClick={() => onUpdateQuantity(product.id, cartQuantity + 1)}
              style={{
                backgroundColor: "transparent",
                color: "#ffffff",
                border: "none",
                padding: "6px 10px",
                fontSize: "14px",
                fontWeight: 700,
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

