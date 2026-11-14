// ============================================================================
// File: ReedSolomonCodec.cs
// Namespace: KiranaHub.ObjectStorage.Erasure
// Description: Systematic Cauchy-matrix Reed-Solomon Erasure Coding encoder
// and decoder over GF(2^8) with Gaussian elimination matrix inversion.
// ============================================================================

using KiranaHub.ObjectStorage.Galois;

namespace KiranaHub.ObjectStorage.Erasure;

/// <summary>
/// Systematic Reed-Solomon Codec allowing recovery of k data shards from any k of (k + m) shards.
/// </summary>
public sealed class ReedSolomonCodec
{
    // [KYA KARTA HAI]: Number of original data shards 'k' store karta hai.
    // [KYU ZAROORI HAI]: Encoding and decoding equations me matrix dimension k par depend karti hai.
    // [KAISE CHALTA HAI]: Readonly integer property set during constructor.
    // [OUTPUT / SIDE EFFECT]: Immutable shard count.
    // [VARIATIONS TO TRY]: 4, 8, or 16 data shards.
    public int DataShards { get; }

    // [KYA KARTA HAI]: Number of parity shards 'm' store karta hai.
    // [KYU ZAROORI HAI]: Maximum tolerable disk failures = m shards.
    // [KAISE CHALTA HAI]: Readonly integer property set during constructor.
    // [OUTPUT / SIDE EFFECT]: Fault tolerance limit.
    // [VARIATIONS TO TRY]: 2, 3, or 4 parity shards.
    public int ParityShards { get; }

    // [KYA KARTA HAI]: Total shards count = k + m store karta hai.
    // [KYU ZAROORI HAI]: Total distributed storage nodes/disks count represent karta hai.
    // [KAISE CHALTA HAI]: Sum of DataShards + ParityShards.
    // [OUTPUT / SIDE EFFECT]: Total fleet shard count.
    // [VARIATIONS TO TRY]: Dynamic configuration based on SLA tier.
    public int TotalShards => DataShards + ParityShards;

    // [KYA KARTA HAI]: (k + m) x k Generator Matrix G store karta hai.
    // [KYU ZAROORI HAI]: Systematic generator matrix top k x k identity aur bottom m x k Cauchy matrix contain karta hai.
    // [KAISE CHALTA HAI]: 2D byte array [TotalShards, DataShards].
    // [OUTPUT / SIDE EFFECT]: Precomputed generator matrix.
    // [VARIATIONS TO TRY]: Vandermonde matrix instead of Cauchy.
    private readonly byte[,] _matrix;

    // [KYA KARTA HAI]: Constructor ReedSolomonCodec initialize karta hai aur generator matrix build karta hai.
    // [KYU ZAROORI HAI]: Codec instance create hote hi encoding/decoding ke liye mathematical matrix ready ho jaye.
    // [KAISE CHALTA HAI]: k aur m validate karta hai, memory allocate karta hai, aur BuildCauchyMatrix run karta hai.
    // [OUTPUT / SIDE EFFECT]: Validated codec instance ready for operations.
    // [VARIATIONS TO TRY]: Standard 4+2 or 8+4 quick-commerce configurations.
    public ReedSolomonCodec(int dataShards, int parityShards)
    {
        // [KYA KARTA HAI]: Validate karta hai dataShards positive hai ya nahi.
        // [KYU ZAROORI HAI]: At least 1 data shard hona mathematical requirement hai.
        // [KAISE CHALTA HAI]: dataShards <= 0 check karta hai.
        // [OUTPUT / SIDE EFFECT]: ArgumentOutOfRangeException guard.
        // [VARIATIONS TO TRY]: Minimum 2 shards for real distributed setups.
        if (dataShards <= 0)
        {
            // [KYA KARTA HAI]: Throw exception.
            // [KYU ZAROORI HAI]: Zero or negative shard count illegal hai.
            // [KAISE CHALTA HAI]: Throws ArgumentOutOfRangeException.
            // [OUTPUT / SIDE EFFECT]: Fail fast.
            // [VARIATIONS TO TRY]: Specialized error handling.
            throw new ArgumentOutOfRangeException(nameof(dataShards), "Data shards must be > 0.");
        }

        // [KYA KARTA HAI]: Validate karta hai parityShards positive hai ya nahi.
        // [KYU ZAROORI HAI]: Zero parity shards provide no fault tolerance.
        // [KAISE CHALTA HAI]: parityShards <= 0 check karta hai.
        // [OUTPUT / SIDE EFFECT]: Guard validation.
        // [VARIATIONS TO TRY]: Check parityShards <= 16.
        if (parityShards <= 0)
        {
            // [KYA KARTA HAI]: Throw exception.
            // [KYU ZAROORI HAI]: Parity shards zero hone par erasure coding meaningless hai.
            // [KAISE CHALTA HAI]: Throws ArgumentOutOfRangeException.
            // [OUTPUT / SIDE EFFECT]: Guard trigger.
            // [VARIATIONS TO TRY]: Custom exception type.
            throw new ArgumentOutOfRangeException(nameof(parityShards), "Parity shards must be > 0.");
        }

        // [KYA KARTA HAI]: Check karta hai k + m <= 256.
        // [KYU ZAROORI HAI]: GF(2^8) me Cauchy matrix elements disjoint subsets hone chahiye, max 256 elements in field.
        // [KAISE CHALTA HAI]: Comparison with 256.
        // [OUTPUT / SIDE EFFECT]: Prevents field exhaustion.
        // [VARIATIONS TO TRY]: Upgrade to GF(2^16) for > 256 disks.
        if (dataShards + parityShards > 256)
        {
            // [KYA KARTA HAI]: Throw exception on field limit exceed.
            // [KYU ZAROORI HAI]: GF(2^8) cannot support more than 256 distinct evaluation points.
            // [KAISE CHALTA HAI]: Throws ArgumentException.
            // [OUTPUT / SIDE EFFECT]: Prevents mathematical collision.
            // [VARIATIONS TO TRY]: 16-bit Galois field.
            throw new ArgumentException("Total shards (k + m) cannot exceed 256 in GF(2^8).");
        }

        // [KYA KARTA HAI]: Assign DataShards property.
        // [KYU ZAROORI HAI]: Retain configuration.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: DataShards set.
        // [VARIATIONS TO TRY]: Readonly record struct.
        DataShards = dataShards;

        // [KYA KARTA HAI]: Assign ParityShards property.
        // [KYU ZAROORI HAI]: Retain configuration.
        // [KAISE CHALTA HAI]: Direct assignment.
        // [OUTPUT / SIDE EFFECT]: ParityShards set.
        // [VARIATIONS TO TRY]: Dynamic config.
        ParityShards = parityShards;

        // [KYA KARTA HAI]: Allocate 2D matrix of size (k + m) x k.
        // [KYU ZAROORI HAI]: Har shard ka encoding vector represent karta hai.
        // [KAISE CHALTA HAI]: new byte[TotalShards, DataShards].
        // [OUTPUT / SIDE EFFECT]: Memory allocated.
        // [VARIATIONS TO TRY]: Flat 1D array for better cache locality.
        _matrix = new byte[TotalShards, DataShards];

        // [KYA KARTA HAI]: Generator matrix populate karta hai.
        // [KYU ZAROORI HAI]: Systematic Cauchy matrix calculate karta hai.
        // [KAISE CHALTA HAI]: Method invocation BuildCauchyMatrix.
        // [OUTPUT / SIDE EFFECT]: _matrix populated with identity and Cauchy rows.
        // [VARIATIONS TO TRY]: Precompiled matrix constants.
        BuildCauchyMatrix();
    }

