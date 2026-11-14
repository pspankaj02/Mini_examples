// [KYA KARTA HAI]: Base Entity interface define karta hai jisme har entity ki unique identifier string hoti hai.
// [KYU ZAROORI HAI]: Generic Repository aur Domain Models ko ensure karne ke liye ki unke paas ek unique 'id' maujood ho.
// [KAISE CHALTA HAI]: TypeScript interface inheritance ke zariye har concrete model ko contract enforce karta hai.
// [OUTPUT / SIDE EFFECT]: Compile-time interface structure generate hota hai jo runtime par zero overhead deta hai.
// [VARIATIONS TO TRY]: string ke bajaye number id ya branded type jaise EntityId = string & { readonly __brand: unique symbol } use kar sakte hain.
export interface Entity {
  readonly id: string;
}

// [KYA KARTA HAI]: Grocery Category ke allowable enum values ko define karta hai.
// [KYU ZAROORI HAI]: Hardcoded strings (magic strings) ko eliminate karta hai aur type safety ensure karta hai.
// [KAISE CHALTA HAI]: String literal union type TypeScript me enum se lightweight aur tree-shakeable alternative hai.
// [OUTPUT / SIDE EFFECT]: Valid categories ka compile-time set create karta hai.
// [VARIATIONS TO TRY]: TypeScript numeric enum ya 'as const' object map use kiya ja sakta hai.
export type GroceryCategory = "Dairy" | "Produce" | "Bakery" | "Snacks" | "Beverages";

// [KYA KARTA HAI]: KiranaHub ke standard product ka contract define karta hai jo Entity interface extend karta hai.
// [KYU ZAROORI HAI]: Catalog me display aur cart calculations ke liye price, stock, aur perishable status track karta hai.
// [KAISE CHALTA HAI]: Entity interface ko inherit karke 'id' property mandatory karta hai aur product-specific attributes add karta hai.
// [OUTPUT / SIDE EFFECT]: Concrete product objects ke liye strict structural type definition.
// [VARIATIONS TO TRY]: Discount percentage ya tax rate add karke pricing aur complex bana sakte hain.
export interface GroceryItem extends Entity {
  readonly name: string;
  readonly price: number;
  readonly category: GroceryCategory;
  readonly stockQuantity: number;
  readonly isPerishable: boolean;
  readonly expiryDays?: number;
}

// [KYA KARTA HAI]: Order ke item ka contract define karta hai jisme GroceryItem aur user ki desired quantity hoti hai.
// [KYU ZAROORI HAI]: Cart aur checkout me calculate karne ke liye ki customer ne kaun si cheez kitni kharidi hai.
// [KAISE CHALTA HAI]: GroceryItem object aur ek numeric quantity field ko combine karta hai.
// [OUTPUT / SIDE EFFECT]: Line-item level pricing aur subtotal calculation ka blueprint banta hai.
// [VARIATIONS TO TRY]: Custom notes (e.g. 'ripe bananas only') field add kiya ja sakta hai.
export interface OrderLineItem {
  readonly item: GroceryItem;
  readonly quantity: number;
}

// [KYA KARTA HAI]: Placed order state define karta hai jo order creation ke turant baad trigger hoti hai.
// [KYU ZAROORI HAI]: State machine me initial phase ko track karta hai jab customer ne payment complete kar di ho.
// [KAISE CHALTA HAI]: Discriminated union ke liye status: 'Placed' tag use karta hai.
// [OUTPUT / SIDE EFFECT]: Status 'Placed' type guard ke saath compatible state object banata hai.
// [VARIATIONS TO TRY]: paymentMethod (UPI, COD, Card) add kar sakte hain.
export interface PlacedOrder {
  readonly status: "Placed";
  readonly orderId: string;
  readonly items: readonly OrderLineItem[];
  readonly totalAmount: number;
  readonly placedAtUtc: Date;
}

