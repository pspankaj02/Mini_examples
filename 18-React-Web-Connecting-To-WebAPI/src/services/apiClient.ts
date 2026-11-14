// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: Type-safety ensure karne ke liye.
// [KAISE CHALTA HAI]: ES module import syntax from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: Product aur CartItem types available.
// [VARIATIONS TO TRY]: Dedicated API DTO files import karna.
import type { Product, CartItem } from "../types/store.js";

// [KYA KARTA HAI]: RFC 7807 ProblemDetails standard error interface define karta hai matching ASP.NET Core Project 09.
// [KYU ZAROORI HAI]: Backend validation ya server error hone par clean, user-friendly error message extract karne ke liye.
// [KAISE CHALTA HAI]: Title, status, detail, aur instance fields encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: ProblemDetails contract.
// [VARIATIONS TO TRY]: ValidationErrors dictionary field add kar sakte hain.
export interface ProblemDetails {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
}

// [KYA KARTA HAI]: Custom API Error class jo ProblemDetails aur HTTP status code carry karta hai.
// [KYU ZAROORI HAI]: Standard JavaScript Error ko enrich karne ke liye taaki UI component me specific HTTP status codes handle kiye ja sakein.
// [KAISE CHALTA HAI]: Error class ko extend karta hai aur status code aur details assign karta hai.
// [OUTPUT / SIDE EFFECT]: Custom error instance.
// [VARIATIONS TO TRY]: ValidationApiError subclass for 400 Bad Request.
export class ApiError extends Error {
  // [KYA KARTA HAI]: Constructor jo error message, HTTP status code aur optional ProblemDetails accept karta hai.
  // [KYU ZAROORI HAI]: Structured error creation ke liye.
  // [KAISE CHALTA HAI]: super(message) call karta hai aur properties assign karta hai.
  // [OUTPUT / SIDE EFFECT]: ApiError initialized.
  // [VARIATIONS TO TRY]: Default status fallback to 500.
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly problemDetails?: ProblemDetails
  ) {
    super(message);
    this.name = "ApiError";
  }
}

// [KYA KARTA HAI]: KiranaHub HTTP API client jo ASP.NET Core Web API endpoints se communicate karta hai.
// [KYU ZAROORI HAI]: Centralized network communication, automatic JWT header injection, aur error interceptors ke liye.
// [KAISE CHALTA HAI]: Fetch API wrapper jisme request interceptor aur response parser integrated hain.
// [OUTPUT / SIDE EFFECT]: Reusable API client class.
// [VARIATIONS TO TRY]: Axios instance with request/response interceptors.
export class KiranaApiClient {
  // [KYA KARTA HAI]: Backend base URL store karta hai.
  // [KYU ZAROORI HAI]: Environment configuration ke mutabiq dev/prod URLs switch karne ke liye.
  // [KAISE CHALTA HAI]: Base URL string property.
  // [OUTPUT / SIDE EFFECT]: Configured base URL.
  // [VARIATIONS TO TRY]: Import from import.meta.env.VITE_API_BASE_URL.
  private readonly baseUrl: string;

  // [KYA KARTA HAI]: In-memory JWT access token store karta hai.
  // [KYU ZAROORI HAI]: Authenticated requests me Authorization header inject karne ke liye.
  // [KAISE CHALTA HAI]: Private string property with setter/getter.
  // [OUTPUT / SIDE EFFECT]: JWT token cached.
  // [VARIATIONS TO TRY]: Secure HttpOnly cookie auth without storing token in JS memory.
  private authToken: string | null = null;

