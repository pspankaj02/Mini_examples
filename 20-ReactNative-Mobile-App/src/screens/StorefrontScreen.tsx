// [KYA KARTA HAI]: React core hooks import karta hai.
// [KYU ZAROORI HAI]: State, effects aur callbacks manage karne ke liye.
// [KAISE CHALTA HAI]: React package se load hote hain.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React Native hooks (useWindowDimensions).
import React, { useState, useEffect, useMemo, useCallback } from "react";

// [KYA KARTA HAI]: Mobile domain types import karta hai.
// [KYU ZAROORI HAI]: MobileProduct, MobileCartItem, MobilePricing types verify karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/mobile.js.
// [OUTPUT / SIDE EFFECT]: Types available.
// [VARIATIONS TO TRY]: Types index barrel export.
import type {
  MobileProduct,
  MobileCartItem,
  MobilePricing,
  MobileGroceryCategory,
  MobileOrderStatus,
  RiderGpsSnapshot,
} from "../types/mobile.js";

// [KYA KARTA HAI]: Offline storage adapter import karta hai.
// [KYU ZAROORI HAI]: Cart items ko device storage me persist aur restore karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ../services/mobileStorage.js.
// [OUTPUT / SIDE EFFECT]: MobileStorageAdapter available.
// [VARIATIONS TO TRY]: Encrypted storage adapter.
import { MobileStorageAdapter } from "../services/mobileStorage.js";

// [KYA KARTA HAI]: Sub-components import karta hai.
// [KYU ZAROORI HAI]: Mobile UI screen assemble karne ke liye.
// [KAISE CHALTA HAI]: ES module imports from ../components/.
// [OUTPUT / SIDE EFFECT]: Components available.
// [VARIATIONS TO TRY]: Dynamic lazy loading.
import { MobileHeader } from "../components/MobileHeader.js";
import { CategoryCarousel } from "../components/CategoryCarousel.js";
import { ProductListItem } from "../components/ProductListItem.js";
import { RiderGpsTrackingSheet } from "../components/RiderGpsTrackingSheet.js";

// [KYA KARTA HAI]: Initial mobile catalog dataset define karta hai.
// [KYU ZAROORI HAI]: App launch par offline-ready grocery items provide karne ke liye.
// [KAISE CHALTA HAI]: Array of MobileProduct objects.
// [OUTPUT / SIDE EFFECT]: Catalog items array.
// [VARIATIONS TO TRY]: Remote sync with SQLite local cache.
const MOBILE_CATALOG: readonly MobileProduct[] = [
  { id: "mob-01", name: "Amul Taaza Homogenised Milk", price: 64, category: "Dairy", unit: "1 Litre", stockQuantity: 40, icon: "🥛" },
  { id: "mob-02", name: "Mother Dairy Paneer Fresh", price: 92, category: "Dairy", unit: "200 g", stockQuantity: 25, icon: "🧀" },
  { id: "mob-03", name: "Aashirvaad Chakki Atta", price: 245, category: "Produce", unit: "5 kg", stockQuantity: 30, icon: "🌾" },
  { id: "mob-04", name: "Fresh Red Onions (Pyaaz)", price: 42, category: "Produce", unit: "1 kg", stockQuantity: 60, icon: "🧅" },
  { id: "mob-05", name: "Harvest Gold White Bread", price: 45, category: "Bakery", unit: "400 g", stockQuantity: 20, icon: "🍞" },
  { id: "mob-06", name: "Kurkure Masala Munch", price: 20, category: "Snacks", unit: "85 g", stockQuantity: 80, icon: "🍿" },
  { id: "mob-07", name: "Real Mixed Fruit Juice", price: 110, category: "Beverages", unit: "1 Litre", stockQuantity: 15, icon: "🧃" },
];

