// [KYA KARTA HAI]: Domain order interfaces aur type guard assertion import karta hai.
// [KYU ZAROORI HAI]: State transitions me type-safety ensure karne ke liye.
// [KAISE CHALTA HAI]: ES module import se specific types aur functions load hote hain.
// [OUTPUT / SIDE EFFECT]: Compile-time domain symbols.
// [VARIATIONS TO TRY]: Re-export from index type file.
import type {
  KiranaOrder,
  PlacedOrder,
  PackedOrder,
  DispatchedOrder,
  DeliveredOrder,
  CancelledOrder,
} from "../types/domain.js";
import { assertValidRiderPhone } from "../types/domain.js";

// [KYA KARTA HAI]: Compile-time exhaustiveness check function jo unhandled union types ko catch karta hai.
// [KYU ZAROORI HAI]: Agar future me koi naya order state (jaise 'Refunded') add kiya jaye aur switch case miss ho jaye, toh TypeScript compile error dega.
// [KAISE CHALTA HAI]: Parameter type 'never' leta hai. Agar koi unhandled case switch fallthrough karega toh wo 'never' me assign nahi hoga aur compiler red line dikhayega.
// [OUTPUT / SIDE EFFECT]: Runtime par fatal error throw karta hai agar invalid value reach ho.
// [VARIATIONS TO TRY]: Custom exhaustive check with detailed runtime logging.
export function assertNever(value: never): never {
  // [KYA KARTA HAI]: Unreachable code par error throw karta hai.
  // [KYU ZAROORI HAI]: Runtime bug se silent failure ko prevent karne ke liye.
  // [KAISE CHALTA HAI]: Error object create karke throw karta hai.
  // [OUTPUT / SIDE EFFECT]: Throws Error.
  // [VARIATIONS TO TRY]: Sentry / monitoring telemetry emit kar sakte hain.
  throw new Error(`Unhandled discriminated union member: ${JSON.stringify(value)}`);
}

// [KYA KARTA HAI]: Order state transitions aur lifecycle management ke static methods provide karta hai.
// [KYU ZAROORI HAI]: Business rules enforce karne ke liye (e.g. Delivered order cancel nahi ho sakta, Packed order hi dispatch ho sakta hai).
// [KAISE CHALTA HAI]: Pure functional transition functions jo naya state object return karte hain bina existing object ko mutate kiye.
// [OUTPUT / SIDE EFFECT]: Immutable state machine execution class.
// [VARIATIONS TO TRY]: Event-driven StateMachine jo transition hooks emit kare.
export class OrderStateMachine {
  // [KYA KARTA HAI]: PlacedOrder ko PackedOrder me transition karta hai jab dark-store picker saaman pack kar le.
  // [KYU ZAROORI HAI]: Picking accuracy aur warehouse traceability maintain karne ke liye.
  // [KAISE CHALTA HAI]: PlacedOrder ke fields copy karta hai, status ko 'Packed' set karta hai, pickerId aur timestamp attach karta hai.
  // [OUTPUT / SIDE EFFECT]: Naya PackedOrder object return karta hai.
  // [VARIATIONS TO TRY]: Picking verification barcode list validate kar sakte hain.
  public static packOrder(order: PlacedOrder, pickerId: string): PackedOrder {
    // [KYA KARTA HAI]: Picker ID non-empty string validation check karta hai.
    // [KYU ZAROORI HAI]: Anonymous picking ko prevent karne ke liye.
    // [KAISE CHALTA HAI]: Trimmed length check karta hai.
    // [OUTPUT / SIDE EFFECT]: Throws Error if invalid.
    // [VARIATIONS TO TRY]: Regex check for dark-store employee ID format.
    if (!pickerId || pickerId.trim().length === 0) {
      // [KYA KARTA HAI]: Validation error throw karta hai.
      // [KYU ZAROORI HAI]: Missing pickerId rokne ke liye.
      // [KAISE CHALTA HAI]: Error throw hota hai.
      // [OUTPUT / SIDE EFFECT]: Execution aborts.
      // [VARIATIONS TO TRY]: Custom DomainRuleException use kar sakte hain.
      throw new Error("Cannot pack order: Picker ID must be provided.");
    }

    // [KYA KARTA HAI]: Naya PackedOrder object return karta hai.
    // [KYU ZAROORI HAI]: Immutable transition pattern ensure karta hai.
    // [KAISE CHALTA HAI]: Object spread aur new properties combine karta hai.
    // [OUTPUT / SIDE EFFECT]: PackedOrder instance.
    // [VARIATIONS TO TRY]: Object.freeze() use kar sakte hain.
    return {
      status: "Packed",
      orderId: order.orderId,
      items: order.items,
      totalAmount: order.totalAmount,
      pickerId: pickerId.trim(),
      packedAtUtc: new Date(),
    };
  }

