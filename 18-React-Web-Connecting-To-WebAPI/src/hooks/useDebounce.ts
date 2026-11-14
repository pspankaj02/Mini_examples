// [KYA KARTA HAI]: React useState aur useEffect hooks import karta hai.
// [KYU ZAROORI HAI]: Debounced value store aur timer lifecycle manage karne ke liye.
// [KAISE CHALTA HAI]: Standard React core library se load hote hain.
// [OUTPUT / SIDE EFFECT]: Hook functions available.
// [VARIATIONS TO TRY]: useDeferredValue hook in React 19.
import { useState, useEffect } from "react";

// [KYA KARTA HAI]: Generic custom hook jo rapidly changing value (e.g. search input) ko delay ke sath update karta hai.
// [KYU ZAROORI HAI]: Har keystroke par API backend ya filtering loop ko spam karne se rokta hai.
// [KAISE CHALTA HAI]: setTimeout timer set karta hai. Agar user delay se pehle dobara type kare toh cleanup function previous timer ko cancel kar deta hai.
// [OUTPUT / SIDE EFFECT]: Debounced generic value T.
// [VARIATIONS TO TRY]: Leading edge vs trailing edge execution flags.
export function useDebounce<T>(value: T, delayMs: number = 300): T {
  // [KYA KARTA HAI]: Debounced value ka state maintain karta hai initialized with original value.
  // [KYU ZAROORI HAI]: Delayed value ko React lifecycle me preserve karne ke liye.
  // [KAISE CHALTA HAI]: useState<T>(value) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: debouncedValue state.
  // [VARIATIONS TO TRY]: Initial value factory function.
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  // [KYA KARTA HAI]: Timer schedule karta hai aur dependency change hone par cleanup karta hai.
  // [KYU ZAROORI HAI]: Unnecessary renders aur memory leaks prevent karne ke liye.
  // [KAISE CHALTA HAI]: setTimeout start hota hai aur cleanup me clearTimeout execute hota hai.
  // [OUTPUT / SIDE EFFECT]: Timer execution after delayMs.
  // [VARIATIONS TO TRY]: Max wait timeout threshold.
  useEffect(() => {
    // [KYA KARTA HAI]: Browser setTimeout start karta hai.
    // [KYU ZAROORI HAI]: Value update ko delayMs milliseconds tak postpone karne ke liye.
    // [KAISE CHALTA HAI]: Callback delay ke baad setDebouncedValue call karega.
    // [OUTPUT / SIDE EFFECT]: Timer ID handle.
    // [VARIATIONS TO TRY]: RequestAnimationFrame for smooth 60fps animations.
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delayMs);

    // [KYA KARTA HAI]: Effect cleanup function return karta hai.
    // [KYU ZAROORI HAI]: Agar user 300ms ke andar doosra character type kar de toh pehle wale timer ko clear karne ke liye.
    // [KAISE CHALTA HAI]: React unmount ya re-run par ye cleanup function execute karta hai.
    // [OUTPUT / SIDE EFFECT]: Previous timer cancelled.
    // [VARIATIONS TO TRY]: Flush pending value on immediate unmount.
    return () => {
      clearTimeout(timer);
    };
  }, [value, delayMs]);

  // [KYA KARTA HAI]: Current debounced value return karta hai.
  // [KYU ZAROORI HAI]: Consumer component is stable value par search filtering execute kar sake.
  // [KAISE CHALTA HAI]: Return statement.
  // [OUTPUT / SIDE EFFECT]: Settled value of type T.
  // [VARIATIONS TO TRY]: Tuple returning [debouncedValue, isPending].
  return debouncedValue;
}

