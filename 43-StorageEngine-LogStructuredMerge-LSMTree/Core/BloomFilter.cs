using System.Text;

namespace KiranaHub.LsmStorage.Core;

/// <summary>
/// Probabilistic space-efficient data structure for testing set membership.
/// Returns false -> Definitely NOT in SSTable (Zero disk I/O needed).
/// Returns true -> Possibly in SSTable (Proceed with binary search on disk).
/// </summary>
public sealed class BloomFilter
{
    // [KYA KARTA HAI]: Bit array ko compact byte array representation me store karta hai.
    // [KYU ZAROORI HAI]: Memory efficiency aur disk persistence ke liye byte alignment zaroori hai.
    // [KAISE CHALTA HAI]: Har bit 0 ya 1 hold karta hai, ek byte me 8 keys ke bits pack hote hain.
    // [OUTPUT / SIDE EFFECT]: In-memory bit array allocated rehta hai.
    // [VARIATIONS TO TRY]: System.Collections.BitArray bhi use kiya ja sakta hai.
    private readonly byte[] _bitArray;

    // [KYA KARTA HAI]: Total number of bits store karta hai.
    // [KYU ZAROORI HAI]: Modulo operations aur boundary safety ke liye size pata hona chahiye.
    // [KAISE CHALTA HAI]: Constructor me capacity aur false-positive rate se compute hota hai.
    // [OUTPUT / SIDE EFFECT]: Immutable bit count integer.
    // [VARIATIONS TO TRY]: 64-bit long use kar sakte hain agar billions of keys hon.
    private readonly int _bitCount;

    // [KYA KARTA HAI]: Number of hash functions count store karta hai.
    // [KYU ZAROORI HAI]: Optimal k hash functions false-positive probability ko minimize karte hain.
    // [KAISE CHALTA HAI]: Mathematical formula k = (m/n) * ln(2) se calculate hota hai.
    // [OUTPUT / SIDE EFFECT]: Integer k store hota hai (typically 5 to 10).
    // [VARIATIONS TO TRY]: Fixed 4 hash functions use karke speed optimize kar sakte hain.
    private readonly int _hashFunctionCount;

    // [KYA KARTA HAI]: Expected items capacity aur target false-positive rate se Bloom Filter initialize karta hai.
    // [KYU ZAROORI HAI]: Dynamic sizing se memory waste nahi hoti aur 1% se kam false positive milta hai.
    // [KAISE CHALTA HAI]: Math formulas se optimal bit array length allocate karta hai.
    // [OUTPUT / SIDE EFFECT]: Clean zero-initialized BloomFilter instance banta hai.
    // [VARIATIONS TO TRY]: Direct byte array pass karke deserialization constructor bana sakte hain.
    public BloomFilter(int expectedElements = 10000, double falsePositiveRate = 0.01)
    {
        // [KYA KARTA HAI]: Expected elements minimum 1 ensure karta hai.
        // [KYU ZAROORI HAI]: Zero ya negative count se division by zero aur crash se bachata hai.
        // [KAISE CHALTA HAI]: Math.Max(1, ...) ensure karta hai valid denominator.
        // [OUTPUT / SIDE EFFECT]: Safe expected elements count.
        // [VARIATIONS TO TRY]: ArgumentOutOfRangeException throw kar sakte hain.
        int n = Math.Max(1, expectedElements);

        // [KYA KARTA HAI]: Optimal bit size m = - (n * ln(p)) / (ln 2)^2 calculate karta hai.
        // [KYU ZAROORI HAI]: Theoretical minimum bits needed for guaranteed false positive rate.
        // [KAISE CHALTA HAI]: Logarithmic math calculation perform karta hai.
        // [OUTPUT / SIDE EFFECT]: Optimum bit count double value.
        // [VARIATIONS TO TRY]: Lookup table use karke pre-calculated sizes assign kar sakte hain.
        double m = -(n * Math.Log(falsePositiveRate)) / Math.Pow(Math.Log(2), 2);

        // [KYA KARTA HAI]: Bit count ko nearest 8-bit multiple par round up karta hai.
        // [KYU ZAROORI HAI]: Disk serialization me clean byte boundary alignment maintain hoti hai.
        // [KAISE CHALTA HAI]: Math.Ceiling karke int conversion karta hai.
        // [OUTPUT / SIDE EFFECT]: _bitCount set ho jata hai.
        // [VARIATIONS TO TRY]: 64-bit word alignment apply kar sakte hain.
        _bitCount = Math.Max(64, (int)Math.Ceiling(m / 8.0) * 8);

        // [KYA KARTA HAI]: Optimal hash function count k = (m / n) * ln(2) calculate karta hai.
        // [KYU ZAROORI HAI]: Kam hash se collisions badhte hain, zyada hash se CPU overhead badhta hai.
        // [KAISE CHALTA HAI]: Dynamic mathematical computation karta hai.
        // [OUTPUT / SIDE EFFECT]: _hashFunctionCount set hota hai (1 se 30 ke beech).
        // [VARIATIONS TO TRY]: Clamp(3, 12) laga kar CPU cost bound kar sakte hain.
        _hashFunctionCount = Math.Clamp((int)Math.Round((_bitCount / (double)n) * Math.Log(2)), 1, 30);

        // [KYA KARTA HAI]: Underlying byte buffer allocate karta hai.
        // [KYU ZAROORI HAI]: Hardware bits ko memory me hold karne ke liye.
        // [KAISE CHALTA HAI]: _bitCount / 8 bytes allocate karta hai GC heap par.
        // [OUTPUT / SIDE EFFECT]: Zero-filled byte array instantiated.
        // [VARIATIONS TO TRY]: NativeMemory.Alloc use karke off-heap allocate kar sakte hain.
        _bitArray = new byte[_bitCount / 8];
    }

