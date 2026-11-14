namespace KiranaHub.ZkSnarks.Circuit;

/// <summary>
/// Arithmetic Circuit Compiler and Constraint System Manager.
/// Allocates variable wires (one, public inputs, private witness),
/// registers R1CS constraints (&lt;A,s&gt; * &lt;B,s&gt; = &lt;C,s&gt;), and synthesizes witness assignments.
/// </summary>
public sealed class ArithmeticCircuit
{
    // [KYA KARTA HAI]: Constant one wire index (hamesha wire 0).
    // [KYU ZAROORI HAI]: R1CS matrix me constant terms (e.g. +5, 1 - b) express karne ke liye base anchor.
    // [KAISE CHALTA HAI]: Index 0 permanently reserved for 1.
    // [OUTPUT / SIDE EFFECT]: Constant 0 index.
    // [VARIATIONS TO TRY]: Configurable wire indices.
    public const int OneWire = 0;

    // [KYA KARTA HAI]: System ke total variables count (wires) track karta hai.
    // [KYU ZAROORI HAI]: Matrix columns aur witness vector ka exact dimension determine karna.
    // [KAISE CHALTA HAI]: New wire allocate hone par increment hota hai.
    // [OUTPUT / SIDE EFFECT]: Integer wire counter.
    // [VARIATIONS TO TRY]: Segmented wire pools for linear and non-linear gates.
    private int _nextVariableIndex = 1;

    // [KYA KARTA HAI]: Public input variables ki list.
    // [KYU ZAROORI HAI]: Verifier ko public inputs pata hote hain (e.g. minAge = 21) jabki private witness nahi!
    // [KAISE CHALTA HAI]: List of int variable indices.
    // [OUTPUT / SIDE EFFECT]: Public input wires tracked.
    // [VARIATIONS TO TRY]: Immutable array representation.
    private readonly List<int> _publicInputs = new();

    // [KYA KARTA HAI]: Private witness variables ki list.
    // [KYU ZAROORI HAI]: Prover ka confidential data (birth year, salary, bit-decomposition flags).
    // [KAISE CHALTA HAI]: List of int variable indices.
    // [OUTPUT / SIDE EFFECT]: Private witness wires tracked.
    // [VARIATIONS TO TRY]: Blinding factors appended as private auxiliary wires.
    private readonly List<int> _privateWitnesses = new();

    // [KYA KARTA HAI]: Circuit ke tamam R1CS constraints ki collection.
    // [KYU ZAROORI HAI]: Cryptographic proof generation aur mathematical verification ka core rulebook.
    // [KAISE CHALTA HAI]: List of R1csConstraint instances.
    // [OUTPUT / SIDE EFFECT]: Collection of algebraic equations.
    // [VARIATIONS TO TRY]: Flattened flat-buffer representation for GPU acceleration.
    private readonly List<R1csConstraint> _constraints = new();

    // [KYA KARTA HAI]: Naya public input variable allocate karta hai.
    // [KYU ZAROORI HAI]: Statements jaise "Threshold = 21" ko circuit me feed karna.
    // [KAISE CHALTA HAI]: Allocates variable index and registers in public inputs list.
    // [OUTPUT / SIDE EFFECT]: Returns assigned wire index.
    // [VARIATIONS TO TRY]: Named public variables with dictionary lookup.
    public int AllocatePublicInput(string name = "")
    {
        int index = _nextVariableIndex++;
        _publicInputs.Add(index);
        return index;
    }

    // [KYA KARTA HAI]: Naya private witness variable allocate karta hai.
    // [KYU ZAROORI HAI]: Secret customer data (e.g. birthYear = 2000, bank balance) ko wire assign karna.
    // [KAISE CHALTA HAI]: Allocates next index and registers in private witnesses list.
    // [OUTPUT / SIDE EFFECT]: Returns assigned wire index.
    // [VARIATIONS TO TRY]: Tagged sensitivity levels (HighPrivacy, IntermediateWire).
    public int AllocatePrivateWitness(string name = "")
    {
        int index = _nextVariableIndex++;
        _privateWitnesses.Add(index);
        return index;
    }

    // [KYA KARTA HAI]: Direct R1CS constraint add karta hai: <A,s> * <B,s> = <C,s>.
    // [KYU ZAROORI HAI]: Circuit me quadratic gates aur linear relationships enforce karna.
    // [KAISE CHALTA HAI]: R1csConstraint instance ko list me append karta hai.
    // [OUTPUT / SIDE EFFECT]: Constraint count increases by 1.
    // [VARIATIONS TO TRY]: Validation check that all variables exist in circuit.
    public void AddConstraint(LinearCombination a, LinearCombination b, LinearCombination c, string description = "")
    {
        _constraints.Add(new R1csConstraint(a, b, c, description));
    }

