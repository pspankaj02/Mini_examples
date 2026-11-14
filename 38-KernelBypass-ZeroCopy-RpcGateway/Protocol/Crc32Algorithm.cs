// [KYA KARTA HAI]: Buffers namespace import karta hai ReadOnlySequence handling ke liye
// [KYU ZAROORI HAI]: Multi-segment segmented memory buffers par directly CRC compute karne ke liye
// [KAISE CHALTA HAI]: System.Buffers types ko namespace me laata hai
// [OUTPUT / SIDE EFFECT]: ReadOnlySequence<byte> type available hota hai
// [VARIATIONS TO TRY]: System.IO.Hashing.Crc32 use karein
using System.Buffers;

// [KYA KARTA HAI]: Protocol namespace define karta hai
// [KYU ZAROORI HAI]: Network data integrity verification algorithms ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project ke protocol contracts me map hota hai
// [OUTPUT / SIDE EFFECT]: Crc32Algorithm type available in KiranaHub.ZeroCopyRpc.Protocol
// [VARIATIONS TO TRY]: KiranaHub.Cryptography.Checksums
namespace KiranaHub.ZeroCopyRpc.Protocol;

// [KYA KARTA HAI]: High-performance Table-Driven CRC32 Checksum Algorithm class
// [KYU ZAROORI HAI]: Zero-copy byte stream data corruption ko wire speed par detect karne ke liye
// [KAISE CHALTA HAI]: 256-entry precomputed polynomial table (0xEDB88320) se byte-by-byte fast lookup karta hai
// [OUTPUT / SIDE EFFECT]: Sub-nanosecond data integrity verification without allocations
// [VARIATIONS TO TRY]: Hardware intrinsic SSE 4.2 / ARM CRC32 instructions use karein
public static class Crc32Algorithm
{
    // [KYA KARTA HAI]: IEEE 802.3 standard reverse polynomial constant
    // [KYU ZAROORI HAI]: Ethernet aur Zip standard CRC32 generate karne ke liye
    // [KAISE CHALTA HAI]: 32-bit polynomial constant 0xEDB88320u
    // [OUTPUT / SIDE EFFECT]: Standard CRC32 polynomial defined
    // [VARIATIONS TO TRY]: Castagnoli polynomial (0x82F63B78) for iSCSI/Btrfs
    private const uint Polynomial = 0xEDB88320u;

    // [KYA KARTA HAI]: 256-entry lookup table static array
    // [KYU ZAROORI HAI]: Runtime loop ke andar bit-shifting eliminate karke O(1) table lookup provide karne ke liye
    // [KAISE CHALTA HAI]: Initialized once in static constructor
    // [OUTPUT / SIDE EFFECT]: Fast lookup table ready in CPU L1 cache
    // [VARIATIONS TO TRY]: 16-way slice-by-8 / slice-by-16 table optimization
    private static readonly uint[] Table = new uint[256];

