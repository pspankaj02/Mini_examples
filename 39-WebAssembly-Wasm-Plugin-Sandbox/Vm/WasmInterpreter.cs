// [KYA KARTA HAI]: System collection generic namespace import karta hai
// [KYU ZAROORI HAI]: Host imported functions dictionary aur instruction lists ke liye
// [KAISE CHALTA HAI]: Namespace mapping provides dictionary types
// [OUTPUT / SIDE EFFECT]: Collections available
// [VARIATIONS TO TRY]: System.Collections.Immutable
namespace KiranaHub.WasmSandbox.Vm;

// [KYA KARTA HAI]: Custom exception thrown when untrusted script exceeds allocated CPU gas limit
// [KYU ZAROORI HAI]: Infinite loops (while(true)) aur CPU denial-of-service (DoS) attacks ko instantly trap karne ke liye
// [KAISE CHALTA HAI]: Instruction interpreter loop me gas <= 0 detect hote hi throw hota hai
// [OUTPUT / SIDE EFFECT]: Host thread protected from hanging; execution aborted safely
// [VARIATIONS TO TRY]: Attach consumed gas and instruction trace in exception
public class GasExhaustedException : Exception
{
    // [KYA KARTA HAI]: GasExhaustedException constructor
    // [KYU ZAROORI HAI]: Audit trace ke sath detailed message dene ke liye
    // [KAISE CHALTA HAI]: Formats message with initial gas budget and PC location
    // [OUTPUT / SIDE EFFECT]: Exception initialized
    // [VARIATIONS TO TRY]: Log security alert to SIEM
    public GasExhaustedException(long gasLimit, int pc, WasmOpcode opcode)
        : base($"WASM Gas Limit Exhausted: Script exceeded gas limit of {gasLimit:N0} units at Program Counter {pc} executing opcode {opcode}. Execution forcefully aborted.")
    {
    }
}

// [KYA KARTA HAI]: In-Process WebAssembly Stack-Based Bytecode Interpreter & Sandbox Engine
// [KYU ZAROORI HAI]: Untrusted merchant coupon code aur pricing logic ko isolated and deterministic sandbox me run karne ke liye
// [KAISE CHALTA HAI]: Instruction metering (gas limits), linear memory bounds, aur controlled host imports ke sath execution loop chalaata hai
// [OUTPUT / SIDE EFFECT]: Completely safe execution; zero thread locking and zero host memory compromise
// [VARIATIONS TO TRY]: JIT compiler using System.Reflection.Emit
public class WasmInterpreter
{
    // [KYA KARTA HAI]: Sandboxed isolated linear memory instance
    // [KYU ZAROORI HAI]: Memory loads and stores ko 64KB pages me isolate karne ke liye
    // [KAISE CHALTA HAI]: Readonly WasmLinearMemory reference
    // [OUTPUT / SIDE EFFECT]: Sandboxed memory access
    // [VARIATIONS TO TRY]: Multiple memory instances
    private readonly WasmLinearMemory _memory;

    // [KYA KARTA HAI]: Host imported function delegates table
    // [KYU ZAROORI HAI]: Guest script ko strictly controlled host capabilities (read subtotal, log promo) provide karne ke liye
    // [KAISE CHALTA HAI]: Map of host function index to Func<int, int>
    // [OUTPUT / SIDE EFFECT]: Controlled guest-to-host syscalls
    // [VARIATIONS TO TRY]: Multi-argument host function signatures
    private readonly Dictionary<int, Func<int, int>> _hostFunctions = new();

    // [KYA KARTA HAI]: Pre-allocated value stack array (1024 slots)
    // [KYU ZAROORI HAI]: Evaluation operand stack ko zero heap allocation ke sath maintain karne ke liye
    // [KAISE CHALTA HAI]: Fixed integer array acting as LIFO stack
    // [OUTPUT / SIDE EFFECT]: Stack memory fixed on heap
    // [VARIATIONS TO TRY]: Expandable stack
    private readonly int[] _valueStack = new int[1024];

    // [KYA KARTA HAI]: Stack pointer index
    // [KYU ZAROORI HAI]: Current top of operand stack track karne ke liye
    // [KAISE CHALTA HAI]: Integer index incremented on push, decremented on pop
    // [OUTPUT / SIDE EFFECT]: Stack top maintained
    // [VARIATIONS TO TRY]: Stack overflow guard check
    private int _sp;