// [KYA KARTA HAI]: Packed order state define karta hai jab dark-store picker grocery pack kar chuka ho.
// [KYU ZAROORI HAI]: Warehouse operations ko track karta hai aur rider dispatch ke liye ready karta hai.
// [KAISE CHALTA HAI]: status: 'Packed' discriminant tag aur pickerId property encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: Order packing verification type create karta hai.
// [VARIATIONS TO TRY]: bagCount (kitne bags me order pack hua) field include kar sakte hain.
export interface PackedOrder {
  readonly status: "Packed";
  readonly orderId: string;
  readonly items: readonly OrderLineItem[];
  readonly totalAmount: number;
  readonly pickerId: string;
  readonly packedAtUtc: Date;
}

// [KYA KARTA HAI]: Dispatched order state define karta hai jab delivery rider dark-store se nikal chuka ho.
// [KYU ZAROORI HAI]: Live GPS tracking aur rider coordinates ko order se bind karta hai.
// [KAISE CHALTA HAI]: status: 'Dispatched' discriminant tag aur riderName, riderPhone, estimatedMinutes capture karta hai.
// [OUTPUT / SIDE EFFECT]: Live tracking UI ke liye necessary data ensure karta hai.
// [VARIATIONS TO TRY]: currentCoordinates: { lat: number; lng: number } add kar sakte hain.
export interface DispatchedOrder {
  readonly status: "Dispatched";
  readonly orderId: string;
  readonly items: readonly OrderLineItem[];
  readonly totalAmount: number;
  readonly riderName: string;
  readonly riderPhone: string;
  readonly estimatedMinutes: number;
  readonly dispatchedAtUtc: Date;
}

// [KYA KARTA HAI]: Delivered order state define karta hai jab customer ko saaman mil chuka ho.
// [KYU ZAROORI HAI]: Successful completion lifecycle track karta hai aur review/feedback trigger karta hai.
// [KAISE CHALTA HAI]: status: 'Delivered' discriminant tag aur deliveredAtUtc timestamp capture karta hai.
// [OUTPUT / SIDE EFFECT]: Order completion milestone create karta hai.
// [VARIATIONS TO TRY]: deliveryOtpVerified: boolean ya customerSignatureUrl property add kar sakte hain.
export interface DeliveredOrder {
  readonly status: "Delivered";
  readonly orderId: string;
  readonly items: readonly OrderLineItem[];
  readonly totalAmount: number;
  readonly deliveredAtUtc: Date;
  readonly receivedBy: string;
}

// [KYA KARTA HAI]: Cancelled order state define karta hai jab order kisi karan se abort hua ho.
// [KYU ZAROORI HAI]: Refund initiation aur cancellation audit trail maintain karne ke liye.
// [KAISE CHALTA HAI]: status: 'Cancelled' discriminant tag aur cancellationReason capture karta hai.
// [OUTPUT / SIDE EFFECT]: Safe cancellation representation provide karta hai.
// [VARIATIONS TO TRY]: refundTransactionId add kiya ja sakta hai.
export interface CancelledOrder {
  readonly status: "Cancelled";
  readonly orderId: string;
  readonly cancellationReason: string;
  readonly cancelledAtUtc: Date;
  readonly refundInitiated: boolean;
}

// [KYA KARTA HAI]: KiranaHub ke saare possible order states ka exhaustive Discriminated Union type create karta hai.
// [KYU ZAROORI HAI]: TypeScript compiler ko allow karta hai ki switch case me har state ko strictly verify kare aur runtime bugs roke.
// [KAISE CHALTA HAI]: PlacedOrder | PackedOrder | DispatchedOrder | DeliveredOrder | CancelledOrder ko union karta hai.
// [OUTPUT / SIDE EFFECT]: Type-safe state machine representation banta hai.
// [VARIATIONS TO TRY]: Naye state jaise 'PartiallyDelivered' add karke exhaustive check dekh sakte hain.
export type KiranaOrder = PlacedOrder | PackedOrder | DispatchedOrder | DeliveredOrder | CancelledOrder;

