// [KYA KARTA HAI]: Next.js server-side response helper import karta hai.
// [KYU ZAROORI HAI]: JSON responses aur HTTP status codes return karne ke liye.
// [KAISE CHALTA HAI]: 'next/server' se NextResponse load hota hai.
// [OUTPUT / SIDE EFFECT]: NextResponse symbol.
// [VARIATIONS TO TRY]: Streaming response via ReadableStream.
import { NextResponse } from "next/server";

// [KYA KARTA HAI]: Static catalog products definition.
// [KYU ZAROORI HAI]: API consumer ko fast, reliable grocery catalog return karne ke liye.
// [KAISE CHALTA HAI]: Array of product objects.
// [OUTPUT / SIDE EFFECT]: Static catalog data.
// [VARIATIONS TO TRY]: Fetch from external database or CMS.
const CATALOG_ITEMS = [
  { id: "nx-01", name: "Amul Gold Buffalo Milk 1L", price: 68, category: "Dairy", unit: "1 L", stock: 40 },
  { id: "nx-02", name: "Epigamia Greek Yogurt Natural 100g", price: 50, category: "Dairy", unit: "100 g", stock: 25 },
  { id: "nx-03", name: "Fortune Sunlite Refined Sunflower Oil 1L", price: 135, category: "Produce", unit: "1 L", stock: 30 },
  { id: "nx-04", name: "Organic Farm Potatoes 1kg", price: 32, category: "Produce", unit: "1 kg", stock: 50 },
  { id: "nx-05", name: "English Oven Multigrain Bread 400g", price: 55, category: "Bakery", unit: "400 g", stock: 15 },
  { id: "nx-06", name: "Doritos Cheese Nachos 60g", price: 30, category: "Snacks", unit: "60 g", stock: 60 },
  { id: "nx-07", name: "Paper Boat Aam Panna 250ml", price: 35, category: "Beverages", unit: "250 ml", stock: 35 },
];

// [KYA KARTA HAI]: Next.js GET Route Handler for /api/catalog endpoint.
// [KYU ZAROORI HAI]: Mobile apps aur external clients ko REST API data provide karne ke liye.
// [KAISE CHALTA HAI]: Async GET function jo NextResponse.json return karta hai.
// [OUTPUT / SIDE EFFECT]: HTTP 200 JSON Response.
// [VARIATIONS TO TRY]: Dynamic query parameters for pagination.
export async function GET() {
  return NextResponse.json(
    {
      success: true,
      data: CATALOG_ITEMS,
      totalCount: CATALOG_ITEMS.length,
      timestamp: new Date().toISOString(),
    },
    {
      status: 200,
      headers: {
        "Cache-Control": "public, s-maxage=60, stale-while-revalidate=120",
        "X-Powered-By": "KiranaHub Next.js App Router",
      },
    }
  );
}

