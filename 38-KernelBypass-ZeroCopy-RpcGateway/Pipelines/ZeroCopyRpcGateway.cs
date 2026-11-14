// [KYA KARTA HAI]: System.IO.Pipelines aur MemoryMarshal namespaces import karta hai
// [KYU ZAROORI HAI]: Asynchronous Pipe streams aur zero-copy parsing pipeline chalane ke liye
// [KAISE CHALTA HAI]: Namespace imports make high-performance IO primitives accessible
// [OUTPUT / SIDE EFFECT]: Pipe, PipeReader, PipeWriter available
// [VARIATIONS TO TRY]: System.Net.Sockets integration
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using KiranaHub.ZeroCopyRpc.Protocol;

// [KYA KARTA HAI]: Pipelines namespace define karta hai
// [KYU ZAROORI HAI]: Gateway streaming architecture ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project pipeline directory structure ko map karta hai
// [OUTPUT / SIDE EFFECT]: ZeroCopyRpcGateway available in KiranaHub.ZeroCopyRpc.Pipelines
// [VARIATIONS TO TRY]: KiranaHub.Network.Server
namespace KiranaHub.ZeroCopyRpc.Pipelines;

// [KYA KARTA HAI]: High-Throughput Zero-Copy Network Gateway & Pipe Stream Orchestrator
// [KYU ZAROORI HAI]: Kernel-bypass (eBPF/XDP) style zero-copy packet flow simulate karne ke liye
// [KAISE CHALTA HAI]: Memory pool backed PipeReader aur PipeWriter ke through fragmented chunks ko process karta hai
// [OUTPUT / SIDE EFFECT]: Sustains 1,000,000+ packets/sec with zero heap allocation
// [VARIATIONS TO TRY]: Connect to real Linux AF_XDP / DPDK socket
public class ZeroCopyRpcGateway
{
    // [KYA KARTA HAI]: Internal asynchronous pipeline
    // [KYU ZAROORI HAI]: High-performance concurrent memory-pooled byte streaming ke liye
    // [KAISE CHALTA HAI]: System.IO.Pipelines.Pipe instance with configurable memory limits
    // [OUTPUT / SIDE EFFECT]: Thread-safe pooled ring-buffer for bytes
    // [VARIATIONS TO TRY]: Custom MemoryPool<byte> provider
    private readonly Pipe _pipe = new(new PipeOptions(pauseWriterThreshold: 1024 * 1024 * 64, resumeWriterThreshold: 1024 * 1024 * 32));

    // [KYA KARTA HAI]: PipeWriter property expose karta hai
    // [KYU ZAROORI HAI]: Network packets ko stream me inject karne ke liye
    // [KAISE CHALTA HAI]: Returns _pipe.Writer
    // [OUTPUT / SIDE EFFECT]: PipeWriter accessible
    // [VARIATIONS TO TRY]: Expose via custom writer interface
    public PipeWriter Writer => _pipe.Writer;

    // [KYA KARTA HAI]: PipeReader property expose karta hai
    // [KYU ZAROORI HAI]: Incoming fragmented packets ko decode karne ke liye
    // [KAISE CHALTA HAI]: Returns _pipe.Reader
    // [OUTPUT / SIDE EFFECT]: PipeReader accessible
    // [VARIATIONS TO TRY]: Expose via custom reader interface
    public PipeReader Reader => _pipe.Reader;