    // [KYA KARTA HAI]: Generator matrix me top identity aur bottom Cauchy coefficients fill karta hai.
    // [KYU ZAROORI HAI]: Cauchy matrices guarantee karti hain ki koi bhi k x k submatrix non-singular (invertible) hogi.
    // [KAISE CHALTA HAI]: Top k rows me identity (1s on diagonal), bottom m rows me 1 / (x_i ^ y_j).
    // [OUTPUT / SIDE EFFECT]: Generator matrix fully filled.
    // [VARIATIONS TO TRY]: Vandermonde matrix with Gaussian reduction.
    private void BuildCauchyMatrix()
    {
        // [KYA KARTA HAI]: Identity matrix build karta hai top k rows me.
        // [KYU ZAROORI HAI]: Systematic code ensure karta hai ki pehle k shards exact raw data hon bina transformation cost ke.
        // [KAISE CHALTA HAI]: For i in [0..k-1], row i ke column i par 1 set karta hai.
        // [OUTPUT / SIDE EFFECT]: Top k x k becomes identity matrix.
        // [VARIATIONS TO TRY]: Non-systematic code (higher compute cost).
        for (int i = 0; i < DataShards; i++)
        {
            // [KYA KARTA HAI]: Diagonal element ko 1 set karta hai.
            // [KYU ZAROORI HAI]: I_k identity matrix requirement.
            // [KAISE CHALTA HAI]: _matrix[i, i] = 1.
            // [OUTPUT / SIDE EFFECT]: Matrix diagonal initialized.
            // [VARIATIONS TO TRY]: None needed.
            _matrix[i, i] = 1;
        }

        // [KYA KARTA HAI]: Cauchy parity rows build karta hai bottom m rows ke liye.
        // [KYU ZAROORI HAI]: Parity shards coefficients generate karta hai.
        // [KAISE CHALTA HAI]: X set = {0 .. m-1}, Y set = {m .. m+k-1}. Disjoint sets ensure zero denominator never happens.
        // [OUTPUT / SIDE EFFECT]: Bottom m rows populated with Cauchy values.
        // [VARIATIONS TO TRY]: Alternate disjoint point distributions.
        for (int i = 0; i < ParityShards; i++)
        {
            // [KYA KARTA HAI]: Point x_i select karta hai (offset i).
            // [KYU ZAROORI HAI]: Disjoint evaluation point for Cauchy parity row.
            // [KAISE CHALTA HAI]: byte cast of i.
            // [OUTPUT / SIDE EFFECT]: x_i defined.
            // [VARIATIONS TO TRY]: x_i = i + 128.
            byte xi = (byte)i;

            // [KYA KARTA HAI]: Loop through all k data shard columns.
            // [KYU ZAROORI HAI]: Har data shard ke saath Cauchy coefficient calculate karna hai.
            // [KAISE CHALTA HAI]: For j in [0..k-1].
            // [OUTPUT / SIDE EFFECT]: Iterates columns.
            // [VARIATIONS TO TRY]: Vectorized row computation.
            for (int j = 0; j < DataShards; j++)
            {
                // [KYA KARTA HAI]: Point y_j select karta hai (offset ParityShards + j).
                // [KYU ZAROORI HAI]: Disjoint from xi so xi ^ yj is never zero.
                // [KAISE CHALTA HAI]: byte cast of (ParityShards + j).
                // [OUTPUT / SIDE EFFECT]: y_j defined.
                // [VARIATIONS TO TRY]: Non-overlapping ranges.
                byte yj = (byte)(ParityShards + j);

                // [KYA KARTA HAI]: Inverted XOR distance 1 / (xi ^ yj) calculate karta hai in GF(2^8).
                // [KYU ZAROORI HAI]: Cauchy matrix fundamental definition: C_ij = 1 / (xi + yj).
                // [KAISE CHALTA HAI]: GaloisField256.Inverse((byte)(xi ^ yj)).
                // [OUTPUT / SIDE EFFECT]: Parity row entry generated.
                // [VARIATIONS TO TRY]: Cauchy with scaling factors.
                _matrix[DataShards + i, j] = GaloisField256.Inverse((byte)(xi ^ yj));
            }
        }
    }

