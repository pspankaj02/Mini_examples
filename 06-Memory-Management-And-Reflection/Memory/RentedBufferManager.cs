using System.Buffers;

namespace KiranaHub.MemoryAndReflection.Memory;

// =========================================================================================
// [CLASS PURPOSE]:
// ArrayPool<byte>.Shared buffer recycling to minimize GC pauses in high-throughput packet processing.
// =========================================================================================
public static class RentedBufferManager
{
    // [PROCESSING METHOD]:
    public static int ProcessIncomingPayload(byte[] sourceData)
    {
        // [BUFFER RENTING STATEMENT]:
        // [KYA KARTA HAI]: Shared pool se pre-allocated byte array rent karta hai.
        // [KYU ZAROORI HAI]: Avoids allocating 'new byte[4096]' on the managed heap for every packet.
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(sourceData.Length);
        try
        {
            // [BUFFER COPY]: Copies data into rented buffer
            sourceData.CopyTo(rentedBuffer, 0);

            // [CHECKSUM COMPUTATION]:
            int checksum = 0;
            for (int i = 0; i < sourceData.Length; i++)
            {
                checksum += rentedBuffer[i];
            }

            // [RETURN CHECKSUM]:
            return checksum;
        }
        finally
        {
            // [CRITICAL BUFFER RETURN IN FINALLY]:
            // [KYU ZAROORI HAI]: Rented arrays must ALWAYS be returned to avoid pool exhaustion.
            // [CLEARARRAY: TRUE]: Zeroes out the memory for security and hygiene.
            ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
        }
    }
}
