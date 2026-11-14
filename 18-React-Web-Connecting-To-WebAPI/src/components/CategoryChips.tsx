// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: GroceryCategory union type access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: GroceryCategory type.
// [VARIATIONS TO TRY]: Import category metadata with icons.
import type { GroceryCategory } from "../types/store.js";

// [KYA KARTA HAI]: CategoryChips component ke props ka interface define karta hai.
// [KYU ZAROORI HAI]: Selected category aur selection callback pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: CategoryChipsProps interface.
// [VARIATIONS TO TRY]: Count of items per category prop.
export interface CategoryChipsProps {
  readonly selectedCategory: GroceryCategory;
  readonly onSelectCategory: (category: GroceryCategory) => void;
}

// [KYA KARTA HAI]: Categories list with display emojis define karta hai.
// [KYU ZAROORI HAI]: User-friendly chips render karne ke liye.
// [KAISE CHALTA HAI]: Array of category objects.
// [OUTPUT / SIDE EFFECT]: Categories configuration array.
// [VARIATIONS TO TRY]: Dynamic categories fetched from API.
const CATEGORIES: readonly { id: GroceryCategory; label: string; icon: string }[] = [
  { id: "All", label: "All Items", icon: "🧺" },
  { id: "Dairy", label: "Dairy & Eggs", icon: "🥛" },
  { id: "Produce", label: "Produce & Staples", icon: "🥦" },
  { id: "Bakery", label: "Bakery & Breads", icon: "🍞" },
  { id: "Snacks", label: "Munchies & Snacks", icon: "🍿" },
  { id: "Beverages", label: "Cold Drinks & Juices", icon: "🧃" },
];

// [KYA KARTA HAI]: Category filter chips bar render karta hai.
// [KYU ZAROORI HAI]: Quick one-tap filtering by product department.
// [KAISE CHALTA HAI]: Categories map karke interactive pill buttons banata hai.
// [OUTPUT / SIDE EFFECT]: Horizontal scrollable chips bar.
// [VARIATIONS TO TRY]: Swiper carousel on touch devices.
export const CategoryChips: React.FC<CategoryChipsProps> = ({
  selectedCategory,
  onSelectCategory,
}) => {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "8px",
        overflowX: "auto",
        padding: "16px 0",
        whiteSpace: "nowrap",
      }}
    >
      {CATEGORIES.map((cat) => {
        const isSelected = selectedCategory === cat.id;
        return (
          <button
            key={cat.id}
            onClick={() => onSelectCategory(cat.id)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              padding: "8px 16px",
              borderRadius: "20px",
              fontSize: "14px",
              fontWeight: isSelected ? 600 : 500,
              backgroundColor: isSelected ? "#059669" : "#ffffff",
              color: isSelected ? "#ffffff" : "#374151",
              border: isSelected ? "1px solid #059669" : "1px solid #d1d5db",
              cursor: "pointer",
              transition: "all 0.15s ease",
            }}
          >
            <span>{cat.icon}</span>
            <span>{cat.label}</span>
          </button>
        );
      })}
    </div>
  );
};

