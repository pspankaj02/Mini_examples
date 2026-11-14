// ==============================================================================================
// KiranaHub Database Sharding: Parallel Scatter-Gather Cross-Shard Query Aggregator
// [KYA KARTA HAI] Multi-shard database cluster me queries ko parallel scatter (fan-out) karke partial results gather (merge) karta hai.
// [KYU ZAROORI HAI] Sharded databases me cross-shard aggregations (e.g. Total Revenue by Category across all cities) efficiently run karna.
// [KAISE CHALTA HAI] Task.WhenAll se sabhi shards ko parallel query karta hai aur in-memory LINQ GroupBy se final aggregated results merge karta hai.
// [OUTPUT / SIDE EFFECT] Cross-shard aggregated business intelligence reports.
// [VARIATIONS TO TRY] MapReduce framework with distributed shuffle stage.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai.
// [KYU ZAROORI HAI] Math, TimeSpan, Exception types ke liye.
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

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Cross-shard aggregation (GroupBy, Sum, Select).
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Threading tasks import karta hai.
// [KYU ZAROORI HAI] Task.WhenAll parallel fan-out ke liye.
// [KAISE CHALTA HAI] TPL library loaded.
// [OUTPUT / SIDE EFFECT] Async support.
// [VARIATIONS TO TRY] None.
using System.Threading.Tasks;

// [KYA KARTA HAI] Sharding namespace import karta hai.
// [KYU ZAROORI HAI] DatabaseShard and ShardOrderEntity references.
// [KAISE CHALTA HAI] References local sharding namespace.
// [OUTPUT / SIDE EFFECT] Sharding types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.DatabaseSharding.Sharding;

// [KYA KARTA HAI] Namespace declare karta hai scatter-gather engine ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.DatabaseSharding.Querying.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Data.DistributedQueries.
namespace KiranaHub.DatabaseSharding.Querying;

// [KYA KARTA HAI] Aggregated category metrics result record.
// [KYU ZAROORI HAI] Cross-shard aggregated report row encapsulate karna.
// [KAISE CHALTA HAI] Immutable record with Category, TotalOrders, TotalRevenue.
// [OUTPUT / SIDE EFFECT] Category metric record.
// [VARIATIONS TO TRY] Average order value (AOV) property.
public record CategoryRevenueSummary(string Category, int TotalOrders, decimal TotalRevenue);

// [KYA KARTA HAI] Scatter-Gather Cross-Shard Query Engine class.
// [KYU ZAROORI HAI] Multi-shard parallel execution aur result merging orchestrate karna.
// [KAISE CHALTA HAI] Fans out requests across all shards, collects partial sums, and combines into global metrics.
// [OUTPUT / SIDE EFFECT] High performance cross-shard query processor.
// [VARIATIONS TO TRY] Circuit breaker on slow shards during fan-out.
public class ScatterGatherQueryEngine
{
    // [KYA KARTA HAI] Shard router dependency reference.
    // [KYU ZAROORI HAI] Saare registered database shards access karna.
    // [KAISE CHALTA HAI] Injected via constructor.
    // [OUTPUT / SIDE EFFECT] Router reference stored.
    // [VARIATIONS TO TRY] None.
    private readonly ShardRouter _router;

    // [KYA KARTA HAI] Scatter-gather engine constructor initialize karta hai.
    // [KYU ZAROORI HAI] ShardRouter receive karna.
    // [KAISE CHALTA HAI] Assigns private field.
    // [OUTPUT / SIDE EFFECT] Engine instance ready.
    // [VARIATIONS TO TRY] None.
    public ScatterGatherQueryEngine(ShardRouter router)
    {
        _router = router;
    }

    // [KYA KARTA HAI] Saare database shards par parallel query scatter karke category revenue aggregate karta hai.
    // [KYU ZAROORI HAI] Executive BI analytics across Delhi, Mumbai, and Bengaluru shards in parallel.
    // [KAISE CHALTA HAI] Task.WhenAll runs shard queries concurrently; LINQ groups partial results by Category.
    // [OUTPUT / SIDE EFFECT] List of CategoryRevenueSummary.
    // [VARIATIONS TO TRY] Add cancellation token with timeout.
    public async Task<IReadOnlyList<CategoryRevenueSummary>> AggregateRevenueByCategoryAsync()
    {
        var allShards = _router.GetAllShards();

        // [KYA KARTA HAI] SCATTER PHASE: Har shard par query parallel me fan-out karta hai.
        // [KYU ZAROORI HAI] Sequential query karne se $N$ times latency lagti; parallel me time = $\max(\text{latency})$.
        // [KAISE CHALTA HAI] Creates list of async Tasks executing in parallel.
        // [OUTPUT / SIDE EFFECT] Parallel tasks array.
        // [VARIATIONS TO TRY] Partition throttling if 1000 shards.
        var scatterTasks = allShards.Select(async shard =>
        {
            // Simulate realistic micro-async I/O to shard
            await Task.Yield();
            return shard.GetAllOrders();
        });

        // [KYA KARTA HAI] Sabhi parallel shard queries ke complete hone ka wait karta hai.
        // [KYU ZAROORI HAI] Synchronization barrier.
        // [KAISE CHALTA HAI] Awaits Task.WhenAll.
        // [OUTPUT / SIDE EFFECT] Array of partial order collections.
        // [VARIATIONS TO TRY] Continue on partial failures (graceful partial result).
        var partialOrderSets = await Task.WhenAll(scatterTasks);

        // [KYA KARTA HAI] GATHER PHASE: Saare partial shard results ko merge karke aggregate karta hai.
        // [KYU ZAROORI HAI] Final global metrics compute karna.
        // [KAISE CHALTA HAI] Flattens lists with SelectMany, GroupBy Category, sums orders and revenue.
        // [OUTPUT / SIDE EFFECT] Merged summary report.
        // [VARIATIONS TO TRY] Stream reducer.
        var gatheredResults = partialOrderSets
            .SelectMany(orders => orders)
            .GroupBy(order => order.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryRevenueSummary(
                Category: group.Key,
                TotalOrders: group.Count(),
                TotalRevenue: group.Sum(o => o.Amount)
            ))
            .OrderByDescending(summary => summary.TotalRevenue)
            .ToList();

        return gatheredResults;
    }
}
