// [KYA KARTA HAI]: KiranaHub ke cart calculation aur pricing logic ko encapsulate karne wala class.
// [KYU ZAROORI HAI]: Centralized single source of truth for pricing to avoid discrepancy between frontend and backend.
// [KAISE CHALTA HAI]: Pure static methods provide karta hai jo inputs ke base par mathematical calculations karte hain.
// [OUTPUT / SIDE EFFECT]: Pricing calculations execution class.
// [VARIATIONS TO TRY]: State-based PricingEngine instance jisme configurable tax slabs inject kiye ja sakte hain.
export class PricingEngine {
    // [KYA KARTA HAI]: Default 5% GST tax rate define karta hai as private static constant.
    // [KYU ZAROORI HAI]: Government grocery tax compliance maintain karne ke liye.
    // [KAISE CHALTA HAI]: Immutable numeric constant rate set karta hai.
    // [OUTPUT / SIDE EFFECT]: Class internal tax calculation rate.
    // [VARIATIONS TO TRY]: Per-item tax slabs (essential foods 0%, snacks 12%) dynamically compute kar sakte hain.
    static TAX_RATE = 0.05;
    // [KYA KARTA HAI]: Free delivery threshold limit (Rs 500) define karta hai.
    // [KYU ZAROORI HAI]: Quick-commerce business model me average order value (AOV) boost karne ke liye free delivery incentive deta hai.
    // [KAISE CHALTA HAI]: Rs 500 se kam order par delivery fee lagata hai, upar hone par waive off karta hai.
    // [OUTPUT / SIDE EFFECT]: Threshold boundary value.
    // [VARIATIONS TO TRY]: Prime membership check ke sath 0 threshold kar sakte hain.
    static FREE_DELIVERY_THRESHOLD = 500;
    // [KYA KARTA HAI]: Standard delivery fee (Rs 30) define karta hai.
    // [KYU ZAROORI HAI]: Low-value orders par logistics rider payout cover karne ke liye.
    // [KAISE CHALTA HAI]: Numeric constant value.
    // [OUTPUT / SIDE EFFECT]: Fixed shipping fee.
    // [VARIATIONS TO TRY]: Distance-based dynamic delivery fee calculate kar sakte hain.
    static STANDARD_DELIVERY_FEE = 30;
    // [KYA KARTA HAI]: Order ke items aur optional discount rules ke hisab se complete PricingBreakdown calculate karta hai.
    // [KYU ZAROORI HAI]: Checkout screen aur invoice printing ke liye sabhi mathematical values ek sath compute karne ke liye.
    // [KAISE CHALTA HAI]: Items array iterate karke subtotal nikalta hai, discount deduct karta hai, tax aur delivery fee jodta hai.
    // [OUTPUT / SIDE EFFECT]: Strongly-typed PricingBreakdown object return karta hai.
    // [VARIATIONS TO TRY]: Rounding strategy (Math.round vs Math.floor) configure kar sakte hain.
    static calculate(items, discountRule) {
        // [KYA KARTA HAI]: Empty items array check karta hai.
        // [KYU ZAROORI HAI]: Empty cart par zero total aur zero delivery fee return karne ke liye.
        // [KAISE CHALTA HAI]: Array length check karta hai.
        // [OUTPUT / SIDE EFFECT]: Empty breakdown return or continues.
        // [VARIATIONS TO TRY]: Empty cart par custom EmptyCartError throw kar sakte hain.
        if (items.length === 0) {
            // [KYA KARTA HAI]: Sabhi zero values wala breakdown return karta hai.
            // [KYU ZAROORI HAI]: Safe fallback without negative numbers or NaN.
            // [KAISE CHALTA HAI]: Direct object literal return.
            // [OUTPUT / SIDE EFFECT]: 0 values PricingBreakdown.
            // [VARIATIONS TO TRY]: Constant ZERO_BREAKDOWN return kar sakte hain.
            return {
                subtotal: 0,
                taxAmount: 0,
                deliveryFee: 0,
                discountAmount: 0,
                grandTotal: 0,
                earnedLoyaltyPoints: 0,
            };
        }
        // [KYA KARTA HAI]: Array.prototype.reduce se har item ka price * quantity sum karta hai.
        // [KYU ZAROORI HAI]: Total grocery value (subtotal) compute karne ke liye.
        // [KAISE CHALTA HAI]: Accumulator me line total add karta jata hai starting from 0.
        // [OUTPUT / SIDE EFFECT]: Raw subtotal number.
        // [VARIATIONS TO TRY]: for...of loop se imperative calculate kar sakte hain.
        const subtotal = items.reduce((acc, line) => acc + line.item.price * line.quantity, 0);
        // [KYA KARTA HAI]: Optional discount rule ko invoke karta hai using optional call syntax or fallback to 0.
        // [KYU ZAROORI HAI]: Promo code ya festival discount apply karne ke liye.
        // [KAISE CHALTA HAI]: discountRule?.(subtotal, items) ?? 0 syntax se safely execute hota hai.
        // [OUTPUT / SIDE EFFECT]: Calculated discount amount.
        // [VARIATIONS TO TRY]: Max discount cap (e.g. Math.min(discount, 100)) enforce kar sakte hain.
        const rawDiscount = discountRule ? discountRule(subtotal, items) : 0;
        // [KYA KARTA HAI]: Discount ko subtotal se jyada hone se rokta hai using Math.min.
        // [KYU ZAROORI HAI]: Negative subtotal aur negative order amount bugs ko prevent karne ke liye.
        // [KAISE CHALTA HAI]: Math.min(rawDiscount, subtotal) se cap karta hai.
        // [OUTPUT / SIDE EFFECT]: Safe discount amount (0 <= discount <= subtotal).
        // [VARIATIONS TO TRY]: Negative discount rejection assertion throw kar sakte hain.
        const discountAmount = Math.max(0, Math.min(rawDiscount, subtotal));
        // [KYA KARTA HAI]: Discounted subtotal par 5% tax calculate karta hai.
        // [KYU ZAROORI HAI]: Tax discount ke baad calculate hota hai standard GST norms ke mutabiq.
        // [KAISE CHALTA HAI]: (subtotal - discountAmount) * TAX_RATE compute karke 2 decimal places round karta hai.
        // [OUTPUT / SIDE EFFECT]: Rounded tax amount.
        // [VARIATIONS TO TRY]: Item-wise tax category calculations use kar sakte hain.
        const taxableAmount = subtotal - discountAmount;
        // [KYA KARTA HAI]: Tax amount calculate karke 2 decimal tak round karta hai.
        // [KYU ZAROORI HAI]: Floating point precision issues (jaise 10.0000000000004) se bachane ke liye.
        // [KAISE CHALTA HAI]: Math.round((taxableAmount * TAX_RATE) * 100) / 100 evaluate karta hai.
        // [OUTPUT / SIDE EFFECT]: Clean 2-decimal tax number.
        // [VARIATIONS TO TRY]: Number.EPSILON use kar sakte hain floating precision ke liye.
        const taxAmount = Math.round(taxableAmount * PricingEngine.TAX_RATE * 100) / 100;
        // [KYA KARTA HAI]: Check karta hai ki subtotal free delivery threshold se jyada hai ya nahi.
        // [KYU ZAROORI HAI]: Free delivery incentive apply karne ke liye.
        // [KAISE CHALTA HAI]: Ternary condition evaluate hoti hai.
        // [OUTPUT / SIDE EFFECT]: 0 ya 30 delivery fee.
        // [VARIATIONS TO TRY]: Distance multiplier add kar sakte hain.
        const deliveryFee = subtotal >= PricingEngine.FREE_DELIVERY_THRESHOLD ? 0 : PricingEngine.STANDARD_DELIVERY_FEE;
        // [KYA KARTA HAI]: Final grand total calculate karta hai: taxableAmount + taxAmount + deliveryFee.
        // [KYU ZAROORI HAI]: Customer ke payment gateway par charge hone wala final legal amount.
        // [KAISE CHALTA HAI]: Sabhi positive components ka addition karke 2 decimal round karta hai.
        // [OUTPUT / SIDE EFFECT]: Grand total amount.
        // [VARIATIONS TO TRY]: Cash-on-delivery extra surcharge add kar sakte hain.
        const grandTotal = Math.round((taxableAmount + taxAmount + deliveryFee) * 100) / 100;
        // [KYA KARTA HAI]: Loyalty points calculate karta hai (har Rs 100 par 1 point).
        // [KYU ZAROORI HAI]: Customer retention aur gamified loyalty rewards ke liye.
        // [KAISE CHALTA HAI]: Math.floor(grandTotal / 100) evaluate karta hai.
        // [OUTPUT / SIDE EFFECT]: Integer points count.
        // [VARIATIONS TO TRY]: 2x multiplier for Prime / VIP members.
        const earnedLoyaltyPoints = Math.floor(grandTotal / 100);
        // [KYA KARTA HAI]: Calculated PricingBreakdown object construct karke return karta hai.
        // [KYU ZAROORI HAI]: Caller ko consistent, immutable pricing details provide karne ke liye.
        // [KAISE CHALTA HAI]: Object literal return karta hai.
        // [OUTPUT / SIDE EFFECT]: PricingBreakdown instance.
        // [VARIATIONS TO TRY]: Object.freeze() laga kar runtime immutability enforce kar sakte hain.
        return {
            subtotal,
            taxAmount,
            deliveryFee,
            discountAmount,
            grandTotal,
            earnedLoyaltyPoints,
        };
    }
}