    // [KYA KARTA HAI]: Original data shards se parity shards encode karta hai.
    // [KYU ZAROORI HAI]: Distributed storage me failure protection ke liye parity shards generate karna mandatory hai.
    // [KAISE CHALTA HAI]: Systematic code hone ke karan data shards already intact hote hain; parity shards matrix multiplication se compute hote hain.
    // [OUTPUT / SIDE EFFECT]: Populates all m parity shards arrays in-place.
    // [VARIATIONS TO TRY]: Multi-threaded shard encoding for multi-gigabyte files.
    public void Encode(byte[][] shards, int shardSize)
    {
        // [KYA KARTA HAI]: Validate karta hai shards array length TotalShards ke barabar hai.
        // [KYU ZAROORI HAI]: Array bounds violation prevent karta hai.
        // [KAISE CHALTA HAI]: shards.Length != TotalShards check.
        // [OUTPUT / SIDE EFFECT]: Safety guard.
        // [VARIATIONS TO TRY]: Span<Memory<byte>>.
        if (shards.Length != TotalShards)
        {
            // [KYA KARTA HAI]: Throw exception.
            // [KYU ZAROORI HAI]: Invalid shard buffer size.
            // [KAISE CHALTA HAI]: Throws ArgumentException.
            // [OUTPUT / SIDE EFFECT]: Prevents crash.
            // [VARIATIONS TO TRY]: Format error string.
            throw new ArgumentException($"Expected {TotalShards} shards buffers, but got {shards.Length}.");
        }

        // [KYA KARTA HAI]: Loop run karta hai har parity shard index ke liye (from 0 to ParityShards - 1).
        // [KYU ZAROORI HAI]: Har parity shard ko independently compute karna hota hai.
        // [KAISE CHALTA HAI]: p iterates through [0..m-1].
        // [OUTPUT / SIDE EFFECT]: Parity shards calculated one by one.
        // [VARIATIONS TO TRY]: Parallel.For(0, ParityShards, ...).
        for (int p = 0; p < ParityShards; p++)
        {
            // [KYA KARTA HAI]: Parity shard row index in generator matrix nikalta hai.
            // [KYU ZAROORI HAI]: Parity rows start after DataShards (k + p).
            // [KAISE CHALTA HAI]: matrixRow = DataShards + p.
            // [OUTPUT / SIDE EFFECT]: Row index identified.
            // [VARIATIONS TO TRY]: Direct 0-indexed parity matrix.
            int matrixRow = DataShards + p;

            // [KYA KARTA HAI]: Target parity shard byte buffer reference fetch karta hai.
            // [KYU ZAROORI HAI]: Computation direct target array me store hogi.
            // [KAISE CHALTA HAI]: targetShard = shards[matrixRow].
            // [OUTPUT / SIDE EFFECT]: Target buffer reference acquired.
            // [VARIATIONS TO TRY]: Span<byte> target.
            byte[] targetShard = shards[matrixRow];

            // [KYA KARTA HAI]: Target parity buffer ko zero se clear karta hai.
            // [KYU ZAROORI HAI]: XOR accumulation se pehle accumulator 0 hona mandatory hai.
            // [KAISE CHALTA HAI]: Array.Clear(targetShard, 0, shardSize).
            // [OUTPUT / SIDE EFFECT]: Clean parity buffer.
            // [VARIATIONS TO TRY]: Unsafe zero-memory.
            Array.Clear(targetShard, 0, shardSize);

            // [KYA KARTA HAI]: Har data shard d (from 0 to DataShards - 1) par iterate karta hai.
            // [KYU ZAROORI HAI]: Parity = Sum(Matrix[p, d] * Data[d]).
            // [KAISE CHALTA HAI]: Nested loop over data shards.
            // [OUTPUT / SIDE EFFECT]: Computes dot product.
            // [VARIATIONS TO TRY]: Loop interchange for cache optimization.
            for (int d = 0; d < DataShards; d++)
            {
                // [KYA KARTA HAI]: Cauchy generator matrix coefficient fetch karta hai.
                // [KYU ZAROORI HAI]: Field multiplication scalar factor.
                // [KAISE CHALTA HAI]: byte coeff = _matrix[matrixRow, d].
                // [OUTPUT / SIDE EFFECT]: Scalar coefficient byte.
                // [VARIATIONS TO TRY]: Skip if coeff == 0 (Cauchy never zero).
                byte coeff = _matrix[matrixRow, d];

                // [KYA KARTA HAI]: Source data shard buffer reference fetch karta hai.
                // [KYU ZAROORI HAI]: Data bytes read karne ke liye.
                // [KAISE CHALTA HAI]: dataShard = shards[d].
                // [OUTPUT / SIDE EFFECT]: Data shard reference.
                // [VARIATIONS TO TRY]: ReadOnlySpan<byte>.
                byte[] dataShard = shards[d];

                // [KYA KARTA HAI]: Har byte offset b (0 to shardSize - 1) par scalar multiply accumulate karta hai.
                // [KYU ZAROORI HAI]: Streaming Galois field vector dot product.
                // [KAISE CHALTA HAI]: target[b] ^= GaloisField256.Multiply(coeff, data[b]).
                // [OUTPUT / SIDE EFFECT]: Accumulates parity byte-by-byte.
                // [VARIATIONS TO TRY]: SIMD AVX2 / AVX-512 Galois Field multiplication.
                for (int b = 0; b < shardSize; b++)
                {
                    // [KYA KARTA HAI]: Galois multiply and XOR accumulate in target shard.
                    // [KYU ZAROORI HAI]: Linear combination in GF(2^8).
                    // [KAISE CHALTA HAI]: targetShard[b] = (byte)(targetShard[b] ^ GaloisField256.Multiply(coeff, dataShard[b])).
                    // [OUTPUT / SIDE EFFECT]: Parity byte updated.
                    // [VARIATIONS TO TRY]: Vectorized XOR accumulation.
                    targetShard[b] ^= GaloisField256.Multiply(coeff, dataShard[b]);
                }
            }
        }
    }

