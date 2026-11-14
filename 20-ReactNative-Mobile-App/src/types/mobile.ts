// [KYA KARTA HAI]: Mobile grocery categories ka allowable union type define karta hai.
// [KYU ZAROORI HAI]: Mobile app category chips aur navigation filtering ke liye type safety ensure karta hai.
// [KAISE CHALTA HAI]: String literal union compile time par validate hota hai.
// [OUTPUT / SIDE EFFECT]: Allowed category strings.
// [VARIATIONS TO TRY]: Const enum representation.
export type MobileGroceryCategory = "All" | "Dairy" | "Produce" | "Bakery" | "Snacks" | "Beverages";

// [KYA KARTA HAI]: Mobile product catalog item ka interface define karta hai.
// [KYU ZAROORI HAI]: Virtualized FlatList cards render karne aur memory efficient tracking ke liye.
// [KAISE CHALTA HAI]: Readonly properties encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: MobileProduct contract.
// [VARIATIONS TO TRY]: Nutritional badges (e.g. 'Gluten-Free') add kar sakte hain.
export interface MobileProduct {
  readonly id: string;
  readonly name: string;
  readonly price: number;
  readonly category: Exclude<MobileGroceryCategory, "All">;
  readonly unit: string;
  readonly stockQuantity: number;
  readonly icon: string;
}

// [KYA KARTA HAI]: Mobile shopping cart line item ka contract.
// [KYU ZAROORI HAI]: Offline AsyncStorage caching aur local quantity adjustments ke liye.
// [KAISE CHALTA HAI]: MobileProduct aur integer quantity field ko pair karta hai.
// [OUTPUT / SIDE EFFECT]: MobileCartItem structure.
// [VARIATIONS TO TRY]: Added timestamp for auto-clearing abandoned carts.
export interface MobileCartItem {
  readonly product: MobileProduct;
  readonly quantity: number;
}

// [KYA KARTA HAI]: Mobile checkout pricing breakdown interface define karta hai.
// [KYU ZAROORI HAI]: Subtotal, GST, Delivery fee, aur coupon discount display karne ke liye.
// [KAISE CHALTA HAI]: Readonly numeric fields encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: MobilePricing structure.
// [VARIATIONS TO TRY]: Wallet balance deduction field.
export interface MobilePricing {
  readonly subtotal: number;
  readonly tax: number;
  readonly deliveryFee: number;
  readonly discount: number;
  readonly grandTotal: number;
  readonly couponCode: string | null;
}

// [KYA KARTA HAI]: Mobile order status lifecycle states union define karta hai.
// [KYU ZAROORI HAI]: Bottom sheet tracking modal me visual progress bars aur live steps dikhane ke liye.
// [KAISE CHALTA HAI]: String literal union ("Placed" | "Packed" | "Dispatched" | "Delivered").
// [OUTPUT / SIDE EFFECT]: Status lifecycle type.
// [VARIATIONS TO TRY]: "RiderArrivedAtDoor" granular step.
export type MobileOrderStatus = "Placed" | "Packed" | "Dispatched" | "Delivered";

// [KYA KARTA HAI]: Real-time live rider GPS location snapshot interface define karta hai.
// [KYU ZAROORI HAI]: Delivery rider movement simulation aur ETA display karne ke liye.
// [KAISE CHALTA HAI]: Coordinates, speed, remaining distance, aur ETA encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: RiderGpsSnapshot contract.
// [VARIATIONS TO TRY]: Heading degrees bearing for rotating bike icon on map.
export interface RiderGpsSnapshot {
  readonly latitude: number;
  readonly longitude: number;
  readonly speedKmph: number;
  readonly remainingMeters: number;
  readonly estimatedMinutes: number;
}