    // [KYA KARTA HAI]: Static constructor jo CRC32 lookup table generate karta hai
    // [KYU ZAROORI HAI]: Application startup par pre-computation complete karne ke liye
    // [KAISE CHALTA HAI]: 0 se 255 tak har byte ke liye polynomial bit-shift simulate karta hai
    // [OUTPUT / SIDE EFFECT]: Table fully populated in memory
    // [VARIATIONS TO TRY]: Compile-time const table via source generator
    static Crc32Algorithm()
    {
        // [KYA KARTA HAI]: 256 iterations ka initialization loop
        // [KYU ZAROORI HAI]: All 256 possible byte values map karne ke liye
        // [KAISE CHALTA HAI]: for (uint i = 0; i < 256; i++)
        // [OUTPUT / SIDE EFFECT]: Initializes table entries
        // [VARIATIONS TO TRY]: Unrolled initialization
        for (uint i = 0; i < 256; i++)
        {
            // [KYA KARTA HAI]: Current entry accumulator variable
            // [KYU ZAROORI HAI]: Polynomial shift perform karne ke liye
            // [KAISE CHALTA HAI]: uint entry = i
            // [OUTPUT / SIDE EFFECT]: entry initialized
            // [VARIATIONS TO TRY]: Local register variable
            uint entry = i;

            // [KYA KARTA HAI]: 8 bits shift loop
            // [KYU ZAROORI HAI]: 1 byte ke 8 bits par polynomial division simulate karne ke liye
            // [KAISE CHALTA HAI]: for (int j = 0; j < 8; j++)
            // [OUTPUT / SIDE EFFECT]: 8-bit shifts calculated
            // [VARIATIONS TO TRY]: Bitwise unroll
            for (int j = 0; j < 8; j++)
            {
                // [KYA KARTA HAI]: Least significant bit check and polynomial XOR
                // [KYU ZAROORI HAI]: Galois field GF(2) polynomial division rule
                // [KAISE CHALTA HAI]: (entry & 1) == 1 ? (entry >> 1) ^ Polynomial : (entry >> 1)
                // [OUTPUT / SIDE EFFECT]: Correct CRC entry calculated
                // [VARIATIONS TO TRY]: Branchless arithmetic
                entry = (entry & 1) == 1 ? (entry >> 1) ^ Polynomial : (entry >> 1);
            }

            // [KYA KARTA HAI]: Table slot me calculated value assign karta hai
            // [KYU ZAROORI HAI]: Lookup table save karne ke liye
            // [KAISE CHALTA HAI]: Table[i] = entry
            // [OUTPUT / SIDE EFFECT]: Table index populated
            // [VARIATIONS TO TRY]: Direct memory pin
            Table[i] = entry;
        }
    }

    // [KYA KARTA HAI]: Contiguous ReadOnlySpan<byte> par CRC32 calculate karta hai
    // [KYU ZAROORI HAI]: Stack buffers ya single contiguous memory slices par checksum nikaalne ke liye
    // [KAISE CHALTA HAI]: Table lookup se running CRC accumulator ko update karta hai
    // [OUTPUT / SIDE EFFECT]: Returns final 32-bit checksum
    // [VARIATIONS TO TRY]: Vectorized SIMD instruction acceleration
    public static uint Compute(ReadOnlySpan<byte> span, uint seed = 0xFFFFFFFFu)
    {
        // [KYA KARTA HAI]: Running checksum accumulator initialize karta hai
        // [KYU ZAROORI HAI]: Bitwise NOT standard CRC initial state ke liye
        // [KAISE CHALTA HAI]: uint crc = seed
        // [OUTPUT / SIDE EFFECT]: Initial CRC state ready
        // [VARIATIONS TO TRY]: Seed customizable
        uint crc = seed;

        // [KYA KARTA HAI]: Span ke har byte par fast loop execute karta hai
        // [KYU ZAROORI HAI]: Zero memory allocation ke sath stream process karne ke liye
        // [KAISE CHALTA HAI]: for loop over span.Length
        // [OUTPUT / SIDE EFFECT]: CRC table lookups executed
        // [VARIATIONS TO TRY]: Pointer arithmetic unrolling
        for (int i = 0; i < span.Length; i++)
        {
            // [KYA KARTA HAI]: Current byte ko CRC accumulator ke sath XOR karke table index nikaalta hai
            // [KYU ZAROORI HAI]: Table entry fetch karne ke liye
            // [KAISE CHALTA HAI]: Table[(crc ^ span[i]) & 0xFF] ^ (crc >> 8)
            // [OUTPUT / SIDE EFFECT]: Accumulator updated
            // [VARIATIONS TO TRY]: Intrinsic hardware instruction
            crc = Table[(crc ^ span[i]) & 0xFF] ^ (crc >> 8);
        }

        // [KYA KARTA HAI]: Inverted final CRC return karta hai
        // [KYU ZAROORI HAI]: IEEE 802.3 standard bitwise inversion rule
        // [KAISE CHALTA HAI]: Returns crc ^ 0xFFFFFFFFu
        // [OUTPUT / SIDE EFFECT]: Wire-ready checksum
        // [VARIATIONS TO TRY]: Return raw uint without inversion for chaining
        return crc ^ 0xFFFFFFFFu;
    }

