// [KYA KARTA HAI]: React core hooks import karta hai.
// [KYU ZAROORI HAI]: State, lifecycle aur memoized filtering manage karne ke liye.
// [KAISE CHALTA HAI]: React standard hooks package se load hota hai.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: React Server Components (Next.js) me convert karna.
import React, { useState, useEffect, useMemo } from "react";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: Product aur GroceryCategory types ke static verification ke liye.
// [KAISE CHALTA HAI]: ES module import from ./types/store.js.
// [OUTPUT / SIDE EFFECT]: Store types.
// [VARIATIONS TO TRY]: Re-export from types index.
import type { Product, GroceryCategory } from "./types/store.js";

// [KYA KARTA HAI]: Shared HTTP API client import karta hai.
// [KYU ZAROORI HAI]: Product catalog fetch aur checkout order place karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ./services/apiClient.js.
// [OUTPUT / SIDE EFFECT]: apiClient instance.
// [VARIATIONS TO TRY]: Mock API client for testing.
import { apiClient } from "./services/apiClient.js";

// [KYA KARTA HAI]: Custom hooks import karta hai.
// [KYU ZAROORI HAI]: Cart management, search debouncing aur SignalR order tracking encapsulate karne ke liye.
// [KAISE CHALTA HAI]: ES module imports from ./hooks/.
// [OUTPUT / SIDE EFFECT]: Custom hooks available.
// [VARIATIONS TO TRY]: Global State Provider.
import { useCart } from "./hooks/useCart.js";
import { useDebounce } from "./hooks/useDebounce.js";
import { useSignalROrderTracking } from "./hooks/useSignalROrderTracking.js";

// [KYA KARTA HAI]: Presentation UI components import karta hai.
// [KYU ZAROORI HAI]: Modular, component-driven layout build karne ke liye.
// [KAISE CHALTA HAI]: ES module imports from ./components/.
// [OUTPUT / SIDE EFFECT]: React components.
// [VARIATIONS TO TRY]: Dynamic code splitting via React.lazy().
import { Header } from "./components/Header.js";
import { CategoryChips } from "./components/CategoryChips.js";
import { ProductCard } from "./components/ProductCard.js";
import { CartDrawer } from "./components/CartDrawer.js";
import { LiveOrderTracking } from "./components/LiveOrderTracking.js";