// [KYA KARTA HAI]: Generic API Success Response interface define karta hai.
// [KYU ZAROORI HAI]: Har successful API endpoint se aane wale payload ko consistent format deta hai.
// [KAISE CHALTA HAI]: Generic type parameter <TData> pass karta hai jo data field ka type decide karta hai.
// [OUTPUT / SIDE EFFECT]: Standard success response structure ensure karta hai.
// [VARIATIONS TO TRY]: pagination metadata (pageNumber, pageSize, totalCount) include kar sakte hain.
export interface ApiSuccessResponse<TData> {
  readonly success: true;
  readonly data: TData;
  readonly timestamp: string;
}

// [KYA KARTA HAI]: Generic API Error Response interface define karta hai.
// [KYU ZAROORI HAI]: RFC 7807 ProblemDetails ki tarah structured error messaging provide karta hai.
// [KAISE CHALTA HAI]: success: false discriminant ke saath errorMessage aur optional errorCode capture karta hai.
// [OUTPUT / SIDE EFFECT]: Standard error envelope ensure karta hai.
// [VARIATIONS TO TRY]: fieldErrors: Record<string, string[]> add karke form validations map kar sakte hain.
export interface ApiErrorResponse {
  readonly success: false;
  readonly errorMessage: string;
  readonly errorCode?: string;
  readonly timestamp: string;
}

// [KYA KARTA HAI]: Discriminated Union generic API response jo success ya failure ko encapsulate karta hai.
// [KYU ZAROORI HAI]: Caller ko force karta hai ki if (res.success) check kare before accessing res.data.
// [KAISE CHALTA HAI]: ApiSuccessResponse<TData> | ApiErrorResponse ka union type create karta hai.
// [OUTPUT / SIDE EFFECT]: Safe HTTP response consumer experience provide karta hai.
// [VARIATIONS TO TRY]: Monadic Result<T, E> type implement kar sakte hain.
export type ApiResponse<TData> = ApiSuccessResponse<TData> | ApiErrorResponse;

// [KYA KARTA HAI]: Generic Repository interface jo CRUD operations enforce karta hai for any entity extending Entity.
// [KYU ZAROORI HAI]: Data access layer ko abstract karta hai taaki in-memory ya REST client interchangeable hon.
// [KAISE CHALTA HAI]: Generic constraint T extends Entity use karke verify karta hai ki T ke paas id property hai.
// [OUTPUT / SIDE EFFECT]: Type-safe asynchronous storage contract provide karta hai.
// [VARIATIONS TO TRY]: Query pagination ya filtering predicate add kar sakte hain.
export interface IRepository<T extends Entity> {
  getById(id: string): Promise<T | null>;
  getAll(): Promise<readonly T[]>;
  save(entity: T): Promise<void>;
  delete(id: string): Promise<boolean>;
}

// [KYA KARTA HAI]: Custom Type Guard jo check karta hai ki koi item perishable hai ya nahi aur optional expiryDays exist karta hai ya nahi.
// [KYU ZAROORI HAI]: Runtime narrowing ke liye taaki compiler ko pata chale ki expiryDays definitely defined number hai.
// [KAISE CHALTA HAI]: 'item is GroceryItem & { expiryDays: number }' return type ke sath boolean check karta hai.
// [OUTPUT / SIDE EFFECT]: TypeScript compiler item ke type ko narrow down kar deta hai.
// [VARIATIONS TO TRY]: Temperature sensitivity ya cold-storage requirements check karne wale guards bana sakte hain.
export function isPerishableItem(item: GroceryItem): item is GroceryItem & { readonly expiryDays: number } {
  // [KYA KARTA HAI]: Boolean expression return karta hai check karke ki isPerishable true hai aur expiryDays number hai.
  // [KYU ZAROORI HAI]: Invalid ya zero expiry wale perishable products ko detect karta hai.
  // [KAISE CHALTA HAI]: Strict boolean evaluation karta hai.
  // [OUTPUT / SIDE EFFECT]: True ya false return hota hai.
  // [VARIATIONS TO TRY]: item.expiryDays > 0 check add kar sakte hain.
  return item.isPerishable === true && typeof item.expiryDays === "number";
}