    // [KYA KARTA HAI]: WasmInterpreter constructor
    // [KYU ZAROORI HAI]: Linear memory aur host dependencies attach karne ke liye
    // [KAISE CHALTA HAI]: Assigns memory instance
    // [OUTPUT / SIDE EFFECT]: Interpreter ready
    // [VARIATIONS TO TRY]: Default linear memory creation
    public WasmInterpreter(WasmLinearMemory memory)
    {
        _memory = memory;
    }

    // [KYA KARTA HAI]: Linear memory property expose karta hai
    // [KYU ZAROORI HAI]: Host ko guest memory me initial arguments (cart items) write karne ke liye
    // [KAISE CHALTA HAI]: Returns _memory
    // [OUTPUT / SIDE EFFECT]: Memory accessible
    // [VARIATIONS TO TRY]: Readonly memory view
    public WasmLinearMemory Memory => _memory;

    // [KYA KARTA HAI]: Controlled host function register karta hai
    // [KYU ZAROORI HAI]: Safe guest-to-host bridge APIs define karne ke liye
    // [KAISE CHALTA HAI]: Maps functionIndex to implementation delegate
    // [OUTPUT / SIDE EFFECT]: Host function available for Call opcode
    // [VARIATIONS TO TRY]: Security permission checks per function
    public void RegisterHostFunction(int functionIndex, Func<int, int> hostFunction)
    {
        _hostFunctions[functionIndex] = hostFunction;
    }

    // [KYA KARTA HAI]: Opcode ke according gas cost calculate karta hai
    // [KYU ZAROORI HAI]: Instruction execution cost deterministic rakhne ke liye
    // [KAISE CHALTA HAI]: Arithmetic = 1, Branch/Call = 2, Memory = 3
    // [OUTPUT / SIDE EFFECT]: Returns gas cost integer
    // [VARIATIONS TO TRY]: Tune cost based on CPU instruction cycles
    public static int GetGasCost(WasmOpcode opcode) => opcode switch
    {
        WasmOpcode.I32Load or WasmOpcode.I32Store => 3,
        WasmOpcode.Call or WasmOpcode.Br or WasmOpcode.BrIf => 2,
        _ => 1
    };

    // [KYA KARTA HAI]: Value stack par integer push karta hai
    // [KYU ZAROORI HAI]: Operands store karne ke liye
    // [KAISE CHALTA HAI]: _valueStack[_sp++] = value
    // [OUTPUT / SIDE EFFECT]: Value added to stack
    // [VARIATIONS TO TRY]: Throw on stack overflow
    private void Push(int value)
    {
        if (_sp >= _valueStack.Length)
        {
            throw new InvalidOperationException("WASM Stack Overflow: Exceeded value stack capacity");
        }
        _valueStack[_sp++] = value;
    }

    // [KYA KARTA HAI]: Value stack se top integer pop karta hai
    // [KYU ZAROORI HAI]: Instruction operands retrieve karne ke liye
    // [KAISE CHALTA HAI]: returns _valueStack[--_sp]
    // [OUTPUT / SIDE EFFECT]: Value removed from stack
    // [VARIATIONS TO TRY]: Throw on stack underflow
    private int Pop()
    {
        if (_sp <= 0)
        {
            throw new InvalidOperationException("WASM Stack Underflow: Attempted to pop from empty stack");
        }
        return _valueStack[--_sp];
    }

