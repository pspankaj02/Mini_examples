// [KYA KARTA HAI]: Next.js Metadata type import karta hai.
// [KYU ZAROORI HAI]: Static SEO tags, OpenGraph aur favicon tags define karne ke liye.
// [KAISE CHALTA HAI]: Next.js App Router metadata convention se head tags generate hote hain.
// [OUTPUT / SIDE EFFECT]: Head metadata symbols available.
// [VARIATIONS TO TRY]: Dynamic generateMetadata() for product pages.
import type { Metadata } from "next";

// [KYA KARTA HAI]: Next.js Google font loader import karta hai.
// [KYU ZAROORI HAI]: Zero layout shift typography provide karne ke liye.
// [KAISE CHALTA HAI]: Inter font family load karta hai.
// [OUTPUT / SIDE EFFECT]: Font configuration object.
// [VARIATIONS TO TRY]: Custom local fonts via next/font/local.
import { Inter } from "next/font/google";

// [KYA KARTA HAI]: Inter font instantiate karta hai with latin subset.
// [KYU ZAROORI HAI]: Font optimization ke liye.
// [KAISE CHALTA HAI]: Inter({ subsets: ['latin'] }) call hota hai.
// [OUTPUT / SIDE EFFECT]: inter class name and CSS variables.
// [VARIATIONS TO TRY]: Variable font weight configuration.
const inter = Inter({ subsets: ["latin"] });

// [KYA KARTA HAI]: Application metadata export karta hai.
// [KYU ZAROORI HAI]: SEO, search engine crawlers, aur browser tab title ke liye.
// [KAISE CHALTA HAI]: Static object export.
// [OUTPUT / SIDE EFFECT]: Rendered <title> and <meta> tags.
// [VARIATIONS TO TRY]: openGraph card images and description.
export const metadata: Metadata = {
  title: "KiranaHub - Next.js Full-Stack & Three.js 3D Storefront",
  description: "Instant Grocery Delivery with React Server Components, Server Actions and Three.js 3D Visualizer",
};

// [KYA KARTA HAI]: Next.js Root Layout component render karta hai.
// [KYU ZAROORI HAI]: Sabhi App Router pages ke liye common HTML shell aur body container provide karne ke liye.
// [KAISE CHALTA HAI]: React Server Component jo children wrap karta hai.
// [OUTPUT / SIDE EFFECT]: Root HTML document structure.
// [VARIATIONS TO TRY]: Global ThemeProvider or Analytics scripts.
export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body
        className={inter.className}
        style={{
          margin: 0,
          padding: 0,
          backgroundColor: "#f9fafb",
          color: "#111827",
          minHeight: "100vh",
        }}
      >
        {children}
      </body>
    </html>
  );
}