// [KYA KARTA HAI]: KiranaHub Customer Web Storefront ka root component render karta hai.
// [KYU ZAROORI HAI]: Complete user journey (Browse -> Search -> Add to Cart -> Checkout -> Live SignalR Tracking) coordinate karne ke liye.
// [KAISE CHALTA HAI]: Hooks aur sub-components ko bind karke single-page application experience provide karta hai.
// [OUTPUT / SIDE EFFECT]: Rendered KiranaHub Storefront application.
// [VARIATIONS TO TRY]: React Router for multi-page navigation.
export const App: React.FC = () => {
  // [KYA KARTA HAI]: Catalog products array state.
  // [KYU ZAROORI HAI]: API se fetch kiye gaye grocery items hold karne ke liye.
  // [KAISE CHALTA HAI]: useState<Product[]>([]) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: products state.
  // [VARIATIONS TO TRY]: SWR / TanStack React Query for auto-cache revalidation.
  const [products, setProducts] = useState<readonly Product[]>([]);

  // [KYA KARTA HAI]: Catalog loading spinner boolean state.
  // [KYU ZAROORI HAI]: Data fetch ke waqt skeleton/loading indicator dikhane ke liye.
  // [KAISE CHALTA HAI]: useState<boolean>(true) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: isLoading state.
  // [VARIATIONS TO TRY]: Suspense boundary with React 19 use() hook.
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // [KYA KARTA HAI]: Selected category filter tab state.
  // [KYU ZAROORI HAI]: Department-wise catalog filtration ke liye.
  // [KAISE CHALTA HAI]: useState<GroceryCategory>("All") initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: selectedCategory state.
  // [VARIATIONS TO TRY]: URL search query sync (?cat=Dairy).
  const [selectedCategory, setSelectedCategory] = useState<GroceryCategory>("All");

  // [KYA KARTA HAI]: Live search input text state.
  // [KYU ZAROORI HAI]: User ke live search keywords capture karne ke liye.
  // [KAISE CHALTA HAI]: useState<string>("") initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: searchTerm state.
  // [VARIATIONS TO TRY]: Voice search speech-to-text.
  const [searchTerm, setSearchTerm] = useState<string>("");

  // [KYA KARTA HAI]: Debounced search keyword calculate karta hai.
  // [KYU ZAROORI HAI]: 250ms rukne ke baad filter evaluate karta hai taaki typing smooth rahe.
  // [KAISE CHALTA HAI]: useDebounce(searchTerm, 250) invoke hota hai.
  // [OUTPUT / SIDE EFFECT]: debouncedSearch string.
  // [VARIATIONS TO TRY]: Configurable delay threshold.
  const debouncedSearch = useDebounce(searchTerm, 250);

  // [KYA KARTA HAI]: Cart drawer open/close boolean visibility state.
  // [KYU ZAROORI HAI]: Slide-out drawer modal show/hide karne ke liye.
  // [KAISE CHALTA HAI]: useState<boolean>(false) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: isCartOpen state.
  // [VARIATIONS TO TRY]: URL hash #cart sync.
  const [isCartOpen, setIsCartOpen] = useState<boolean>(false);

  // [KYA KARTA HAI]: Tracking modal open/close boolean visibility state.
  // [KYU ZAROORI HAI]: Live order status dialog show/hide karne ke liye.
  // [KAISE CHALTA HAI]: useState<boolean>(false) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: isTrackingOpen state.
  // [VARIATIONS TO TRY]: Floating minimized PIP widget.
  const [isTrackingOpen, setIsTrackingOpen] = useState<boolean>(false);

  // [KYA KARTA HAI]: Checkout network call in-progress state.
  // [KYU ZAROORI HAI]: Double-click order placement prevent karne ke liye.
  // [KAISE CHALTA HAI]: useState<boolean>(false) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: isCheckingOut state.
  // [VARIATIONS TO TRY]: AbortController for canceling stuck checkout.
  const [isCheckingOut, setIsCheckingOut] = useState<boolean>(false);

  // [KYA KARTA HAI]: useCart hook se reactive cart state aur actions extract karta hai.
  // [KYU ZAROORI HAI]: Cart mutations perform karne ke liye.
  // [KAISE CHALTA HAI]: useCart() invocation.
  // [OUTPUT / SIDE EFFECT]: Cart state object.
  // [VARIATIONS TO TRY]: Redux dispatch.
  const {
    items: cartItems,
    pricing,
    totalItemCount,
    addToCart,
    updateQuantity,
    removeFromCart,
    clearCart,
    applyCoupon,
    removeCoupon,
  } = useCart();

  // [KYA KARTA HAI]: useSignalROrderTracking hook se WebSocket streaming state extract karta hai.
  // [KYU ZAROORI HAI]: Real-time status updates receive karne ke liye.
  // [KAISE CHALTA HAI]: useSignalROrderTracking() invocation.
  // [OUTPUT / SIDE EFFECT]: Tracking state object.
  // [VARIATIONS TO TRY]: Push notification web worker.
  const {
    activeOrderId,
    orderStatus,
    riderLocation,
    isConnected: isSignalRConnected,
    startTracking,
    stopTracking,
  } = useSignalROrderTracking();

  // [KYA KARTA HAI]: Initial product catalog fetch effect.
  // [KYU ZAROORI HAI]: App mount hone par catalog load karne ke liye.
  // [KAISE CHALTA HAI]: apiClient.getProducts() call karta hai.
  // [OUTPUT / SIDE EFFECT]: products state populated.
  // [VARIATIONS TO TRY]: Infinite scroll pagination.
  useEffect(() => {
    let isMounted = true;
    apiClient
      .getProducts()
      .then((data) => {
        if (isMounted) {
          setProducts(data);
          setIsLoading(false);
        }
      })
      .catch(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  // [KYA KARTA HAI]: Category filter aur search filter ke mutabiq catalog products filter karta hai.
  // [KYU ZAROORI HAI]: Fast instant client-side filtering without server turnaround time.
  // [KAISE CHALTA HAI]: useMemo se category matching aur name case-insensitive includes check karta hai.
  // [OUTPUT / SIDE EFFECT]: Memoized filtered products array.
  // [VARIATIONS TO TRY]: Fuse.js fuzzy search matching.
  const filteredProducts = useMemo(() => {
    return products.filter((product) => {
      const matchesCategory =
        selectedCategory === "All" || product.category === selectedCategory;
      const matchesSearch =
        debouncedSearch.trim() === "" ||
        product.name.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
        product.category.toLowerCase().includes(debouncedSearch.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }, [products, selectedCategory, debouncedSearch]);

  // [KYA KARTA HAI]: Product ID ke base par cart me current quantity lookup karta hai.
  // [KYU ZAROORI HAI]: Har card ko apna correct stepper count supply karne ke liye.
  // [KAISE CHALTA HAI]: cartItems.find() check karta hai.
  // [OUTPUT / SIDE EFFECT]: Integer quantity (0 if not in cart).
  // [VARIATIONS TO TRY]: Pre-computed Map<string, number> lookup for O(1) efficiency.
  const getCartQuantity = (productId: string): number => {
    const found = cartItems.find((x) => x.product.id === productId);
    return found ? found.quantity : 0;
  };

  // [KYA KARTA HAI]: Checkout order placement handler execute karta hai.
  // [KYU ZAROORI HAI]: API par order submit karke cart clear karna aur live tracking start karna.
  // [KAISE CHALTA HAI]: apiClient.placeOrder call karta hai, cart drawer close karta hai, aur startTracking() trigger karta hai.
  // [OUTPUT / SIDE EFFECT]: Order placed, tracking launched.
  // [VARIATIONS TO TRY]: Payment gateway (Razorpay / Stripe) SDK checkout invocation.
  const handleCheckout = async (deliveryAddress: string) => {
    setIsCheckingOut(true);
    try {
      const result = await apiClient.placeOrder(
        cartItems,
        deliveryAddress,
        pricing.couponCode
      );
      clearCart();
      setIsCartOpen(false);
      startTracking(result.orderId);
      setIsTrackingOpen(true);
    } catch (err) {
      alert(`Checkout failed: ${err instanceof Error ? err.message : "Unknown error"}`);
    } finally {
      setIsCheckingOut(false);
    }
  };

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
      {/* Top Navigation Bar */}
      <Header
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        cartItemCount={totalItemCount}
        onOpenCart={() => setIsCartOpen(true)}
        isTrackingActive={Boolean(activeOrderId)}
        onOpenTracking={() => setIsTrackingOpen(true)}
      />

      {/* Main Container */}
      <main style={{ maxWidth: "1200px", margin: "0 auto", padding: "0 24px 48px", width: "100%", flex: 1 }}>
        {/* Category Horizontal Filter Bar */}
        <CategoryChips
          selectedCategory={selectedCategory}
          onSelectCategory={setSelectedCategory}
        />

        {/* Catalog Header & Count */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            margin: "12px 0 20px",
          }}
        >
          <h1 style={{ fontSize: "20px", fontWeight: 700, color: "#111827" }}>
            {selectedCategory === "All" ? "Fresh Groceries" : selectedCategory}
          </h1>
          <span style={{ fontSize: "14px", color: "#6b7280" }}>
            Showing {filteredProducts.length} items
          </span>
        </div>

        {/* Loading State or Products Grid */}
        {isLoading ? (
          <div style={{ textAlign: "center", padding: "80px 20px", color: "#6b7280" }}>
            <div style={{ fontSize: "32px", marginBottom: "12px" }}>⏳</div>
            <p>Loading fresh groceries from warehouse...</p>
          </div>
        ) : filteredProducts.length === 0 ? (
          <div
            style={{
              textAlign: "center",
              padding: "60px 20px",
              backgroundColor: "#ffffff",
              borderRadius: "12px",
              border: "1px dashed #d1d5db",
            }}
          >
            <div style={{ fontSize: "40px", marginBottom: "12px" }}>🔍</div>
            <h3 style={{ fontSize: "16px", fontWeight: 600, color: "#374151", marginBottom: "4px" }}>
              No groceries found matching "{debouncedSearch}"
            </h3>
            <p style={{ fontSize: "14px", color: "#6b7280" }}>
              Try searching for "Milk", "Atta", "Bread" or reset your category filter.
            </p>
          </div>
        ) : (
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
              gap: "20px",
            }}
          >
            {filteredProducts.map((product) => (
              <ProductCard
                key={product.id}
                product={product}
                cartQuantity={getCartQuantity(product.id)}
                onAddToCart={addToCart}
                onUpdateQuantity={updateQuantity}
              />
            ))}
          </div>
        )}
      </main>

      {/* Slide-out Shopping Cart Drawer */}
      <CartDrawer
        isOpen={isCartOpen}
        onClose={() => setIsCartOpen(false)}
        items={cartItems}
        pricing={pricing}
        onUpdateQuantity={updateQuantity}
        onRemoveFromCart={removeFromCart}
        onApplyCoupon={applyCoupon}
        onRemoveCoupon={removeCoupon}
        onCheckout={handleCheckout}
        isCheckingOut={isCheckingOut}
      />

      {/* Real-Time SignalR Order Tracking Dialog */}
      <LiveOrderTracking
        isOpen={isTrackingOpen}
        onClose={() => {
          stopTracking();
          setIsTrackingOpen(false);
        }}
        orderId={activeOrderId}
        status={orderStatus}
        riderLocation={riderLocation}
        isConnected={isSignalRConnected}
      />

      {/* Footer */}
      <footer
        style={{
          backgroundColor: "#ffffff",
          borderTop: "1px solid #e5e7eb",
          padding: "20px 24px",
          textAlign: "center",
          fontSize: "13px",
          color: "#6b7280",
        }}
      >
        KiranaHub Quick-Commerce • Level 5 React 19 + TypeScript + SignalR Web Storefront
      </footer>
    </div>
  );
};
