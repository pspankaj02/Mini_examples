// ==============================================================================================
// KiranaHub Database Sharding: Regional Database Shard Models & Consistent Shard Router
// [KYA KARTA HAI] Regional dark-store database shards maintain karta hai aur ShardKey ke according transactions route karta hai.
// [KYU ZAROORI HAI] Single database bottleneck khatam karna — Delhi ka data Delhi shard me, Mumbai ka data Mumbai shard me store ho.
// [KAISE CHALTA HAI] ConsistentHashRing se target shard resolve karta hai aur isolated partition database me read/write execute karta hai.
// [OUTPUT / SIDE EFFECT] Transparent horizontal database sharding.
// [VARIATIONS TO TRY] Citus Data PostgreSQL or Vitess MySQL horizontal sharding.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, DateTimeOffset types provide karna.
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
// [KYU ZAROORI HAI] Shard in-memory tables thread-safely maintain karna.
// [KAISE CHALTA HAI] ConcurrentDictionary provides thread safety.
// [OUTPUT / SIDE EFFECT] Thread-safe storage ready.
// [VARIATIONS TO TRY] Partitioned memory buffers.
using System.Collections.Concurrent;

// [KYA KARTA HAI] Consistent hashing namespace import karta hai.
// [KYU ZAROORI HAI] ConsistentHashRing reference access karna.
// [KAISE CHALTA HAI] References local hashing namespace.
// [OUTPUT / SIDE EFFECT] ConsistentHashRing accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.DatabaseSharding.Hashing;

// [KYA KARTA HAI] Namespace declare karta hai sharding router ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.DatabaseSharding.Sharding.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Data.Sharding.
namespace KiranaHub.DatabaseSharding.Sharding;

// [KYA KARTA HAI] Sharded order database entity record.
// [KYU ZAROORI HAI] Shard table me persist hone wala document model.
// [KAISE CHALTA HAI] Immutable record with OrderId, ShardKey, CustomerId, Amount, Category, CreatedAtUtc.
// [OUTPUT / SIDE EFFECT] Sharded order entity.
// [VARIATIONS TO TRY] JSON payload column.
public record ShardOrderEntity(
    string OrderId,
    string ShardKey,
    string CustomerId,
    decimal Amount,
    string Category,
    DateTimeOffset CreatedAtUtc
);

// [KYA KARTA HAI] Physical Database Shard instance class.
// [KYU ZAROORI HAI] Independent database server/partition represent karna.
// [KAISE CHALTA HAI] Encapsulates ShardId, RegionName, and local table dictionary.
// [OUTPUT / SIDE EFFECT] Database shard object.
// [VARIATIONS TO TRY] Real ADO.NET / Npgsql connection string wrapper.
public class DatabaseShard
{
    // [KYA KARTA HAI] Shard unique identifier (e.g. "SHARD-NORTH-DELHI").
    // [KYU ZAROORI HAI] Cluster identification.
    // [KAISE CHALTA HAI] String property.
    // [OUTPUT / SIDE EFFECT] Shard ID stored.
    // [VARIATIONS TO TRY] None.
    public string ShardId { get; }

    // [KYA KARTA HAI] Geographic region name (e.g. "Delhi-NCR").
    // [KYU ZAROORI HAI] Data residency and routing compliance.
    // [KAISE CHALTA HAI] String property.
    // [OUTPUT / SIDE EFFECT] Region stored.
    // [VARIATIONS TO TRY] None.
    public string RegionName { get; }

    // [KYA KARTA HAI] Shard internal orders table: OrderId -> ShardOrderEntity.
    // [KYU ZAROORI HAI] Regional isolated storage simulate karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary storage.
    // [OUTPUT / SIDE EFFECT] Local shard table.
    // [VARIATIONS TO TRY] SQLite file per shard.
    private readonly ConcurrentDictionary<string, ShardOrderEntity> _ordersTable = new();

    // [KYA KARTA HAI] Database shard constructor initialize karta hai.
    // [KYU ZAROORI HAI] ShardId aur RegionName assign karna.
    // [KAISE CHALTA HAI] Assigns properties.
    // [OUTPUT / SIDE EFFECT] Shard instance ready.
    // [VARIATIONS TO TRY] None.
    public DatabaseShard(string shardId, string regionName)
    {
        ShardId = shardId;
        RegionName = regionName;
    }

    // [KYA KARTA HAI] Order ko local shard table me insert karta hai.
    // [KYU ZAROORI HAI] Regional database write operation.
    // [KAISE CHALTA HAI] Adds or updates _ordersTable.
    // [OUTPUT / SIDE EFFECT] Order saved in this shard.
    // [VARIATIONS TO TRY] Async database execution.
    public void InsertOrder(ShardOrderEntity order)
    {
        _ordersTable[order.OrderId] = order;
    }