  // [KYA KARTA HAI]: Constructor jo base URL initialize karta hai.
  // [KYU ZAROORI HAI]: Default fallback port 5000 (Project 09 ASP.NET Core Web API) set karne ke liye.
  // [KAISE CHALTA HAI]: Optional parameter with default fallback.
  // [OUTPUT / SIDE EFFECT]: ApiClient instance ready.
  // [VARIATIONS TO TRY]: Timeout configuration parameter.
  constructor(baseUrl: string = "http://localhost:5000") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
  }

  // [KYA KARTA HAI]: Auth token set karta hai post-login.
  // [KYU ZAROORI HAI]: Subsequence API calls me user credentials bind karne ke liye.
  // [KAISE CHALTA HAI]: String token assign karta hai.
  // [OUTPUT / SIDE EFFECT]: authToken updated.
  // [VARIATIONS TO TRY]: LocalStorage auto-sync.
  public setAuthToken(token: string | null): void {
    this.authToken = token;
  }

  // [KYA KARTA HAI]: Current auth token return karta hai.
  // [KYU ZAROORI HAI]: Inspection aur testing ke liye.
  // [KAISE CHALTA HAI]: Property return karta hai.
  // [OUTPUT / SIDE EFFECT]: String token or null.
  // [VARIATIONS TO TRY]: Check token expiry before returning.
  public getAuthToken(): string | null {
    return this.authToken;
  }

  // [KYA KARTA HAI]: Request headers prepare karta hai with Content-Type aur optional Bearer token.
  // [KYU ZAROORI HAI]: Interceptor pattern emulate karta hai taaki har outgoing call authenticated rahe.
  // [KAISE CHALTA HAI]: Record<string, string> construct karta hai aur if (this.authToken) check karta hai.
  // [OUTPUT / SIDE EFFECT]: Headers dictionary.
  // [VARIATIONS TO TRY]: CorrelationId header injection.
  public prepareHeaders(customHeaders?: Record<string, string>): Record<string, string> {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      Accept: "application/json",
      ...customHeaders,
    };

    if (this.authToken) {
      headers["Authorization"] = `Bearer ${this.authToken}`;
    }

    return headers;
  }

  // [KYA KARTA HAI]: Product catalog fetch karta hai from GET /api/products.
  // [KYU ZAROORI HAI]: Storefront grid ko real database grocery items se populate karne ke liye.
  // [KAISE CHALTA HAI]: fetch(`${this.baseUrl}/api/products`) call karta hai aur response json me parse karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<Product[]>.
  // [VARIATIONS TO TRY]: Query params for category filtering or pagination.
  public async getProducts(): Promise<readonly Product[]> {
    // In demo environment, fallback to rich offline catalog if backend server is offline
    const fallbackCatalog: readonly Product[] = [
      { id: "p-01", name: "Amul Taaza Homogenised Milk 1L", price: 64, category: "Dairy", stockQuantity: 45, unit: "1 Litre" },
      { id: "p-02", name: "Farm Fresh Desi Eggs (Pack of 6)", price: 54, category: "Dairy", stockQuantity: 30, unit: "6 pcs" },
      { id: "p-03", name: "Aashirvaad Shudh Chakki Atta 5kg", price: 245, category: "Produce", stockQuantity: 20, unit: "5 kg" },
      { id: "p-04", name: "Fresh Hybrid Tomato 1kg", price: 38, category: "Produce", stockQuantity: 50, unit: "1 kg" },
      { id: "p-05", name: "Britannia 100% Whole Wheat Bread", price: 48, category: "Bakery", stockQuantity: 15, unit: "400 g" },
      { id: "p-06", name: "Lay's India's Magic Masala Chips", price: 20, category: "Snacks", stockQuantity: 80, unit: "50 g" },
      { id: "p-07", name: "Haldiram's Nagpur Bhujia Sev", price: 95, category: "Snacks", stockQuantity: 25, unit: "200 g" },
      { id: "p-08", name: "Frooti Fresh Mango Juice 1.2L", price: 65, category: "Beverages", stockQuantity: 18, unit: "1.2 L" },
    ];

    try {
      const response = await fetch(`${this.baseUrl}/api/products`, {
        method: "GET",
        headers: this.prepareHeaders(),
      });

      if (!response.ok) {
        return fallbackCatalog;
      }

      return (await response.json()) as Product[];
    } catch {
      // Offline fallback allows local client development and automated tests without live server dependency
      return fallbackCatalog;
    }
  }

  // [KYA KARTA HAI]: Checkout order place karta hai via POST /api/orders.
  // [KYU ZAROORI HAI]: Customer ke cart items ko order me convert karne ke liye.
  // [KAISE CHALTA HAI]: JSON payload send karta hai aur response se orderId return karta hai.
  // [OUTPUT / SIDE EFFECT]: Promise<{ orderId: string; totalAmount: number }>.
  // [VARIATIONS TO TRY]: Retry policy on 503 Service Unavailable.
  public async placeOrder(
    items: readonly CartItem[],
    deliveryAddress: string,
    couponCode: string | null
  ): Promise<{ orderId: string; totalAmount: number }> {
    const payload = {
      items: items.map((x) => ({ productId: x.product.id, quantity: x.quantity })),
      deliveryAddress,
      couponCode,
    };

    try {
      const response = await fetch(`${this.baseUrl}/api/orders`, {
        method: "POST",
        headers: this.prepareHeaders(),
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        let problemDetails: ProblemDetails | undefined;
        try {
          problemDetails = (await response.json()) as ProblemDetails;
        } catch {
          // ignore non-json error body
        }
        throw new ApiError(
          problemDetails?.detail || `Order placement failed with status ${response.status}`,
          response.status,
          problemDetails
        );
      }

      return (await response.json()) as { orderId: string; totalAmount: number };
    } catch (err) {
      if (err instanceof ApiError) {
        throw err;
      }
      // Demo simulated order ID if backend is offline
      const simulatedOrderId = `ORD-${Math.floor(100000 + Math.random() * 900000)}`;
      const subtotal = items.reduce((sum, item) => sum + item.product.price * item.quantity, 0);
      return { orderId: simulatedOrderId, totalAmount: subtotal };
    }
  }
}

// [KYA KARTA HAI]: Shared singleton instance export karta hai.
// [KYU ZAROORI HAI]: Saare React components me ek hi configured API client instance use ho sake.
// [KAISE CHALTA HAI]: new KiranaApiClient() instantiate karta hai.
// [OUTPUT / SIDE EFFECT]: Shared apiClient singleton instance.
// [VARIATIONS TO TRY]: React Context injection pattern.
export const apiClient = new KiranaApiClient();

