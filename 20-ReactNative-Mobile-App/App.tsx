// [KYA KARTA HAI]: React library import karta hai.
// [KYU ZAROORI HAI]: TSX component structure support karne ke liye.
// [KAISE CHALTA HAI]: React standard library import.
// [OUTPUT / SIDE EFFECT]: React symbols.
// [VARIATIONS TO TRY]: Expo AppLoading splash screen wrapper.
import React from "react";

// [KYA KARTA HAI]: StorefrontScreen mobile screen import karta hai.
// [KYU ZAROORI HAI]: Mobile app ka primary customer screen render karne ke liye.
// [KAISE CHALTA HAI]: ES module import from ./src/screens/StorefrontScreen.js.
// [OUTPUT / SIDE EFFECT]: StorefrontScreen component.
// [VARIATIONS TO TRY]: React Navigation NavigationContainer with Stack.Navigator.
import { StorefrontScreen } from "./src/screens/StorefrontScreen.js";

// [KYA KARTA HAI]: Mobile Application root entrypoint component render karta hai.
// [KYU ZAROORI HAI]: Expo mobile bundler ke liye main App entry function provide karne ke liye.
// [KAISE CHALTA HAI]: Functional component returning <StorefrontScreen />.
// [OUTPUT / SIDE EFFECT]: Mobile App root tree.
// [VARIATIONS TO TRY]: Redux / Context Provider wrapper.
export default function App(): React.ReactElement {
  return <StorefrontScreen />;
}

