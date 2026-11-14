// [KYA KARTA HAI]: Wasm VM types namespace import karta hai
// [KYU ZAROORI HAI]: WasmInstruction aur WasmOpcode use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: VM types available
// [VARIATIONS TO TRY]: System.Collections.Generic
using KiranaHub.WasmSandbox.Vm;

// [KYA KARTA HAI]: Merchant plugins namespace define karta hai
// [KYU ZAROORI HAI]: Business rule bytecode generators ko organize karne ke liye
// [KAISE CHALTA HAI]: Project plugins directory map karta hai
// [OUTPUT / SIDE EFFECT]: MerchantPromotionPlugin available in KiranaHub.WasmSandbox.Plugins
// [VARIATIONS TO TRY]: KiranaHub.Plugins.Promotions
namespace KiranaHub.WasmSandbox.Plugins;

// [KYA KARTA HAI]: Factory class jo merchant business rules ke liye Wasm bytecode scripts assemble karta hai
// [KYU ZAROORI HAI]: Realistic e-commerce scenarios (valid discount, runaway loop, memory breach) simulate karne ke liye
// [KAISE CHALTA HAI]: WasmInstruction lists construct karta hai jo standard WebAssembly bytecode represent karti hain
// [OUTPUT / SIDE EFFECT]: Pre-compiled bytecode scripts ready for WasmInterpreter
// [VARIATIONS TO TRY]: Wasm binary parser from .wasm file bytes
public static class MerchantPromotionPlugin
{
    // [KYA KARTA HAI]: Valid merchant festival discount promotion bytecode construct karta hai
    // [KYU ZAROORI HAI]: Real-world dynamic promo evaluation simulate karne ke liye
    // [KAISE CHALTA HAI]: Checks if subtotal >= Rs 500 (50,000 paise), calculates 15% discount, calls host tier check, stores in memory and returns net total
    // [OUTPUT / SIDE EFFECT]: Instruction list representing valid discount logic
    // [VARIATIONS TO TRY]: Dynamic VAT/GST tax calculation bytecode
    public static List<WasmInstruction> BuildFestivalDiscountBytecode()
    {
        // Local 0: subtotalPaise (input argument, e.g. 60,000 paise = Rs 600)
        // Local 1: computedDiscount
        // Local 2: customerTier
        // Memory 0x00: stores computed discount
        return new List<WasmInstruction>
        {
            // [0] Check if subtotal >= 50,000 paise (Rs 500)
            new(WasmOpcode.LocalGet, 0),        // push subtotal
            new(WasmOpcode.I32Const, 50000),    // push 50000 threshold
            new(WasmOpcode.I32GeS),             // (subtotal >= 50000) -> 1 or 0
            new(WasmOpcode.BrIf, 6),            // if true, jump to instruction 6 (qualifies for discount)

            // [4] No discount branch: discount = 0
            new(WasmOpcode.I32Const, 0),
            new(WasmOpcode.Br, 17),             // jump to finalize calculation

            // [6] Qualifies for 15% discount: discount = (subtotal * 15) / 100
            new(WasmOpcode.LocalGet, 0),        // push subtotal
            new(WasmOpcode.I32Const, 15),       // push 15
            new(WasmOpcode.I32Mul),             // subtotal * 15
            new(WasmOpcode.I32Const, 100),      // push 100
            new(WasmOpcode.I32DivS),            // (subtotal * 15) / 100
            new(WasmOpcode.LocalSet, 1),        // store discount in Local 1

            // [12] Call host function 1 (host_get_customer_tier) passing customer ID 88201
            new(WasmOpcode.I32Const, 88201),
            new(WasmOpcode.Call, 1),            // host call -> returns 1 (VIP) or 0 (Regular)
            new(WasmOpcode.I32Const, 1),
            new(WasmOpcode.I32Eq),              // if VIP tier == 1
            new(WasmOpcode.BrIf, 19),           // jump to add VIP bonus discount

            // [17] Finalize regular discount: push Local 1
            new(WasmOpcode.LocalGet, 1),
            new(WasmOpcode.Br, 24),             // jump to memory store and return

            // [19] VIP bonus branch: discount = discount + 5000 paise (Rs 50 extra off)
            new(WasmOpcode.LocalGet, 1),
            new(WasmOpcode.I32Const, 5000),     // Rs 50 bonus
            new(WasmOpcode.I32Add),
            new(WasmOpcode.LocalSet, 1),        // update Local 1
            new(WasmOpcode.LocalGet, 1),

            // [24] Store discount in sandboxed linear memory address 0x00
            new(WasmOpcode.I32Const, 0),        // push memory address 0
            new(WasmOpcode.LocalGet, 1),        // push discount value
            new(WasmOpcode.I32Store),           // memory[0] = discount

            // [27] Calculate and return net payable: (subtotal - discount)
            new(WasmOpcode.LocalGet, 0),        // push subtotal
            new(WasmOpcode.LocalGet, 1),        // push discount
            new(WasmOpcode.I32Sub),             // subtotal - discount
            new(WasmOpcode.Return)              // return net payable amount
        };
    }

    // [KYA KARTA HAI]: Malicious infinite loop vendor script bytecode construct karta hai
    // [KYU ZAROORI HAI]: Prove karne ke liye ki gas metering infinite loops ko terminate karti hai
    // [KAISE CHALTA HAI]: Instruction loop me Nop aur unconditional branch jump karta hai apne aap par (Br 0)
    // [OUTPUT / SIDE EFFECT]: Will exhaust gas limit and trigger GasExhaustedException
    // [VARIATIONS TO TRY]: Indirect recursion call loop
    public static List<WasmInstruction> BuildMaliciousInfiniteLoopBytecode()
    {
        return new List<WasmInstruction>
        {
            // [0] Nop instruction (consumes 1 gas unit)
            new(WasmOpcode.Nop),
            // [1] Unconditional branch jumping back to instruction 0 infinitely (consumes 2 gas units)
            new(WasmOpcode.Br, 0),
            // [2] Unreachable return
            new(WasmOpcode.Return)
        };
    }

    // [KYA KARTA HAI]: Malicious memory breach vendor script construct karta hai
    // [KYU ZAROORI HAI]: Memory sandbox isolation bounds verify karne ke liye
    // [KAISE CHALTA HAI]: 64KB sandbox limit (0x10000) ke bahar address 0x00FF0000 par write karne ki koshish karta hai
    // [OUTPUT / SIDE EFFECT]: Will trigger WasmMemoryOutOfBoundsException safely
    // [VARIATIONS TO TRY]: Negative address access
    public static List<WasmInstruction> BuildMaliciousMemoryBreachBytecode()
    {
        return new List<WasmInstruction>
        {
            // [0] Push unauthorized high memory address 0x00FF0000 (16 MB offset)
            new(WasmOpcode.I32Const, 0x00FF0000),
            // [1] Push malicious payload integer 0xDEADBEEF
            new(WasmOpcode.I32Const, unchecked((int)0xDEADBEEF)),
            // [2] Attempt I32Store outside linear memory
            new(WasmOpcode.I32Store),
            // [3] Return
            new(WasmOpcode.Return)
        };
    }
}
