// [KYA KARTA HAI]: Vite configuration helper import karta hai.
// [KYU ZAROORI HAI]: Type-safe Vite bundler configuration define karne ke liye.
// [KAISE CHALTA HAI]: defineConfig function typed options accept karta hai.
// [OUTPUT / SIDE EFFECT]: Vite configuration object export karta hai.
// [VARIATIONS TO TRY]: Custom plugins ya proxy settings add kar sakte hain.
import { defineConfig } from "vite";

// [KYA KARTA HAI]: Official React plugin for Vite import karta hai.
// [KYU ZAROORI HAI]: Fast Refresh (HMR) aur JSX/TSX compilation enable karta hai.
// [KAISE CHALTA HAI]: Babel/esbuild transform pipeline inject karta hai.
// [OUTPUT / SIDE EFFECT]: React fast refresh plugin instance.
// [VARIATIONS TO TRY]: SWC plugin (@vitejs/plugin-react-swc) use kar sakte hain.
import react from "@vitejs/plugin-react";

// [KYA KARTA HAI]: Vite build aur dev server options export karta hai.
// [KYU ZAROORI HAI]: Development server port aur build optimization control karne ke liye.
// [KAISE CHALTA HAI]: Plugins array me react() pass karta hai aur dev server port 5173 configure karta hai.
// [OUTPUT / SIDE EFFECT]: Exported config for Vite CLI.
// [VARIATIONS TO TRY]: ASP.NET Core backend proxy rule configure kar sakte hain.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
  },
});