    // [KYA KARTA HAI]: Any k intact shards se missing data shards reconstruct karta hai.
    // [KYU ZAROORI HAI]: Disk crash ya corruption ke baad original quick-commerce data 100% bit-exact restore karna.
    // [KAISE CHALTA HAI]: Submatrix of available shards nikal kar Gaussian elimination se invert karta hai aur available shards se multiply karta hai.
    // [OUTPUT / SIDE EFFECT]: Reconstructs missing data shards in shards array in-place.
    // [VARIATIONS TO TRY]: Reconstruct only requested shards instead of all.
    public void Decode(byte[][] shards, bool[] shardPresent, int shardSize)
    {
        // [KYA KARTA HAI]: Count karta hai total present shards kitne hain.
        // [KYU ZAROORI HAI]: RS decoding ke liye at least DataShards (k) healthy shards hona strictly mandatory hai.
        // [KAISE CHALTA HAI]: Counts true entries in shardPresent.
        // [OUTPUT / SIDE EFFECT]: presentCount calculated.
        // [VARIATIONS TO TRY]: BitArray or bitmask.
        int presentCount = 0;

        // [KYA KARTA HAI]: Loop run karta hai through shardPresent array.
        // [KYU ZAROORI HAI]: Count healthy disks/shards.
        // [KAISE CHALTA HAI]: For i in [0..TotalShards-1].
        // [OUTPUT / SIDE EFFECT]: Counts presence.
        // [VARIATIONS TO TRY]: LINQ .Count(x => x).
        for (int i = 0; i < TotalShards; i++)
        {
            // [KYA KARTA HAI]: Increment if present.
            // [KYU ZAROORI HAI]: Count accumulation.
            // [KAISE CHALTA HAI]: Ternary check.
            // [OUTPUT / SIDE EFFECT]: Count increments.
            // [VARIATIONS TO TRY]: Unrolled branchless count.
            if (shardPresent[i]) presentCount++;
        }

        // [KYA KARTA HAI]: Check karta hai if presentCount < DataShards.
        // [KYU ZAROORI HAI]: Agar available shards k se kam hain to mathematically data irrecoverable hai (catastrophic failure).
        // [KAISE CHALTA HAI]: Comparison presentCount < DataShards.
        // [OUTPUT / SIDE EFFECT]: Irrecoverable error guard.
        // [VARIATIONS TO TRY]: Return error boolean instead of exception.
        if (presentCount < DataShards)
        {
            // [KYA KARTA HAI]: Throw exception on catastrophic loss.
            // [KYU ZAROORI HAI]: Insufficient shards for Reed-Solomon reconstruction.
            // [KAISE CHALTA HAI]: Throws InvalidOperationException.
            // [OUTPUT / SIDE EFFECT]: Halts decoding.
            // [VARIATIONS TO TRY]: Custom ErasureDecodingException.
            throw new InvalidOperationException($"Cannot reconstruct: Need at least {DataShards} shards, but only {presentCount} available.");
        }

        // [KYA KARTA HAI]: Check karta hai kya saare data shards already present hain.
        // [KYU ZAROORI HAI]: Agar pehle k shards intact hain to no decoding math is needed (fast path).
        // [KAISE CHALTA HAI]: Check shardPresent[0..k-1].
        // [OUTPUT / SIDE EFFECT]: Fast early exit.
        // [VARIATIONS TO TRY]: Skip parity validation if not requested.
        bool allDataPresent = true;

        // [KYA KARTA HAI]: Check first k shards presence.
        // [KYU ZAROORI HAI]: Detect if any data shard is missing.
        // [KAISE CHALTA HAI]: Loop 0 to DataShards - 1.
        // [OUTPUT / SIDE EFFECT]: Sets allDataPresent flag.
        // [VARIATIONS TO TRY]: Early break.
        for (int i = 0; i < DataShards; i++)
        {
            // [KYA KARTA HAI]: Check individual shard presence.
            // [KYU ZAROORI HAI]: Set flag to false if missing.
            // [KAISE CHALTA HAI]: If !shardPresent[i] then false.
            // [OUTPUT / SIDE EFFECT]: Flag updated.
            // [VARIATIONS TO TRY]: Break immediately.
            if (!shardPresent[i])
            {
                // [KYA KARTA HAI]: Set allDataPresent false.
                // [KYU ZAROORI HAI]: Indicates reconstruction math required.
                // [KAISE CHALTA HAI]: Assignment.
                // [OUTPUT / SIDE EFFECT]: Needs decoding path.
                // [VARIATIONS TO TRY]: Direct goto decode.
                allDataPresent = false;
                break;
            }
        }

        // [KYA KARTA HAI]: Agar saare data shards present hain to decoding bypass kar deta hai.
        // [KYU ZAROORI HAI]: Zero CPU overhead when reading healthy data in systematic RS.
        // [KAISE CHALTA HAI]: if (allDataPresent) return.
        // [OUTPUT / SIDE EFFECT]: Instant return.
        // [VARIATIONS TO TRY]: Verify parity checksums even if data intact.
        if (allDataPresent)
        {
            // [KYA KARTA HAI]: Return immediately.
            // [KYU ZAROORI HAI]: No missing data to reconstruct.
            // [KAISE CHALTA HAI]: Void return.
            // [OUTPUT / SIDE EFFECT]: 0 ms execution.
            // [VARIATIONS TO TRY]: None.
            return;
        }

        // [KYA KARTA HAI]: Available k shards ke indices select karta hai.
        // [KYU ZAROORI HAI]: Submatrix bananey ke liye pehle k available shards pick karte hain.
        // [KAISE CHALTA HAI]: Array of size DataShards filled with shard indices.
        // [OUTPUT / SIDE EFFECT]: selectedShardIndices array filled.
        // [VARIATIONS TO TRY]: Prioritize faster disks or local nodes.
        int[] selectedShardIndices = new int[DataShards];

        // [KYA KARTA HAI]: Counter variable for selected shards.
        // [KYU ZAROORI HAI]: Tracks how many shards picked so far.
        // [KAISE CHALTA HAI]: selected = 0.
        // [OUTPUT / SIDE EFFECT]: Initialized index.
        // [VARIATIONS TO TRY]: None.
        int selected = 0;

        // [KYA KARTA HAI]: Shards iterate karke pehle k healthy shards pick karta hai.
        // [KYU ZAROORI HAI]: Inversion submatrix construct karne ke liye k rows chahiye.
        // [KAISE CHALTA HAI]: Loop through TotalShards until selected == DataShards.
        // [OUTPUT / SIDE EFFECT]: Exactly k indices selected.
        // [VARIATIONS TO TRY]: Select lowest-latency disks first.
        for (int i = 0; i < TotalShards && selected < DataShards; i++)
        {
            // [KYA KARTA HAI]: If shard is present, record its index.
            // [KYU ZAROORI HAI]: Add to selected list.
            // [KAISE CHALTA HAI]: if (shardPresent[i]) selectedShardIndices[selected++] = i.
            // [OUTPUT / SIDE EFFECT]: Array index recorded.
            // [VARIATIONS TO TRY]: None.
            if (shardPresent[i])
            {
                // [KYA KARTA HAI]: Store index in array.
                // [KYU ZAROORI HAI]: Submatrix mapping.
                // [KAISE CHALTA HAI]: selectedShardIndices[selected++] = i.
                // [OUTPUT / SIDE EFFECT]: Index assigned and counter incremented.
                // [VARIATIONS TO TRY]: Direct submatrix extraction.
                selectedShardIndices[selected++] = i;
            }
        }

        // [KYA KARTA HAI]: k x k submatrix allocate karta hai from generator matrix.
        // [KYU ZAROORI HAI]: Selected rows ki matrix invert karke decoding matrix nikalenge.
        // [KAISE CHALTA HAI]: new byte[DataShards, DataShards].
        // [OUTPUT / SIDE EFFECT]: Submatrix memory allocated.
        // [VARIATIONS TO TRY]: Reusable scratchpad matrix.
        byte[,] subMatrix = new byte[DataShards, DataShards];

        // [KYA KARTA HAI]: Submatrix populate karta hai from _matrix corresponding to selected rows.
        // [KYU ZAROORI HAI]: Mathematical linear system A * Data = AvailableShards define karta hai.
        // [KAISE CHALTA HAI]: For each row r, copy _matrix[selectedShardIndices[r], c].
        // [OUTPUT / SIDE EFFECT]: subMatrix filled.
        // [VARIATIONS TO TRY]: Unsafe memory block copy.
        for (int r = 0; r < DataShards; r++)
        {
            // [KYA KARTA HAI]: Row index in original generator matrix fetch karta hai.
            // [KYU ZAROORI HAI]: Source row identification.
            // [KAISE CHALTA HAI]: srcRow = selectedShardIndices[r].
            // [OUTPUT / SIDE EFFECT]: Row mapped.
            // [VARIATIONS TO TRY]: None.
            int srcRow = selectedShardIndices[r];

            // [KYA KARTA HAI]: Column loop through 0 to DataShards - 1.
            // [KYU ZAROORI HAI]: Copy each column coefficient.
            // [KAISE CHALTA HAI]: For c in [0..k-1].
            // [OUTPUT / SIDE EFFECT]: Row elements copied.
            // [VARIATIONS TO TRY]: Buffer.BlockCopy.
            for (int c = 0; c < DataShards; c++)
            {
                // [KYA KARTA HAI]: Copy matrix entry.
                // [KYU ZAROORI HAI]: Build square linear equation matrix.
                // [KAISE CHALTA HAI]: subMatrix[r, c] = _matrix[srcRow, c].
                // [OUTPUT / SIDE EFFECT]: Entry assigned.
                // [VARIATIONS TO TRY]: None.
                subMatrix[r, c] = _matrix[srcRow, c];
            }
        }

        // [KYA KARTA HAI]: Gaussian elimination run karke subMatrix ka inverse nikalta hai.
        // [KYU ZAROORI HAI]: A^-1 * AvailableShards = DataShards; invert matrix decoding allow karta hai.
        // [KAISE CHALTA HAI]: InvertMatrix(subMatrix) executes in GF(2^8).
        // [OUTPUT / SIDE EFFECT]: Inverted k x k decoding matrix.
        // [VARIATIONS TO TRY]: LU decomposition over Galois fields.
        byte[,] invertedMatrix = InvertMatrix(subMatrix, DataShards);

        // [KYA KARTA HAI]: Missing data shards ko reconstruct karta hai using inverted matrix dot products.
        // [KYU ZAROORI HAI]: Damaged/missing disks ka data exact byte-level precision me recover karna.
        // [KAISE CHALTA HAI]: For each missing data shard d, Shard[d] = Sum(invertedMatrix[d, j] * availableShard[j]).
        // [OUTPUT / SIDE EFFECT]: Missing shards populated with 100% recovered data.
        // [VARIATIONS TO TRY]: Reconstruct only missing shards instead of all.
        for (int d = 0; d < DataShards; d++)
        {
            // [KYA KARTA HAI]: Agar data shard pehle se present hai to skip karta hai.
            // [KYU ZAROORI HAI]: Jo data intact hai use dobara compute karne ki zaroorat nahi hai.
            // [KAISE CHALTA HAI]: if (shardPresent[d]) continue.
            // [OUTPUT / SIDE EFFECT]: Reconstructs only lost shards.
            // [VARIATIONS TO TRY]: Overwrite to verify integrity.
            if (shardPresent[d]) continue;

            // [KYA KARTA HAI]: Target buffer reference fetch karta hai.
            // [KYU ZAROORI HAI]: Recovered bytes yahan store honge.
            // [KAISE CHALTA HAI]: target = shards[d].
            // [OUTPUT / SIDE EFFECT]: Target buffer reference.
            // [VARIATIONS TO TRY]: None.
            byte[] target = shards[d];

            // [KYA KARTA HAI]: Target buffer ko clear karta hai.
            // [KYU ZAROORI HAI]: XOR accumulation se pehle zeros hona mandatory hai.
            // [KAISE CHALTA HAI]: Array.Clear.
            // [OUTPUT / SIDE EFFECT]: Zeroed buffer.
            // [VARIATIONS TO TRY]: Memory clear.
            Array.Clear(target, 0, shardSize);

            // [KYA KARTA HAI]: Loop run karta hai through all k selected available shards.
            // [KYU ZAROORI HAI]: Dot product compute karna hai inverted matrix row d ke saath.
            // [KAISE CHALTA HAI]: For j in [0..k-1].
            // [OUTPUT / SIDE EFFECT]: Matrix-vector product in Galois field.
            // [VARIATIONS TO TRY]: SIMD dot product.
            for (int j = 0; j < DataShards; j++)
            {
                // [KYA KARTA HAI]: Inverted matrix coefficient fetch karta hai.
                // [KYU ZAROORI HAI]: Galois multiplication weight.
                // [KAISE CHALTA HAI]: byte coeff = invertedMatrix[d, j].
                // [OUTPUT / SIDE EFFECT]: Multiplicative weight.
                // [VARIATIONS TO TRY]: Check if coeff == 0.
                byte coeff = invertedMatrix[d, j];

                // [KYA KARTA HAI]: Available shard buffer fetch karta hai.
                // [KYU ZAROORI HAI]: Source bytes for recovery.
                // [KAISE CHALTA HAI]: availableShard = shards[selectedShardIndices[j]].
                // [OUTPUT / SIDE EFFECT]: Source shard reference.
                // [VARIATIONS TO TRY]: ReadOnlySpan.
                byte[] availableShard = shards[selectedShardIndices[j]];

                // [KYA KARTA HAI]: Byte-by-byte scalar multiplication aur XOR accumulation karta hai.
                // [KYU ZAROORI HAI]: Exact byte restoration through GF(2^8) linear combination.
                // [KAISE CHALTA HAI]: target[b] ^= GaloisField256.Multiply(coeff, availableShard[b]).
                // [OUTPUT / SIDE EFFECT]: Target shard restored byte-for-byte.
                // [VARIATIONS TO TRY]: AVX2 vectorized XOR multiplication.
                for (int b = 0; b < shardSize; b++)
                {
                    // [KYA KARTA HAI]: Galois multiply and XOR accumulate.
                    // [KYU ZAROORI HAI]: Core erasure decoding equation.
                    // [KAISE CHALTA HAI]: target[b] ^= GaloisField256.Multiply(coeff, availableShard[b]).
                    // [OUTPUT / SIDE EFFECT]: Reconstructed byte.
                    // [VARIATIONS TO TRY]: SIMD kernel.
                    target[b] ^= GaloisField256.Multiply(coeff, availableShard[b]);
                }
            }

            // [KYA KARTA HAI]: Mark shardPresent[d] = true.
            // [KYU ZAROORI HAI]: Shard successfully reconstruct ho gaya.
            // [KAISE CHALTA HAI]: shardPresent[d] = true.
            // [OUTPUT / SIDE EFFECT]: Health status updated.
            // [VARIATIONS TO TRY]: Fire ShardHealedEvent.
            shardPresent[d] = true;
        }
    }

