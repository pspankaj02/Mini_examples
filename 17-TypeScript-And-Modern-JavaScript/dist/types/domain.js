// [KYA KARTA HAI]: Custom Type Guard jo check karta hai ki koi item perishable hai ya nahi aur optional expiryDays exist karta hai ya nahi.
// [KYU ZAROORI HAI]: Runtime narrowing ke liye taaki compiler ko pata chale ki expiryDays definitely defined number hai.
// [KAISE CHALTA HAI]: 'item is GroceryItem & { expiryDays: number }' return type ke sath boolean check karta hai.
// [OUTPUT / SIDE EFFECT]: TypeScript compiler item ke type ko narrow down kar deta hai.
// [VARIATIONS TO TRY]: Temperature sensitivity ya cold-storage requirements check karne wale guards bana sakte hain.
export function isPerishableItem(item) {
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
export function assertValidRiderPhone(phone) {
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
