// ============================================================================
// File: EbpfOpCodes.cs
// Namespace: KiranaHub.PacketFilterEbpf.Vm
// Description: Standard eBPF instruction opcodes and XDP action return codes.
// ============================================================================

namespace KiranaHub.PacketFilterEbpf.Vm;

/// <summary>
/// Standard 8-bit Linux eBPF instruction opcodes.
/// </summary>
public static class EbpfOpCodes
{
    // [KYA KARTA HAI]: BPF_ALU64 MOV immediate opcode define karta hai (0xb7).
    // [KYU ZAROORI HAI]: Register me constant 32/64-bit value load karne ke liye (rX = imm).
    // [KAISE CHALTA HAI]: Byte constant 0xb7.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: 32-bit ALU MOV (0xb4).
    public const byte BPF_MOV64_IMM = 0xb7;

    // [KYA KARTA HAI]: BPF_ALU64 MOV register-to-register opcode define karta hai (0xbf).
    // [KYU ZAROORI HAI]: Ek register se doosre register me 64-bit value copy karne ke liye (dst = src).
    // [KAISE CHALTA HAI]: Byte constant 0xbf.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_MOV64_REG = 0xbf;

    // [KYA KARTA HAI]: BPF_ALU64 ADD immediate opcode define karta hai (0x07).
    // [KYU ZAROORI HAI]: Register me immediate value add karne ke liye (dst += imm).
    // [KAISE CHALTA HAI]: Byte constant 0x07.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: Add with carry.
    public const byte BPF_ADD64_IMM = 0x07;

    // [KYA KARTA HAI]: BPF_ALU64 ADD register opcode define karta hai (0x0f).
    // [KYU ZAROORI HAI]: Do registers add karne ke liye (dst += src).
    // [KAISE CHALTA HAI]: Byte constant 0x0f.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_ADD64_REG = 0x0f;

    // [KYA KARTA HAI]: BPF_ALU64 SUB immediate opcode define karta hai (0x17).
    // [KYU ZAROORI HAI]: Register me se immediate subtract karne ke liye (dst -= imm).
    // [KAISE CHALTA HAI]: Byte constant 0x17.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_SUB64_IMM = 0x17;

    // [KYA KARTA HAI]: BPF_ALU64 AND immediate opcode define karta hai (0x57).
    // [KYU ZAROORI HAI]: Bitwise masking ke liye (e.g. checking IP flags, subnet CIDR mask).
    // [KAISE CHALTA HAI]: Byte constant 0x57.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: BPF_AND64_REG (0x5f).
    public const byte BPF_AND64_IMM = 0x57;

    // [KYA KARTA HAI]: BPF_ALU64 OR immediate opcode define karta hai (0x47).
    // [KYU ZAROORI HAI]: Bitwise flags set karne ke liye (dst |= imm).
    // [KAISE CHALTA HAI]: Byte constant 0x47.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_OR64_IMM = 0x47;

    // [KYA KARTA HAI]: BPF_ALU64 XOR immediate opcode define karta hai (0xa7).
    // [KYU ZAROORI HAI]: Bitwise inversion ya hashing logic ke liye (dst ^= imm).
    // [KAISE CHALTA HAI]: Byte constant 0xa7.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_XOR64_IMM = 0xa7;

    // [KYA KARTA HAI]: BPF_ALU64 LSH (Left Shift) immediate opcode define karta hai (0x67).
    // [KYU ZAROORI HAI]: Register bits left shift karne ke liye (dst <<= imm).
    // [KAISE CHALTA HAI]: Byte constant 0x67.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_LSH64_IMM = 0x67;

    // [KYA KARTA HAI]: BPF_ALU64 RSH (Right Shift) immediate opcode define karta hai (0x77).
    // [KYU ZAROORI HAI]: Register bits right shift karne ke liye (dst >>= imm).
    // [KAISE CHALTA HAI]: Byte constant 0x77.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: Arithmetic right shift BPF_ARSH.
    public const byte BPF_RSH64_IMM = 0x77;

    // [KYA KARTA HAI]: BPF_LDX_B (Load 1 byte from memory) opcode define karta hai (0x71).
    // [KYU ZAROORI HAI]: Packet buffer se single byte (e.g. IP Protocol 8-bit, TTL) read karne ke liye.
    // [KAISE CHALTA HAI]: dst = *(uint8*)(src + offset). Byte constant 0x71.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: Direct memory load BPF_LD_B.
    public const byte BPF_LDX_B = 0x71;

    // [KYA KARTA HAI]: BPF_LDX_H (Load 2 bytes half-word from memory) opcode define karta hai (0x69).
    // [KYU ZAROORI HAI]: Packet buffer se 16-bit integer (e.g. TCP/UDP Destination Port, EtherType) read karne ke liye.
    // [KAISE CHALTA HAI]: dst = *(uint16*)(src + offset). Byte constant 0x69.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: Big-endian network byte order handling.
    public const byte BPF_LDX_H = 0x69;

    // [KYA KARTA HAI]: BPF_LDX_W (Load 4 bytes word from memory) opcode define karta hai (0x61).
    // [KYU ZAROORI HAI]: Packet buffer se 32-bit integer (e.g. IPv4 Source/Destination Address) read karne ke liye.
    // [KAISE CHALTA HAI]: dst = *(uint32*)(src + offset). Byte constant 0x61.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_LDX_W = 0x61;

