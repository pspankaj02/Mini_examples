namespace KiranaHub.CleanArchitecture.Domain.Entities;

// =========================================================================================
// [TOPIC]: Domain Order Status Lifecycle
// [PURPOSE]: Explicit states for Order aggregate root state transitions.
// =========================================================================================

// [KYA KARTA HAI]: Order ke allowed states ko represent karne wala enum.
// [KYU ZAROORI HAI]: State pattern aur valid state transitions enforce karna.
public enum OrderStatus
{
    // [KYA KARTA HAI]: Initial state jab order create hua ho.
    PendingPayment = 1,

    // [KYA KARTA HAI]: Payment complete ho chuki hai; warehouse dispatch pending.
    Paid = 2,

    // [KYA KARTA HAI]: Rider delivery ke liye nikal chuka hai.
    OutForDelivery = 3,

    // [KYA KARTA HAI]: Customer tak grocery successfully pahuch gayi.
    Delivered = 4,

    // [KYA KARTA HAI]: Order cancel ho chuka hai.
    Cancelled = 5
}