// [KYA KARTA HAI]: Main Mobile Storefront Screen component render karta hai.
// [KYU ZAROORI HAI]: Mobile user journey (Catalog browse -> Quick cart -> Offline persistence -> Simulated live tracking) encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Mobile screen container with max phone width styling.
// [OUTPUT / SIDE EFFECT]: Mobile storefront UI screen.
// [VARIATIONS TO TRY]: React Native Screen in React Navigation Stack.
export const StorefrontScreen: React.FC = () => {
  const [cartItems, setCartItems] = useState<MobileCartItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<MobileGroceryCategory>("All");
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);
  const [isCartModalOpen, setIsCartModalOpen] = useState<boolean>(false);

  // Live order tracking states
  const [activeOrderId, setActiveOrderId] = useState<string | null>(null);
  const [orderStatus, setOrderStatus] = useState<MobileOrderStatus>("Placed");
  const [gpsSnapshot, setGpsSnapshot] = useState<RiderGpsSnapshot | null>(null);
  const [isTrackingSheetVisible, setIsTrackingSheetVisible] = useState<boolean>(false);

  // [KYA KARTA HAI]: Initial mount par persisted cart load karta hai.
  // [KYU ZAROORI HAI]: App reopen hone par customer cart restore karne ke liye.
  // [KAISE CHALTA HAI]: MobileStorageAdapter.loadCart() execute karta hai.
  // [OUTPUT / SIDE EFFECT]: cartItems populated from storage.
  // [VARIATIONS TO TRY]: Encrypted storage restore.
  useEffect(() => {
    MobileStorageAdapter.loadCart().then((loaded) => {
      if (loaded.length > 0) {
        setCartItems([...loaded]);
      }
    });
  }, []);

  // [KYA KARTA HAI]: Cart items change hone par storage me save karta hai.
  // [KYU ZAROORI HAI]: Persistent offline cart sync ke liye.
  // [KAISE CHALTA HAI]: MobileStorageAdapter.saveCart(cartItems) execute hota hai.
  // [OUTPUT / SIDE EFFECT]: Storage synced.
  // [VARIATIONS TO TRY]: Debounced save on rapid quantity clicks.
  useEffect(() => {
    MobileStorageAdapter.saveCart(cartItems);
  }, [cartItems]);

  // [KYA KARTA HAI]: Add to cart handler.
  // [KYU ZAROORI HAI]: Item cart me dalne ya count badhane ke liye.
  // [KAISE CHALTA HAI]: Existing item check karke quantity update karta hai.
  // [OUTPUT / SIDE EFFECT]: cartItems state updated.
  // [VARIATIONS TO TRY]: Haptic feedback trigger on mobile tap.
  const handleAddToCart = useCallback((product: MobileProduct) => {
    setCartItems((prev) => {
      const idx = prev.findIndex((x) => x.product.id === product.id);
      if (idx >= 0) {
        const copy = [...prev];
        const existing = copy[idx];
        if (existing) {
          copy[idx] = { ...existing, quantity: existing.quantity + 1 };
        }
        return copy;
      }
      return [...prev, { product, quantity: 1 }];
    });
  }, []);

  // [KYA KARTA HAI]: Quantity update handler.
  // [KYU ZAROORI HAI]: Stepper minus/plus handle karne ke liye.
  // [KAISE CHALTA HAI]: Quantity <= 0 par filter out karta hai.
  // [OUTPUT / SIDE EFFECT]: Updated cart items.
  // [VARIATIONS TO TRY]: Max order count limit check.
  const handleUpdateQuantity = useCallback((id: string, quantity: number) => {
    setCartItems((prev) => {
      if (quantity <= 0) {
        return prev.filter((x) => x.product.id !== id);
      }
      return prev.map((x) => (x.product.id === id ? { ...x, quantity } : x));
    });
  }, []);

  // [KYA KARTA HAI]: Filtered products calculation.
  // [KYU ZAROORI HAI]: Instant mobile search aur category filtering ke liye.
  // [KAISE CHALTA HAI]: useMemo se category aur search string match karta hai.
  // [OUTPUT / SIDE EFFECT]: Filtered items list.
  // [VARIATIONS TO TRY]: Fuzzy search scoring.
  const filteredProducts = useMemo(() => {
    return MOBILE_CATALOG.filter((item) => {
      const catMatch = selectedCategory === "All" || item.category === selectedCategory;
      const searchMatch =
        searchQuery.trim() === "" ||
        item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.category.toLowerCase().includes(searchQuery.toLowerCase());
      return catMatch && searchMatch;
    });
  }, [selectedCategory, searchQuery]);

  // [KYA KARTA HAI]: Pricing breakdown calculation.
  // [KYU ZAROORI HAI]: Checkout summary calculate karne ke liye.
  // [KAISE CHALTA HAI]: Subtotal, 5% GST, free delivery (>= 500) aur flat 10% coupon compute karta hai.
  // [OUTPUT / SIDE EFFECT]: MobilePricing object.
  // [VARIATIONS TO TRY]: Promo coupon code state.
  const pricing: MobilePricing = useMemo(() => {
    const subtotal = cartItems.reduce((sum, x) => sum + x.product.price * x.quantity, 0);
    const discount = Math.round(subtotal * 0.1); // 10% auto discount
    const taxable = Math.max(0, subtotal - discount);
    const tax = Math.round(taxable * 0.05 * 100) / 100;
    const deliveryFee = subtotal >= 500 || subtotal === 0 ? 0 : 30;
    const grandTotal = Math.round((taxable + tax + deliveryFee) * 100) / 100;

    return {
      subtotal,
      tax,
      deliveryFee,
      discount,
      grandTotal,
      couponCode: "KIRANA10",
    };
  }, [cartItems]);

  const totalItemCount = useMemo(() => {
    return cartItems.reduce((sum, x) => sum + x.quantity, 0);
  }, [cartItems]);

  // [KYA KARTA HAI]: Pull-to-refresh simulation handler.
  // [KYU ZAROORI HAI]: Mobile app UX standard support karne ke liye.
  // [KAISE CHALTA HAI]: 800ms spinner delay ke sath catalog re-sync simulate karta hai.
  // [OUTPUT / SIDE EFFECT]: Refresh spinner state.
  // [VARIATIONS TO TRY]: Native RefreshControl integration.
  const handlePullToRefresh = () => {
    setIsRefreshing(true);
    setTimeout(() => {
      setIsRefreshing(false);
    }, 800);
  };

  // [KYA KARTA HAI]: Mobile order checkout trigger handler.
  // [KYU ZAROORI HAI]: Order submit karke live tracking launch karne ke liye.
  // [KAISE CHALTA HAI]: Cart clear karta hai, storage flush karta hai, aur live GPS simulation start karta hai.
  // [OUTPUT / SIDE EFFECT]: Live tracking active.
  // [VARIATIONS TO TRY]: Push notification trigger.
  const handleCheckout = async () => {
    if (cartItems.length === 0) return;
    const orderId = `MOB-${Math.floor(100000 + Math.random() * 900000)}`;

    await MobileStorageAdapter.clearCart();
    setCartItems([]);
    setIsCartModalOpen(false);

    setActiveOrderId(orderId);
    setOrderStatus("Placed");
    setIsTrackingSheetVisible(true);

    // Simulate Rider GPS movement steps
    setTimeout(() => {
      setOrderStatus("Packed");
      setTimeout(() => {
        setOrderStatus("Dispatched");
        setGpsSnapshot({
          latitude: 28.628,
          longitude: 77.365,
          speedKmph: 32,
          remainingMeters: 1200,
          estimatedMinutes: 6,
        });

        setTimeout(() => {
          setGpsSnapshot({
            latitude: 28.6295,
            longitude: 77.37,
            speedKmph: 24,
            remainingMeters: 300,
            estimatedMinutes: 2,
          });

          setTimeout(() => {
            setOrderStatus("Delivered");
            setGpsSnapshot({
              latitude: 28.63,
              longitude: 77.372,
              speedKmph: 0,
              remainingMeters: 0,
              estimatedMinutes: 0,
            });
          }, 2500);
        }, 2000);
      }, 2000);
    }, 1500);
  };

  return (
    <div
      style={{
        maxWidth: "430px",
        margin: "0 auto",
        minHeight: "100vh",
        backgroundColor: "#f9fafb",
        display: "flex",
        flexDirection: "column",
        boxShadow: "0 0 20px rgba(0,0,0,0.1)",
        position: "relative",
      }}
    >
      {/* Mobile Top Header */}
      <MobileHeader
        deliveryLocation="Sector 62, Noida"
        cartItemCount={totalItemCount}
        onCartPress={() => setIsCartModalOpen(true)}
        isTrackingActive={Boolean(activeOrderId)}
        onTrackingPress={() => setIsTrackingSheetVisible(true)}
      />

      {/* Mobile Search Bar */}
      <div style={{ padding: "12px 16px 4px" }}>
        <input
          type="text"
          placeholder="🔍 Search 'Milk', 'Atta', 'Bread'..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{
            width: "100%",
            padding: "10px 14px",
            borderRadius: "12px",
            border: "1px solid #d1d5db",
            fontSize: "14px",
            backgroundColor: "#ffffff",
            outline: "none",
          }}
        />
      </div>

      {/* Horizontal Category Carousel */}
      <CategoryCarousel
        selectedCategory={selectedCategory}
        onSelectCategory={setSelectedCategory}
      />

      {/* Pull To Refresh Trigger Bar */}
      <div
        onClick={handlePullToRefresh}
        style={{
          textAlign: "center",
          fontSize: "11px",
          color: "#9ca3af",
          padding: "4px 0",
          cursor: "pointer",
        }}
      >
        {isRefreshing ? "🔄 Syncing fresh stock..." : "⇣ Tap to refresh inventory"}
      </div>

      {/* Products Virtualized List */}
      <div style={{ flex: 1, padding: "8px 16px 80px" }}>
        {filteredProducts.map((product) => {
          const cartItem = cartItems.find((x) => x.product.id === product.id);
          const quantity = cartItem ? cartItem.quantity : 0;
          return (
            <ProductListItem
              key={product.id}
              product={product}
              quantity={quantity}
              onAdd={handleAddToCart}
              onUpdateQuantity={handleUpdateQuantity}
            />
          );
        })}
      </div>

      {/* Bottom Floating Cart Bar (if cart has items) */}
      {totalItemCount > 0 && !isCartModalOpen && (
        <div
          onClick={() => setIsCartModalOpen(true)}
          style={{
            position: "fixed",
            bottom: "16px",
            left: "50%",
            transform: "translateX(-50%)",
            width: "calc(100% - 32px)",
            maxWidth: "398px",
            backgroundColor: "#059669",
            color: "#ffffff",
            borderRadius: "14px",
            padding: "12px 18px",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            boxShadow: "0 6px 16px rgba(5,150,105,0.4)",
            cursor: "pointer",
            zIndex: 90,
          }}
        >
          <div>
            <div style={{ fontSize: "12px", opacity: 0.9 }}>{totalItemCount} ITEMS</div>
            <div style={{ fontSize: "16px", fontWeight: 700 }}>₹{pricing.grandTotal}</div>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "6px", fontWeight: 700, fontSize: "14px" }}>
            <span>View Cart</span>
            <span>➔</span>
          </div>
        </div>
      )}

      {/* Mobile Cart Modal */}
      {isCartModalOpen && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0,0,0,0.5)",
            zIndex: 150,
            display: "flex",
            flexDirection: "column",
            justifyContent: "flex-end",
          }}
        >
          <div
            style={{
              backgroundColor: "#ffffff",
              borderTopLeftRadius: "20px",
              borderTopRightRadius: "20px",
              padding: "20px 20px 28px",
              maxHeight: "80vh",
              overflowY: "auto",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "16px" }}>
              <h3 style={{ fontSize: "18px", fontWeight: 700, margin: 0 }}>
                Shopping Cart ({totalItemCount})
              </h3>
              <button
                onClick={() => setIsCartModalOpen(false)}
                style={{ background: "none", border: "none", fontSize: "20px", cursor: "pointer", color: "#6b7280" }}
              >
                ✕
              </button>
            </div>

            {/* Pricing Details */}
            <div style={{ backgroundColor: "#f9fafb", borderRadius: "10px", padding: "12px", marginBottom: "16px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
                <span>Subtotal:</span>
                <span>₹{pricing.subtotal}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", color: "#059669", marginBottom: "4px" }}>
                <span>Coupon (KIRANA10):</span>
                <span>-₹{pricing.discount}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
                <span>5% GST:</span>
                <span>₹{pricing.tax}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "13px", marginBottom: "4px" }}>
                <span>Delivery:</span>
                <span>{pricing.deliveryFee === 0 ? "FREE" : `₹${pricing.deliveryFee}`}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "16px", fontWeight: 700, borderTop: "1px solid #e5e7eb", paddingTop: "8px", marginTop: "4px" }}>
                <span>Total Amount:</span>
                <span>₹{pricing.grandTotal}</span>
              </div>
            </div>

            <button
              onClick={handleCheckout}
              style={{
                width: "100%",
                padding: "14px",
                backgroundColor: "#059669",
                color: "#ffffff",
                border: "none",
                borderRadius: "12px",
                fontSize: "15px",
                fontWeight: 700,
                cursor: "pointer",
              }}
            >
              Pay ₹{pricing.grandTotal} & Place Order
            </button>
          </div>
        </div>
      )}

      {/* Live Rider GPS Bottom Sheet */}
      <RiderGpsTrackingSheet
        isVisible={isTrackingSheetVisible}
        onClose={() => setIsTrackingSheetVisible(false)}
        orderId={activeOrderId}
        status={orderStatus}
        gpsSnapshot={gpsSnapshot}
      />
    </div>
  );
};