    // [KYA KARTA HAI]: Gadget: Enforce karta hai ki variable b binary ho: b \in {0, 1}.
    // [KYU ZAROORI HAI]: Bit decomposition me har bit 0 ya 1 hona strictly mandatory hai (b * (1 - b) = 0).
    // [KAISE CHALTA HAI]: A = [b], B = [1*one - 1*b], C = [0]. Equation: b * (1 - b) = 0.
    // [OUTPUT / SIDE EFFECT]: One quadratic constraint added to circuit.
    // [VARIATIONS TO TRY]: Multi-bit range proofs using lookups.
    public void EnforceBoolean(int bitVar, string description = "Boolean Constraint")
    {
        var a = new LinearCombination().AddTerm(bitVar, FieldElement.One);
        var b = new LinearCombination().AddTerm(OneWire, FieldElement.One).AddTerm(bitVar, -FieldElement.One);
        var c = new LinearCombination(); // 0

        AddConstraint(a, b, c, description);
    }

    // [KYA KARTA HAI]: Gadget: Value ko N-bit binary decomposition me enforce karta hai.
    // [KYU ZAROORI HAI]: Finite field me "val >= 0" prove karna (Range Proof). Agar negative ho toh wrap-around detect ho jata hai!
    // [KAISE CHALTA HAI]: val = \sum (2^i * bit_i) aur har bit_i par EnforceBoolean chalata hai.
    // [OUTPUT / SIDE EFFECT]: N + 1 constraints added.
    // [VARIATIONS TO TRY]: 16-bit, 32-bit, or 64-bit decomposition.
    public void EnforceBitDecomposition(int valueVar, int[] bitVars, string description = "Bit Decomposition")
    {
        var bitSum = new LinearCombination();
        long powerOfTwo = 1;

        for (int i = 0; i < bitVars.Length; i++)
        {
            EnforceBoolean(bitVars[i], $"{description}: Bit {i} boolean check");
            bitSum.AddTerm(bitVars[i], new FieldElement(powerOfTwo));
            powerOfTwo *= 2;
        }

        // Enforce: 1 * (\sum 2^i * bit_i) = valueVar
        var one = new LinearCombination().AddTerm(OneWire, FieldElement.One);
        var targetVal = new LinearCombination().AddTerm(valueVar, FieldElement.One);

        AddConstraint(one, bitSum, targetVal, $"{description}: Sum equals value");
    }

    // [KYA KARTA HAI]: Gadget: Enforce karta hai: privateValue >= publicThreshold.
    // [KYU ZAROORI HAI]: Age >= 21 ya Balance >= 5000 proof without revealing private value!
    // [KAISE CHALTA HAI]: diff = privateVal - threshold; diff ko 8-bit decomposition me satisfy hona padega!
    // [OUTPUT / SIDE EFFECT]: Sets up difference equation and positive range proof constraints.
    // [VARIATIONS TO TRY]: 16-bit range for wider numerical limits.
    public void EnforceGreaterOrEqual(int privateValVar, int publicThresholdVar, int diffVar, int[] diffBits, string description = "Greater-or-Equal")
    {
        // Enforce: 1 * (privateVal - publicThreshold) = diffVar
        var one = new LinearCombination().AddTerm(OneWire, FieldElement.One);
        var diffEquation = new LinearCombination()
            .AddTerm(privateValVar, FieldElement.One)
            .AddTerm(publicThresholdVar, -FieldElement.One);
        var targetDiff = new LinearCombination().AddTerm(diffVar, FieldElement.One);

        AddConstraint(one, diffEquation, targetDiff, $"{description}: diff = private - threshold");

        // Enforce: diffVar is a valid positive 8-bit integer (0 <= diff <= 255)
        EnforceBitDecomposition(diffVar, diffBits, $"{description}: diff is positive 8-bit range");
    }

    // [KYA KARTA HAI]: Pure witness vector s ko circuit ke tamam constraints ke against test karta hai.
    // [KYU ZAROORI HAI]: Prover ko pata chale ki witness valid hai ya kahan violation hua hai.
    // [KAISE CHALTA HAI]: Har constraint ka IsSatisfied check karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns true if 100% constraints satisfied, false if any fails.
    // [VARIATIONS TO TRY]: Output detailed diagnostic report of failing constraint names.
    public bool IsWitnessValid(IReadOnlyList<FieldElement> witnessVector, out string? failureReason)
    {
        failureReason = null;
        for (int i = 0; i < _constraints.Count; i++)
        {
            var c = _constraints[i];
            if (!c.IsSatisfied(witnessVector))
            {
                failureReason = $"Constraint #{i} failed ({c.Description}): <A,s>*<B,s> != <C,s>";
                return false;
            }
        }
        return true;
    }

