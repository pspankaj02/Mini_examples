# Project 51: High-Throughput Kernel Packet Filtering with Sandboxed eBPF Virtual Machine (XDP DDoS Shield)

## Part 1: Curriculum & Architecture Roadmap
```
KiranaHub Master Curriculum
├── Level 01-10: C# Foundations to Apex AI Swarms (Projects 01-46)
└── Level 11: Hyper-Frontier Systems & Distributed Storage
    ├── 47: Columnar OLAP Vectorized Query Engine
    ├── 48: Byzantine Fault Tolerant (PBFT) Consensus & Merkle Trie
    ├── 49: Stateful CEP Stream Join & Dynamic Watermarking
    ├── 50: Distributed Object Storage & Reed-Solomon Erasure Coding
    └── 51: High-Throughput Packet Filter & Sandboxed eBPF VM [YOU ARE HERE]
```

During flash-sale events in quick-commerce ("KiranaHub")—such as 10-minute mango festivals or cricket match meal delivery spikes—edge gateways are bombarded by over **1,000,000 malicious packets per second** (SYN floods, rogue UDP amplification, and botnet scrapers). 

Traditional user-space firewalls and reverse proxies (Envoy, Nginx, ASP.NET Kestrel) process packets after they travel through:
1. Physical NIC interrupt handling.
2. Kernel socket buffer allocation (`sk_buff`).
3. OS TCP/IP protocol stack traversal.
4. User-space context-switching.

At millions of packets per second, this causes severe kernel buffer starvation, massive CPU context-switch churn, and complete gateway collapse. Project 51 solves this by implementing a **pure C# 64-bit eBPF (Extended Berkeley Packet Filter) Virtual Machine and XDP (eXpress Data Path) Engine** with a **formal static DAG safety verifier**. Packets are filtered directly on ingress network buffers in **nanoseconds per packet** with **zero memory allocations**.

---

## Part 2: Deep Foundations: eBPF Register Architecture & Static DAG Verification

```
                          [ Incoming Network Packet Stream ]
                                          │
                                          ▼
                      ┌───────────────────────────────────────┐
                      │    In-Kernel Static DAG Verifier      │
                      │  • No backward jumps (DAG / Loop-free)│
                      │  • Memory bounds checks (R1 length)   │
                      │  • Register initialization enforcement│
                      │  • Max instruction budget (<= 4096)   │
                      └──────────────────┬────────────────────┘
                                         │ Approved Bytecode
                                         ▼
                      ┌───────────────────────────────────────┐
                      │   Sandboxed 64-bit eBPF Virtual Machine│
                      │   (Registers R0-R10, Stack, Zero-Alloc)│
                      └───────┬───────────────────────┬───────┘
                              │                       │
                       XDP_DROP (1)             XDP_PASS (0)
                              │                       │
                              ▼                       ▼
                     [ Silently Dropped ]     [ Kirana App Socket ]
                     (Sub-microsecond)        (ASP.NET Core Order API)
```

### 1. eBPF Instruction Format (64-Bit / 8-Byte Word)
Every eBPF instruction conforms to the standard 64-bit Linux kernel binary format:
```
+---------------+---------------+---------------+---------------+
|    Immediate (32 bits)        | Offset (16b)  |Src(4b)|Dst(4b)| Opcode(8b)|
+---------------+---------------+---------------+---------------+
```
- **Registers**: 11 64-bit registers:
  - `R0`: Return value (e.g. `XDP_PASS = 0`, `XDP_DROP = 1`, `XDP_TX = 2`).
  - `R1`: Context argument pointer / packet buffer reference.
  - `R2 - R5`: Scratch and argument registers.
  - `R6 - R9`: Callee-saved registers.
  - `R10`: Read-only frame pointer to stack.

### 2. The Linux In-Kernel Static Verifier
The kernel cannot allow arbitrary user code to run inside interrupt context without strict safety guarantees:
- **Loop-Freedom Guarantee**: Backward jumps (`offset < 0`) are strictly forbidden. All jump targets must point forward within instruction bounds, mathematically proving termination in at most $N$ instructions.
- **Uninitialized Register Guards**: Registers cannot be read before being written to, preventing kernel memory information leaks.
- **Strict Memory Bounds Verification**: All memory loads (`BPF_LDX_B`, `BPF_LDX_H`, `BPF_LDX_W`) must verify that target offsets remain strictly within the packet buffer (`offset + size <= packet.Length`).

---

## Part 3: Architecture, Mechanics & Evolution

### 1. The KiranaHub XDP Protection Pipeline
1. **Header Bounds Check**: Inspects `R1` (packet length). If length $< 38$ bytes, drops truncated packets immediately.
2. **Protocol Demultiplexing**: Loads byte at offset 23 (IPv4 protocol). If not TCP (`6`), drops UDP/IGMP flood traffic.
3. **Subnet / CIDR Blacklisting**: Loads 32-bit Source IP at offset 26, masks with `/24` subnet mask (`0xFFFFFF00`), and drops known malicious botnet IPs (e.g. `198.51.100.0/24`).
4. **Port Enforcement**: Loads 16-bit Destination Port at offset 36. Drops all traffic not destined for Kirana API port `5000`.
5. **Verdict**: If all checks pass, returns `XDP_PASS` to deliver packet to user-space application.

