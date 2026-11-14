namespace KiranaHub.ZkSnarks.Circuit;

/// <summary>
/// A Linear Combination of circuit variables: \sum (coeff_i * wire_i).
/// In R1CS, vectors A, B, and C are each linear combinations over the witness vector s.
/// </summary>
public sealed class LinearCombination
{
    // [KYA KARTA HAI]: Variable index se FieldElement coefficient ka mapping dictionary.
    // [KYU ZAROORI HAI]: Sparse representation memory efficient hoti hai kyunki har gate me 2-3 variables hi involve hote hain.
    // [KAISE CHALTA HAI]: Dictionary<int, FieldElement> maps wire index -> coefficient.
    // [OUTPUT / SIDE EFFECT]: In-memory sparse vector.
    // [VARIATIONS TO TRY]: Parallel arrays of int[] and FieldElement[] for cache locality.
    private readonly Dictionary<int, FieldElement> _terms = new();

    // [KYA KARTA HAI]: Linear combination me ek term add karta hai: coeff * wire[varIndex].
    // [KYU ZAROORI HAI]: Circuit build karte waqt linear equations construct karna.
    // [KAISE CHALTA HAI]: Agar variable already exists toh coefficients add hote hain modulo p.
    // [OUTPUT / SIDE EFFECT]: Sparse term added or accumulated.
    // [VARIATIONS TO TRY]: SubtractTerm method.
    public LinearCombination AddTerm(int varIndex, FieldElement coeff)
    {
        if (coeff.Value == 0) return this;

        if (_terms.TryGetValue(varIndex, out var existing))
        {
            var sum = existing + coeff;
            if (sum.Value == 0)
            {
                _terms.Remove(varIndex);
            }
            else
            {
                _terms[varIndex] = sum;
            }
        }
        else
        {
            _terms[varIndex] = coeff;
        }

        return this;
    }

    // [KYA KARTA HAI]: Witness vector s ke sath dot product calculate karta hai: <L, s> = \sum (coeff_i * s_i).
    // [KYU ZAROORI HAI]: Constraint satisfaction test aur proving time par polynomial value nikalne ke liye.
    // [KAISE CHALTA HAI]: Sparse terms iterate karke multiply aur sum karta hai modulo p.
    // [OUTPUT / SIDE EFFECT]: Scalar FieldElement dot product.
    // [VARIATIONS TO TRY]: SIMD vectorization over dense arrays.
    public FieldElement Evaluate(IReadOnlyList<FieldElement> witnessVector)
    {
        FieldElement sum = FieldElement.Zero;
        foreach (var (varIndex, coeff) in _terms)
        {
            if (varIndex < 0 || varIndex >= witnessVector.Count)
            {
                throw new IndexOutOfRangeException($"Variable index {varIndex} out of bounds for witness vector of size {witnessVector.Count}");
            }
            sum += coeff * witnessVector[varIndex];
        }
        return sum;
    }

    // [KYA KARTA HAI]: Internal terms mapping expose karta hai.
    // [KYU ZAROORI HAI]: Matrix serialization aur CRS polynomial synthesis ke liye.
    // [KAISE CHALTA HAI]: Returns dictionary key-value collection.
    // [OUTPUT / SIDE EFFECT]: IReadOnlyDictionary of terms.
    // [VARIATIONS TO TRY]: ReadOnlySpan representation.
    public IReadOnlyDictionary<int, FieldElement> Terms => _terms;
}

/// <summary>
/// Rank-1 Constraint System (R1CS) Single Constraint:
/// Enforces the algebraic equation: &lt;A, s&gt; * &lt;B, s&gt; = &lt;C, s&gt;
/// where s is the complete witness vector [1, publicInputs..., privateWitness...].
/// </summary>
public sealed class R1csConstraint
{
    // [KYA KARTA HAI]: Left-hand linear combination vector A.
    // [KYU ZAROORI HAI]: Multiplication gate ka pehla operand.
    // [KAISE CHALTA HAI]: LinearCombination instance.
    // [OUTPUT / SIDE EFFECT]: Vector A.
    // [VARIATIONS TO TRY]: Compressed bitmask representation.
    public LinearCombination A { get; }

    // [KYA KARTA HAI]: Right-hand linear combination vector B.
    // [KYU ZAROORI HAI]: Multiplication gate ka doosra operand.
    // [KAISE CHALTA HAI]: LinearCombination instance.
    // [OUTPUT / SIDE EFFECT]: Vector B.
    // [VARIATIONS TO TRY]: Constant operand specialization.
    public LinearCombination B { get; }

    // [KYA KARTA HAI]: Output linear combination vector C.
    // [KYU ZAROORI HAI]: Multiplication gate ka expected result.
    // [KAISE CHALTA HAI]: LinearCombination instance.
    // [OUTPUT / SIDE EFFECT]: Vector C.
    // [VARIATIONS TO TRY]: Multi-target linear combination.
    public LinearCombination C { get; }

    // [KYA KARTA HAI]: Human-readable description/label for debugging circuits.
    // [KYU ZAROORI HAI]: Jab circuit me koi constraint violate ho toh exact line ya logic identify karna.
    // [KAISE CHALTA HAI]: String label stored in field.
    // [OUTPUT / SIDE EFFECT]: Constraint name tag.
    // [VARIATIONS TO TRY]: Source code file/line tagging.
    public string Description { get; }

    // [KYA KARTA HAI]: R1CS constraint constructor.
    // [KYU ZAROORI HAI]: <A,s> * <B,s> = <C,s> equation define karna.
    // [KAISE CHALTA HAI]: Initializes A, B, C linear combinations.
    // [OUTPUT / SIDE EFFECT]: R1csConstraint instance created.
    // [VARIATIONS TO TRY]: Builder pattern fluent chaining.
    public R1csConstraint(LinearCombination a, LinearCombination b, LinearCombination c, string description = "")
    {
        A = a;
        B = b;
        C = c;
        Description = description;
    }

    // [KYA KARTA HAI]: Verify karta hai ki kya witness vector s iss constraint ko satisfy karta hai: <A,s> * <B,s> == <C,s>.
    // [KYU ZAROORI HAI]: Prover circuit validation aur witness generation verification.
    // [KAISE CHALTA HAI]: A.Evaluate(s) * B.Evaluate(s) == C.Evaluate(s).
    // [OUTPUT / SIDE EFFECT]: Boolean true if satisfied, false if violated.
    // [VARIATIONS TO TRY]: Return difference delta for numerical debugging.
    public bool IsSatisfied(IReadOnlyList<FieldElement> witnessVector)
    {
        FieldElement evalA = A.Evaluate(witnessVector);
        FieldElement evalB = B.Evaluate(witnessVector);
        FieldElement evalC = C.Evaluate(witnessVector);

        return (evalA * evalB) == evalC;
    }
}