    // [KYA KARTA HAI]: Serialized byte stream se BloomFilter restore karne wala constructor.
    // [KYU ZAROORI HAI]: Disk se SSTable read karte waqt in-memory filter load karne ke liye.
    // [KAISE CHALTA HAI]: Pre-existing byte array aur hash count ko directly wrap karta hai.
    // [OUTPUT / SIDE EFFECT]: Hydrated BloomFilter ready for instant lookups.
    // [VARIATIONS TO TRY]: Stream reader se direct streaming deserialization kar sakte hain.
    public BloomFilter(byte[] rawBits, int hashCount)
    {
        // [KYA KARTA HAI]: Raw bit array reference save karta hai.
        // [KYU ZAROORI HAI]: Fast pointer access without re-allocation.
        // [KAISE CHALTA HAI]: Array reference assignment.
        // [OUTPUT / SIDE EFFECT]: _bitArray points to deserialized buffer.
        // [VARIATIONS TO TRY]: Array.Clone() call karke immutability enforce kar sakte hain.
        _bitArray = rawBits;

        // [KYA KARTA HAI]: Bit count assign karta hai based on byte array length * 8.
        // [KYU ZAROORI HAI]: Deserialized buffer ka exact bit capacity establish karna.
        // [KAISE CHALTA HAI]: Length * 8 simple arithmetic.
        // [OUTPUT / SIDE EFFECT]: _bitCount calculated.
        // [VARIATIONS TO TRY]: Header me explicit bitCount store kar sakte hain.
        _bitCount = rawBits.Length * 8;

        // [KYA KARTA HAI]: Hash function count assign karta hai.
        // [KYU ZAROORI HAI]: Exact wahi hash functions chalane ke liye jo write time par use hue the.
        // [KAISE CHALTA HAI]: Field assignment.
        // [OUTPUT / SIDE EFFECT]: _hashFunctionCount set.
        // [VARIATIONS TO TRY]: Default to 7 if passed <= 0.
        _hashFunctionCount = Math.Max(1, hashCount);
    }

