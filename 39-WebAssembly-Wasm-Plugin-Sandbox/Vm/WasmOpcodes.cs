// [KYA KARTA HAI]: Wasm Virtual Machine namespace define karta hai
// [KYU ZAROORI HAI]: Wasm bytecode opcodes aur instruction representations ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project ke VM engine directory ko organize karta hai
// [OUTPUT / SIDE EFFECT]: Opcodes registered in KiranaHub.WasmSandbox.Vm
// [VARIATIONS TO TRY]: KiranaHub.Wasm.Bytecode namespace
namespace KiranaHub.WasmSandbox.Vm;

// [KYA KARTA HAI]: Standard WebAssembly 1.0 binary bytecode opcode enumeration
// [KYU ZAROORI HAI]: Bytecode parser aur interpreter me standard byte instructions ko map karne ke liye
// [KAISE CHALTA HAI]: Byte values correspond exactly to Wasm MVP binary specification
// [OUTPUT / SIDE EFFECT]: Standard Wasm instruction codes defined
// [VARIATIONS TO TRY]: Add SIMD / v128 opcodes
public enum WasmOpcode : byte
{
    // [KYA KARTA HAI]: Unreachable execution trap opcode (0x00)
    // [KYU ZAROORI HAI]: Fatal failure trap trigger karne ke liye
    // [KAISE CHALTA HAI]: Immediate VM trap throw karta hai
    // [OUTPUT / SIDE EFFECT]: Execution aborted
    // [VARIATIONS TO TRY]: Return error code instead of trap
    Unreachable = 0x00,

    // [KYA KARTA HAI]: No operation opcode (0x01)
    // [KYU ZAROORI HAI]: Alignment padding ya placeholders ke liye
    // [KAISE CHALTA HAI]: PC advances without doing anything
    // [OUTPUT / SIDE EFFECT]: Consumes 1 gas unit
    // [VARIATIONS TO TRY]: Compiler dead code elimination
    Nop = 0x01,

    // [KYA KARTA HAI]: End of block or function marker (0x0B)
    // [KYU ZAROORI HAI]: Control flow block termination indicate karne ke liye
    // [KAISE CHALTA HAI]: Pops control frame and resumes caller
    // [OUTPUT / SIDE EFFECT]: Exits block or function
    // [VARIATIONS TO TRY]: Multi-value return block
    End = 0x0B,

    // [KYA KARTA HAI]: Unconditional branch jump (0x0C)
    // [KYU ZAROORI HAI]: Loop back edges aur unconditional jumps ke liye
    // [KAISE CHALTA HAI]: Jumps to target instruction index
    // [OUTPUT / SIDE EFFECT]: Changes program counter PC
    // [VARIATIONS TO TRY]: Branch table lookup
    Br = 0x0C,

    // [KYA KARTA HAI]: Conditional branch if non-zero (0x0D)
    // [KYU ZAROORI HAI]: If-else conditional branching implement karne ke liye
    // [KAISE CHALTA HAI]: Pops top of stack; agar non-zero ho to branch karta hai
    // [OUTPUT / SIDE EFFECT]: Conditional PC jump
    // [VARIATIONS TO TRY]: Branch prediction hints
    BrIf = 0x0D,

    // [KYA KARTA HAI]: Function return opcode (0x0F)
    // [KYU ZAROORI HAI]: Function execution complete karke caller frame par return karne ke liye
    // [KAISE CHALTA HAI]: Restores caller PC and base pointer
    // [OUTPUT / SIDE EFFECT]: Returns to caller
    // [VARIATIONS TO TRY]: Tail call optimization
    Return = 0x0F,

    // [KYA KARTA HAI]: Function call opcode (0x10)
    // [KYU ZAROORI HAI]: Guest function ya host imported function invoke karne ke liye
    // [KAISE CHALTA HAI]: Pushes call frame and branches to function index
    // [OUTPUT / SIDE EFFECT]: Call stack depth increases
    // [VARIATIONS TO TRY]: Call indirect via table
    Call = 0x10,

