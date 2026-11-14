// ============================================================================
// File: EbpfVirtualMachine.cs
// Namespace: KiranaHub.PacketFilterEbpf.Vm
// Description: High-performance 11-register 64-bit eBPF Virtual Machine
// executing sandboxed bytecode programs over network packet memory buffers.
// ============================================================================

using System.Buffers.Binary;

namespace KiranaHub.PacketFilterEbpf.Vm;

/// <summary>
/// Sandboxed 64-bit eBPF Virtual Machine executing in-kernel packet filtering logic.
/// </summary>
public sealed class EbpfVirtualMachine
{
    // [KYA KARTA HAI]: Loaded eBPF program bytecode instructions store karta hai.
    // [KYU ZAROORI HAI]: Packet execution loop instruction sequence fetch karta hai.
    // [KAISE CHALTA HAI]: Readonly EbpfInstruction array.
    // [OUTPUT / SIDE EFFECT]: Bytecode array.
    // [VARIATIONS TO TRY]: JIT-compiled dynamic delegates.
    public EbpfInstruction[] Program { get; }

    // [KYA KARTA HAI]: 11 64-bit registers array (R0 to R10).
    // [KYU ZAROORI HAI]: CPU register file emulation.
    // [KAISE CHALTA HAI]: ulong[11] buffer.
    // [OUTPUT / SIDE EFFECT]: Register bank state.
    // [VARIATIONS TO TRY]: Thread-local or stack-allocated registers for zero-allocation concurrency.
    private readonly ulong[] _registers = new ulong[11];

    // [KYA KARTA HAI]: Constructor EbpfVirtualMachine bytecode load karta hai.
    // [KYU ZAROORI HAI]: Pre-verified program ko virtual machine me bind karna.
    // [KAISE CHALTA HAI]: Program = program.
    // [OUTPUT / SIDE EFFECT]: Ready VM instance.
    // [VARIATIONS TO TRY]: Automatic verifier invocation in constructor.
    public EbpfVirtualMachine(EbpfInstruction[] program)
    {
        // [KYA KARTA HAI]: Set Program reference.
        // [KYU ZAROORI HAI]: Program persistence.
        // [KAISE CHALTA HAI]: Program = program.
        // [OUTPUT / SIDE EFFECT]: Assigned.
        // [VARIATIONS TO TRY]: None.
        Program = program;
    }

