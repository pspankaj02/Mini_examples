// ==============================================================================================
// KiranaHub Kafka Streaming: High-Throughput Multi-Partition Distributed Commit Log Engine
// [KYA KARTA HAI] Apache Kafka topic partitions, consumer group offset tracking, aur high-throughput message streaming simulate karta hai.
// [KYU ZAROORI HAI] Quick-commerce me 100,000 events/second ingest karna horizontally partitioned parallel consumer pods ke sath.
// [KAISE CHALTA HAI] Key hash se partition route karta hai, append-only array me offset increment karta hai aur consumer groups ke committed offsets track karta hai.
// [OUTPUT / SIDE EFFECT] High-throughput in-memory Kafka broker abstraction.
// [VARIATIONS TO TRY] Confluent.Kafka integration with a real Kafka/Redpanda cluster.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, Math, Exception types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, Dictionary, IReadOnlyList provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] Multi-threaded broker partitions aur consumer offsets thread-safely maintain karna.
// [KAISE CHALTA HAI] ConcurrentDictionary lock-free operations provide karta hai.
// [OUTPUT / SIDE EFFECT] Thread-safe storage ready.
// [VARIATIONS TO TRY] ReaderWriterLockSlim.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Partition slicing aur offset range queries ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task async methods ke liye.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Namespace declare karta hai streaming components ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.KafkaStreaming.Streaming.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Streaming.Kafka.
namespace KiranaHub.KafkaStreaming.Streaming;

// [KYA KARTA HAI] Kafka message payload record.
// [KYU ZAROORI HAI] Key, Value, Offset, Timestamp aur Headers encapsulate karna.
// [KAISE CHALTA HAI] Immutable record with generic Key and Value.
// [OUTPUT / SIDE EFFECT] Structured stream record.
// [VARIATIONS TO TRY] Binary byte[] payload.
public record KafkaRecord<TKey, TValue>(
    TKey Key,
    TValue Value,
    int Partition,
    long Offset,
    DateTimeOffset Timestamp
);

// [KYA KARTA HAI] Individual topic partition log class.
// [KYU ZAROORI HAI] Append-only linear ordering of records within a partition guarantee karna.
// [KAISE CHALTA HAI] Thread-safe list with monotonic long offset counter.
// [OUTPUT / SIDE EFFECT] Partition commit log.
// [VARIATIONS TO TRY] Segmented file log on disk.
public class TopicPartition<TKey, TValue>
{
    // [KYA KARTA HAI] Partition ID number (0, 1, 2...).
    // [KYU ZAROORI HAI] Sharding identification.
    // [KAISE CHALTA HAI] Integer index.
    // [OUTPUT / SIDE EFFECT] Partition ID stored.
    // [VARIATIONS TO TRY] None.
    public int PartitionId { get; }

    // [KYA KARTA HAI] Internal commit log list.
    // [KYU ZAROORI HAI] Append-only records preserve karna.
    // [KAISE CHALTA HAI] List of KafkaRecord.
    // [OUTPUT / SIDE EFFECT] Records buffer.
    // [VARIATIONS TO TRY] Ring buffer for retention policy.
    private readonly List<KafkaRecord<TKey, TValue>> _records = new();

    // [KYA KARTA HAI] Partition sync lock object.
    // [KYU ZAROORI HAI] Atomic offset generation guarantee karna.
    // [KAISE CHALTA HAI] Standard lock object.
    // [OUTPUT / SIDE EFFECT] Lock token.
    // [VARIATIONS TO TRY] SpinLock.
    private readonly object _lock = new();

    // [KYA KARTA HAI] Next monotonic offset counter.
    // [KYU ZAROORI HAI] Har record ko strictly increasing offset number assign karna.
    // [KAISE CHALTA HAI] Starts at 0 and increments with each append.
    // [OUTPUT / SIDE EFFECT] Monotonic offset.
    // [VARIATIONS TO TRY] 64-bit atomic counter.
    private long _nextOffset = 0;

