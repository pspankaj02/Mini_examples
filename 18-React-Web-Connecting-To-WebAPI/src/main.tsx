// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: React 19 core features aur JSX support ke liye.
// [KAISE CHALTA HAI]: ES module import.
// [OUTPUT / SIDE EFFECT]: React runtime.
// [VARIATIONS TO TRY]: StrictMode toggle.
import React from "react";

// [KYA KARTA HAI]: ReactDOM client createRoot method import karta hai.
// [KYU ZAROORI HAI]: Modern Concurrent React root rendering create karne ke liye.
// [KAISE CHALTA HAI]: 'react-dom/client' se load hota hai.
// [OUTPUT / SIDE EFFECT]: createRoot function available.
// [VARIATIONS TO TRY]: hydrateRoot for server-side rendered apps.
import ReactDOM from "react-dom/client";

// [KYA KARTA HAI]: Root App component import karta hai.
// [KYU ZAROORI HAI]: Application tree ka main entrypoint render karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ./App.js.
// [OUTPUT / SIDE EFFECT]: App component symbol.
// [VARIATIONS TO TRY]: Router provider wrapper.
import { App } from "./App.js";

// [KYA KARTA HAI]: DOM me 'root' element search karta hai.
// [KYU ZAROORI HAI]: React virtual DOM ko actual browser HTML DOM container se connect karne ke liye.
// [KAISE CHALTA HAI]: document.getElementById("root") call karta hai.
// [OUTPUT / SIDE EFFECT]: HTMLElement reference.
// [VARIATIONS TO TRY]: Dynamic root creation if missing.
const rootElement = document.getElementById("root");

// [KYA KARTA HAI]: Guard check karta hai ki root element DOM me present hai ya nahi.
// [KYU ZAROORI HAI]: Missing DOM node par fatal runtime crash ko informative error message ke sath catch karne ke liye.
// [KAISE CHALTA HAI]: If statement condition check.
// [OUTPUT / SIDE EFFECT]: Continues or throws Error.
// [VARIATIONS TO TRY]: document.body.appendChild fallback.
if (!rootElement) {
  throw new Error("Fatal: Could not find #root element in index.html to mount React.");
}

// [KYA KARTA HAI]: React concurrent root create karke <App /> ko render karta hai under React.StrictMode.
// [KYU ZAROORI HAI]: Development warnings, side-effects checks, aur modern async rendering enable karne ke liye.
// [KAISE CHALTA HAI]: ReactDOM.createRoot(rootElement).render(...) call hota hai.
// [OUTPUT / SIDE EFFECT]: React application mounted on browser DOM.
// [VARIATIONS TO TRY]: ErrorBoundary wrapper around App.
ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

