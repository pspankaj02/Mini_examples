// [KYA KARTA HAI]: Runtime memory interop namespace import karta hai
// [KYU ZAROORI HAI]: StructLayout attributes aur byte packing control karne ke liye
// [KAISE CHALTA HAI]: Low-level struct packing directives CLR ko provide karta hai
// [OUTPUT / SIDE EFFECT]: Memory layout attributes accessible hote hain
// [VARIATIONS TO TRY]: System.Runtime.CompilerServices include karein
using System.Runtime.InteropServices;

// [KYA KARTA HAI]: Binary RPC protocol contracts namespace define karta hai
// [KYU ZAROORI HAI]: Wire-level binary packet formats ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project ke protocol definitions ko organize karta hai
// [OUTPUT / SIDE EFFECT]: Protocol structs registered under KiranaHub.ZeroCopyRpc.Protocol
// [VARIATIONS TO TRY]: KiranaHub.Network.Framing namespace
namespace KiranaHub.ZeroCopyRpc.Protocol;

// [KYA KARTA HAI]: 16-byte fixed-size binary packet header unmanaged struct
// [KYU ZAROORI HAI]: Zero-copy byte stream framing aur instant memory casting ke liye
// [KAISE CHALTA HAI]: Pack = 1 se compiler struct me zero padding bytes rakhta hai (exact 16 bytes on wire)
// [OUTPUT / SIDE EFFECT]: Direct blittable mapping over network byte streams
// [VARIATIONS TO TRY]: 32-byte header with timestamp and flags
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BinaryPacketHeader
{
    // [KYA KARTA HAI]: Magic protocol identifier constant ("KH" = 0x4B48 in little-endian)
    // [KYU ZAROORI HAI]: Corrupt network streams me packet boundary synchronization verify karne ke liye
    // [KAISE CHALTA HAI]: Binary comparison against 0x4B48
    // [OUTPUT / SIDE EFFECT]: Prevents parsing of invalid bytes
    // [VARIATIONS TO TRY]: 4-byte magic word "KIRH"
    public const ushort ExpectedMagic = 0x4B48;

    // [KYA KARTA HAI]: Wire magic bytes (2 bytes)
    // [KYU ZAROORI HAI]: Network packet start delimiter check karne ke liye
    // [KAISE CHALTA HAI]: ushort value read from first 2 bytes of packet
    // [OUTPUT / SIDE EFFECT]: Packet boundary identified
    // [VARIATIONS TO TRY]: Dynamic magic per channel
    public ushort Magic;

    // [KYA KARTA HAI]: Protocol version identifier (1 byte)
    // [KYU ZAROORI HAI]: Backward and forward wire compatibility maintain karne ke liye
    // [KAISE CHALTA HAI]: Byte value (e.g. 1)
    // [OUTPUT / SIDE EFFECT]: Protocol version tracked
    // [VARIATIONS TO TRY]: SemVer major/minor nibbles
    public byte Version;

    // [KYA KARTA HAI]: RPC message type opcode (1 byte)
    // [KYU ZAROORI HAI]: Fast O(1) packet dispatch ke liye (1: Order, 2: Stock, 3: Ping)
    // [KAISE CHALTA HAI]: Byte opcode mapped to enum
    // [OUTPUT / SIDE EFFECT]: Direct jump table routing in parser
    // [VARIATIONS TO TRY]: ushort for 65,536 distinct RPC commands
    public byte MessageType;

    // [KYA KARTA HAI]: Monotonic packet sequence ID (4 bytes)
    // [KYU ZAROORI HAI]: Network packet reordering aur duplicate packet detection ke liye
    // [KAISE CHALTA HAI]: uint counter value
    // [OUTPUT / SIDE EFFECT]: Packet sequencing maintained
    // [VARIATIONS TO TRY]: ulong 64-bit sequence ID
    public uint SequenceId;

    // [KYA KARTA HAI]: Variable length payload byte count (4 bytes)
    // [KYU ZAROORI HAI]: PipeReader ko batane ke liye ki complete packet read karne ke liye kitne bytes chahiye
    // [KAISE CHALTA HAI]: uint integer length
    // [OUTPUT / SIDE EFFECT]: Exact frame boundary defined
    // [VARIATIONS TO TRY]: ushort if max packet is 64KB
    public uint PayloadLength;

    // [KYA KARTA HAI]: Header checksum (4 bytes)
    // [KYU ZAROORI HAI]: Corrupt header fields ko payload parsing se pehle hi reject karne ke liye
    // [KAISE CHALTA HAI]: CRC32 calculated over first 12 bytes of header
    // [OUTPUT / SIDE EFFECT]: Header data integrity guaranteed
    // [VARIATIONS TO TRY]: Adler-32 or MurmurHash3
    public uint HeaderChecksum;
}