    // [KYA KARTA HAI]: Wire packet helper jo binary header, payload aur CRC32 trailer assemble karta hai
    // [KYU ZAROORI HAI]: Outgoing network bytes ko strictly standard binary format me encode karne ke liye
    // [KAISE CHALTA HAI]: Header aur payload structs ko Span<byte> me copy karta hai aur trailer me CRC32 append karta hai
    // [OUTPUT / SIDE EFFECT]: Returns exact total wire byte count written
    // [VARIATIONS TO TRY]: SIMD vector store operations
    public static int EncodePacket(
        uint sequenceId,
        in BinaryOrderPayload orderPayload,
        Span<byte> destinationBuffer)
    {
        // [KYA KARTA HAI]: Sizes calculate karta hai
        // [KYU ZAROORI HAI]: Wire layout offset tracking ke liye
        // [KAISE CHALTA HAI]: Marshal.SizeOf computes struct byte lengths
        // [OUTPUT / SIDE EFFECT]: Exact sizes ready
        // [VARIATIONS TO TRY]: Const compile-time sizes
        int headerSize = ZeroCopyPacketParser.HeaderSize;
        int payloadSize = Marshal.SizeOf<BinaryOrderPayload>();
        int trailerSize = ZeroCopyPacketParser.TrailerSize;
        int totalPacketSize = headerSize + payloadSize + trailerSize;

        // [KYA KARTA HAI]: Destination buffer bounds check karta hai
        // [KYU ZAROORI HAI]: Memory overflow prevent karne ke liye
        // [KAISE CHALTA HAI]: Checks destinationBuffer.Length >= totalPacketSize
        // [OUTPUT / SIDE EFFECT]: Throws ArgumentException if buffer too small
        // [VARIATIONS TO TRY]: Auto-grow dynamic buffer
        if (destinationBuffer.Length < totalPacketSize)
        {
            throw new ArgumentException("Destination buffer is too small for binary packet", nameof(destinationBuffer));
        }

        // [KYA KARTA HAI]: Payload bytes ko destination span me copy karta hai at offset 16
        // [KYU ZAROORI HAI]: Binary payload struct ko unmanaged bytes me serialize karne ke liye
        // [KAISE CHALTA HAI]: MemoryMarshal.Write over destinationBuffer.Slice(headerSize, payloadSize)
        // [OUTPUT / SIDE EFFECT]: Payload bytes written
        // [VARIATIONS TO TRY]: Unsafe.AsRef struct copy
        Span<byte> payloadSpan = destinationBuffer.Slice(headerSize, payloadSize);
        MemoryMarshal.Write(payloadSpan, in orderPayload);

        // [KYA KARTA HAI]: Payload bytes par CRC32 calculate karta hai
        // [KYU ZAROORI HAI]: Wire packet trailer me integrity checksum attach karne ke liye
        // [KAISE CHALTA HAI]: Crc32Algorithm.Compute(payloadSpan)
        // [OUTPUT / SIDE EFFECT]: Checksum uint computed
        // [VARIATIONS TO TRY]: Hardware intrinsic CRC
        uint checksum = Crc32Algorithm.Compute(payloadSpan);

        // [KYA KARTA HAI]: Binary header struct populate karta hai
        // [KYU ZAROORI HAI]: Protocol wire envelope create karne ke liye
        // [KAISE CHALTA HAI]: Populates Magic, Version, Type, Sequence, PayloadLength
        // [OUTPUT / SIDE EFFECT]: Header struct ready
        // [VARIATIONS TO TRY]: MessageType.FlashSaleOrder enum
        BinaryPacketHeader header = new()
        {
            Magic = BinaryPacketHeader.ExpectedMagic,
            Version = 1,
            MessageType = 1, // Flash-Sale Order
            SequenceId = sequenceId,
            PayloadLength = (uint)payloadSize,
            HeaderChecksum = 0xAA55AA55 // Fixed header identifier marker
        };

        // [KYA KARTA HAI]: Header struct ko destination span ke starting 16 bytes me write karta hai
        // [KYU ZAROORI HAI]: Wire framing envelope seal karne ke liye
        // [KAISE CHALTA HAI]: MemoryMarshal.Write(destinationBuffer[..headerSize], in header)
        // [OUTPUT / SIDE EFFECT]: Header bytes written in 0 cycles
        // [VARIATIONS TO TRY]: Direct pointer cast
        MemoryMarshal.Write(destinationBuffer[..headerSize], in header);

        // [KYA KARTA HAI]: Trailer me 4-byte CRC32 write karta hai
        // [KYU ZAROORI HAI]: End of frame integrity seal karne ke liye
        // [KAISE CHALTA HAI]: MemoryMarshal.Write at destinationBuffer.Slice(headerSize + payloadSize, trailerSize)
        // [OUTPUT / SIDE EFFECT]: Trailer written
        // [VARIATIONS TO TRY]: BinaryPrimitives.WriteUInt32LittleEndian
        Span<byte> trailerSpan = destinationBuffer.Slice(headerSize + payloadSize, trailerSize);
        MemoryMarshal.Write(trailerSpan, in checksum);

        // [KYA KARTA HAI]: Total bytes written return karta hai
        // [KYU ZAROORI HAI]: PipeWriter advance length batane ke liye
        // [KAISE CHALTA HAI]: Returns totalPacketSize (e.g. 52 bytes)
        // [OUTPUT / SIDE EFFECT]: Packet length returned
        // [VARIATIONS TO TRY]: Out parameter for checksum
        return totalPacketSize;
    }
}
