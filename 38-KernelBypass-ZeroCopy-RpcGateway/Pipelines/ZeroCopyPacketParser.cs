// [KYA KARTA HAI]: Buffers and Interop namespaces import karta hai
// [KYU ZAROORI HAI]: ReadOnlySequence, SequencePosition aur MemoryMarshal direct unmanaged casting ke liye
// [KAISE CHALTA HAI]: System.Buffers aur System.Runtime.InteropServices namespaces provide karta hai
// [OUTPUT / SIDE EFFECT]: Fast zero-copy memory primitives available
// [VARIATIONS TO TRY]: System.Runtime.CompilerServices include karein
using System.Buffers;
using System.Runtime.InteropServices;
// [KYA KARTA HAI]: Protocol contracts import karta hai
// [KYU ZAROORI HAI]: BinaryPacketHeader, BinaryOrderPayload, aur Crc32Algorithm use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Protocol types available
// [VARIATIONS TO TRY]: Alias types
using KiranaHub.ZeroCopyRpc.Protocol;

// [KYA KARTA HAI]: Pipelines namespace define karta hai
// [KYU ZAROORI HAI]: Streaming zero-copy packet parser ko modularize karne ke liye
// [KAISE CHALTA HAI]: Project pipeline directory structure ko map karta hai
// [OUTPUT / SIDE EFFECT]: Types registered in KiranaHub.ZeroCopyRpc.Pipelines
// [VARIATIONS TO TRY]: KiranaHub.Network.Pipelines
namespace KiranaHub.ZeroCopyRpc.Pipelines;

// [KYA KARTA HAI]: Zero-Copy Binary Packet Parser class
// [KYU ZAROORI HAI]: Fragmented network byte streams ko zero heap allocation ke sath parse aur validate karne ke liye
// [KAISE CHALTA HAI]: ReadOnlySequence<byte> par stack memory aur unmanaged memory casting se struct slice karta hai
// [OUTPUT / SIDE EFFECT]: Sub-microsecond packet extraction without string or buffer allocations
// [VARIATIONS TO TRY]: Implement streaming parser with state machine
public static class ZeroCopyPacketParser
{
    // [KYA KARTA HAI]: Header size constant define karta hai (16 bytes)
    // [KYU ZAROORI HAI]: BinaryPacketHeader wire size reference ke liye
    // [KAISE CHALTA HAI]: sizeof(BinaryPacketHeader) equivalent constant
    // [OUTPUT / SIDE EFFECT]: 16 bytes header threshold
    // [VARIATIONS TO TRY]: Unsafe.SizeOf<BinaryPacketHeader>()
    public const int HeaderSize = 16;

    // [KYA KARTA HAI]: Trailer checksum size constant define karta hai (4 bytes for CRC32)
    // [KYU ZAROORI HAI]: Packet wire envelope calculate karne ke liye
    // [KAISE CHALTA HAI]: Constant 4 bytes
    // [OUTPUT / SIDE EFFECT]: 4 bytes trailer size
    // [VARIATIONS TO TRY]: 8 bytes for CRC64
    public const int TrailerSize = 4;