    // [KYA KARTA HAI] Partition constructor initialize karta hai.
    // [KYU ZAROORI HAI] Partition ID assign karna.
    // [KAISE CHALTA HAI] Assigns PartitionId property.
    // [OUTPUT / SIDE EFFECT] Partition instance ready.
    // [VARIATIONS TO TRY] None.
    public TopicPartition(int partitionId)
    {
        PartitionId = partitionId;
    }

    // [KYA KARTA HAI] Record append karta hai partition log me.
    // [KYU ZAROORI HAI] Monotonic offset assign karke commit log me store karna.
    // [KAISE CHALTA HAI] Acquires lock, assigns offset, adds record, increments offset.
    // [OUTPUT / SIDE EFFECT] Committed record with offset.
    // [VARIATIONS TO TRY] Batch append for throughput.
    public KafkaRecord<TKey, TValue> Append(TKey key, TValue value)
    {
        lock (_lock)
        {
            var offset = _nextOffset++;
            var record = new KafkaRecord<TKey, TValue>(key, value, PartitionId, offset, DateTimeOffset.UtcNow);
            _records.Add(record);
            return record;
        }
    }

    // [KYA KARTA HAI] Specified offset se records batch fetch karta hai.
    // [KYU ZAROORI HAI] Consumer group ke reading requests serve karna.
    // [KAISE CHALTA HAI] Filters records >= fromOffset up to maxBatchSize.
    // [OUTPUT / SIDE EFFECT] List of records.
    // [VARIATIONS TO TRY] Zero-copy memory slicing.
    public IReadOnlyList<KafkaRecord<TKey, TValue>> Fetch(long fromOffset, int maxBatchSize = 100)
    {
        lock (_lock)
        {
            return _records
                .Where(r => r.Offset >= fromOffset)
                .Take(maxBatchSize)
                .ToList();
        }
    }

    // [KYA KARTA HAI] Total records count in partition.
    // [KYU ZAROORI HAI] Metrics and lag monitoring.
    // [KAISE CHALTA HAI] Returns _records.Count.
    // [OUTPUT / SIDE EFFECT] Total count.
    // [VARIATIONS TO TRY] None.
    public int Count
    {
        get { lock (_lock) { return _records.Count; } }
    }
}

// [KYA KARTA HAI] Multi-partition Kafka Topic abstraction class.
// [KYU ZAROORI HAI] Multiple partitions manage karna aur key hashing se uniform distribution dena.
// [KAISE CHALTA HAI] Partitions array allocate karta hai; key hash modulo partitionCount se routing karta hai.
// [OUTPUT / SIDE EFFECT] Topic instance with partitions.
// [VARIATIONS TO TRY] Dynamic partition rebalancing.
public class KafkaTopic<TKey, TValue>
{
    // [KYA KARTA HAI] Topic name.
    // [KYU ZAROORI HAI] Identifier (e.g. "kirana.checkout.events").
    // [KAISE CHALTA HAI] String property.
    // [OUTPUT / SIDE EFFECT] Topic name stored.
    // [VARIATIONS TO TRY] None.
    public string Name { get; }

    // [KYA KARTA HAI] Partitions array.
    // [KYU ZAROORI HAI] Parallel processing streams hold karna.
    // [KAISE CHALTA HAI] Array of TopicPartition.
    // [OUTPUT / SIDE EFFECT] Partitions collection.
    // [VARIATIONS TO TRY] List of partitions.
    public TopicPartition<TKey, TValue>[] Partitions { get; }

    // [KYA KARTA HAI] Consumer group committed offsets map: "consumerGroup:partitionId" -> committedOffset.
    // [KYU ZAROORI HAI] Track karna ki kis consumer group ne kis partition me kahan tak process kiya hai.
    // [KAISE CHALTA HAI] ConcurrentDictionary keeps committed offset longs.
    // [OUTPUT / SIDE EFFECT] Offset commit registry.
    // [VARIATIONS TO TRY] Store in internal __consumer_offsets topic.
    private readonly ConcurrentDictionary<string, long> _committedOffsets = new();

