// ============================================================================
// File: EbpfInstruction.cs
// Namespace: KiranaHub.PacketFilterEbpf.Vm
// Description: 64-bit Linux eBPF instruction representation with static factory helpers.
// ============================================================================

namespace KiranaHub.PacketFilterEbpf.Vm;

/// <summary>
/// Represents a single 64-bit eBPF bytecode instruction.
/// Layout: OpCode (8 bits), DstReg (4 bits), SrcReg (4 bits), Offset (16 bits), Immediate (32 bits).
/// </summary>
public readonly struct EbpfInstruction
{
    // [KYA KARTA HAI]: 8-bit instruction opcode store karta hai.
    // [KYU ZAROORI HAI]: Virtual machine ko specific operation decode karne ke liye opcode chahiye.
    // [KAISE CHALTA HAI]: Readonly byte field.
    // [OUTPUT / SIDE EFFECT]: Opcode value.
    // [VARIATIONS TO TRY]: Enum EbpfOpCode.
    public byte OpCode { get; }

    // [KYA KARTA HAI]: Destination register index (0 to 10) store karta hai.
    // [KYU ZAROORI HAI]: Result ya memory write target register specify karta hai.
    // [KAISE CHALTA HAI]: Readonly byte field (4 bits used in wire format).
    // [OUTPUT / SIDE EFFECT]: Dst register ID.
    // [VARIATIONS TO TRY]: Register enum.
    public byte DstReg { get; }

    // [KYA KARTA HAI]: Source register index (0 to 10) store karta hai.
    // [KYU ZAROORI HAI]: ALU operand ya memory base address register specify karta hai.
    // [KAISE CHALTA HAI]: Readonly byte field (4 bits used in wire format).
    // [OUTPUT / SIDE EFFECT]: Src register ID.
    // [VARIATIONS TO TRY]: None.
    public byte SrcReg { get; }

    // [KYA KARTA HAI]: 16-bit signed jump offset ya memory displacement store karta hai.
    // [KYU ZAROORI HAI]: Relative branch target aur packet offset access define karta hai.
    // [KAISE CHALTA HAI]: Readonly short field.
    // [OUTPUT / SIDE EFFECT]: Offset value.
    // [VARIATIONS TO TRY]: None.
    public short Offset { get; }

    // [KYA KARTA HAI]: 32-bit constant immediate integer store karta hai.
    // [KYU ZAROORI HAI]: Comparison literals, port numbers, IP addresses, ya bitmasks hold karta hai.
    // [KAISE CHALTA HAI]: Readonly int field.
    // [OUTPUT / SIDE EFFECT]: Immediate operand.
    // [VARIATIONS TO TRY]: uint immediate.
    public int Immediate { get; }

    // [KYA KARTA HAI]: Constructor EbpfInstruction initialize karta hai.
    // [KYU ZAROORI HAI]: Full 64-bit instruction tuple construct karna.
    // [KAISE CHALTA HAI]: Sets all 5 instruction fields.
    // [OUTPUT / SIDE EFFECT]: Immutable EbpfInstruction instance.
    // [VARIATIONS TO TRY]: Binary parser from byte stream.
    public EbpfInstruction(byte opCode, byte dstReg, byte srcReg, short offset, int immediate)
    {
        // [KYA KARTA HAI]: Set OpCode.
        // [KYU ZAROORI HAI]: Operation identity.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: OpCode assigned.
        // [VARIATIONS TO TRY]: None.
        OpCode = opCode;

        // [KYA KARTA HAI]: Set DstReg.
        // [KYU ZAROORI HAI]: Target register.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: DstReg assigned.
        // [VARIATIONS TO TRY]: None.
        DstReg = dstReg;

        // [KYA KARTA HAI]: Set SrcReg.
        // [KYU ZAROORI HAI]: Source register.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: SrcReg assigned.
        // [VARIATIONS TO TRY]: None.
        SrcReg = srcReg;

        // [KYA KARTA HAI]: Set Offset.
        // [KYU ZAROORI HAI]: Jump / memory displacement.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: Offset assigned.
        // [VARIATIONS TO TRY]: None.
        Offset = offset;

        // [KYA KARTA HAI]: Set Immediate.
        // [KYU ZAROORI HAI]: Constant operand.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: Immediate assigned.
        // [VARIATIONS TO TRY]: None.
        Immediate = immediate;
    }

    // [KYA KARTA HAI]: MOV immediate instruction banata hai (dst = imm).
    // [KYU ZAROORI HAI]: Register me constant value load karna (e.g. action code XDP_DROP).
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_MOV64_IMM ke saath struct create karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: 32-bit MOV.
    public static EbpfInstruction MovImm(byte dst, int imm) =>
        new(EbpfOpCodes.BPF_MOV64_IMM, dst, 0, 0, imm);

    // [KYA KARTA HAI]: MOV register-to-register instruction banata hai (dst = src).
    // [KYU ZAROORI HAI]: Context ya packet pointers copy karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_MOV64_REG ke saath struct create karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction MovReg(byte dst, byte src) =>
        new(EbpfOpCodes.BPF_MOV64_REG, dst, src, 0, 0);

    // [KYA KARTA HAI]: ADD immediate instruction banata hai (dst += imm).
    // [KYU ZAROORI HAI]: Pointers increment karne ya offsets calculate karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_ADD64_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction AddImm(byte dst, int imm) =>
        new(EbpfOpCodes.BPF_ADD64_IMM, dst, 0, 0, imm);

    // [KYA KARTA HAI]: AND immediate instruction banata hai (dst &= imm).
    // [KYU ZAROORI HAI]: Bitwise subnet masking ke liye (e.g. 192.168.1.0/24 subnet match).
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_AND64_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction AndImm(byte dst, int imm) =>
        new(EbpfOpCodes.BPF_AND64_IMM, dst, 0, 0, imm);

    // [KYA KARTA HAI]: Load byte from memory instruction banata hai: dst = *(uint8*)(src + offset).
    // [KYU ZAROORI HAI]: Packet me se IP Protocol (TCP/UDP) read karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_LDX_B.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction LoadByte(byte dst, byte src, short offset) =>
        new(EbpfOpCodes.BPF_LDX_B, dst, src, offset, 0);

    // [KYA KARTA HAI]: Load half-word (16 bits) from memory: dst = *(uint16*)(src + offset).
    // [KYU ZAROORI HAI]: TCP / UDP destination port number read karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_LDX_H.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: Network byte order swap.
    public static EbpfInstruction LoadHalf(byte dst, byte src, short offset) =>
        new(EbpfOpCodes.BPF_LDX_H, dst, src, offset, 0);

    // [KYA KARTA HAI]: Load word (32 bits) from memory: dst = *(uint32*)(src + offset).
    // [KYU ZAROORI HAI]: IPv4 source address ya destination address read karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_LDX_W.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction LoadWord(byte dst, byte src, short offset) =>
        new(EbpfOpCodes.BPF_LDX_W, dst, src, offset, 0);

    // [KYA KARTA HAI]: Unconditional Jump instruction banata hai: pc += offset.
    // [KYU ZAROORI HAI]: Control flow me code blocks bypass karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JA.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction Jump(short offset) =>
        new(EbpfOpCodes.BPF_JA, 0, 0, offset, 0);

    // [KYA KARTA HAI]: Jump if Equal Immediate banata hai: if (dst == imm) pc += offset.
    // [KYU ZAROORI HAI]: Target port ya target IP match hone par jump karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JEQ_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction JumpEqualImm(byte dst, int imm, short offset) =>
        new(EbpfOpCodes.BPF_JEQ_IMM, dst, 0, offset, imm);

    // [KYA KARTA HAI]: Jump if Not Equal Immediate banata hai: if (dst != imm) pc += offset.
    // [KYU ZAROORI HAI]: Non-matching traffic ko bypass karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JNE_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction JumpNotEqualImm(byte dst, int imm, short offset) =>
        new(EbpfOpCodes.BPF_JNE_IMM, dst, 0, offset, imm);

    // [KYA KARTA HAI]: Jump if Greater Than Immediate banata hai: if (dst > imm) pc += offset.
    // [KYU ZAROORI HAI]: Rate limiting threshold exceeded check karne ke liye.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JGT_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction JumpGreaterThanImm(byte dst, int imm, short offset) =>
        new(EbpfOpCodes.BPF_JGT_IMM, dst, 0, offset, imm);

    // [KYA KARTA HAI]: Jump if Greater or Equal Immediate banata hai: if (dst >= imm) pc += offset.
    // [KYU ZAROORI HAI]: Packet length check karne ke liye before dereferencing headers.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JGE_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction JumpGreaterOrEqualImm(byte dst, int imm, short offset) =>
        new(EbpfOpCodes.BPF_JGE_IMM, dst, 0, offset, imm);

    // [KYA KARTA HAI]: Jump if Less Than Immediate banata hai: if (dst < imm) pc += offset.
    // [KYU ZAROORI HAI]: Out-of-bounds packet bounds check: if (packet_len < min_size) drop.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_JLT_IMM.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction JumpLessThanImm(byte dst, int imm, short offset) =>
        new(EbpfOpCodes.BPF_JLT_IMM, dst, 0, offset, imm);

    // [KYA KARTA HAI]: EXIT instruction banata hai: program halts and returns R0.
    // [KYU ZAROORI HAI]: Execution complete karke kernel XDP engine ko action return karna.
    // [KAISE CHALTA HAI]: EbpfOpCodes.BPF_EXIT.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction.
    // [VARIATIONS TO TRY]: None.
    public static EbpfInstruction Exit() =>
        new(EbpfOpCodes.BPF_EXIT, 0, 0, 0, 0);
}