  // [KYA KARTA HAI]: PackedOrder ko DispatchedOrder me transition karta hai jab delivery rider order pickup karta hai.
  // [KYU ZAROORI HAI]: Customer ko live tracking enable karne aur ETA calculate karne ke liye.
  // [KAISE CHALTA HAI]: Rider phone number assert karta hai aur status 'Dispatched' set karta hai.
  // [OUTPUT / SIDE EFFECT]: Naya DispatchedOrder object.
  // [VARIATIONS TO TRY]: Multiple bags confirmation parameter add kar sakte hain.
  public static dispatchOrder(
    order: PackedOrder,
    riderName: string,
    riderPhone: string,
    estimatedMinutes: number
  ): DispatchedOrder {
    // [KYA KARTA HAI]: Rider phone number validation assertion function call karta hai.
    // [KYU ZAROORI HAI]: Invalid phone number se SMS delivery failure ko prevent karne ke liye.
    // [KAISE CHALTA HAI]: assertValidRiderPhone execute hota hai.
    // [OUTPUT / SIDE EFFECT]: Phone valid verify hota hai ya throw karta hai.
    // [VARIATIONS TO TRY]: Optional fallback emergency number support.
    assertValidRiderPhone(riderPhone);

    // [KYA KARTA HAI]: Estimated delivery minutes positive number check karta hai.
    // [KYU ZAROORI HAI]: Negative ya zero ETA customer app par dikhane se rokne ke liye.
    // [KAISE CHALTA HAI]: Numeric boundary check karta hai.
    // [OUTPUT / SIDE EFFECT]: Throws Error if ETA is zero or negative.
    // [VARIATIONS TO TRY]: Maximum ETA threshold (e.g. max 60 minutes) enforce kar sakte hain.
    if (estimatedMinutes <= 0) {
      // [KYA KARTA HAI]: Error throw karta hai.
      // [KYU ZAROORI HAI]: Invalid ETA prevent karne ke liye.
      // [KAISE CHALTA HAI]: Error instantiation.
      // [OUTPUT / SIDE EFFECT]: Transition fails.
      // [VARIATIONS TO TRY]: Default fallback to 15 minutes.
      throw new Error(`Estimated minutes must be positive. Received: ${estimatedMinutes}`);
    }

    // [KYA KARTA HAI]: DispatchedOrder object create aur return karta hai.
    // [KYU ZAROORI HAI]: Safe immutable state transition.
    // [KAISE CHALTA HAI]: Properties assign hoti hain.
    // [OUTPUT / SIDE EFFECT]: DispatchedOrder instance.
    // [VARIATIONS TO TRY]: GPS coordinate snapshot capture kar sakte hain.
    return {
      status: "Dispatched",
      orderId: order.orderId,
      items: order.items,
      totalAmount: order.totalAmount,
      riderName: riderName.trim(),
      riderPhone,
      estimatedMinutes,
      dispatchedAtUtc: new Date(),
    };
  }