    // [KYA KARTA HAI]: Discontiguous multi-segment ReadOnlySequence<byte> par zero-copy CRC32 calculate karta hai
    // [KYU ZAROORI HAI]: Network packets jab memory segments ke beech split ho jayein to unhe copy kiye bina CRC calculate karne ke liye
    // [KAISE CHALTA HAI]: Har memory segment ke span ko sequence me loop karke running CRC accumulate karta hai
    // [OUTPUT / SIDE EFFECT]: Zero-copy checksum computation across segmented network buffers
    // [VARIATIONS TO TRY]: Hardware scatter-gather checksum
    public static uint Compute(in ReadOnlySequence<byte> sequence)
    {
        // [KYA KARTA HAI]: Agar sequence single contiguous segment hai to direct fast path call karta hai
        // [KYU ZAROORI HAI]: Loop overhead avoid karke microsecond latency bachane ke liye
        // [KAISE CHALTA HAI]: Checks sequence.IsSingleSegment
        // [OUTPUT / SIDE EFFECT]: Fast path execution
        // [VARIATIONS TO TRY]: Always use enumerator
        if (sequence.IsSingleSegment)
        {
            return Compute(sequence.FirstSpan);
        }

        // [KYA KARTA HAI]: Running accumulator initialize karta hai
        // [KYU ZAROORI HAI]: Multi-segment rolling calculation ke liye
        // [KAISE CHALTA HAI]: uint crc = 0xFFFFFFFFu
        // [OUTPUT / SIDE EFFECT]: Rolling accumulator ready
        // [VARIATIONS TO TRY]: Initial seed parameter
        uint crc = 0xFFFFFFFFu;

        // [KYA KARTA HAI]: ReadOnlySequence ke Memory segments par zero-copy foreach loop chalaata hai
        // [KYU ZAROORI HAI]: Segmented buffers ko single array me concatenate kiye bina process karne ke liye
        // [KAISE CHALTA HAI]: Enumerates ReadOnlyMemory<byte> segments
        // [OUTPUT / SIDE EFFECT]: Zero heap allocation traversal
        // [VARIATIONS TO TRY]: Custom sequence reader loop
        foreach (ReadOnlyMemory<byte> memory in sequence)
        {
            // [KYA KARTA HAI]: Memory chunk ka Span fetch karta hai
            // [KYU ZAROORI HAI]: Fast pointer-based indexing ke liye
            // [KAISE CHALTA HAI]: ReadOnlySpan<byte> span = memory.Span
            // [OUTPUT / SIDE EFFECT]: Span ready
            // [VARIATIONS TO TRY]: Direct span property
            ReadOnlySpan<byte> span = memory.Span;

            // [KYA KARTA HAI]: Segment ke har byte ko running CRC me incorporate karta hai
            // [KYU ZAROORI HAI]: Multi-chunk rolling checksum maintain karne ke liye
            // [KAISE CHALTA HAI]: for loop over span.Length
            // [OUTPUT / SIDE EFFECT]: Rolling CRC advanced across segment
            // [VARIATIONS TO TRY]: SIMD vectorize segment
            for (int i = 0; i < span.Length; i++)
            {
                crc = Table[(crc ^ span[i]) & 0xFF] ^ (crc >> 8);
            }
        }

        // [KYA KARTA HAI]: Inverted final CRC32 return karta hai
        // [KYU ZAROORI HAI]: IEEE 802.3 standard compliance ke liye
        // [KAISE CHALTA HAI]: crc ^ 0xFFFFFFFFu
        // [OUTPUT / SIDE EFFECT]: Final segmented CRC32 returned
        // [VARIATIONS TO TRY]: Custom bitmask
        return crc ^ 0xFFFFFFFFu;
    }
}
