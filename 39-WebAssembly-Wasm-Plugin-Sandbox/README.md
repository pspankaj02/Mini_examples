# Project 39: WebAssembly (Wasm) Sandbox & Gas-Metered Execution Engine

## 1. Reading Roadmap ("Iss project ko scratch se kaise padhein")
Agar aap WebAssembly (Wasm), virtual machines, instruction metering (gas limits), ya sandboxed multi-tenant architectures पहली बार padh rahe hain, toh files ko iss sequence me padhein:
1. **`Vm/WasmOpcodes.cs`**: Dekhein ki standard Wasm 1.0 binary opcodes (`I32Const`, `I32Add`, `I32Store`, `LocalGet`, `Call`, `BrIf`) kaise represent hote hain aur har category ki gas cost kaise define ki gayi hai.
2. **`Vm/WasmLinearMemory.cs`**: Samjhein ki Wasm me memory kaise isolated 64KB pages me divide hoti hai aur har read/write par strict boundary check lagakar host RAM escape ko kaise physically block kiya jata hai.
3. **`Vm/WasmInterpreter.cs`**: Padhein ki stack-based VM loop har instruction execution se pehle gas meter se gas deduct kaise karti hai aur `GasExhaustedException` se runaway loops ko kaise abort karti hai.
4. **`Plugins/MerchantPromotionPlugin.cs`**: Dekhein ki e-commerce discount business logic (15% festival discount + host VIP check + memory store) Wasm bytecode me kaise encode hoti hai, aur kaise infinite loop aur memory breach attack scripts formulate kiye jaate hain.
5. **`Program.cs`**: Run karke teeno test cases (Valid discount, Gas limit DoS trap, Memory bounds trap) ko live verify karein.

---

## 2. Foundations ("Kya pehle bna, kaise bna")
Pehle multi-tenant platforms (jaise KiranaHub jahan 10,000+ local grocery stores aur FMCG brands custom discount rules upload karte hain) me third-party scripts run karne ke liye unsafe approaches use hoti thi:
- **Approach 1: Raw C# DLLs / Reflection (`Assembly.Load`)**:
  - Agar kisi vendor ke code me bug hua ya `while(true)` hua, toh poora Kestrel web worker freeze ho jata tha.
  - Sabse bada security disaster: vendor code `File.ReadAllText("/etc/shadow")` ya `new HttpClient()` se private customer wallet secrets leak kar sakta tha!
- **Approach 2: Embedded JavaScript (V8 / ClearScript) ya Lua**:
  - Heavy memory footprint (100 MB per V8 isolate).
  - V8 execution thread ko deterministically interrupt karna complex tha, aur memory limits hard-to-enforce the.

---

## 3. Evolution & Mechanics ("Phir kya aaya, kyu aaya, kaise aaya, kya karta hai")

### A. WebAssembly (Wasm) & In-Process Sandboxing
WebAssembly ek binary instruction format hai jo web browsers aur cloud sandboxes ke liye create kiya gaya:
1. **Isolated Linear Memory Paging**:
   - Guest Wasm module ko sirf ek continuous byte array milti hai (pages of 64KB = 65,536 bytes).
   - Guest ke paas raw memory pointers nahi hote; wo sirf zero-indexed offsets access kar sakta hai. Agar guest address `0x00FF0000` access karne ki koshish kare, toh `WasmMemoryOutOfBoundsException` throw hoti hai. Host ka RAM, stack, aur operating system resources 100% invisible aur inaccessible rehte hain!
2. **Deterministic Instruction Metering ("Gas Limit")**:
   - Ethereum smart contracts ki tarah, har Wasm opcode ka ek fixed gas cost hota hai (Arithmetic = 1 gas, Memory IO = 3 gas, Call/Branch = 2 gas).
   - Host tenant ko ek fixed budget deta hai (e.g., 50,000 gas units per execution).
   - Agar vendor ne galti se `while(true)` ya complex infinite recursion likh diya, toh gas limit reach hote hi VM `GasExhaustedException` throw karke execution abort kar deti hai bina host thread ko freeze kiye!
3. **Host-Guest ABI & Syscall Bridge**:
   - Guest script system APIs (disk, sockets, processes) call nahi kar sakti.
   - Host sirf explicit safe capabilities expose karta hai via function index imports (e.g., `host_get_customer_tier`, `host_log_discount`).

---

## 4. Counter-Factual Risk Analysis ("⚠️ Uske na hone se kya hoga")
1. **Agar Gas Metering na ho**:
   - Koi bhi vendor simple 3-line infinite loop script upload karke server ke CPU cores 100% saturate kar dega, poori KiranaHub grocery ordering freeze ho jayegi (Denial-of-Service outage).
2. **Agar Linear Memory Bounds Checking na ho**:
   - Buffer overflow exploit ke through malicious vendor host process ki memory read kar sakega (customer credit card tokens aur double-entry financial ledger credentials leak ho jayenge).
3. **Agar Host Functions Strictly Controlled na hon**:
   - Vendor script OS level ke file descriptors ya network sockets ko manipulate kar sakti hai, malware download kar sakti hai ya server ko crypto-mining me engage kar sakti hai.

---

## 5. How to Run & Verify
Run the standalone benchmark using .NET 10:
```powershell
dotnet run --project 39-WebAssembly-Wasm-Plugin-Sandbox/KiranaHub.WasmSandbox.csproj
```

### Expected Output
```text
================================================================================
  KiranaHub Level 9: WebAssembly (Wasm) Sandboxing & Gas-Metered Script Engine
  Features: 64KB Linear Memory Paging, Instruction Metering, Host-Guest ABI
================================================================================

[TEST 1] Executing Valid Third-Party Merchant Festival Promotion Script...
      [HOST SYSCALL] 'host_get_customer_tier' invoked for Customer ID 88201 -> Returned 1 (VIP Member)
  - Input Cart Subtotal      : Rs 600.00 (60,000 paise)
  - Execution Time           : 0 ms (14.2 µs)
  - Gas Budget Allocated     : 50,000 gas units
  - Gas Units Consumed       : 41 units
  - Computed Total Discount  : Rs 140.00 (15% promo + Rs 50 VIP bonus)
  - Final Net Payable Amount : Rs 460.00 (46,000 paise)
  - Sandboxed Memory [0x00]  : 14000 paise (Verified from 64KB Sandbox)
  [RESULT] -> SUCCESS: Valid promotion executed safely in Wasm sandbox!

[TEST 2] Executing Malicious Runaway Infinite-Loop Script (Denial-of-Service Attack)...
  - [TRAPPED] WASM Gas Limit Exhausted: Script exceeded gas limit of 10,000 units at Program Counter 1 executing opcode Br. Execution forcefully aborted.
  - Execution Halting Time   : 0 ms (220.5 µs)
  - Host Thread State        : 100% UNBLOCKED & PROTECTED (Host did not freeze!)
  [RESULT] -> SUCCESS: Deterministic gas metering halted runaway infinite loop!

[TEST 3] Executing Malicious Memory Out-Of-Bounds Breach Script (Host RAM Escape)...
  - [TRAPPED] WASM Sandboxed Memory Out-Of-Bounds: Attempted to access 4 bytes at address 0x00FF0000, but total sandbox capacity is 65536 bytes (0x00010000)
  - Trap Response Time       : 0 ms (8.1 µs)
  - Host RAM Integrity       : 100% SECURE (Unauthorized memory address write blocked)
  [RESULT] -> SUCCESS: Linear memory bounds check trapped host memory escape attempt!

================================================================================
  [SUMMARY] All 3 WebAssembly Sandboxing & Gas-Metering Tests PASSED!
================================================================================
```
