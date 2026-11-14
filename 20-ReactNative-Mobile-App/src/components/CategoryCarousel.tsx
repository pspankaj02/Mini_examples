// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX components me JSX syntax support karne ke liye.
// [KAISE CHALTA HAI]: React library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React.memo wrapping.
import React from "react";

// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: MobileGroceryCategory type access karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/mobile.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Dynamic categories list.
import type { MobileGroceryCategory } from "../types/mobile.js";

// [KYA KARTA HAI]: CategoryCarousel props interface define karta hai.
// [KYU ZAROORI HAI]: Active category aur tap callback pass karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties define karta hai.
// [OUTPUT / SIDE EFFECT]: CategoryCarouselProps interface.
// [VARIATIONS TO TRY]: Badges count per category.
export interface CategoryCarouselProps {
  readonly selectedCategory: MobileGroceryCategory;
  readonly onSelectCategory: (category: MobileGroceryCategory) => void;
}

// [KYA KARTA HAI]: Mobile category definitions with emoji icons.
// [KYU ZAROORI HAI]: Mobile carousel chips render karne ke liye.
// [KAISE CHALTA HAI]: Array of category objects.
// [OUTPUT / SIDE EFFECT]: Categories configuration array.
// [VARIATIONS TO TRY]: Localized Hindi category names.
const MOBILE_CATEGORIES: readonly { id: MobileGroceryCategory; label: string; icon: string }[] = [
  { id: "All", label: "All", icon: "🧺" },
  { id: "Dairy", label: "Dairy & Milk", icon: "🥛" },
  { id: "Produce", label: "Vegetables & Atta", icon: "🥦" },
  { id: "Bakery", label: "Bakery", icon: "🍞" },
  { id: "Snacks", label: "Munchies", icon: "🍿" },
  { id: "Beverages", label: "Juices", icon: "🧃" },
];

// [KYA KARTA HAI]: Horizontal swipeable category carousel render karta hai.
// [KYU ZAROORI HAI]: Mobile app me single-thumb category switching facilitate karne ke liye.
// [KAISE CHALTA HAI]: Overflow-x auto container with smooth scrolling.
// [OUTPUT / SIDE EFFECT]: Mobile category pills carousel.
// [VARIATIONS TO TRY]: React Native ScrollView horizontal={true} in native build.
export const CategoryCarousel: React.FC<CategoryCarouselProps> = ({
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
        padding: "12px 16px",
        whiteSpace: "nowrap",
        scrollbarWidth: "none",
      }}
    >
      {MOBILE_CATEGORIES.map((cat) => {
        const isSelected = selectedCategory === cat.id;
        return (
          <button
            key={cat.id}
            onClick={() => onSelectCategory(cat.id)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              padding: "6px 14px",
              borderRadius: "16px",
              fontSize: "13px",
              fontWeight: isSelected ? 700 : 500,
              backgroundColor: isSelected ? "#059669" : "#ffffff",
              color: isSelected ? "#ffffff" : "#374151",
              border: isSelected ? "1px solid #059669" : "1px solid #e5e7eb",
              cursor: "pointer",
              boxShadow: isSelected ? "0 2px 4px rgba(5,150,105,0.2)" : "none",
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

