namespace KiranaHub.BasicsAndOops.Interfaces;

// =========================================================================================
// [KYA KARTA HAI]:
// Delivery handling ke liye ek pure contract define karta hai (OOPS Abstraction).
//
// [KYU ZAROORI HAI]:
// Logistics system ko is baat se matlab nahi ki product kya hai (Doodh hai ya Atta bag),
// use bas yeh pata hona chahiye ki shipping label generate ho sakta hai ya nahi.
//
// [KAISE CHALTA HAI]:
// Interface me koi code nahi hota, sirf method signatures hote hain.
// Jo bhi class isko implement karegi, use contract fulfill karna padega.
// =========================================================================================
public interface IDeliverable
{
    string GenerateShippingLabel();
}