  // [KYA KARTA HAI]: DispatchedOrder ko DeliveredOrder me transition karta hai jab customer delivery accept kar le.
  // [KYU ZAROORI HAI]: Delivery confirmation aur order completion audit trail ke liye.
  // [KAISE CHALTA HAI]: status 'Delivered' set karta hai aur receiver ka naam store karta hai.
  // [OUTPUT / SIDE EFFECT]: Naya DeliveredOrder object.
  // [VARIATIONS TO TRY]: OTP verification check pass kar sakte hain.
  public static deliverOrder(order: DispatchedOrder, receivedBy: string): DeliveredOrder {
    // [KYA KARTA HAI]: Naya DeliveredOrder return karta hai.
    // [KYU ZAROORI HAI]: Lifecycle complete karne ke liye.
    // [KAISE CHALTA HAI]: Object literal instantiation.
    // [OUTPUT / SIDE EFFECT]: DeliveredOrder instance.
    // [VARIATIONS TO TRY]: Feedback rating trigger callback add kar sakte hain.
    return {
      status: "Delivered",
      orderId: order.orderId,
      items: order.items,
      totalAmount: order.totalAmount,
      deliveredAtUtc: new Date(),
      receivedBy: receivedBy.trim() || "Customer",
    };
  }

  // [KYA KARTA HAI]: Kisi bhi unfulfilled order (Placed, Packed, Dispatched) ko CancelledOrder me transition karta hai.
  // [KYU ZAROORI HAI]: Customer cancellation ya dark-store out-of-stock scenario me order abort karne ke liye.
  // [KAISE CHALTA HAI]: Check karta hai ki order already Delivered ya Cancelled toh nahi hai, fir CancelledOrder return karta hai.
  // [OUTPUT / SIDE EFFECT]: Naya CancelledOrder object ya throw agar already terminal state me ho.
  // [VARIATIONS TO TRY]: Cancellation fee deduction rule add kar sakte hain.
  public static cancelOrder(order: KiranaOrder, reason: string): CancelledOrder {
    // [KYA KARTA HAI]: Check karta hai ki order already Delivered toh nahi hai.
    // [KYU ZAROORI HAI]: Post-delivery cancellation allow nahi hoti (return workflow alag hota hai).
    // [KAISE CHALTA HAI]: Discriminated status equality check.
    // [OUTPUT / SIDE EFFECT]: Throws Error if delivered.
    // [VARIATIONS TO TRY]: 10-minute return window exception allow kar sakte hain.
    if (order.status === "Delivered") {
      // [KYA KARTA HAI]: Error throw karta hai.
      // [KYU ZAROORI HAI]: Delivered order cancel hone se rokne ke liye.
      // [KAISE CHALTA HAI]: Throws standard Error.
      // [OUTPUT / SIDE EFFECT]: Operation aborted.
      // [VARIATIONS TO TRY]: Custom BusinessRuleViolationException.
      throw new Error(`Order '${order.orderId}' has already been delivered. Use the Return workflow instead.`);
    }

    // [KYA KARTA HAI]: Check karta hai ki order already Cancelled toh nahi hai.
    // [KYU ZAROORI HAI]: Double cancellation aur duplicate refunds se bachane ke liye.
    // [KAISE CHALTA HAI]: Discriminated status equality check.
    // [OUTPUT / SIDE EFFECT]: Throws Error if already cancelled.
    // [VARIATIONS TO TRY]: Idempotent return without throwing error.
    if (order.status === "Cancelled") {
      // [KYA KARTA HAI]: Error throw karta hai.
      // [KYU ZAROORI HAI]: Redundant cancellation rokne ke liye.
      // [KAISE CHALTA HAI]: Throws Error.
      // [OUTPUT / SIDE EFFECT]: Operation aborted.
      // [VARIATIONS TO TRY]: Return existing order directly.
      throw new Error(`Order '${order.orderId}' is already cancelled.`);
    }

    // [KYA KARTA HAI]: CancelledOrder create aur return karta hai.
    // [KYU ZAROORI HAI]: Refund processing ke liye.
    // [KAISE CHALTA HAI]: Properties assign hoti hain.
    // [OUTPUT / SIDE EFFECT]: CancelledOrder instance.
    // [VARIATIONS TO TRY]: refundTransactionId attach kar sakte hain.
    return {
      status: "Cancelled",
      orderId: order.orderId,
      cancellationReason: reason.trim() || "Customer requested cancellation",
      cancelledAtUtc: new Date(),
      refundInitiated: true,
    };
  }