    // [KYA KARTA HAI]: Main Wasm bytecode execution method with gas metering
    // [KYU ZAROORI HAI]: Untrusted merchant instructions ko execute karke output return karne ke liye
    // [KAISE CHALTA HAI]: Program counter loop chalaata hai, gas deduct karta hai, instructions evaluate karta hai
    // [OUTPUT / SIDE EFFECT]: Returns final computation result or throws GasExhaustedException / WasmMemoryOutOfBoundsException
    // [VARIATIONS TO TRY]: Execution trace debugger
    public int Execute(IReadOnlyList<WasmInstruction> instructions, int[] initialLocals, long gasLimit, out long gasConsumed)
    {
        // [KYA KARTA HAI]: Stack pointer reset karta hai
        // [KYU ZAROORI HAI]: Clean operand stack state ensure karne ke liye
        // [KAISE CHALTA HAI]: _sp = 0
        // [OUTPUT / SIDE EFFECT]: Fresh stack ready
        // [VARIATIONS TO TRY]: Save previous stack on call
        _sp = 0;

        // [KYA KARTA HAI]: Local variables array copy karta hai
        // [KYU ZAROORI HAI]: Function parameters aur local variables maintain karne ke liye
        // [KAISE CHALTA HAI]: Array.Clone on initialLocals
        // [OUTPUT / SIDE EFFECT]: Local variables frame ready
        // [VARIATIONS TO TRY]: Fixed register frame
        int[] locals = (int[])initialLocals.Clone();

        // [KYA KARTA HAI]: Gas remaining tracker initialize karta hai
        // [KYU ZAROORI HAI]: CPU budget metering ke liye
        // [KAISE CHALTA HAI]: long gas = gasLimit
        // [OUTPUT / SIDE EFFECT]: Gas budget active
        // [VARIATIONS TO TRY]: Microsecond timeout timer alongside gas
        long gasRemaining = gasLimit;

        // [KYA KARTA HAI]: Program counter (PC) variable initialize karta hai
        // [KYU ZAROORI HAI]: Next instruction address track karne ke liye
        // [KAISE CHALTA HAI]: int pc = 0
        // [OUTPUT / SIDE EFFECT]: Execution starts at instruction 0
        // [VARIATIONS TO TRY]: Function entrypoint index
        int pc = 0;

        // [KYA KARTA HAI]: Interpreter execution loop
        // [KYU ZAROORI HAI]: End of instructions ya Return opcode aane tak execute karne ke liye
        // [KAISE CHALTA HAI]: while (pc < instructions.Count)
        // [OUTPUT / SIDE EFFECT]: Executes instructions sequentially or via branch jumps
        // [VARIATIONS TO TRY]: Threaded interpreter dispatch table
        while (pc < instructions.Count)
        {
            // [KYA KARTA HAI]: Current instruction fetch karta hai
            // [KYU ZAROORI HAI]: Opcode inspect aur evaluate karne ke liye
            // [KAISE CHALTA HAI]: WasmInstruction instr = instructions[pc]
            // [OUTPUT / SIDE EFFECT]: Current instruction fetched
            // [VARIATIONS TO TRY]: Direct span pointer
            WasmInstruction instr = instructions[pc];

            // [KYA KARTA HAI]: Instruction gas cost determine karta hai
            // [KYU ZAROORI HAI]: Gas budget me se deduct karne ke liye
            // [KAISE CHALTA HAI]: GetGasCost(instr.Opcode)
            // [OUTPUT / SIDE EFFECT]: Gas cost computed
            // [VARIATIONS TO TRY]: Custom dynamic gas cost
            int cost = GetGasCost(instr.Opcode);

            // [KYA KARTA HAI]: Gas remaining check karta hai
            // [KYU ZAROORI HAI]: CPU runaway infinite loop detect karke process freeze hone se bachane ke liye
            // [KAISE CHALTA HAI]: if (gasRemaining < cost) throw GasExhaustedException
            // [OUTPUT / SIDE EFFECT]: Aborts script if gas exhausted
            // [VARIATIONS TO TRY]: Log audit event
            if (gasRemaining < cost)
            {
                throw new GasExhaustedException(gasLimit, pc, instr.Opcode);
            }

            // [KYA KARTA HAI]: Gas budget deduct karta hai
            // [KYU ZAROORI HAI]: Running gas expenditure track karne ke liye
            // [KAISE CHALTA HAI]: gasRemaining -= cost
            // [OUTPUT / SIDE EFFECT]: Gas decremented
            // [VARIATIONS TO TRY]: Atomic decrement
            gasRemaining -= cost;

            // [KYA KARTA HAI]: Opcode evaluation switch statement
            // [KYU ZAROORI HAI]: WebAssembly instructions ko execute karne ke liye
            // [KAISE CHALTA HAI]: switch (instr.Opcode)
            // [OUTPUT / SIDE EFFECT]: Stack and memory mutated according to Wasm spec
            // [VARIATIONS TO TRY]: Direct jump table in IL
            switch (instr.Opcode)
            {
                case WasmOpcode.Nop:
                    pc++;
                    break;

                case WasmOpcode.I32Const:
                    Push(instr.Immediate);
                    pc++;
                    break;

                case WasmOpcode.LocalGet:
                    Push(locals[instr.Immediate]);
                    pc++;
                    break;

                case WasmOpcode.LocalSet:
                    locals[instr.Immediate] = Pop();
                    pc++;
                    break;

                case WasmOpcode.I32Load:
                    int loadAddr = Pop() + instr.Immediate;
                    Push(_memory.ReadInt32(loadAddr));
                    pc++;
                    break;

                case WasmOpcode.I32Store:
                    int valToStore = Pop();
                    int storeAddr = Pop() + instr.Immediate;
                    _memory.WriteInt32(storeAddr, valToStore);
                    pc++;
                    break;

                case WasmOpcode.I32Add:
                    Push(Pop() + Pop());
                    pc++;
                    break;

                case WasmOpcode.I32Sub:
                    int subB = Pop();
                    int subA = Pop();
                    Push(subA - subB);
                    pc++;
                    break;

                case WasmOpcode.I32Mul:
                    Push(Pop() * Pop());
                    pc++;
                    break;

                case WasmOpcode.I32DivS:
                    int divB = Pop();
                    int divA = Pop();
                    Push(divB == 0 ? 0 : divA / divB);
                    pc++;
                    break;

                case WasmOpcode.I32GtS:
                    int gtB = Pop();
                    int gtA = Pop();
                    Push(gtA > gtB ? 1 : 0);
                    pc++;
                    break;

                case WasmOpcode.I32GeS:
                    int geB = Pop();
                    int geA = Pop();
                    Push(geA >= geB ? 1 : 0);
                    pc++;
                    break;

                case WasmOpcode.I32LtS:
                    int ltB = Pop();
                    int ltA = Pop();
                    Push(ltA < ltB ? 1 : 0);
                    pc++;
                    break;

                case WasmOpcode.I32Eq:
                    Push(Pop() == Pop() ? 1 : 0);
                    pc++;
                    break;

                case WasmOpcode.I32Eqz:
                    Push(Pop() == 0 ? 1 : 0);
                    pc++;
                    break;

                case WasmOpcode.Br:
                    // Immediate represents target instruction PC
                    pc = instr.Immediate;
                    break;

                case WasmOpcode.BrIf:
                    int condition = Pop();
                    if (condition != 0)
                    {
                        pc = instr.Immediate;
                    }
                    else
                    {
                        pc++;
                    }
                    break;

                case WasmOpcode.Call:
                    // Host imported function call
                    int hostFnIndex = instr.Immediate;
                    if (_hostFunctions.TryGetValue(hostFnIndex, out Func<int, int>? hostFn))
                    {
                        int arg = Pop();
                        int res = hostFn(arg);
                        Push(res);
                        pc++;
                    }
                    else
                    {
                        throw new InvalidOperationException($"WASM Unknown Host Function: Index {hostFnIndex} is not registered");
                    }
                    break;

                case WasmOpcode.Drop:
                    Pop();
                    pc++;
                    break;

                case WasmOpcode.Return:
                case WasmOpcode.End:
                    gasConsumed = gasLimit - gasRemaining;
                    return _sp > 0 ? Pop() : 0;

                default:
                    throw new NotSupportedException($"WASM Opcode {instr.Opcode} (0x{(byte)instr.Opcode:X2}) is not implemented");
            }
        }

        // [KYA KARTA HAI]: Total consumed gas compute karta hai
        // [KYU ZAROORI HAI]: Telemetry aur billing metrics ke liye
        // [KAISE CHALTA HAI]: gasLimit - gasRemaining
        // [OUTPUT / SIDE EFFECT]: gasConsumed output assigned
        // [VARIATIONS TO TRY]: Record in audit trail
        gasConsumed = gasLimit - gasRemaining;

        // [KYA KARTA HAI]: Stack top return value fetch karta hai
        // [KYU ZAROORI HAI]: Function execution result return karne ke liye
        // [KAISE CHALTA HAI]: Returns Pop() if stack not empty, else 0
        // [OUTPUT / SIDE EFFECT]: Result returned
        // [VARIATIONS TO TRY]: Multi-value tuple return
        return _sp > 0 ? Pop() : 0;
    }
}
