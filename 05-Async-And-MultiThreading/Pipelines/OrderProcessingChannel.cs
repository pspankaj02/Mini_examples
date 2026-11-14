using System.Threading.Channels;

namespace KiranaHub.AsyncAndThreads.Pipelines;

// [RECORD DECLARATION]:
public record IncomingOrder(string OrderId, string CustomerName, decimal Amount);

// =========================================================================================
// [CLASS PURPOSE]:
// High-throughput, bounded asynchronous Producer-Consumer pipeline using System.Threading.Channels.
// =========================================================================================
public class OrderProcessingChannel
{
    // [CHANNEL FIELD]:
    private readonly Channel<IncomingOrder> _channel;

    // [EXPOSED WRITER]: Exposes only writer interface to producers
    public ChannelWriter<IncomingOrder> Writer => _channel.Writer;

    // [EXPOSED READER]: Exposes only reader interface to consumers
    public ChannelReader<IncomingOrder> Reader => _channel.Reader;

    // [CONSTRUCTOR]:
    public OrderProcessingChannel(int capacity = 50)
    {
        // [OPTIONS CONFIGURATION]:
        // FullMode.Wait enables Backpressure: jab buffer full ho jata hai, producer wait karta hai.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        };

        // [CHANNEL INSTANTIATION]:
        _channel = Channel.CreateBounded<IncomingOrder>(options);
    }

    // [PRODUCER METHOD]:
    public async Task PublishOrderAsync(IncomingOrder order, CancellationToken cancellationToken = default)
    {
        // Writes order to lock-free ring buffer
        await _channel.Writer.WriteAsync(order, cancellationToken).ConfigureAwait(false);
    }

    // [COMPLETION METHOD]:
    public void CompletePublishing()
    {
        // Signals consumers that no more messages will arrive
        _channel.Writer.Complete();
    }
}