    // [KYA KARTA HAI]: Network byte sequence se single packet parse karne ki koshish karta hai
    // [KYU ZAROORI HAI]: Incomplete network frames ko buffer me accumulate hone dene aur complete frames ko instantly extract karne ke liye
    // [KAISE CHALTA HAI]: Sequence length check karta hai, header decode karta hai, CRC32 verify karta hai aur buffer advance karta hai
    // [OUTPUT / SIDE EFFECT]: Returns true agar full valid packet mila; buffer slice advance ho jata hai
    // [VARIATIONS TO TRY]: Return enum ParseResult (Success, NeedMoreData, CorruptStream)
    public static bool TryParsePacket(
        ref ReadOnlySequence<byte> buffer,
        out BinaryPacketHeader header,
        out BinaryOrderPayload orderPayload)
    {
        // [KYA KARTA HAI]: Output parameters default initialize karta hai
        // [KYU ZAROORI HAI]: C# out parameter compliance ke liye
        // [KAISE CHALTA HAI]: default struct values assigned
        // [OUTPUT / SIDE EFFECT]: Safe initial outputs
        // [VARIATIONS TO TRY]: Nullable return struct
        header = default;
        orderPayload = default;

        // [KYA KARTA HAI]: Check karta hai ki kya buffer me kam se kam 16 bytes (Header) available hain
        // [KYU ZAROORI HAI]: Underflow aur bounds violation prevent karne ke liye
        // [KAISE CHALTA HAI]: buffer.Length < HeaderSize check
        // [OUTPUT / SIDE EFFECT]: Returns false if incomplete header
        // [VARIATIONS TO TRY]: Length check optimization with IsSingleSegment
        if (buffer.Length < HeaderSize)
        {
            return false;
        }

        // [KYA KARTA HAI]: Header bytes ko safely extract karta hai (fast path if contiguous, stackalloc if split)
        // [KYU ZAROORI HAI]: Multi-segment discontiguous memory buffers ko zero heap allocation ke sath handle karne ke liye
        // [KAISE CHALTA HAI]: stackalloc byte[16] use karta hai agar segment split ho
        // [OUTPUT / SIDE EFFECT]: 16-byte span ready for casting
        // [VARIATIONS TO TRY]: Ref struct sequence reader
        Span<byte> headerBytes = stackalloc byte[HeaderSize];

        // [KYA KARTA HAI]: Agar buffer ka pehla segment contiguous hai to direct span use karta hai
        // [KYU ZAROORI HAI]: Memory copy ko completely eliminate karne ke liye
        // [KAISE CHALTA HAI]: buffer.FirstSpan.Length >= HeaderSize check
        // [OUTPUT / SIDE EFFECT]: Zero-overhead fast path
        // [VARIATIONS TO TRY]: Unsafe pointer dereference
        if (buffer.FirstSpan.Length >= HeaderSize)
        {
            header = MemoryMarshal.Read<BinaryPacketHeader>(buffer.FirstSpan[..HeaderSize]);
        }
        else
        {
            // [KYA KARTA HAI]: Split segment se 16 bytes stack buffer me copy karta hai
            // [KYU ZAROORI HAI]: Heap allocate kiye bina discontiguous header assemble karne ke liye
            // [KAISE CHALTA HAI]: buffer.Slice(0, HeaderSize).CopyTo(headerBytes)
            // [OUTPUT / SIDE EFFECT]: Assembled on CPU stack in 0 GC cycles
            // [VARIATIONS TO TRY]: Read byte by byte into struct fields
            buffer.Slice(0, HeaderSize).CopyTo(headerBytes);
            header = MemoryMarshal.Read<BinaryPacketHeader>(headerBytes);
        }

        // [KYA KARTA HAI]: Magic bytes validation ("KH" = 0x4B48)
        // [KYU ZAROORI HAI]: Stream corruption ya desynchronization catch karne ke liye
        // [KAISE CHALTA HAI]: header.Magic == BinaryPacketHeader.ExpectedMagic
        // [OUTPUT / SIDE EFFECT]: Throws or returns false on protocol violation
        // [VARIATIONS TO TRY]: Scan forward in buffer to find next magic
        if (header.Magic != BinaryPacketHeader.ExpectedMagic)
        {
            throw new InvalidOperationException($"Invalid protocol magic: 0x{header.Magic:X4}, expected 0x{BinaryPacketHeader.ExpectedMagic:X4}");
        }

        // [KYA KARTA HAI]: Total complete packet wire size calculate karta hai
        // [KYU ZAROORI HAI]: Verify karne ke liye ki full payload + trailer receive ho chuka hai ya nahi
        // [KAISE CHALTA HAI]: HeaderSize + header.PayloadLength + TrailerSize
        // [OUTPUT / SIDE EFFECT]: Total wire byte requirement computed
        // [VARIATIONS TO TRY]: Guard against payload length > 10MB
        long totalPacketLength = HeaderSize + header.PayloadLength + TrailerSize;

        // [KYA KARTA HAI]: Check karta hai ki kya full packet bytes buffer me available hain
        // [KYU ZAROORI HAI]: Partial packets ko wait karwane ke liye (PipeReader will fetch more data)
        // [KAISE CHALTA HAI]: buffer.Length < totalPacketLength check
        // [OUTPUT / SIDE EFFECT]: Returns false; parser pauses until next socket chunk arrives
        // [VARIATIONS TO TRY]: Return needed byte count hint
        if (buffer.Length < totalPacketLength)
        {
            return false;
        }

        // [KYA KARTA HAI]: Payload bytes ka zero-copy slice extract karta hai
        // [KYU ZAROORI HAI]: Checksum verify aur payload decode karne ke liye
        // [KAISE CHALTA HAI]: buffer.Slice(HeaderSize, header.PayloadLength)
        // [OUTPUT / SIDE EFFECT]: Sub-sequence representing exact payload
        // [VARIATIONS TO TRY]: Convert to ReadOnlySpan if single segment
        ReadOnlySequence<byte> payloadSequence = buffer.Slice(HeaderSize, header.PayloadLength);

        // [KYA KARTA HAI]: 4-byte trailer CRC32 slice extract karta hai
        // [KYU ZAROORI HAI]: Expected wire checksum read karne ke liye
        // [KAISE CHALTA HAI]: buffer.Slice(HeaderSize + header.PayloadLength, TrailerSize)
        // [OUTPUT / SIDE EFFECT]: Trailer sequence ready
        // [VARIATIONS TO TRY]: Fixed trailer struct mapping
        ReadOnlySequence<byte> trailerSequence = buffer.Slice(HeaderSize + header.PayloadLength, TrailerSize);

        // [KYA KARTA HAI]: Trailer CRC32 uint value read karta hai from stack span
        // [KYU ZAROORI HAI]: Binary comparison ke liye
        // [KAISE CHALTA HAI]: stackalloc byte[4] copy and MemoryMarshal.Read<uint>
        // [OUTPUT / SIDE EFFECT]: Expected CRC32 uint ready
        // [VARIATIONS TO TRY]: BinaryPrimitives.ReadUInt32LittleEndian
        Span<byte> trailerBytes = stackalloc byte[TrailerSize];
        trailerSequence.CopyTo(trailerBytes);
        uint expectedChecksum = MemoryMarshal.Read<uint>(trailerBytes);

        // [KYA KARTA HAI]: Zero-copy segmented CRC32 calculate karta hai payload par
        // [KYU ZAROORI HAI]: Data corruption (flipped bits, truncated packets) detect karne ke liye
        // [KAISE CHALTA HAI]: Crc32Algorithm.Compute(payloadSequence)
        // [OUTPUT / SIDE EFFECT]: Computed checksum value
        // [VARIATIONS TO TRY]: Hardware accelerated CRC32
        uint actualChecksum = Crc32Algorithm.Compute(payloadSequence);

        // [KYA KARTA HAI]: Checksum comparison verify karta hai
        // [KYU ZAROORI HAI]: Corrupt orders ko execute hone se rokne ke liye
        // [KAISE CHALTA HAI]: actualChecksum == expectedChecksum
        // [OUTPUT / SIDE EFFECT]: Throws exception if corrupted
        // [VARIATIONS TO TRY]: Route to telemetry drop counter
        if (actualChecksum != expectedChecksum)
        {
            throw new InvalidOperationException($"CRC32 checksum mismatch: wire 0x{expectedChecksum:X8} != computed 0x{actualChecksum:X8}");
        }

        // [KYA KARTA HAI]: BinaryOrderPayload struct ko zero-copy decode karta hai
        // [KYU ZAROORI HAI]: High-speed grocery order execution without domain object allocations
        // [KAISE CHALTA HAI]: Stack span mapping or direct memory reading
        // [OUTPUT / SIDE EFFECT]: Populated orderPayload struct
        // [VARIATIONS TO TRY]: Multi-item dynamic payload array parsing
        int payloadSize = Marshal.SizeOf<BinaryOrderPayload>();
        if (header.PayloadLength >= payloadSize)
        {
            Span<byte> orderBytes = stackalloc byte[payloadSize];
            payloadSequence.Slice(0, payloadSize).CopyTo(orderBytes);
            orderPayload = MemoryMarshal.Read<BinaryOrderPayload>(orderBytes);
        }

        // [KYA KARTA HAI]: Buffer ko consumed packet boundary se aage slice karta hai
        // [KYU ZAROORI HAI]: Processed bytes ko PipeReader se advance/discard karne ke liye
        // [KAISE CHALTA HAI]: buffer = buffer.Slice(totalPacketLength)
        // [OUTPUT / SIDE EFFECT]: Buffer ready for next packet in stream
        // [VARIATIONS TO TRY]: Pass SequencePosition directly
        buffer = buffer.Slice(totalPacketLength);

        // [KYA KARTA HAI]: Success return karta hai
        // [KYU ZAROORI HAI]: Caller ko signal karta hai ki packet successfully parsed
        // [KAISE CHALTA HAI]: Returns true
        // [OUTPUT / SIDE EFFECT]: Downstream processing proceeds
        // [VARIATIONS TO TRY]: Return batch count
        return true;
    }
}