    // [KYA KARTA HAI]: k x k square matrix ka inverse nikalta hai over GF(2^8) using Gaussian Elimination.
    // [KYU ZAROORI HAI]: Reed-Solomon decoding me missing shards ko decode karne ke liye generator submatrix ka inverse nikalna zaroori hota hai.
    // [KAISE CHALTA HAI]: Augmented matrix [A | I_k] create karke row operations se [I_k | A^-1] me transform karta hai.
    // [OUTPUT / SIDE EFFECT]: Returns inverted k x k matrix.
    // [VARIATIONS TO TRY]: LU decomposition for large matrix sizes.
    public static byte[,] InvertMatrix(byte[,] matrix, int size)
    {
        // [KYA KARTA HAI]: Augmented matrix allocate karta hai of size k x 2k.
        // [KYU ZAROORI HAI]: Left side original matrix rakhenge, right side identity matrix rakhenge.
        // [KAISE CHALTA HAI]: new byte[size, size * 2].
        // [OUTPUT / SIDE EFFECT]: Augmented matrix memory allocated.
        // [VARIATIONS TO TRY]: In-place inversion using pivot tracking.
        byte[,] aug = new byte[size, size * 2];

        // [KYA KARTA HAI]: Left half me input matrix aur right half me identity matrix copy karta hai.
        // [KYU ZAROORI HAI]: Standard Gauss-Jordan elimination initialization.
        // [KAISE CHALTA HAI]: For i in [0..size-1], aug[i, j] = matrix[i, j] and aug[i, size + i] = 1.
        // [OUTPUT / SIDE EFFECT]: [A | I] augmented system initialized.
        // [VARIATIONS TO TRY]: Fast memory copy.
        for (int i = 0; i < size; i++)
        {
            // [KYA KARTA HAI]: Copy original row elements into left half.
            // [KYU ZAROORI HAI]: Preserves input matrix in working space.
            // [KAISE CHALTA HAI]: For j in [0..size-1].
            // [OUTPUT / SIDE EFFECT]: Left half initialized.
            // [VARIATIONS TO TRY]: Buffer block copy.
            for (int j = 0; j < size; j++)
            {
                // [KYA KARTA HAI]: aug[i, j] = matrix[i, j].
                // [KYU ZAROORI HAI]: Left matrix setup.
                // [KAISE CHALTA HAI]: Array assignment.
                // [OUTPUT / SIDE EFFECT]: Matrix value copied.
                // [VARIATIONS TO TRY]: None.
                aug[i, j] = matrix[i, j];
            }

            // [KYA KARTA HAI]: Identity diagonal 1 set karta hai right half me.
            // [KYU ZAROORI HAI]: Right half starts as Identity matrix I.
            // [KAISE CHALTA HAI]: aug[i, size + i] = 1.
            // [OUTPUT / SIDE EFFECT]: Identity entry created.
            // [VARIATIONS TO TRY]: None.
            aug[i, size + i] = 1;
        }

        // [KYA KARTA HAI]: Gauss-Jordan elimination column-by-column execute karta hai.
        // [KYU ZAROORI HAI]: Matrix ko reduced row echelon form (identity) me transform karta hai.
        // [KAISE CHALTA HAI]: For col in [0..size-1], pivot find karta hai, normalize karta hai, aur baaki rows zero karta hai.
        // [OUTPUT / SIDE EFFECT]: Transforms [A | I] to [I | A^-1].
        // [VARIATIONS TO TRY]: Partial pivoting optimization.
        for (int col = 0; col < size; col++)
        {
            // [KYA KARTA HAI]: Pivot row find karta hai jisme col par non-zero element ho.
            // [KYU ZAROORI HAI]: Division by zero pivot avoid karna zaroori hai.
            // [KAISE CHALTA HAI]: Search row >= col where aug[row, col] != 0.
            // [OUTPUT / SIDE EFFECT]: pivotRow index found.
            // [VARIATIONS TO TRY]: Pick largest element for numerical stability (irrelevant in finite fields).
            int pivotRow = -1;

            // [KYA KARTA HAI]: Loop run karta hai from col to size - 1 to find pivot.
            // [KYU ZAROORI HAI]: Diagonal ya lower rows me non-zero pivot search.
            // [KAISE CHALTA HAI]: If aug[r, col] != 0 then pivotRow = r.
            // [OUTPUT / SIDE EFFECT]: Pivot row identified.
            // [VARIATIONS TO TRY]: Break on first found.
            for (int r = col; r < size; r++)
            {
                // [KYA KARTA HAI]: Check non-zero.
                // [KYU ZAROORI HAI]: Field element must be non-zero for multiplicative inverse.
                // [KAISE CHALTA HAI]: aug[r, col] != 0.
                // [OUTPUT / SIDE EFFECT]: Match found.
                // [VARIATIONS TO TRY]: None.
                if (aug[r, col] != 0)
                {
                    // [KYA KARTA HAI]: Record pivot row.
                    // [KYU ZAROORI HAI]: Used for row swapping.
                    // [KAISE CHALTA HAI]: pivotRow = r; break;
                    // [OUTPUT / SIDE EFFECT]: pivotRow saved.
                    // [VARIATIONS TO TRY]: Continue searching.
                    pivotRow = r;
                    break;
                }
            }

            // [KYA KARTA HAI]: Check if no valid pivot found.
            // [KYU ZAROORI HAI]: Singular matrix cannot be inverted.
            // [KAISE CHALTA HAI]: pivotRow == -1 check.
            // [OUTPUT / SIDE EFFECT]: Singular matrix guard.
            // [VARIATIONS TO TRY]: Return error result.
            if (pivotRow == -1)
            {
                // [KYA KARTA HAI]: Throw exception.
                // [KYU ZAROORI HAI]: Matrix is singular (determinant = 0).
                // [KAISE CHALTA HAI]: Throws InvalidOperationException.
                // [OUTPUT / SIDE EFFECT]: Inversion failed.
                // [VARIATIONS TO TRY]: Custom exception.
                throw new InvalidOperationException("Matrix is singular and cannot be inverted in GF(2^8).");
            }

            // [KYA KARTA HAI]: Agar pivot row current col nahi hai to row swap karta hai.
            // [KYU ZAROORI HAI]: Pivot element ko diagonal position (col, col) par laana mandatory hai.
            // [KAISE CHALTA HAI]: Swaps entire row pivotRow and row col across all 2k columns.
            // [OUTPUT / SIDE EFFECT]: Rows swapped.
            // [VARIATIONS TO TRY]: Row pointer array swap instead of copying elements.
            if (pivotRow != col)
            {
                // [KYA KARTA HAI]: Loop through all 2k columns to swap elements.
                // [KYU ZAROORI HAI]: Full augmented row swap.
                // [KAISE CHALTA HAI]: For c in [0..2*size-1].
                // [OUTPUT / SIDE EFFECT]: Row values exchanged.
                // [VARIATIONS TO TRY]: Pointer swap.
                for (int c = 0; c < size * 2; c++)
                {
                    // [KYA KARTA HAI]: Swap aug[col, c] and aug[pivotRow, c].
                    // [KYU ZAROORI HAI]: In-place variable swap.
                    // [KAISE CHALTA HAI]: Tuple swap: (aug[col, c], aug[pivotRow, c]) = (aug[pivotRow, c], aug[col, c]).
                    // [OUTPUT / SIDE EFFECT]: Elements swapped.
                    // [VARIATIONS TO TRY]: Temp variable.
                    (aug[col, c], aug[pivotRow, c]) = (aug[pivotRow, c], aug[col, c]);
                }
            }

            // [KYA KARTA HAI]: Diagonal pivot element ka inverse nikalta hai.
            // [KYU ZAROORI HAI]: Pivot row ko divide karke diagonal element ko 1 banana hai.
            // [KAISE CHALTA HAI]: GaloisField256.Inverse(aug[col, col]).
            // [OUTPUT / SIDE EFFECT]: pivotInverse byte.
            // [VARIATIONS TO TRY]: Precomputed inverse.
            byte pivotInverse = GaloisField256.Inverse(aug[col, col]);

            // [KYA KARTA HAI]: Pivot row ke saare elements ko pivotInverse se multiply karta hai.
            // [KYU ZAROORI HAI]: Diagonal element becomes 1 (normed pivot).
            // [KAISE CHALTA HAI]: For c in [0..2*size-1], aug[col, c] = Multiply(aug[col, c], pivotInverse).
            // [OUTPUT / SIDE EFFECT]: Pivot row normalized.
            // [VARIATIONS TO TRY]: Vectorized row scaling.
            for (int c = 0; c < size * 2; c++)
            {
                // [KYA KARTA HAI]: Multiply row element by pivot inverse.
                // [KYU ZAROORI HAI]: Row normalization in GF(2^8).
                // [KAISE CHALTA HAI]: aug[col, c] = GaloisField256.Multiply(aug[col, c], pivotInverse).
                // [OUTPUT / SIDE EFFECT]: Diagonal element becomes 1.
                // [VARIATIONS TO TRY]: SIMD multiplication.
                aug[col, c] = GaloisField256.Multiply(aug[col, c], pivotInverse);
            }

            // [KYA KARTA HAI]: Baaki saari rows r (r != col) me se col column ko eliminate karta hai.
            // [KYU ZAROORI HAI]: Column col me diagonal ke alawa saare elements 0 banane ke liye.
            // [KAISE CHALTA HAI]: Row r = Row r - factor * Row col (subtraction is XOR in GF(2^8)).
            // [OUTPUT / SIDE EFFECT]: Entire column col eliminated except diagonal.
            // [VARIATIONS TO TRY]: Parallel row reduction.
            for (int r = 0; r < size; r++)
            {
                // [KYA KARTA HAI]: Skip pivot row itself.
                // [KYU ZAROORI HAI]: Pivot row is already normalized.
                // [KAISE CHALTA HAI]: if (r == col) continue.
                // [OUTPUT / SIDE EFFECT]: Skips self.
                // [VARIATIONS TO TRY]: None.
                if (r == col) continue;

                // [KYA KARTA HAI]: Element to eliminate fetch karta hai.
                // [KYU ZAROORI HAI]: Elimination factor.
                // [KAISE CHALTA HAI]: byte factor = aug[r, col].
                // [OUTPUT / SIDE EFFECT]: Factor byte.
                // [VARIATIONS TO TRY]: None.
                byte factor = aug[r, col];

                // [KYA KARTA HAI]: Agar factor pehle se zero hai to elimination zaroorat nahi hai.
                // [KYU ZAROORI HAI]: Zero multiply zero rehta hai.
                // [KAISE CHALTA HAI]: if (factor == 0) continue.
                // [OUTPUT / SIDE EFFECT]: Skips zero entries.
                // [VARIATIONS TO TRY]: None.
                if (factor == 0) continue;

                // [KYA KARTA HAI]: Row r ke saare columns par factor * pivotRow subtract (XOR) karta hai.
                // [KYU ZAROORI HAI]: Elementary row operation to zero out non-diagonal entry.
                // [KAISE CHALTA HAI]: aug[r, c] ^= GaloisField256.Multiply(factor, aug[col, c]).
                // [OUTPUT / SIDE EFFECT]: Column entry becomes 0, other columns updated.
                // [VARIATIONS TO TRY]: SIMD XOR multiply.
                for (int c = 0; c < size * 2; c++)
                {
                    // [KYA KARTA HAI]: Subtract (XOR) product from row r.
                    // [KYU ZAROORI HAI]: Gauss-Jordan elimination step.
                    // [KAISE CHALTA HAI]: aug[r, c] ^= GaloisField256.Multiply(factor, aug[col, c]).
                    // [OUTPUT / SIDE EFFECT]: Element updated.
                    // [VARIATIONS TO TRY]: AVX2 vectorization.
                    aug[r, c] ^= GaloisField256.Multiply(factor, aug[col, c]);
                }
            }
        }

        // [KYA KARTA HAI]: Inverted matrix allocate karta hai of size k x k.
        // [KYU ZAROORI HAI]: Augmented matrix ke right half ko extract karke return karna hai.
        // [KAISE CHALTA HAI]: new byte[size, size].
        // [OUTPUT / SIDE EFFECT]: Result matrix allocated.
        // [VARIATIONS TO TRY]: Reusable buffer.
        byte[,] result = new byte[size, size];

        // [KYA KARTA HAI]: Right half (columns size to 2*size - 1) copy karta hai result me.
        // [KYU ZAROORI HAI]: Right half contains the mathematically inverted matrix A^-1.
        // [KAISE CHALTA HAI]: For i in [0..size-1], for j in [0..size-1], result[i, j] = aug[i, size + j].
        // [OUTPUT / SIDE EFFECT]: Result matrix populated.
        // [VARIATIONS TO TRY]: Buffer block copy.
        for (int i = 0; i < size; i++)
        {
            // [KYA KARTA HAI]: Loop columns.
            // [KYU ZAROORI HAI]: Extract right block.
            // [KAISE CHALTA HAI]: For j in [0..size-1].
            // [OUTPUT / SIDE EFFECT]: Iterates matrix elements.
            // [VARIATIONS TO TRY]: Pointer copy.
            for (int j = 0; j < size; j++)
            {
                // [KYA KARTA HAI]: Copy inverted element.
                // [KYU ZAROORI HAI]: Extract A^-1.
                // [KAISE CHALTA HAI]: result[i, j] = aug[i, size + j].
                // [OUTPUT / SIDE EFFECT]: Result cell filled.
                // [VARIATIONS TO TRY]: None.
                result[i, j] = aug[i, size + j];
            }
        }

        // [KYA KARTA HAI]: Inverted matrix return karta hai.
        // [KYU ZAROORI HAI]: Used by decoder to reconstruct original data shards.
        // [KAISE CHALTA HAI]: Returns result byte[,].
        // [OUTPUT / SIDE EFFECT]: Inverted matrix returned to caller.
        // [VARIATIONS TO TRY]: None.
        return result;
    }
}