  // [KYA KARTA HAI]: Discriminated Union par exhaustive pattern matching karke human-readable summary string return karta hai.
  // [KYU ZAROORI HAI]: Demonstrate karta hai ki TypeScript compiler har case ko verify karta hai aur switch ke end par assertNever se exhaustiveness check enforce hota hai.
  // [KAISE CHALTA HAI]: switch (order.status) use karta hai. Har case me TypeScript order ko us specific interface me narrow kar deta hai.
  // [OUTPUT / SIDE EFFECT]: Formatted status summary string.
  // [VARIATIONS TO TRY]: Rich HTML badge string return kar sakte hain.
  public static getStatusSummary(order: KiranaOrder): string {
    // [KYA KARTA HAI]: Order ke discriminated 'status' field par switch evaluation karta hai.
    // [KYU ZAROORI HAI]: Clean branching logic without risky type casting.
    // [KAISE CHALTA HAI]: Exact string equality matching.
    // [OUTPUT / SIDE EFFECT]: Matching case execution.
    // [VARIATIONS TO TRY]: Record-based lookup table pattern.
    switch (order.status) {
      // [KYA KARTA HAI]: Placed state handle karta hai.
      // [KYU ZAROORI HAI]: Placed order specific message dikhane ke liye.
      // [KAISE CHALTA HAI]: order.totalAmount access karta hai (automatically narrowed).
      // [OUTPUT / SIDE EFFECT]: Placed string.
      // [VARIATIONS TO TRY]: Time elapsed since placement show kar sakte hain.
      case "Placed":
        return `[Placed] Order #${order.orderId} received. Total: Rs ${order.totalAmount}. Waiting for warehouse picker.`;

      // [KYA KARTA HAI]: Packed state handle karta hai.
      // [KYU ZAROORI HAI]: Picker ID display karne ke liye.
      // [KAISE CHALTA HAI]: order.pickerId safely access karta hai.
      // [OUTPUT / SIDE EFFECT]: Packed string.
      // [VARIATIONS TO TRY]: Bag count include kar sakte hain.
      case "Packed":
        return `[Packed] Order #${order.orderId} packed by Picker #${order.pickerId}. Ready for rider pickup.`;

      // [KYA KARTA HAI]: Dispatched state handle karta hai.
      // [KYU ZAROORI HAI]: Rider details aur ETA display karne ke liye.
      // [KAISE CHALTA HAI]: order.riderName aur order.estimatedMinutes access karta hai.
      // [OUTPUT / SIDE EFFECT]: Dispatched string.
      // [VARIATIONS TO TRY]: Live tracking link embed kar sakte hain.
      case "Dispatched":
        return `[Dispatched] Rider ${order.riderName} (${order.riderPhone}) is on the way! ETA: ~${order.estimatedMinutes} mins.`;

      // [KYA KARTA HAI]: Delivered state handle karta hai.
      // [KYU ZAROORI HAI]: Delivery confirmation aur receiver name dikhane ke liye.
      // [KAISE CHALTA HAI]: order.receivedBy safely access karta hai.
      // [OUTPUT / SIDE EFFECT]: Delivered string.
      // [VARIATIONS TO TRY]: Feedback prompt add kar sakte hain.
      case "Delivered":
        return `[Delivered] Order #${order.orderId} successfully handed to '${order.receivedBy}'. Enjoy your groceries!`;

      // [KYA KARTA HAI]: Cancelled state handle karta hai.
      // [KYU ZAROORI HAI]: Cancellation reason aur refund status display karne ke liye.
      // [KAISE CHALTA HAI]: order.cancellationReason access karta hai.
      // [OUTPUT / SIDE EFFECT]: Cancelled string.
      // [VARIATIONS TO TRY]: Helpdesk contact number show kar sakte hain.
      case "Cancelled":
        return `[Cancelled] Order #${order.orderId} was cancelled. Reason: ${order.cancellationReason}. Refund initiated: ${order.refundInitiated}.`;

      // [KYA KARTA HAI]: Default unhandled fallback case jo compile time par guarantee deta hai ki saare states handle hain.
      // [KYU ZAROORI HAI]: Agar union me naya state add ho jaye aur switch me miss ho jaye toh compilation turant fail ho.
      // [KAISE CHALTA HAI]: assertNever(order) call karta hai.
      // [OUTPUT / SIDE EFFECT]: Throws Error if executed.
      // [VARIATIONS TO TRY]: Compile-time diagnostic tool.
      default:
        return assertNever(order);
    }
  }
}