    // [KYA KARTA HAI] Order ID se local shard table me search karta hai.
    // [KYU ZAROORI HAI] Regional database read operation.
    // [KAISE CHALTA HAI] Dictionary TryGetValue.
    // [OUTPUT / SIDE EFFECT] ShardOrderEntity or null.
    // [VARIATIONS TO TRY] None.
    public ShardOrderEntity? GetOrder(string orderId)
    {
        _ordersTable.TryGetValue(orderId, out var order);
        return order;
    }

    // [KYA KARTA HAI] Shard ke saare orders return karta hai.
    // [KYU ZAROORI HAI] Scatter-gather queries ke parallel aggregation ke liye.
    // [KAISE CHALTA HAI] Returns snapshot of _ordersTable.Values.
    // [OUTPUT / SIDE EFFECT] List of all orders on this shard.
    // [VARIATIONS TO TRY] Stream with IAsyncEnumerable.
    public IReadOnlyList<ShardOrderEntity> GetAllOrders()
    {
        return _ordersTable.Values.ToList();
    }

    // [KYA KARTA HAI] ToString override returns ShardId for consistent hashing ring.
    // [KYU ZAROORI HAI] ConsistentHashRing nodes ko string keys me convert karta hai.
    // [KAISE CHALTA HAI] Returns ShardId.
    // [OUTPUT / SIDE EFFECT] String representation.
    // [VARIATIONS TO TRY] None.
    public override string ToString() => ShardId;
}

// [KYA KARTA HAI] Dynamic Shard Router class.
// [KYU ZAROORI HAI] Application queries ko appropriate database shard me route karna using Consistent Hashing.
// [KAISE CHALTA HAI] Wraps ConsistentHashRing<DatabaseShard> and provides routing helper methods.
// [OUTPUT / SIDE EFFECT] Shard router instance.
// [VARIATIONS TO TRY] Dynamic read-replica routing (write to primary, read from replica).
public class ShardRouter
{
    // [KYA KARTA HAI] Consistent Hash Ring instance.
    // [KYU ZAROORI HAI] ShardKey to DatabaseShard mapping.
    // [KAISE CHALTA HAI] Managed ring with virtual nodes.
    // [OUTPUT / SIDE EFFECT] Ring handle.
    // [VARIATIONS TO TRY] None.
    private readonly ConsistentHashRing<DatabaseShard> _ring;

    // [KYA KARTA HAI] Shard router constructor initialize karta hai.
    // [KYU ZAROORI HAI] Virtual nodes count configure karna (default 100 vnodes).
    // [KAISE CHALTA HAI] Instantiates ConsistentHashRing.
    // [OUTPUT / SIDE EFFECT] Router instance ready.
    // [VARIATIONS TO TRY] None.
    public ShardRouter(int virtualNodeCount = 100)
    {
        _ring = new ConsistentHashRing<DatabaseShard>(virtualNodeCount);
    }

    // [KYA KARTA HAI] Naya database shard register karta hai.
    // [KYU ZAROORI HAI] Cluster capacity scale out karna.
    // [KAISE CHALTA HAI] Calls _ring.AddNode.
    // [OUTPUT / SIDE EFFECT] Shard registered on ring.
    // [VARIATIONS TO TRY] None.
    public void RegisterShard(DatabaseShard shard) => _ring.AddNode(shard);

    // [KYA KARTA HAI] Shard unregister karta hai.
    // [KYU ZAROORI HAI] Decommissioning.
    // [KAISE CHALTA HAI] Calls _ring.RemoveNode.
    // [OUTPUT / SIDE EFFECT] Shard removed from ring.
    // [VARIATIONS TO TRY] None.
    public void UnregisterShard(DatabaseShard shard) => _ring.RemoveNode(shard);

    // [KYA KARTA HAI] ShardKey ke according target database shard dhoondhta hai.
    // [KYU ZAROORI HAI] Determine karna kis shard me connect karna hai.
    // [KAISE CHALTA HAI] Calls _ring.GetNode(shardKey).
    // [OUTPUT / SIDE EFFECT] DatabaseShard instance.
    // [VARIATIONS TO TRY] Multi-key routing.
    public DatabaseShard ResolveShard(string shardKey) => _ring.GetNode(shardKey);

    // [KYA KARTA HAI] Order ko resolved shard me transparently write karta hai.
    // [KYU ZAROORI HAI] One-stop routed insert execution.
    // [KAISE CHALTA HAI] Resolves shard via order.ShardKey and inserts.
    // [OUTPUT / SIDE EFFECT] Inserted to target shard.
    // [VARIATIONS TO TRY] Return routed shard ID.
    public DatabaseShard RouteInsert(ShardOrderEntity order)
    {
        var targetShard = ResolveShard(order.ShardKey);
        targetShard.InsertOrder(order);
        return targetShard;
    }

    // [KYA KARTA HAI] Registered shards ki list return karta hai.
    // [KYU ZAROORI HAI] Scatter-gather queries ko all shards access karna.
    // [KAISE CHALTA HAI] Calls _ring.GetAllNodes.
    // [OUTPUT / SIDE EFFECT] List of all shards.
    // [VARIATIONS TO TRY] None.
    public IReadOnlyList<DatabaseShard> GetAllShards() => _ring.GetAllNodes();
}