    // [KYA KARTA HAI]: Inventory key ko Bloom Filter me insert karta hai (bits ko 1 set karke).
    // [KYU ZAROORI HAI]: SSTable flush ke waqt sabhi inserted keys ke footprint record karne ke liye.
    // [KAISE CHALTA HAI]: Double hashing technique (MurmurHash3 equivalent) se k bits set karta hai.
    // [OUTPUT / SIDE EFFECT]: Bit array me k bit locations 1 ho jati hain.
    // [VARIATIONS TO TRY]: ReadOnlySpan<char> accept kar sakte hain zero-allocation ke liye.
    public void Add(string key)
    {
        // [KYA KARTA HAI]: Key ke do primary 32-bit hashes compute karta hai.
        // [KYU ZAROORI HAI]: Kirsch-Mitzenmacher optimization: 2 hashes se k hashes generate ho sakte hain.
        // [KAISE CHALTA HAI]: ComputeHashes method call karke h1 aur h2 nikalta hai.
        // [OUTPUT / SIDE EFFECT]: (uint h1, uint h2) tuple milta hai.
        // [VARIATIONS TO TRY]: SHA256 ya FNV-1a use kar sakte hain.
        var (h1, h2) = ComputeHashes(key);

        // [KYA KARTA HAI]: k different hash positions par bit ko 1 banata hai.
        // [KYU ZAROORI HAI]: Multi-hash indexing collision rate ko exponentially drop karti hai.
        // [KAISE CHALTA HAI]: Loop chalakar h1 + i * h2 modulo _bitCount calculate karta hai.
        // [OUTPUT / SIDE EFFECT]: _bitArray ke specific byte offsets par bitwise OR hota hai.
        // [VARIATIONS TO TRY]: SIMD vectorization se parallel bits set kar sakte hain.
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            // [KYA KARTA HAI]: Combined hash index calculate karta hai.
            // [KYU ZAROORI HAI]: Uniformly distributed bit position nikalne ke liye.
            // [KAISE CHALTA HAI]: (h1 + i * h2) % _bitCount modulo arithmetic.
            // [OUTPUT / SIDE EFFECT]: Safe index between 0 and _bitCount - 1.
            // [VARIATIONS TO TRY]: Bitwise AND mask if _bitCount is power of two.
            int bitIndex = (int)((h1 + (uint)i * h2) % (uint)_bitCount);

            // [KYA KARTA HAI]: Target byte offset aur bit offset me decompose karta hai.
            // [KYU ZAROORI HAI]: Byte array me indexing karne ke liye division aur remainder chahiye.
            // [KAISE CHALTA HAI]: bitIndex / 8 and bitIndex % 8 bit operations.
            // [OUTPUT / SIDE EFFECT]: Byte address and bitmask identified.
            // [VARIATIONS TO TRY]: bitIndex >> 3 and bitIndex & 7 for fastest bitwise speed.
            int byteIndex = bitIndex >> 3;
            int bitOffset = bitIndex & 7;

            // [KYA KARTA HAI]: Specific bit ko 1 set karta hai bitwise OR ke zariye.
            // [KYU ZAROORI HAI]: Existing bits ko overwrite kiye bina naya bit turn ON karna.
            // [KAISE CHALTA HAI]: _bitArray[byteIndex] |= (byte)(1 << bitOffset).
            // [OUTPUT / SIDE EFFECT]: Byte me targeted bit permanently 1 ho jati hai.
            // [VARIATIONS TO TRY]: Interlocked.Or use kar sakte hain concurrent writers ke liye.
            _bitArray[byteIndex] |= (byte)(1 << bitOffset);
        }
    }

    // [KYA KARTA HAI]: Key Bloom Filter me exist karti hai ya nahi verify karta hai in O(1) time.
    // [KYU ZAROORI HAI]: Agar false mila toh guaranteed key disk par nahi hai -> Disk seek zero!
    // [KAISE CHALTA HAI]: k bit positions inspect karta hai; agar ek bhi bit 0 mili toh return false!
    // [OUTPUT / SIDE EFFECT]: Boolean true (maybe present) ya false (definitely absent).
    // [VARIATIONS TO TRY]: Return enum: DefinitelyNo, HighlyProbableYes.
    public bool MayContain(string key)
    {
        // [KYA KARTA HAI]: Key ke two independent 32-bit hashes compute karta hai.
        // [KYU ZAROORI HAI]: Add ke identical bit locations probe karne ke liye.
        // [KAISE CHALTA HAI]: Double hashing algorithm invoke hota hai.
        // [OUTPUT / SIDE EFFECT]: Hash tuple (h1, h2) ready.
        // [VARIATIONS TO TRY]: Cache frequently probed hashes in L1 memory.
        var (h1, h2) = ComputeHashes(key);

        // [KYA KARTA HAI]: Tamam k bit positions check karta hai.
        // [KYU ZAROORI HAI]: Agar ek bhi bit 0 hui toh key set me kabhi insert nahi hui thi!
        // [KAISE CHALTA HAI]: Short-circuit loop: pehle 0 bit par turant false return.
        // [OUTPUT / SIDE EFFECT]: Fast rejection in 1 CPU cycle on average.
        // [VARIATIONS TO TRY]: Branchless unrolled bitwise test for speed.
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            // [KYA KARTA HAI]: Target bit index calculate karta hai.
            // [KYU ZAROORI HAI]: Correct bit location probe karne ke liye.
            // [KAISE CHALTA HAI]: (h1 + i * h2) % _bitCount.
            // [OUTPUT / SIDE EFFECT]: Int index inside bounds.
            // [VARIATIONS TO TRY]: Pre-computed offsets array.
            int bitIndex = (int)((h1 + (uint)i * h2) % (uint)_bitCount);

            // [KYA KARTA HAI]: Byte aur bit position compute karta hai.
            // [KYU ZAROORI HAI]: Byte array indexing.
            // [KAISE CHALTA HAI]: Shift right by 3 and bitwise AND with 7.
            // [OUTPUT / SIDE EFFECT]: Byte address and bit mask.
            // [VARIATIONS TO TRY]: Unsafe byte pointer dereferencing for micro-benchmarks.
            int byteIndex = bitIndex >> 3;
            int bitOffset = bitIndex & 7;

            // [KYA KARTA HAI]: Bit status inspect karta hai: kya yeh 0 hai?
            // [KYU ZAROORI HAI]: Agar 0 hai toh absence mathematically guaranteed hai!
            // [KAISE CHALTA HAI]: Bitwise AND check: (_bitArray[byteIndex] & (1 << bitOffset)) == 0.
            // [OUTPUT / SIDE EFFECT]: Immediate false exit if bit is zero.
            // [VARIATIONS TO TRY]: Log metric counters on bloom filter rejections.
            if ((_bitArray[byteIndex] & (1 << bitOffset)) == 0)
            {
                return false;
            }
        }

        // [KYA KARTA HAI]: Jab sabhi k bits 1 hon tab true return karta hai.
        // [KYU ZAROORI HAI]: Caller ko SSTable sparse index search karne ki permission deta hai.
        // [KAISE CHALTA HAI]: Loop successfully complete hone par execute hota hai.
        // [OUTPUT / SIDE EFFECT]: Returns true (key may exist on disk).
        // [VARIATIONS TO TRY]: Return confidence score based on saturation.
        return true;
    }

    // [KYA KARTA HAI]: MurmurHash3-inspired fast non-cryptographic 64-bit dual hash produce karta hai.
    // [KYU ZAROORI HAI]: Uniform avalanche effect provide karta hai minimal CPU cycles me.
    // [KAISE CHALTA HAI]: String bytes par multiply-rotate-xor mixing algorithm lagata hai.
    // [OUTPUT / SIDE EFFECT]: Pair of independent uint hashes (h1, h2).
    // [VARIATIONS TO TRY]: Hardware CRC32C intrinsics use kar sakte hain.
    private static (uint h1, uint h2) ComputeHashes(string key)
    {
        // [KYA KARTA HAI]: String ko UTF-8 byte span me decode karta hai.
        // [KYU ZAROORI HAI]: Uniform byte-level hashing regardless of character encoding.
        // [KAISE CHALTA HAI]: Encoding.UTF8.GetBytes standard transformation.
        // [OUTPUT / SIDE EFFECT]: Byte array representation.
        // [VARIATIONS TO TRY]: Span<byte> on stack using stackalloc to avoid GC allocation.
        byte[] bytes = Encoding.UTF8.GetBytes(key);

        // [KYA KARTA HAI]: Primary accumulator seeds initialize karta hai.
        // [KYU ZAROORI HAI]: Two distinct mathematical starting states ensure independent hash streams.
        // [KAISE CHALTA HAI]: Prime numbers as initial seeds.
        // [OUTPUT / SIDE EFFECT]: Initialized state variables.
        // [VARIATIONS TO TRY]: Seed with custom tenant salt.
        uint h1 = 0x9747b28c;
        uint h2 = 0xadc83b19;

        // [KYA KARTA HAI]: Har byte ko hash state me mix karta hai.
        // [KYU ZAROORI HAI]: String ke har character ka bit output par strong avalanche effect hona chahiye.
        // [KAISE CHALTA HAI]: Rotate-left, multiply by magic prime constants, and XOR.
        // [OUTPUT / SIDE EFFECT]: Thoroughly mixed hash state.
        // [VARIATIONS TO TRY]: Process 4-byte chunks with uint pointers.
        foreach (byte b in bytes)
        {
            // [KYA KARTA HAI]: Accumulator h1 ko byte value se mix karta hai.
            // [KYU ZAROORI HAI]: First independent hash stream generation.
            // [KAISE CHALTA HAI]: Multiply-add with prime constant 0xcc9e2d51.
            // [OUTPUT / SIDE EFFECT]: Scrambled h1.
            // [VARIATIONS TO TRY]: Bitwise rotation (ROL).
            h1 = (h1 ^ b) * 0xcc9e2d51;

            // [KYA KARTA HAI]: Accumulator h2 ko byte value se alternate prime se mix karta hai.
            // [KYU ZAROORI HAI]: Second independent hash stream generation without correlation.
            // [KAISE CHALTA HAI]: Multiply-add with prime constant 0x1b873593.
            // [OUTPUT / SIDE EFFECT]: Scrambled h2.
            // [VARIATIONS TO TRY]: Use Murmur3 32-bit finalizer.
            h2 = (h2 ^ b) * 0x1b873593;
        }

        // [KYA KARTA HAI]: Final avalanche bit mixing apply karta hai.
        // [KYU ZAROORI HAI]: Remaining low-entropy bit correlations ko destroy karne ke liye.
        // [KAISE CHALTA HAI]: Shift-XOR-Multiply cascade.
        // [OUTPUT / SIDE EFFECT]: High quality pseudo-random hashes.
        // [VARIATIONS TO TRY]: Murmur3 fmix32 function.
        h1 ^= h1 >> 16;
        h1 *= 0x85ebca6b;
        h1 ^= h1 >> 13;

        h2 ^= h2 >> 16;
        h2 *= 0xc2b2ae35;
        h2 ^= h2 >> 13;

        return (h1, h2);
    }

    // [KYA KARTA HAI]: Internal byte array ki copy return karta hai.
    // [KYU ZAROORI HAI]: SSTable file ke header/footer me disk par persist karne ke liye.
    // [KAISE CHALTA HAI]: Array clone karke safe decoupled byte array deta hai.
    // [OUTPUT / SIDE EFFECT]: Byte array copy returned.
    // [VARIATIONS TO TRY]: ReadOnlySpan<byte> return kar sakte hain.
    public byte[] GetRawBits() => (byte[])_bitArray.Clone();

    // [KYA KARTA HAI]: Hash function count property expose karta hai.
    // [KYU ZAROORI HAI]: Deserialization ke waqt disk file me hash count store aur read karne ke liye.
    // [KAISE CHALTA HAI]: Private field value getter.
    // [OUTPUT / SIDE EFFECT]: Returns _hashFunctionCount integer.
    // [VARIATIONS TO TRY]: Auto property with private setter.
    public int HashFunctionCount => _hashFunctionCount;

    // [KYA KARTA HAI]: Total bit array capacity expose karta hai.
    // [KYU ZAROORI HAI]: Memory statistics aur health telemetry monitor karne ke liye.
    // [KAISE CHALTA HAI]: Field getter.
    // [OUTPUT / SIDE EFFECT]: Returns bit count integer.
    // [VARIATIONS TO TRY]: Convert to bytes by dividing by 8.
    public int BitCount => _bitCount;
}