---

## Part 4: Counter-Factual Risk Analysis

| Architectural Decision | If We Did the Naive / Opposite Approach | Production Quick-Commerce Catastrophe Avoided |
|---|---|---|
| **Driver-Level XDP Drop vs. Application-Level HTTP Filter** | Letting malicious packets enter ASP.NET Core Kestrel middleware to return `403 Forbidden`. | At 1,000,000 requests/sec, Kestrel allocates millions of `HttpContext` objects, triggering massive GC pauses, socket buffer exhaustion, and 100% gateway downtime for legitimate shoppers. |
| **Static DAG Verification vs. Unchecked Bytecode Execution** | Executing bytecode directly without forward-jump and register checks. | An attacker could upload bytecode with an infinite loop or negative jump, freezing kernel worker threads, locking CPU cores at 100%, and causing hard kernel panics. |
| **Zero-Allocation Ingress vs. Object-Oriented Packet Modeling** | Allocating `new Packet()` or `new MemoryStream()` for every incoming network frame. | Slicing 1,000,000 packets into heap objects allocates 100+ MB of RAM per second, triggering Gen 2 GC storms and ballooning latency from 200 nanoseconds to 50 milliseconds. |
| **Register-Based VM vs. Stack-Based VM (JVM/Wasm)** | Using an operand stack (push/pop) model for packet filtering. | Stack VMs require multiple push/pop operations per calculation, generating 3x more instruction dispatches. eBPF's register model directly maps to x86-64 hardware registers for maximum execution speed. |
| **Big-Endian Integer Loading vs. Manual Byte Shifting** | Manually decoding `(b0 << 24) | (b1 << 16)...` for every IP and Port check. | `BinaryPrimitives.ReadUInt16BigEndian` compiles down to single `mov` and `bswap` CPU instructions, saving 10+ CPU cycles per packet. |

---

## Part 5: Run, Verify & Benchmark

### Build and Run:
```bash
dotnet run --project 51-Programmable-KernelPacketFilter-eBPF/KiranaHub.PacketFilterEbpf.csproj
```

### Expected Output Summary:
```
================================================================================
🛡️ KIRANAHUB: HIGH-THROUGHPUT KERNEL PACKET FILTER & SANDBOXED eBPF VM
   Linux XDP Architecture • Static DAG Verifier • Line-Rate DDoS Shield
================================================================================

🧪 [TEST 1: IN-KERNEL STATIC BYTECODE VERIFIER]
   Verifying safety guarantees before bytecode is allowed to run in the kernel...

   • Test 1A (Infinite Loop / Backward Jump):
     Result: ✅ REJECTED AS EXPECTED!
     Verifier Guard Reason: Verification failed at PC 1: Backward jump detected (Offset = -1). Infinite loops forbidden in eBPF.

   • Test 1B (Uninitialized Register Read):
     Result: ✅ REJECTED AS EXPECTED!
     Verifier Guard Reason: Verification failed at PC 0: ALU operation on uninitialized register R7.

⚡ [TEST 2: COMPILING & VERIFYING KIRANAHUB DDoS SHIELD eBPF BYTECODE]
   • Program Length: 19 instructions (Well below 4096 budget)
   • Verifier Approval: ✅ APPROVED FOR INGRESS EXECUTION!
   • Invariants Verified: Loop-free DAG • Memory bounds checks • R0 exit initialized

🎯 [TEST 3: DISCRETE PACKET SCENARIO AUDIT]
   • Scenario A [Legitimate Customer Order]: TCP, Port 5000, IP 203.0.113.50
     Verdict: Pass ✅ (ACCEPTED TO APP SOCKET)
   • Scenario B [Rogue UDP Flood]: UDP Protocol 17, Port 5000
     Verdict: Drop ✅ (DROPPED AT NIC DRIVER LEVEL)
   • Scenario C [Botnet Subnet Flood]: TCP, Port 5000, IP 198.51.100.42 (Blacklisted)
     Verdict: Drop ✅ (DROPPED BY CIDR MASK)
   • Scenario D [Port Scan Reconnaissance]: TCP, Port 8080 (Unapproved Port)
     Verdict: Drop ✅ (DROPPED UNAPPROVED PORT)
   • Scenario E [Truncated Runt Packet]: Length 20 bytes (< 38 bytes minimum)
     Verdict: Drop ✅ (DROPPED BOUNDS VIOLATION)

🚀 [TEST 4: LINE-RATE 1,000,000 PACKET BURST BENCHMARK]
   Simulating massive flash-sale network ingress burst...
   • Generating 10,00,000 synthetic ingress frames (8,00,000 attacks, 2,00,000 legitimate)...

📊 [BENCHMARK RESULTS]
   • Total Ingress Packets Processed: 10,00,000
   • Legitimate Passed (XDP_PASS):    2,00,000 (Target: 2,00,000)
   • Malicious Dropped (XDP_DROP):    8,00,000 (Target: 8,00,000)
   • Aborted / Errors:                0
   • Execution Time:                  275 ms (0.2756 seconds)
   • Throughput:                      3.63 Million packets/sec (Mpps)
   • Average Latency per Packet:      275.6 nanoseconds!
   • Heap Memory Allocated:           40 bytes (✅ ZERO-ALLOCATION INGRESS!)
```
