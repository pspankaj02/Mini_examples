// [KYA KARTA HAI]: Grocery categories ka allowable union type define karta hai.
// [KYU ZAROORI HAI]: Hardcoded strings se bachata hai aur filter tabs ke liye type safety deta hai.
// [KAISE CHALTA HAI]: String literal union compile time par validate hota hai.
// [OUTPUT / SIDE EFFECT]: Valid category literal types.
// [VARIATIONS TO TRY]: Const array of categories with 'typeof CATEGORIES[number]'.
export type GroceryCategory = "All" | "Dairy" | "Produce" | "Bakery" | "Snacks" | "Beverages";

// [KYA KARTA HAI]: Frontend catalog product ka interface define karta hai.
// [KYU ZAROORI HAI]: Product grid cards aur item details display karne ke liye.
// [KAISE CHALTA HAI]: Readonly properties encapsulate karta hai taaki direct mutation roki ja sake.
// [OUTPUT / SIDE EFFECT]: Product entity model contract.
// [VARIATIONS TO TRY]: discountPercentage field add kar sakte hain.
export interface Product {
  readonly id: string;
  readonly name: string;
  readonly price: number;
  readonly category: Exclude<GroceryCategory, "All">;
  readonly stockQuantity: number;
  readonly imageUrl?: string;
  readonly unit: string;
}

// [KYA KARTA HAI]: Cart me rakhe hue item ka contract define karta hai with quantity.
// [KYU ZAROORI HAI]: User selection, quantity adjustments, aur subtotal multiply karne ke liye.
// [KAISE CHALTA HAI]: Product object aur integer quantity field ko pair karta hai.
// [OUTPUT / SIDE EFFECT]: Shopping cart line item structure.
// [VARIATIONS TO TRY]: addedAt timestamp for expiring stale items in cart.
export interface CartItem {
  readonly product: Product;
  readonly quantity: number;
}

// [KYA KARTA HAI]: Cart ke complete mathematical breakdown ka interface.
// [KYU ZAROORI HAI]: Tax, Delivery fee, Promo discount, aur Grand Total display karne ke liye.
// [KAISE CHALTA HAI]: Readonly numeric values encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: Pricing summary object blueprint.
// [VARIATIONS TO TRY]: itemSavings amount field add kar sakte hain.
export interface PricingSummary {
  readonly subtotal: number;
  readonly tax: number;
  readonly deliveryFee: number;
  readonly discount: number;
  readonly grandTotal: number;
  readonly couponCode: string | null;
}

// [KYA KARTA HAI]: Order live status union type define karta hai matching backend SignalR hub.
// [KYU ZAROORI HAI]: Real-time status badge aur progression tracker ke states reflect karne ke liye.
// [KAISE CHALTA HAI]: String literal union type.
// [OUTPUT / SIDE EFFECT]: Valid lifecycle status strings.
// [VARIATIONS TO TRY]: Numeric enum or discriminated union state objects.
export type OrderStatus = "Idle" | "Placed" | "Packed" | "Dispatched" | "Delivered" | "Cancelled";

// [KYA KARTA HAI]: Real-time delivery rider GPS coordinates aur telemetry packet interface.
// [KYU ZAROORI HAI]: SignalR push updates se rider ki live location UI map/progress bar par dikhane ke liye.
// [KAISE CHALTA HAI]: Latitude, longitude, ETA minutes, aur speed encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: Telemetry packet contract.
// [VARIATIONS TO TRY]: Battery percent ya rider heading bearing add kar sakte hain.
export interface RiderLocation {
  readonly orderId: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly estimatedMinutes: number;
  readonly updatedAtUtc: string;
}

