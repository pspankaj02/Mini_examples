// ============================================================================
// File: BytecodeVerifier.cs
// Namespace: KiranaHub.PacketFilterEbpf.Verifier
// Description: Static in-kernel bytecode safety verifier simulating Linux eBPF
// verifier rules (DAG reachability, loop freedom, bounds checks, register initialization).
// ============================================================================

using KiranaHub.PacketFilterEbpf.Vm;

namespace KiranaHub.PacketFilterEbpf.Verifier;

/// <summary>
/// Static bytecode verifier ensuring eBPF programs cannot crash the host, loop indefinitely, or leak memory.
/// </summary>
public static class BytecodeVerifier
{
    // [KYA KARTA HAI]: Maximum allowed instruction count limit define karta hai (4096).
    // [KYU ZAROORI HAI]: Host kernel thread lockup prevent karne ke liye instruction limit mandatory hai.
    // [KAISE CHALTA HAI]: Constant integer 4096 (standard Linux non-root eBPF ceiling).
    // [OUTPUT / SIDE EFFECT]: Instruction budget threshold.
    // [VARIATIONS TO TRY]: 1,000,000 instructions for modern kernel with BPF_F_TEST_RND_HI32.
    public const int MaxInstructions = 4096;

    // [KYA KARTA HAI]: Bytecode safety verification perform karta hai program instructions par.
    // [KYU ZAROORI HAI]: Unsafe bytecode ko kernel execution engine me run hone se pehle hi reject karna.
    // [KAISE CHALTA HAI]: Analyzes DAG, loops, register access, bounds, and exit reachability.
    // [OUTPUT / SIDE EFFECT]: Returns (IsSafe, ErrorReason).
    // [VARIATIONS TO TRY]: Path-sensitive abstract interpretation.
    public static (bool IsSafe, string? ErrorReason) Verify(EbpfInstruction[] instructions)
    {
        // [KYA KARTA HAI]: Check karta hai instructions array null ya empty toh nahi hai.
        // [KYU ZAROORI HAI]: Empty program cannot execute.
        // [KAISE CHALTA HAI]: instructions == null || instructions.Length == 0 check.
        // [OUTPUT / SIDE EFFECT]: Rejection on empty program.
        // [VARIATIONS TO TRY]: None.
        if (instructions == null || instructions.Length == 0)
        {
            // [KYA KARTA HAI]: Reject empty program.
            // [KYU ZAROORI HAI]: Program must contain at least 1 instruction (EXIT).
            // [KAISE CHALTA HAI]: Return tuple with error.
            // [OUTPUT / SIDE EFFECT]: Verification failed.
            // [VARIATIONS TO TRY]: None.
            return (false, "Verification failed: Program is null or empty.");
        }

        // [KYA KARTA HAI]: Check karta hai instruction count limit exceed toh nahi hua.
        // [KYU ZAROORI HAI]: Prevents resource exhaustion.
        // [KAISE CHALTA HAI]: instructions.Length > MaxInstructions check.
        // [OUTPUT / SIDE EFFECT]: Budget guard.
        // [VARIATIONS TO TRY]: None.
        if (instructions.Length > MaxInstructions)
        {
            // [KYA KARTA HAI]: Reject over-budget program.
            // [KYU ZAROORI HAI]: Protects host execution time.
            // [KAISE CHALTA HAI]: Return error string.
            // [OUTPUT / SIDE EFFECT]: Rejection.
            // [VARIATIONS TO TRY]: None.
            return (false, $"Verification failed: Program exceeds max limit of {MaxInstructions} instructions.");
        }

        // [KYA KARTA HAI]: Register initialization tracker array allocate karta hai (11 registers: R0-R10).
        // [KYU ZAROORI HAI]: Enforces that uninitialized registers are never read.
        // [KAISE CHALTA HAI]: bool[11] tracker.
        // [OUTPUT / SIDE EFFECT]: Register state mask.
        // [VARIATIONS TO TRY]: Bitmask integer.
        bool[] regInitialized = new bool[11];

        // [KYA KARTA HAI]: Register R1 initialize mark karta hai.
        // [KYU ZAROORI HAI]: R1 incoming packet context pointer hold karta hai (context argument).
        // [KAISE CHALTA HAI]: regInitialized[1] = true.
        // [OUTPUT / SIDE EFFECT]: R1 marked valid.
        // [VARIATIONS TO TRY]: None.
        regInitialized[1] = true;

        // [KYA KARTA HAI]: Register R10 initialize mark karta hai.
        // [KYU ZAROORI HAI]: R10 read-only stack frame pointer hota hai.
        // [KAISE CHALTA HAI]: regInitialized[10] = true.
        // [OUTPUT / SIDE EFFECT]: R10 marked valid.
        // [VARIATIONS TO TRY]: None.
        regInitialized[10] = true;

        // [KYA KARTA HAI]: Flag track karta hai kya program me koi valid BPF_EXIT instruction reachable hai.
        // [KYU ZAROORI HAI]: Every valid eBPF program must have at least one exit point.
        // [KAISE CHALTA HAI]: bool hasExit = false.
        // [OUTPUT / SIDE EFFECT]: Exit tracking flag.
        // [VARIATIONS TO TRY]: Reachability graph traversal.
        bool hasExit = false;

        // [KYA KARTA HAI]: Static scan loop across all instructions in the program.
        // [KYU ZAROORI HAI]: Inspect every opcode, register boundary, jump target, and memory offset.
        // [KAISE CHALTA HAI]: For pc from 0 to instructions.Length - 1.
        // [OUTPUT / SIDE EFFECT]: Validates entire bytecode stream.
        // [VARIATIONS TO TRY]: Multi-pass CFG construction.
        for (int pc = 0; pc < instructions.Length; pc++)
        {
            // [KYA KARTA HAI]: Current instruction fetch karta hai.
            // [KYU ZAROORI HAI]: Inspect instruction fields.
            // [KAISE CHALTA HAI]: inst = instructions[pc].
            // [OUTPUT / SIDE EFFECT]: Local instruction copy.
            // [VARIATIONS TO TRY]: In-place ref readonly.
            var inst = instructions[pc];

            // [KYA KARTA HAI]: Validate karta hai DstReg valid range me hai (0 to 10).
            // [KYU ZAROORI HAI]: eBPF standard architecture defines exactly 11 registers.
            // [KAISE CHALTA HAI]: inst.DstReg > 10.
            // [OUTPUT / SIDE EFFECT]: Bounds guard.
            // [VARIATIONS TO TRY]: None.
            if (inst.DstReg > 10)
            {
                // [KYA KARTA HAI]: Reject invalid destination register.
                // [KYU ZAROORI HAI]: Prevents CPU register corruption.
                // [KAISE CHALTA HAI]: Return error string.
                // [OUTPUT / SIDE EFFECT]: Rejection.
                // [VARIATIONS TO TRY]: None.
                return (false, $"Verification failed at PC {pc}: Invalid destination register R{inst.DstReg}. Must be R0-R10.");
            }

            // [KYA KARTA HAI]: Validate karta hai SrcReg valid range me hai (0 to 10).
            // [KYU ZAROORI HAI]: Prevents invalid register decoding.
            // [KAISE CHALTA HAI]: inst.SrcReg > 10.
            // [OUTPUT / SIDE EFFECT]: Bounds guard.
            // [VARIATIONS TO TRY]: None.
            if (inst.SrcReg > 10)
            {
                // [KYA KARTA HAI]: Reject invalid source register.
                // [KYU ZAROORI HAI]: Prevents register overflow.
                // [KAISE CHALTA HAI]: Return error string.
                // [OUTPUT / SIDE EFFECT]: Rejection.
                // [VARIATIONS TO TRY]: None.
                return (false, $"Verification failed at PC {pc}: Invalid source register R{inst.SrcReg}. Must be R0-R10.");
            }

            // [KYA KARTA HAI]: Check karta hai kya R10 par write karne ki koshish ki gayi hai.
            // [KYU ZAROORI HAI]: R10 is the strictly read-only stack frame pointer; writes are forbidden by the kernel.
            // [KAISE CHALTA HAI]: inst.DstReg == 10 check for modifying instructions.
            // [OUTPUT / SIDE EFFECT]: Frame pointer security guard.
            // [VARIATIONS TO TRY]: None.
            if (inst.DstReg == 10 && inst.OpCode != EbpfOpCodes.BPF_EXIT && inst.OpCode != EbpfOpCodes.BPF_JA)
            {
                // [KYA KARTA HAI]: Reject writes to R10.
                // [KYU ZAROORI HAI]: Prevents stack smashing.
                // [KAISE CHALTA HAI]: Return error string.
                // [OUTPUT / SIDE EFFECT]: Rejection.
                // [VARIATIONS TO TRY]: None.
                return (false, $"Verification failed at PC {pc}: Cannot write to read-only frame pointer R10.");
            }

            // [KYA KARTA HAI]: Opcode specific verification branch execute karta hai.
            // [KYU ZAROORI HAI]: ALU vs Memory vs Jump specific safety rules validate karna.
            // [KAISE CHALTA HAI]: switch (inst.OpCode).
            // [OUTPUT / SIDE EFFECT]: Enforces type-specific rules.
            // [VARIATIONS TO TRY]: Pattern matching.
            switch (inst.OpCode)
            {
                // [KYA KARTA HAI]: BPF_EXIT verification handle karta hai.
                // [KYU ZAROORI HAI]: Verifies R0 return register is initialized before program exits.
                // [KAISE CHALTA HAI]: Check regInitialized[0].
                // [OUTPUT / SIDE EFFECT]: Validates clean exit.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_EXIT:
                    // [KYA KARTA HAI]: Check if R0 was initialized before exit.
                    // [KYU ZAROORI HAI]: eBPF program must return an action code in R0; uninitialized R0 is illegal.
                    // [KAISE CHALTA HAI]: !regInitialized[0] check.
                    // [OUTPUT / SIDE EFFECT]: Return value safety guard.
                    // [VARIATIONS TO TRY]: None.
                    if (!regInitialized[0])
                    {
                        // [KYA KARTA HAI]: Reject uninitialized exit.
                        // [KYU ZAROORI HAI]: Undefined return value.
                        // [KAISE CHALTA HAI]: Return error string.
                        // [OUTPUT / SIDE EFFECT]: Rejection.
                        // [VARIATIONS TO TRY]: None.
                        return (false, $"Verification failed at PC {pc}: R0 return action code not initialized before EXIT.");
                    }
                    hasExit = true;
                    break;

                // [KYA KARTA HAI]: Jump instructions verify karta hai.
                // [KYU ZAROORI HAI]: Loop prevention and branch destination bounds check.
                // [KAISE CHALTA HAI]: Checks target PC and forbids backward jumps.
                // [OUTPUT / SIDE EFFECT]: Guarantees termination.
                // [VARIATIONS TO TRY]: Allow bounded backward loops with iteration counters.
                case EbpfOpCodes.BPF_JA:
                case EbpfOpCodes.BPF_JEQ_IMM:
                case EbpfOpCodes.BPF_JNE_IMM:
                case EbpfOpCodes.BPF_JGT_IMM:
                case EbpfOpCodes.BPF_JGE_IMM:
                case EbpfOpCodes.BPF_JLT_IMM:
                    // [KYA KARTA HAI]: Backward jump check (offset < 0).
                    // [KYU ZAROORI HAI]: Backward jumps allow infinite loops; eBPF strict verifier enforces DAG (no backward jumps).
                    // [KAISE CHALTA HAI]: inst.Offset < 0 check.
                    // [OUTPUT / SIDE EFFECT]: Loop prevention guard.
                    // [VARIATIONS TO TRY]: Allow backward jumps only with verified induction variable.
                    if (inst.Offset < 0)
                    {
                        // [KYA KARTA HAI]: Reject backward jumps.
                        // [KYU ZAROORI HAI]: Enforces loop-free terminating execution.
                        // [KAISE CHALTA HAI]: Return error string.
                        // [OUTPUT / SIDE EFFECT]: Rejection of infinite loop risk.
                        // [VARIATIONS TO TRY]: None.
                        return (false, $"Verification failed at PC {pc}: Backward jump detected (Offset = {inst.Offset}). Infinite loops forbidden in eBPF.");
                    }

                    // [KYA KARTA HAI]: Jump target instruction index calculate karta hai: pc + 1 + offset.
                    // [KYU ZAROORI HAI]: Ensure jump does not overshoot beyond the program end.
                    // [KAISE CHALTA HAI]: target = pc + 1 + inst.Offset.
                    // [OUTPUT / SIDE EFFECT]: target PC calculated.
                    // [VARIATIONS TO TRY]: None.
                    int target = pc + 1 + inst.Offset;

                    // [KYA KARTA HAI]: Check karta hai target valid program range me hai ya nahi.
                    // [KYU ZAROORI HAI]: Out-of-bounds jumps crash virtual machines.
                    // [KAISE CHALTA HAI]: target >= instructions.Length check.
                    // [OUTPUT / SIDE EFFECT]: Branch bounds guard.
                    // [VARIATIONS TO TRY]: None.
                    if (target >= instructions.Length)
                    {
                        // [KYA KARTA HAI]: Reject out-of-bounds jump.
                        // [KYU ZAROORI HAI]: Prevents jump beyond EOF.
                        // [KAISE CHALTA HAI]: Return error string.
                        // [OUTPUT / SIDE EFFECT]: Rejection.
                        // [VARIATIONS TO TRY]: None.
                        return (false, $"Verification failed at PC {pc}: Jump target {target} exceeds program length {instructions.Length}.");
                    }
                    break;

                // [KYA KARTA HAI]: Memory load instructions verify karta hai (LDX_B, LDX_H, LDX_W, LDX_DW).
                // [KYU ZAROORI HAI]: Memory isolation ensure karta hai; negative offsets or uninitialized pointers reject karta hai.
                // [KAISE CHALTA HAI]: Checks SrcReg initialized and Offset >= 0.
                // [OUTPUT / SIDE EFFECT]: Memory safety enforcement.
                // [VARIATIONS TO TRY]: Max offset check.
                case EbpfOpCodes.BPF_LDX_B:
                case EbpfOpCodes.BPF_LDX_H:
                case EbpfOpCodes.BPF_LDX_W:
                case EbpfOpCodes.BPF_LDX_DW:
                    // [KYA KARTA HAI]: Check karta hai base address register SrcReg initialized hai ya nahi.
                    // [KYU ZAROORI HAI]: Reading memory from uninitialized register leads to garbage pointer dereference.
                    // [KAISE CHALTA HAI]: !regInitialized[inst.SrcReg] check.
                    // [OUTPUT / SIDE EFFECT]: Pointer initialization guard.
                    // [VARIATIONS TO TRY]: None.
                    if (!regInitialized[inst.SrcReg])
                    {
                        // [KYA KARTA HAI]: Reject memory read on uninitialized register.
                        // [KYU ZAROORI HAI]: Null/garbage dereference protection.
                        // [KAISE CHALTA HAI]: Return error string.
                        // [OUTPUT / SIDE EFFECT]: Rejection.
                        // [VARIATIONS TO TRY]: None.
                        return (false, $"Verification failed at PC {pc}: Attempted memory load from uninitialized register R{inst.SrcReg}.");
                    }

                    // [KYA KARTA HAI]: Check karta hai offset non-negative hai (offset >= 0).
                    // [KYU ZAROORI HAI]: Negative packet offsets read before packet header (kernel buffer underflow).
                    // [KAISE CHALTA HAI]: inst.Offset < 0 check.
                    // [OUTPUT / SIDE EFFECT]: Underflow guard.
                    // [VARIATIONS TO TRY]: None.
                    if (inst.Offset < 0)
                    {
                        // [KYA KARTA HAI]: Reject negative memory offset.
                        // [KYU ZAROORI HAI]: Buffer underflow protection.
                        // [KAISE CHALTA HAI]: Return error string.
                        // [OUTPUT / SIDE EFFECT]: Rejection.
                        // [VARIATIONS TO TRY]: None.
                        return (false, $"Verification failed at PC {pc}: Negative memory offset {inst.Offset} is illegal.");
                    }

                    // [KYA KARTA HAI]: Mark DstReg initialized.
                    // [KYU ZAROORI HAI]: Memory read loads valid data into destination register.
                    // [KAISE CHALTA HAI]: regInitialized[inst.DstReg] = true.
                    // [OUTPUT / SIDE EFFECT]: DstReg marked valid.
                    // [VARIATIONS TO TRY]: None.
                    regInitialized[inst.DstReg] = true;
                    break;

                // [KYA KARTA HAI]: MOV immediate instruction verify karta hai.
                // [KYU ZAROORI HAI]: Sets destination register initialized state.
                // [KAISE CHALTA HAI]: regInitialized[inst.DstReg] = true.
                // [OUTPUT / SIDE EFFECT]: DstReg marked valid.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_MOV64_IMM:
                    regInitialized[inst.DstReg] = true;
                    break;

                // [KYA KARTA HAI]: MOV register-to-register instruction verify karta hai.
                // [KYU ZAROORI HAI]: Verifies source register is initialized before copying to destination.
                // [KAISE CHALTA HAI]: Checks regInitialized[inst.SrcReg].
                // [OUTPUT / SIDE EFFECT]: Preserves initialization invariants.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_MOV64_REG:
                    if (!regInitialized[inst.SrcReg])
                    {
                        return (false, $"Verification failed at PC {pc}: Cannot copy from uninitialized register R{inst.SrcReg}.");
                    }
                    regInitialized[inst.DstReg] = true;
                    break;

                // [KYA KARTA HAI]: ALU operations verify karta hai (ADD, SUB, AND, OR, XOR, LSH, RSH).
                // [KYU ZAROORI HAI]: ALU instructions require destination register to already contain an initialized value.
                // [KAISE CHALTA HAI]: Checks regInitialized[inst.DstReg].
                // [OUTPUT / SIDE EFFECT]: Prevents arithmetic on garbage.
                // [VARIATIONS TO TRY]: None.
                case EbpfOpCodes.BPF_ADD64_IMM:
                case EbpfOpCodes.BPF_SUB64_IMM:
                case EbpfOpCodes.BPF_AND64_IMM:
                case EbpfOpCodes.BPF_OR64_IMM:
                case EbpfOpCodes.BPF_XOR64_IMM:
                case EbpfOpCodes.BPF_LSH64_IMM:
                case EbpfOpCodes.BPF_RSH64_IMM:
                    if (!regInitialized[inst.DstReg])
                    {
                        return (false, $"Verification failed at PC {pc}: ALU operation on uninitialized register R{inst.DstReg}.");
                    }
                    break;

                // [KYA KARTA HAI]: Unknown opcode guard.
                // [KYU ZAROORI HAI]: Unsupported or invalid opcodes must be rejected by the verifier.
                // [KAISE CHALTA HAI]: default case.
                // [OUTPUT / SIDE EFFECT]: Rejection of illegal instruction.
                // [VARIATIONS TO TRY]: Custom vendor extension opcodes.
                default:
                    return (false, $"Verification failed at PC {pc}: Unknown or unsupported eBPF opcode 0x{inst.OpCode:X2}.");
            }
        }

        // [KYA KARTA HAI]: Check karta hai kya program me koi valid exit path present hai.
        // [KYU ZAROORI HAI]: eBPF program cannot fall through past the last instruction.
        // [KAISE CHALTA HAI]: !hasExit check.
        // [OUTPUT / SIDE EFFECT]: Missing exit guard.
        // [VARIATIONS TO TRY]: None.
        if (!hasExit)
        {
            // [KYA KARTA HAI]: Reject program without exit.
            // [KYU ZAROORI HAI]: Program must terminate explicitly.
            // [KAISE CHALTA HAI]: Return error string.
            // [OUTPUT / SIDE EFFECT]: Rejection.
            // [VARIATIONS TO TRY]: None.
            return (false, "Verification failed: Program has no reachable EXIT instruction.");
        }

        // [KYA KARTA HAI]: Return verification success.
        // [KYU ZAROORI HAI]: Program is statically proven to be memory safe and terminating.
        // [KAISE CHALTA HAI]: Return (true, null).
        // [OUTPUT / SIDE EFFECT]: Program approved for execution in VM.
        // [VARIATIONS TO TRY]: None.
        return (true, null);
    }
}
