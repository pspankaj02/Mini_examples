// [KYA KARTA HAI]: Client components import karta hai.
// [KYU ZAROORI HAI]: Interactive 3D Canvas aur Server Action client widget ko React Server Component page me embed karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ./components/.
// [OUTPUT / SIDE EFFECT]: Component symbols available.
// [VARIATIONS TO TRY]: Dynamic imports with Suspense fallback.
import { ThreeDeliveryCrate } from "./components/ThreeDeliveryCrate";
import { ServerActionCart } from "./components/ServerActionCart";

// [KYA KARTA HAI]: Static catalog data define karta hai server rendering ke liye.
// [KYU ZAROORI HAI]: React Server Component me zero client JS payload ke sath initial SSR table render karne ke liye.
// [KAISE CHALTA HAI]: Array of product objects.
// [OUTPUT / SIDE EFFECT]: Server data.
// [VARIATIONS TO TRY]: Direct database query with Prisma or Drizzle inside RSC.
const FEATURED_GROCERIES = [
  { id: "nx-01", name: "Amul Gold Buffalo Milk 1L", price: 68, category: "Dairy", unit: "1 L", icon: "🥛" },
  { id: "nx-02", name: "Epigamia Greek Yogurt Natural 100g", price: 50, category: "Dairy", unit: "100 g", icon: "🥣" },
  { id: "nx-03", name: "Fortune Sunlite Refined Sunflower Oil 1L", price: 135, category: "Produce", unit: "1 L", icon: "🌻" },
  { id: "nx-04", name: "Organic Farm Potatoes 1kg", price: 32, category: "Produce", unit: "1 kg", icon: "🥔" },
  { id: "nx-05", name: "English Oven Multigrain Bread 400g", price: 55, category: "Bakery", unit: "400 g", icon: "🍞" },
  { id: "nx-06", name: "Doritos Cheese Nachos 60g", price: 30, category: "Snacks", unit: "60 g", icon: "🧀" },
];

// [KYA KARTA HAI]: Next.js App Router Page component (React Server Component).
// [KYU ZAROORI HAI]: Lightning-fast zero-hydration initial SSR page serve karne ke liye.
// [KAISE CHALTA HAI]: Server par HTML render karta hai aur interactive parts ko Client Component boundaries me isolate karta hai.
// [OUTPUT / SIDE EFFECT]: Server Rendered HTML page stream.
// [VARIATIONS TO TRY]: Streaming with <Suspense>.
export default function HomePage() {
  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "24px 20px 48px" }}>
      {/* Top Banner */}
      <header
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          paddingBottom: "20px",
          borderBottom: "1px solid #e5e7eb",
          marginBottom: "28px",
          flexWrap: "wrap",
          gap: "12px",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <div
            style={{
              backgroundColor: "#059669",
              color: "#ffffff",
              padding: "8px 16px",
              borderRadius: "10px",
              fontWeight: 800,
              fontSize: "20px",
            }}
          >
            🛒 KiranaHub 3D
          </div>
          <div>
            <div style={{ fontWeight: 700, fontSize: "16px", color: "#111827" }}>
              Next.js 15 Full-Stack & Three.js Storefront
            </div>
            <div style={{ fontSize: "12px", color: "#6b7280" }}>
              React Server Components (RSC) • Server Actions • WebGL 3D Graphics
            </div>
          </div>
        </div>

        <div style={{ display: "flex", gap: "8px" }}>
          <span
            style={{
              backgroundColor: "#ecfdf5",
              color: "#065f46",
              padding: "4px 10px",
              borderRadius: "12px",
              fontSize: "12px",
              fontWeight: 600,
              border: "1px solid #a7f3d0",
            }}
          >
            ⚡ Next.js App Router
          </span>
          <span
            style={{
              backgroundColor: "#eff6ff",
              color: "#1e40af",
              padding: "4px 10px",
              borderRadius: "12px",
              fontSize: "12px",
              fontWeight: 600,
              border: "1px solid #bfdbfe",
            }}
          >
            🎮 Three.js WebGL
          </span>
        </div>
      </header>

      {/* Main Dual Grid: 3D Visualizer & Server Action Cart */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(340px, 1fr))",
          gap: "24px",
          marginBottom: "36px",
        }}
      >
        {/* Left Column: Interactive 3D Delivery Crate */}
        <div>
          <ThreeDeliveryCrate />
        </div>

        {/* Right Column: Server Action Checkout Widget */}
        <div>
          <ServerActionCart />
        </div>
      </div>

      {/* Bottom Section: Server-Rendered Catalog (Zero Client JS) */}
      <section>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "16px" }}>
          <div>
            <h2 style={{ fontSize: "18px", fontWeight: 700, color: "#111827" }}>
              RSC Lightning Fast Grocery Catalog
            </h2>
            <p style={{ fontSize: "13px", color: "#6b7280" }}>
              Server Rendered with 0 KB client bundle size overhead.
            </p>
          </div>
          <span style={{ fontSize: "12px", color: "#059669", fontWeight: 600 }}>
            ⚡ SSR Instant Paint
          </span>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))",
            gap: "16px",
          }}
        >
          {FEATURED_GROCERIES.map((item) => (
            <div
              key={item.id}
              style={{
                backgroundColor: "#ffffff",
                border: "1px solid #e5e7eb",
                borderRadius: "12px",
                padding: "16px",
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                textAlign: "center",
              }}
            >
              <div style={{ fontSize: "36px", marginBottom: "8px" }}>{item.icon}</div>
              <div style={{ fontSize: "14px", fontWeight: 600, color: "#111827", marginBottom: "4px" }}>
                {item.name}
              </div>
              <div style={{ fontSize: "12px", color: "#6b7280", marginBottom: "8px" }}>
                {item.unit}
              </div>
              <div style={{ fontSize: "15px", fontWeight: 700, color: "#059669" }}>
                ₹{item.price}
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Footer */}
      <footer
        style={{
          marginTop: "48px",
          paddingTop: "20px",
          borderTop: "1px solid #e5e7eb",
          textAlign: "center",
          fontSize: "13px",
          color: "#6b7280",
        }}
      >
        KiranaHub Level 5 • Next.js App Router + Three.js 3D Delivery Crate Showcase
      </footer>
    </div>
  );
}