    // [KYA KARTA HAI]: Total wires count property.
    // [KYU ZAROORI HAI]: Matrix dimension sizing.
    // [KAISE CHALTA HAI]: Field getter.
    // [OUTPUT / SIDE EFFECT]: _nextVariableIndex.
    // [VARIATIONS TO TRY]: Variable count breakdown.
    public int TotalVariables => _nextVariableIndex;

    // [KYA KARTA HAI]: Public inputs indices list return karta hai.
    // [KYU ZAROORI HAI]: Verifier ko public inputs extract karne ke liye.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: IReadOnlyList of wire indices.
    // [VARIATIONS TO TRY]: ReadOnlySpan.
    public IReadOnlyList<int> PublicInputs => _publicInputs;

    // [KYA KARTA HAI]: Private witness indices list return karta hai.
    // [KYU ZAROORI HAI]: Prover synthesis pipeline.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: IReadOnlyList of private wire indices.
    // [VARIATIONS TO TRY]: Sensitive memory zeroing.
    public IReadOnlyList<int> PrivateWitnesses => _privateWitnesses;

    // [KYA KARTA HAI]: Constraints list property.
    // [KYU ZAROORI HAI]: Proving key setup aur polynomial interpolations.
    // [KAISE CHALTA HAI]: Property getter.
    // [OUTPUT / SIDE EFFECT]: IReadOnlyList of R1csConstraint.
    // [VARIATIONS TO TRY]: Immutable collection.
    public IReadOnlyList<R1csConstraint> Constraints => _constraints;

    // -------------------------------------------------------------------------
    // PRE-BUILT CIRCUITS FOR QUICK-COMMERCE DOMAIN
    // -------------------------------------------------------------------------

    // [KYA KARTA HAI]: Zero-Knowledge Age Verification Circuit build karta hai (Age >= MinAge).
    // [KYU ZAROORI HAI]: Alcohol/tobacco delivery me customer privacy protect karna (DPDP Act compliance).
    // [KAISE CHALTA HAI]: Allocates wires for currentYear, minAge, birthYear, age, diff, and 8 diff bits.
    // [OUTPUT / SIDE EFFECT]: Configured ArithmeticCircuit and AgeCircuitWires bundle.
    // [VARIATIONS TO TRY]: Birth date timestamp with day precision.
    public static (ArithmeticCircuit Circuit, AgeCircuitWires Wires) BuildAgeVerificationCircuit()
    {
        var circuit = new ArithmeticCircuit();

        // Public Inputs: Verifier knows these
        int currentYearVar = circuit.AllocatePublicInput("CurrentYear");
        int minAgeVar = circuit.AllocatePublicInput("MinAgeThreshold");

        // Private Witnesses: ONLY Prover (Customer) knows these
        int birthYearVar = circuit.AllocatePrivateWitness("BirthYear");
        int ageVar = circuit.AllocatePrivateWitness("CalculatedAge");
        int diffVar = circuit.AllocatePrivateWitness("AgeDifference");

        // 8 bits for range proof (supports age differences up to 255 years)
        int[] diffBits = new int[8];
        for (int i = 0; i < 8; i++)
        {
            diffBits[i] = circuit.AllocatePrivateWitness($"DiffBit_{i}");
        }

        // Constraint 1: 1 * (currentYear - birthYear) = ageVar
        var one = new LinearCombination().AddTerm(OneWire, FieldElement.One);
        var ageCalc = new LinearCombination()
            .AddTerm(currentYearVar, FieldElement.One)
            .AddTerm(birthYearVar, -FieldElement.One);
        var ageTarget = new LinearCombination().AddTerm(ageVar, FieldElement.One);
        circuit.AddConstraint(one, ageCalc, ageTarget, "Age = CurrentYear - BirthYear");

        // Constraint 2: Age >= MinAge via bit-decomposition of diff = (age - minAge)
        circuit.EnforceGreaterOrEqual(ageVar, minAgeVar, diffVar, diffBits, "Age >= MinAge Proof");

        var wires = new AgeCircuitWires(currentYearVar, minAgeVar, birthYearVar, ageVar, diffVar, diffBits);
        return (circuit, wires);
    }
}

/// <summary>
/// Container holding wire indices for the Zero-Knowledge Age Verification Circuit.
/// </summary>
public sealed record AgeCircuitWires(
    int CurrentYearWire,
    int MinAgeThresholdWire,
    int BirthYearWire,
    int AgeWire,
    int DiffWire,
    int[] DiffBitsWires);