    // [KYA KARTA HAI]: BPF_LDX_DW (Load 8 bytes double-word from memory) opcode define karta hai (0x79).
    // [KYU ZAROORI HAI]: Packet buffer ya context structure se 64-bit value read karne ke liye.
    // [KAISE CHALTA HAI]: dst = *(uint64*)(src + offset). Byte constant 0x79.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_LDX_DW = 0x79;

    // [KYA KARTA HAI]: BPF_JA (Unconditional Jump) opcode define karta hai (0x05).
    // [KYU ZAROORI HAI]: Program counter ko direct offset instructions aage jump karane ke liye.
    // [KAISE CHALTA HAI]: pc += offset. Byte constant 0x05.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_JA = 0x05;

    // [KYA KARTA HAI]: BPF_JEQ_IMM (Jump if Equal Immediate) opcode define karta hai (0x15).
    // [KYU ZAROORI HAI]: Conditional check: agar dst == imm to jump offset execute karo.
    // [KAISE CHALTA HAI]: if (dst == imm) pc += offset. Byte constant 0x15.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: BPF_JEQ_REG.
    public const byte BPF_JEQ_IMM = 0x15;

    // [KYA KARTA HAI]: BPF_JNE_IMM (Jump if Not Equal Immediate) opcode define karta hai (0x55).
    // [KYU ZAROORI HAI]: Conditional check: agar dst != imm to jump offset execute karo.
    // [KAISE CHALTA HAI]: if (dst != imm) pc += offset. Byte constant 0x55.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_JNE_IMM = 0x55;

    // [KYA KARTA HAI]: BPF_JGT_IMM (Jump if Greater Than Immediate) opcode define karta hai (0x25).
    // [KYU ZAROORI HAI]: Threshold check: agar dst > imm to jump offset karo.
    // [KAISE CHALTA HAI]: if (dst > imm) pc += offset. Byte constant 0x25.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: Unsigned vs Signed comparison.
    public const byte BPF_JGT_IMM = 0x25;

    // [KYA KARTA HAI]: BPF_JGE_IMM (Jump if Greater Than or Equal Immediate) opcode define karta hai (0x35).
    // [KYU ZAROORI HAI]: Packet bounds check: packet length >= expected header size verify karne ke liye.
    // [KAISE CHALTA HAI]: if (dst >= imm) pc += offset. Byte constant 0x35.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_JGE_IMM = 0x35;

    // [KYA KARTA HAI]: BPF_JLT_IMM (Jump if Less Than Immediate) opcode define karta hai (0xa5).
    // [KYU ZAROORI HAI]: Lower bound check: agar dst < imm to jump offset karo.
    // [KAISE CHALTA HAI]: if (dst < imm) pc += offset. Byte constant 0xa5.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_JLT_IMM = 0xa5;

    // [KYA KARTA HAI]: BPF_EXIT (Program Exit) opcode define karta hai (0x95).
    // [KYU ZAROORI HAI]: eBPF program execution terminate karke R0 register ka action code return karne ke liye.
    // [KAISE CHALTA HAI]: Returns R0 value to kernel/host. Byte constant 0x95.
    // [OUTPUT / SIDE EFFECT]: Opcode constant.
    // [VARIATIONS TO TRY]: None.
    public const byte BPF_EXIT = 0x95;
}

/// <summary>
/// High-speed Linux XDP (eXpress Data Path) kernel packet filter action return codes.
/// </summary>
public enum XdpAction : uint
{
    // [KYA KARTA HAI]: XDP_PASS (Action code 0) define karta hai.
    // [KYU ZAROORI HAI]: Legitimate quick-commerce packet ko normal OS network stack aur ASP.NET Core ko forward karne ke liye.
    // [KAISE CHALTA HAI]: Packet flows up to user-space application socket.
    // [OUTPUT / SIDE EFFECT]: Normal packet delivery.
    // [VARIATIONS TO TRY]: Metric increment.
    Pass = 0,

    // [KYA KARTA HAI]: XDP_DROP (Action code 1) define karta hai.
    // [KYU ZAROORI HAI]: Malicious DDoS flood packet ko NIC driver level par silently discard karne ke liye (zero OS kernel stack overhead).
    // [KAISE CHALTA HAI]: NIC ring buffer drops packet instantly.
    // [OUTPUT / SIDE EFFECT]: Line-rate DDoS mitigation.
    // [VARIATIONS TO TRY]: Drop counter telemetry.
    Drop = 1,

    // [KYA KARTA HAI]: XDP_TX (Action code 2) define karta hai.
    // [KYU ZAROORI HAI]: Packet ko usi network interface se wapas bounce (reflect) karne ke liye.
    // [KAISE CHALTA HAI]: Retransmits packet out same interface.
    // [OUTPUT / SIDE EFFECT]: Reflected packet.
    // [VARIATIONS TO TRY]: ICMP echo reply.
    Tx = 2,

    // [KYA KARTA HAI]: XDP_ABORTED (Action code 3) define karta hai.
    // [KYU ZAROORI HAI]: Filter exception ya malformed packet error par abort drop karne ke liye.
    // [KAISE CHALTA HAI]: Drops packet and logs eBPF tracepoint error.
    // [OUTPUT / SIDE EFFECT]: Error drop.
    // [VARIATIONS TO TRY]: Alert generation.
    Aborted = 3
}