    // [KYA KARTA HAI]: Drop top stack operand opcode (0x1A)
    // [KYU ZAROORI HAI]: Unused return values ya intermediate calculations discard karne ke liye
    // [KAISE CHALTA HAI]: Pops and ignores top of stack
    // [OUTPUT / SIDE EFFECT]: Stack size decrements by 1
    // [VARIATIONS TO TRY]: Validate stack not empty
    Drop = 0x1A,

    // [KYA KARTA HAI]: Get local variable opcode (0x20)
    // [KYU ZAROORI HAI]: Function parameter ya local variable value stack par push karne ke liye
    // [KAISE CHALTA HAI]: Reads local at index and pushes to stack
    // [OUTPUT / SIDE EFFECT]: Stack grows by 1
    // [VARIATIONS TO TRY]: Direct register allocation
    LocalGet = 0x20,

    // [KYA KARTA HAI]: Set local variable opcode (0x21)
    // [KYU ZAROORI HAI]: Stack value ko local variable me store karne ke liye
    // [KAISE CHALTA HAI]: Pops value from stack and writes to local slot
    // [OUTPUT / SIDE EFFECT]: Local updated; stack shrinks
    // [VARIATIONS TO TRY]: Type safety validation
    LocalSet = 0x21,

    // [KYA KARTA HAI]: Read 32-bit integer from linear memory opcode (0x28)
    // [KYU ZAROORI HAI]: Sandboxed isolated linear memory se data fetch karne ke liye
    // [KAISE CHALTA HAI]: Pops memory offset, reads 4 bytes, pushes int32 to stack
    // [OUTPUT / SIDE EFFECT]: Bounds check verified; memory read executed
    // [VARIATIONS TO TRY]: Unaligned memory read
    I32Load = 0x28,

    // [KYA KARTA HAI]: Write 32-bit integer to linear memory opcode (0x36)
    // [KYU ZAROORI HAI]: Sandboxed isolated linear memory me data store karne ke liye
    // [KAISE CHALTA HAI]: Pops value and memory offset, writes 4 bytes to linear memory
    // [OUTPUT / SIDE EFFECT]: Linear memory mutated within bounds
    // [VARIATIONS TO TRY]: Read-only memory protect
    I32Store = 0x36,

    // [KYA KARTA HAI]: Push 32-bit integer constant opcode (0x41)
    // [KYU ZAROORI HAI]: Literal numerical values (prices, counts, thresholds) stack par laane ke liye
    // [KAISE CHALTA HAI]: Decodes immediate LEB128 integer and pushes to stack
    // [OUTPUT / SIDE EFFECT]: Immediate value on operand stack
    // [VARIATIONS TO TRY]: 64-bit integer constant I64Const
    I32Const = 0x41,

    // [KYA KARTA HAI]: Equal to zero comparison opcode (0x45)
    // [KYU ZAROORI HAI]: Boolean NOT ya zero equality check ke liye
    // [KAISE CHALTA HAI]: Pops value; pushes 1 if 0, else 0
    // [OUTPUT / SIDE EFFECT]: Boolean integer on stack
    // [VARIATIONS TO TRY]: Direct test instruction
    I32Eqz = 0x45,

    // [KYA KARTA HAI]: Equality comparison opcode (0x46)
    // [KYU ZAROORI HAI]: Two values equality verify karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a == b ? 1 : 0)
    // [OUTPUT / SIDE EFFECT]: Comparison result pushed
    // [VARIATIONS TO TRY]: Type-specialized equality
    I32Eq = 0x46,

    // [KYA KARTA HAI]: Signed less-than comparison opcode (0x48)
    // [KYU ZAROORI HAI]: Price and stock thresholds evaluate karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a < b ? 1 : 0)
    // [OUTPUT / SIDE EFFECT]: Comparison boolean pushed
    // [VARIATIONS TO TRY]: Unsigned comparison I32LtU
    I32LtS = 0x48,