    // [KYA KARTA HAI] Topic constructor initialize karta hai.
    // [KYU ZAROORI HAI] Topic name aur partition count configure karna.
    // [KAISE CHALTA HAI] Allocates array of partitions.
    // [OUTPUT / SIDE EFFECT] Topic initialized.
    // [VARIATIONS TO TRY] Configurable replica count.
    public KafkaTopic(string name, int partitionCount = 3)
    {
        Name = name;
        Partitions = new TopicPartition<TKey, TValue>[partitionCount];
        for (int i = 0; i < partitionCount; i++)
        {
            Partitions[i] = new TopicPartition<TKey, TValue>(i);
        }
    }

    // [KYA KARTA HAI] Key hashing se target partition determine karta hai: abs(Key.GetHashCode()) % PartitionCount.
    // [KYU ZAROORI HAI] Same key (e.g. same customerId or darkStoreId) hamesha same partition me jaye strictly in-order processing ke liye.
    // [KAISE CHALTA HAI] Computes hash and modulo.
    // [OUTPUT / SIDE EFFECT] Target partition index.
    // [VARIATIONS TO TRY] MurmurHash2 or custom partitioner.
    public int RouteToPartition(TKey key)
    {
        if (key == null) return 0;
        var hash = key.GetHashCode();
        var index = Math.Abs(hash) % Partitions.Length;
        return index;
    }

    // [KYA KARTA HAI] Message publish karta hai topic me.
    // [KYU ZAROORI HAI] Key hashing se partition dhoond kar append karna.
    // [KAISE CHALTA HAI] Finds partition and calls partition.Append.
    // [OUTPUT / SIDE EFFECT] Published KafkaRecord.
    // [VARIATIONS TO TRY] Asynchronous batch publishing.
    public KafkaRecord<TKey, TValue> Produce(TKey key, TValue value)
    {
        var partitionId = RouteToPartition(key);
        return Partitions[partitionId].Append(key, value);
    }

    // [KYA KARTA HAI] Consumer group ke liye uncommitted records fetch karta hai.
    // [KYU ZAROORI HAI] Consumer group offset se aage ke naye records read karna.
    // [KAISE CHALTA HAI] Reads committed offset and calls partition.Fetch.
    // [OUTPUT / SIDE EFFECT] List of unconsumed records.
    // [VARIATIONS TO TRY] Auto-commit offset flag.
    public IReadOnlyList<KafkaRecord<TKey, TValue>> Consume(string consumerGroup, int partitionId, int maxBatchSize = 100)
    {
        var key = $"{consumerGroup}:{partitionId}";
        var offset = _committedOffsets.GetOrAdd(key, 0);
        return Partitions[partitionId].Fetch(offset, maxBatchSize);
    }

    // [KYA KARTA HAI] Consumer group ka offset commit karta hai.
    // [KYU ZAROORI HAI] Process hone ke baad offset save karna taaki restart par duplicate processing na ho.
    // [KAISE CHALTA HAI] Updates _committedOffsets dictionary.
    // [OUTPUT / SIDE EFFECT] Offset committed.
    // [VARIATIONS TO TRY] Synchronous commit vs async commit.
    public void Commit(string consumerGroup, int partitionId, long processedOffset)
    {
        var key = $"{consumerGroup}:{partitionId}";
        // Kafka offset commitment points to next offset to read (processedOffset + 1)
        _committedOffsets[key] = processedOffset + 1;
    }

    // [KYA KARTA HAI] Consumer group ka consumer lag calculate karta hai.
    // [KYU ZAROORI HAI] Lag monitoring — kitne messages process hone baaki hain.
    // [KAISE CHALTA HAI] Difference between partition head count and committed offset.
    // [OUTPUT / SIDE EFFECT] Long lag count.
    // [VARIATIONS TO TRY] Alert when lag > 1000.
    public long GetLag(string consumerGroup, int partitionId)
    {
        var key = $"{consumerGroup}:{partitionId}";
        var committed = _committedOffsets.GetOrAdd(key, 0);
        var head = Partitions[partitionId].Count;
        return Math.Max(0, head - committed);
    }
}