    // [KYA KARTA HAI]: Packet memory buffer par sandboxed eBPF bytecode execute karta hai.
    // [KYU ZAROORI HAI]: Line-rate network packet filtering and DDoS mitigation (XDP_PASS vs XDP_DROP).
    // [KAISE CHALTA HAI]: R1 ko packet context set karta hai aur instruction loop dispatch karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns XdpAction enum code (Pass, Drop, Tx, Aborted).
    // [VARIATIONS TO TRY]: Unsafe pointer execution for ultra-low latency.
    public XdpAction Execute(ReadOnlySpan<byte> packet)
    {
        // [KYA KARTA HAI]: Clear registers to 0.
        // [KYU ZAROORI HAI]: Clean register state between packet evaluations.
        // [KAISE CHALTA HAI]: Array.Clear(_registers).
        // [OUTPUT / SIDE EFFECT]: Registers reset to 0.
        // [VARIATIONS TO TRY]: Inline unrolled zeroing.
        Array.Clear(_registers);

        // [KYA KARTA HAI]: R1 register me packet length set karta hai (context packet length).
        // [KYU ZAROORI HAI]: eBPF filter packet length inspect karke bounds check kar sake.
        // [KAISE CHALTA HAI]: _registers[1] = (ulong)packet.Length.
        // [OUTPUT / SIDE EFFECT]: R1 initialized with packet length.
        // [VARIATIONS TO TRY]: Pointer to xdp_md struct.
        _registers[1] = (ulong)packet.Length;

        // [KYA KARTA HAI]: Program counter (PC) 0 se initialize karta hai.
        // [KYU ZAROORI HAI]: Starts execution from the first instruction.
        // [KAISE CHALTA HAI]: int pc = 0.
        // [OUTPUT / SIDE EFFECT]: Initial PC.
        // [VARIATIONS TO TRY]: None.
        int pc = 0;

        // [KYA KARTA HAI]: Max execution steps safeguard counter define karta hai (prevent runaway execution).
        // [KYU ZAROORI HAI]: Hardware watchdog simulation.
        // [KAISE CHALTA HAI]: int maxSteps = 4096.
        // [OUTPUT / SIDE EFFECT]: Step budget.
        // [VARIATIONS TO TRY]: Configurable step limit.
        int stepsRemaining = 4096;

        // [KYA KARTA HAI]: Core VM interpreter execution loop.
        // [KYU ZAROORI HAI]: Dispatches instruction opcodes at maximum CPU speed.
        // [KAISE CHALTA HAI]: while (pc >= 0 && pc < Program.Length && stepsRemaining-- > 0).
        // [OUTPUT / SIDE EFFECT]: Executes bytecode stream.
        // [VARIATIONS TO TRY]: Direct threaded code / jump tables.
        while (pc >= 0 && pc < Program.Length && stepsRemaining-- > 0)
        {
            // [KYA KARTA HAI]: Current instruction fetch karta hai.
            // [KYU ZAROORI HAI]: Instruction decoding.
            // [KAISE CHALTA HAI]: ref readonly inst = ref Program[pc].
            // [OUTPUT / SIDE EFFECT]: Fast instruction reference.
            // [VARIATIONS TO TRY]: Copy to local struct.
            ref readonly var inst = ref Program[pc];

            // [KYA KARTA HAI]: Opcode execution branch switch karta hai.
            // [KYU ZAROORI HAI]: Dispatches ALU, Jump, Memory, and Exit operations.
            // [KAISE CHALTA HAI]: switch (inst.OpCode).
            // [OUTPUT / SIDE EFFECT]: Executes specific instruction logic.
            // [VARIATIONS TO TRY]: Function pointer table.
            switch (inst.OpCode)
            {
                // [KYA KARTA HAI]: BPF_MOV64_IMM execute karta hai: dst = imm.
                // [KYU ZAROORI HAI]: Load 32-bit constant into 64-bit register.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] = (ulong)(uint)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Register updated, PC advanced.
                // [VARIATIONS TO TRY]: Sign-extended load.
                case EbpfOpCodes.BPF_MOV64_IMM:
                    _registers[inst.DstReg] = (ulong)(uint)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_MOV64_REG execute karta hai: dst = src.
                // [KYU ZAROORI HAI]: Register to register copy.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] = _registers[inst.SrcReg]; pc++;
                // [OUTPUT / SIDE EFFECT]: Register copied, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_MOV64_REG:
                    _registers[inst.DstReg] = _registers[inst.SrcReg];
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_ADD64_IMM execute karta hai: dst += imm.
                // [KYU ZAROORI HAI]: Immediate addition.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] += (ulong)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Register sum updated, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_ADD64_IMM:
                    _registers[inst.DstReg] += (ulong)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_SUB64_IMM execute karta hai: dst -= imm.
                // [KYU ZAROORI HAI]: Immediate subtraction.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] -= (ulong)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Register difference updated, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_SUB64_IMM:
                    _registers[inst.DstReg] -= (ulong)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_AND64_IMM execute karta hai: dst &= imm.
                // [KYU ZAROORI HAI]: Bitwise masking for IP subnet or protocol flags.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] &= (ulong)(uint)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Masked register, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_AND64_IMM:
                    _registers[inst.DstReg] &= (ulong)(uint)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_OR64_IMM execute karta hai: dst |= imm.
                // [KYU ZAROORI HAI]: Bitwise flags union.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] |= (ulong)(uint)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Bitwise OR result, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_OR64_IMM:
                    _registers[inst.DstReg] |= (ulong)(uint)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_XOR64_IMM execute karta hai: dst ^= imm.
                // [KYU ZAROORI HAI]: Bitwise XOR.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] ^= (ulong)(uint)inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Bitwise XOR result, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_XOR64_IMM:
                    _registers[inst.DstReg] ^= (ulong)(uint)inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_LSH64_IMM execute karta hai: dst <<= imm.
                // [KYU ZAROORI HAI]: Logical left shift.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] <<= inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Shifted register, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_LSH64_IMM:
                    _registers[inst.DstReg] <<= inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_RSH64_IMM execute karta hai: dst >>= imm.
                // [KYU ZAROORI HAI]: Logical right shift.
                // [KAISE CHALTA HAI]: _registers[inst.DstReg] >>= inst.Immediate; pc++;
                // [OUTPUT / SIDE EFFECT]: Shifted register, PC advanced.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_RSH64_IMM:
                    _registers[inst.DstReg] >>= inst.Immediate;
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_LDX_B execute karta hai: 1 byte load from packet buffer.
                // [KYU ZAROORI HAI]: Read 8-bit IP protocol or TTL field directly from packet.
                // [KAISE CHALTA HAI]: Checks packet bounds, reads packet[offset], advances PC.
                // [OUTPUT / SIDE EFFECT]: DstReg loaded with byte value.
                // [VARIATIONS TO TRY]: Unsafe pointer dereference.
                case EbpfOpCodes.BPF_LDX_B:
                    if (inst.Offset < 0 || inst.Offset >= packet.Length)
                    {
                        return XdpAction.Aborted;
                    }
                    _registers[inst.DstReg] = packet[inst.Offset];
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_LDX_H execute karta hai: 2 bytes (Big Endian) load from packet.
                // [KYU ZAROORI HAI]: Read 16-bit network port number (TCP/UDP port).
                // [KAISE CHALTA HAI]: BinaryPrimitives.ReadUInt16BigEndian at packet[offset].
                // [OUTPUT / SIDE EFFECT]: DstReg loaded with port number.
                // [VARIATIONS TO TRY]: Little endian read.
                case EbpfOpCodes.BPF_LDX_H:
                    if (inst.Offset < 0 || inst.Offset + 2 > packet.Length)
                    {
                        return XdpAction.Aborted;
                    }
                    _registers[inst.DstReg] = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(inst.Offset, 2));
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_LDX_W execute karta hai: 4 bytes (Big Endian) load from packet.
                // [KYU ZAROORI HAI]: Read 32-bit IPv4 address from packet buffer.
                // [KAISE CHALTA HAI]: BinaryPrimitives.ReadUInt32BigEndian at packet[offset].
                // [OUTPUT / SIDE EFFECT]: DstReg loaded with IPv4 address integer.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_LDX_W:
                    if (inst.Offset < 0 || inst.Offset + 4 > packet.Length)
                    {
                        return XdpAction.Aborted;
                    }
                    _registers[inst.DstReg] = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(inst.Offset, 4));
                    pc++;
                    break;

                // [KYA KARTA HAI]: BPF_JA execute karta hai: unconditional jump.
                // [KYU ZAROORI HAI]: Advance PC by relative offset.
                // [KAISE CHALTA HAI]: pc += 1 + inst.Offset.
                // [OUTPUT / SIDE EFFECT]: Branch taken.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JA:
                    pc += 1 + inst.Offset;
                    break;

                // [KYA KARTA HAI]: BPF_JEQ_IMM execute karta hai: if (dst == imm) jump.
                // [KYU ZAROORI HAI]: Conditional equality jump.
                // [KAISE CHALTA HAI]: if (_registers[inst.DstReg] == (ulong)(uint)inst.Immediate) pc += 1 + offset else pc++.
                // [OUTPUT / SIDE EFFECT]: Conditional branch evaluated.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JEQ_IMM:
                    if (_registers[inst.DstReg] == (ulong)(uint)inst.Immediate)
                    {
                        pc += 1 + inst.Offset;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                // [KYA KARTA HAI]: BPF_JNE_IMM execute karta hai: if (dst != imm) jump.
                // [KYU ZAROORI HAI]: Conditional inequality jump.
                // [KAISE CHALTA HAI]: if (_registers[inst.DstReg] != (ulong)(uint)inst.Immediate) pc += 1 + offset else pc++.
                // [OUTPUT / SIDE EFFECT]: Conditional branch evaluated.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JNE_IMM:
                    if (_registers[inst.DstReg] != (ulong)(uint)inst.Immediate)
                    {
                        pc += 1 + inst.Offset;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                // [KYA KARTA HAI]: BPF_JGT_IMM execute karta hai: if (dst > imm) jump.
                // [KYU ZAROORI HAI]: Unsigned greater-than jump.
                // [KAISE CHALTA HAI]: if (_registers[inst.DstReg] > (ulong)(uint)inst.Immediate) pc += 1 + offset else pc++.
                // [OUTPUT / SIDE EFFECT]: Branch evaluated.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JGT_IMM:
                    if (_registers[inst.DstReg] > (ulong)(uint)inst.Immediate)
                    {
                        pc += 1 + inst.Offset;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                // [KYA KARTA HAI]: BPF_JGE_IMM execute karta hai: if (dst >= imm) jump.
                // [KYU ZAROORI HAI]: Packet minimum length bounds jump.
                // [KAISE CHALTA HAI]: if (_registers[inst.DstReg] >= (ulong)(uint)inst.Immediate) pc += 1 + offset else pc++.
                // [OUTPUT / SIDE EFFECT]: Branch evaluated.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JGE_IMM:
                    if (_registers[inst.DstReg] >= (ulong)(uint)inst.Immediate)
                    {
                        pc += 1 + inst.Offset;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                // [KYA KARTA HAI]: BPF_JLT_IMM execute karta hai: if (dst < imm) jump.
                // [KYU ZAROORI HAI]: Unsigned less-than jump.
                // [KAISE CHALTA HAI]: if (_registers[inst.DstReg] < (ulong)(uint)inst.Immediate) pc += 1 + offset else pc++.
                // [OUTPUT / SIDE EFFECT]: Branch evaluated.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_JLT_IMM:
                    if (_registers[inst.DstReg] < (ulong)(uint)inst.Immediate)
                    {
                        pc += 1 + inst.Offset;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                // [KYA KARTA HAI]: BPF_EXIT execute karta hai: program terminates and returns R0 action code.
                // [KYU ZAROORI HAI]: Returns final XDP verdict (Pass, Drop, Tx).
                // [KAISE CHALTA HAI]: Casts _registers[0] to XdpAction and returns immediately.
                // [OUTPUT / SIDE EFFECT]: Halts VM and yields result.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_EXIT:
                    return (XdpAction)_registers[0];

                // [KYA KARTA HAI]: Unknown instruction safeguard.
                // [KYU ZAROORI HAI]: Prevents illegal VM execution.
                // [KAISE CHALTA HAI]: Returns XdpAction.Aborted.
                // [OUTPUT / SIDE EFFECT]: Error drop.
                // [VARIATIONS TO TRY]: Throw exception.
                default:
                    return XdpAction.Aborted;
            }
        }

        // [KYA KARTA HAI]: Fall-through / step-exhaustion safeguard: returns XdpAction.Aborted.
        // [KYU ZAROORI HAI]: If program exceeded step budget without explicit exit.
        // [KAISE CHALTA HAI]: Returns Aborted.
        // [OUTPUT / SIDE EFFECT]: Abort drop.
        // [VARIATIONS TO TRY]: None.
        return XdpAction.Aborted;
    }
}