    // [KYA KARTA HAI]: Signed greater-than comparison opcode (0x4A)
    // [KYU ZAROORI HAI]: Order subtotal > discount minimum check karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a > b ? 1 : 0)
    // [OUTPUT / SIDE EFFECT]: Comparison boolean pushed
    // [VARIATIONS TO TRY]: Unsigned comparison I32GtU
    I32GtS = 0x4A,

    // [KYA KARTA HAI]: Signed greater-or-equal comparison opcode (0x4E)
    // [KYU ZAROORI HAI]: Minimum qualifying order thresholds evaluate karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a >= b ? 1 : 0)
    // [OUTPUT / SIDE EFFECT]: Comparison boolean pushed
    // [VARIATIONS TO TRY]: I32LeS
    I32GeS = 0x4E,

    // [KYA KARTA HAI]: 32-bit integer addition opcode (0x6A)
    // [KYU ZAROORI HAI]: Prices aur quantities sum karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a + b) with two's complement wrap-around
    // [OUTPUT / SIDE EFFECT]: Sum on stack
    // [VARIATIONS TO TRY]: Saturating addition
    I32Add = 0x6A,

    // [KYA KARTA HAI]: 32-bit integer subtraction opcode (0x6B)
    // [KYU ZAROORI HAI]: Discount deduction aur balance computation ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a - b)
    // [OUTPUT / SIDE EFFECT]: Difference on stack
    // [VARIATIONS TO TRY]: Checked subtraction
    I32Sub = 0x6B,

    // [KYA KARTA HAI]: 32-bit integer multiplication opcode (0x6C)
    // [KYU ZAROORI HAI]: Percentage discounts aur unit pricing calculate karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; pushes (a * b)
    // [OUTPUT / SIDE EFFECT]: Product on stack
    // [VARIATIONS TO TRY]: 64-bit intermediate multiplication
    I32Mul = 0x6C,

    // [KYA KARTA HAI]: 32-bit signed integer division opcode (0x6D)
    // [KYU ZAROORI HAI]: Discount percentages (e.g. total / 100) compute karne ke liye
    // [KAISE CHALTA HAI]: Pops b and a; checks division by zero and pushes (a / b)
    // [OUTPUT / SIDE EFFECT]: Quotient on stack
    // [VARIATIONS TO TRY]: Trap on division by zero
    I32DivS = 0x6D
}

// [KYA KARTA HAI]: Executable Wasm instruction struct with opcode and immediate operand
// [KYU ZAROORI HAI]: Bytecode representation ko structured and fast-to-execute format me hold karne ke liye
// [KAISE CHALTA HAI]: Holds Opcode and 32-bit Immediate argument
// [OUTPUT / SIDE EFFECT]: Single instruction representation
// [VARIATIONS TO TRY]: Packed struct with unions for float immediates
public readonly struct WasmInstruction
{
    // [KYA KARTA HAI]: Opcode instruction type property
    // [KYU ZAROORI HAI]: Interpreter switch statement me opcode identify karne ke liye
    // [KAISE CHALTA HAI]: Readonly WasmOpcode field
    // [OUTPUT / SIDE EFFECT]: Opcode ready
    // [VARIATIONS TO TRY]: Byte representation
    public WasmOpcode Opcode { get; }

    // [KYA KARTA HAI]: Immediate numerical argument (e.g., constant value, local index, branch target)
    // [KYU ZAROORI HAI]: Instructions like I32Const, LocalGet, Call require parameter
    // [KAISE CHALTA HAI]: Readonly 32-bit integer
    // [OUTPUT / SIDE EFFECT]: Immediate operand ready
    // [VARIATIONS TO TRY]: Multi-operand array
    public int Immediate { get; }

    // [KYA KARTA HAI]: WasmInstruction constructor
    // [KYU ZAROORI HAI]: Instruction instance create karne ke liye
    // [KAISE CHALTA HAI]: Assigns opcode and immediate
    // [OUTPUT / SIDE EFFECT]: Configured instruction
    // [VARIATIONS TO TRY]: Factory helper methods
    public WasmInstruction(WasmOpcode opcode, int immediate = 0)
    {
        Opcode = opcode;
        Immediate = immediate;
    }
}