// [KYA KARTA HAI]: Assertion Function jo check karta hai ki rider ka phone number valid 10-digit Indian mobile number hai.
// [KYU ZAROORI HAI]: SMS dispatch aur delivery call service me bad phone number se crash hone se bachata hai.
// [KAISE CHALTA HAI]: 'asserts phone is string' return type syntax se runtime condition fail hone par throw karta hai.
// [OUTPUT / SIDE EFFECT]: Function ke baad compiler guarantee deta hai ki phone string non-null aur valid hai.
// [VARIATIONS TO TRY]: Country code (+91) regex validation support add kar sakte hain.
export function assertValidRiderPhone(phone: string): asserts phone is string {
  // [KYA KARTA HAI]: 10 digit regex pattern se phone match karta hai.
  // [KYU ZAROORI HAI]: Alpha characters ya incomplete phone numbers ko reject karta hai.
  // [KAISE CHALTA HAI]: RegExp test method execute karta hai.
  // [OUTPUT / SIDE EFFECT]: Match boolean milta hai.
  // [VARIATIONS TO TRY]: Landline validation ya international format regex add kar sakte hain.
  const isValid = /^[6-9]\d{9}$/.test(phone);
  // [KYA KARTA HAI]: Check karta hai ki regex match hui ya nahi.
  // [KYU ZAROORI HAI]: Invalid phone par exception throw karne ke liye.
  // [KAISE CHALTA HAI]: If statement se condition evaluate hoti hai.
  // [OUTPUT / SIDE EFFECT]: Throw error or proceed.
  // [VARIATIONS TO TRY]: Custom ValidationError class throw kar sakte hain.
  if (!isValid) {
    // [KYA KARTA HAI]: Descriptive Error object throw karta hai.
    // [KYU ZAROORI HAI]: Caller ko clearly batata hai ki validation kyu fail hua.
    // [KAISE CHALTA HAI]: JavaScript Error instantiate hota hai.
    // [OUTPUT / SIDE EFFECT]: Execution ruk jata hai aur stack trace capture hota hai.
    // [VARIATIONS TO TRY]: Localization message pass kar sakte hain.
    throw new Error(`Invalid rider phone number: '${phone}'. Must be a valid 10-digit mobile number.`);
  }
}

// [KYA KARTA HAI]: Utility Types demonstration ke liye ReadonlyGrocery type banata hai using Readonly<T>.
// [KYU ZAROORI HAI]: Mutability bugs se bachata hai jab product objects catalog me cache kiye jayein.
// [KAISE CHALTA HAI]: TypeScript utility type Readonly<GroceryItem> har property ko readonly bana deta hai.
// [OUTPUT / SIDE EFFECT]: Immutable grocery type declaration.
// [VARIATIONS TO TRY]: DeepReadonly<T> custom recursive type use kar sakte hain.
export type ReadonlyGrocery = Readonly<GroceryItem>;

// [KYA KARTA HAI]: Product update form ke liye DTO banata hai using Partial<T> aur Omit<T, K>.
// [KYU ZAROORI HAI]: Update endpoint par user ko saare fields bhejne ki zaroorat nahi hoti, aur 'id' modify nahi ho sakti.
// [KAISE CHALTA HAI]: Omit se 'id' exclude hoti hai aur bache hue fields par Partial apply hota hai.
// [OUTPUT / SIDE EFFECT]: Type-safe patch update payload structure.
// [VARIATIONS TO TRY]: Pick<GroceryItem, 'price' | 'stockQuantity'> se dedicated fast-update DTO bana sakte hain.
export type UpdateProductDto = Partial<Omit<GroceryItem, "id">>;

// [KYA KARTA HAI]: Category-wise inventory count map define karta hai using Record<K, V>.
// [KYU ZAROORI HAI]: Dashboard statistics me category aggregate numbers store karne ke liye.
// [KAISE CHALTA HAI]: Key ko GroceryCategory union aur value ko number me strictly bind karta hai.
// [OUTPUT / SIDE EFFECT]: Exhaustive category-to-count map type.
// [VARIATIONS TO TRY]: Record<string, GroceryItem[]> se category-to-products grouping type bana sakte hain.
export type CategoryStockRecord = Record<GroceryCategory, number>;