// [KYA KARTA HAI]: Exact 32-byte binary order payload unmanaged struct
// [KYU ZAROORI HAI]: Zero heap allocation ke sath binary grocery order represent karne ke liye
// [KAISE CHALTA HAI]: MemoryMarshal.Read<BinaryOrderPayload> directly converts raw Span<byte> to struct in 0 cycles
// [OUTPUT / SIDE EFFECT]: Completely unmanaged, stack-blittable representation
// [VARIATIONS TO TRY]: Add geographical GPS coordinates as longitudes/latitudes
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BinaryOrderPayload
{
    // [KYA KARTA HAI]: Unique 64-bit numerical order identifier (8 bytes)
    // [KYU ZAROORI HAI]: High-scale order identification without string allocations
    // [KAISE CHALTA HAI]: ulong numeric ID
    // [OUTPUT / SIDE EFFECT]: Order tracked
    // [VARIATIONS TO TRY]: 128-bit GUID byte array
    public ulong OrderId;

    // [KYA KARTA HAI]: Customer numeric ID (4 bytes)
    // [KYU ZAROORI HAI]: Customer authentication aur quota check ke liye
    // [KAISE CHALTA HAI]: uint customer identifier
    // [OUTPUT / SIDE EFFECT]: Customer mapped
    // [VARIATIONS TO TRY]: Hash of customer phone number
    public uint CustomerId;

    // [KYA KARTA HAI]: Dark-store warehouse hub ID (4 bytes)
    // [KYU ZAROORI HAI]: Routing order to physical local dark-store
    // [KAISE CHALTA HAI]: uint store code
    // [OUTPUT / SIDE EFFECT]: Warehouse destination identified
    // [VARIATIONS TO TRY]: Geo-spatial grid index
    public uint StoreId;

    // [KYA KARTA HAI]: Grocery item SKU numerical code (4 bytes)
    // [KYU ZAROORI HAI]: Inventory stock deduction ke liye
    // [KAISE CHALTA HAI]: uint SKU code
    // [OUTPUT / SIDE EFFECT]: Specific item identified
    // [VARIATIONS TO TRY]: 64-bit universal barcode EAN-13
    public uint SkuId;

    // [KYA KARTA HAI]: Ordered item quantity (4 bytes)
    // [KYU ZAROORI HAI]: Order units count store karne ke liye
    // [KAISE CHALTA HAI]: uint quantity count
    // [OUTPUT / SIDE EFFECT]: Quantity specified
    // [VARIATIONS TO TRY]: Fractional grams for loose vegetables
    public uint Quantity;

    // [KYA KARTA HAI]: Unit price in paise/cents (8 bytes)
    // [KYU ZAROORI HAI]: Sub-penny integer financial accuracy without floating-point errors
    // [KAISE CHALTA HAI]: ulong integer paise (e.g., 49900 = Rs 499.00)
    // [OUTPUT / SIDE EFFECT]: Exact monetary amount
    // [VARIATIONS TO TRY]: Currency code identifier byte
    public ulong UnitPricePaise;
}
