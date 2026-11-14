// ==============================================================================================
// KiranaHub Database Sharding: Consistent Hashing Ring with Virtual Nodes (vnodes)
// [KYA KARTA HAI] Shard keys ko 32-bit integer circle par virtual nodes ke through uniformly map karta hai.
// [KYU ZAROORI HAI] Database servers add ya remove karne par 100% data reshuffle hone ke bajaye sirf 1/N keys migrate hon.
// [KAISE CHALTA HAI] MD5 cryptographic hash se uniform 32-bit ring generate karta hai; binary search se clockwise nearest node dhoondhta hai.
// [OUTPUT / SIDE EFFECT] Uniform key-to-shard distribution without hotspots.
// [VARIATIONS TO TRY] Weighted virtual nodes based on shard hardware capacity.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] String, Math, ArgumentNullException types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, SortedDictionary collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sorting and querying collections.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Cryptography namespace import karta hai.
// [KYU ZAROORI HAI] MD5 hash algorithm provide karna for uniform bit distribution.
// [KAISE CHALTA HAI] Security cryptography loaded.
// [OUTPUT / SIDE EFFECT] MD5 available.
// [VARIATIONS TO TRY] MurmurHash3 or xxHash for faster non-cryptographic hashing.
using System.Security.Cryptography;

// [KYA KARTA HAI] Text encoding namespace import karta hai.
// [KYU ZAROORI HAI] String to UTF8 bytes conversion.
// [KAISE CHALTA HAI] UTF8 encoding loaded.
// [OUTPUT / SIDE EFFECT] Encoding ready.
// [VARIATIONS TO TRY] ASCII encoding.
using System.Text;

// [KYA KARTA HAI] Namespace declare karta hai consistent hashing ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.DatabaseSharding.Hashing.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.Distributed.Hashing.
namespace KiranaHub.DatabaseSharding.Hashing;

// [KYA KARTA HAI] Consistent Hashing Ring generic class define karta hai.
// [KYU ZAROORI HAI] Distributed partitioning topology manage karna.
// [KAISE CHALTA HAI] Maintains a sorted map of uint hashes to physical nodes.
// [OUTPUT / SIDE EFFECT] Consistent hash ring instance.
// [VARIATIONS TO TRY] Rendezvous Hashing (Highest Random Weight).
public class ConsistentHashRing<TNode>
{
    // [KYA KARTA HAI] Virtual nodes count per physical node (e.g. 100 vnodes).
    // [KYU ZAROORI HAI] Statistical uniformity guarantee karna — jitne zyada vnodes honge, data utna hi evenly balance hoga.
    // [KAISE CHALTA HAI] Integer multiplier.
    // [OUTPUT / SIDE EFFECT] Vnodes count configured.
    // [VARIATIONS TO TRY] 250 vnodes for large-scale enterprise clusters.
    public int VirtualNodeCount { get; }

    // [KYA KARTA HAI] Sorted hash ring map: 32-bit unsigned integer hash -> TNode.
    // [KYU ZAROORI HAI] Hash circle par positions ascending order me sorted rakhna.
    // [KAISE CHALTA HAI] SortedDictionary (Red-Black self-balancing BST).
    // [OUTPUT / SIDE EFFECT] Ordered ring structure.
    // [VARIATIONS TO TRY] SortedList for faster binary search index.
    private readonly SortedDictionary<uint, TNode> _ring = new();

    // [KYA KARTA HAI] Physical registered nodes set.
    // [KYU ZAROORI HAI] Unique active physical nodes list maintain karna.
    // [KAISE CHALTA HAI] HashSet of TNode.
    // [OUTPUT / SIDE EFFECT] Nodes registry.
    // [VARIATIONS TO TRY] List of nodes.
    private readonly HashSet<TNode> _nodes = new();

    // [KYA KARTA HAI] Ring synchronization lock object.
    // [KYU ZAROORI HAI] Multi-threaded shard additions/removals guard karna.
    // [KAISE CHALTA HAI] Standard lock object.
    // [OUTPUT / SIDE EFFECT] Concurrency lock.
    // [VARIATIONS TO TRY] ReaderWriterLockSlim.
    private readonly object _lock = new();

    // [KYA KARTA HAI] Consistent hash ring constructor initialize karta hai.
    // [KYU ZAROORI HAI] Virtual nodes configuration set karna (default 100 vnodes per physical node).
    // [KAISE CHALTA HAI] Assigns VirtualNodeCount.
    // [OUTPUT / SIDE EFFECT] Ring instance ready.
    // [VARIATIONS TO TRY] Pass custom hash function delegate.
    public ConsistentHashRing(int virtualNodeCount = 100)
    {
        if (virtualNodeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(virtualNodeCount), "Virtual node count must be positive!");

        VirtualNodeCount = virtualNodeCount;
    }

    // [KYA KARTA HAI] String key ko 32-bit unsigned integer hash me convert karta hai.
    // [KYU ZAROORI HAI] Uniform distribution on circle [0 .. 2^32 - 1].
    // [KAISE CHALTA HAI] Computes MD5 hash bytes and converts first 4 bytes to uint.
    // [OUTPUT / SIDE EFFECT] 32-bit unsigned integer coordinate.
    // [VARIATIONS TO TRY] MurmurHash3 algorithm.
    private static uint ComputeHash(string key)
    {
        var inputBytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = MD5.HashData(inputBytes);
        // BitConverter extracts uint from first 4 bytes of MD5 digest
        return BitConverter.ToUInt32(hashBytes, 0);
    }

    // [KYA KARTA HAI] Physical node ko ring me add karta hai with its virtual nodes.
    // [KYU ZAROORI HAI] Naya database shard cluster me register karna.
    // [KAISE CHALTA HAI] For each virtual index (0..V-1), computes hash of "nodeKey#vindex" and registers on ring.
    // [OUTPUT / SIDE EFFECT] Adds V virtual nodes to ring.
    // [VARIATIONS TO TRY] Asynchronous cluster membership broadcast.
    public void AddNode(TNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        lock (_lock)
        {
            if (_nodes.Contains(node)) return;
            _nodes.Add(node);

            var nodeString = node.ToString() ?? string.Empty;
            for (int i = 0; i < VirtualNodeCount; i++)
            {
                var vnodeKey = $"{nodeString}#vnode{i}";
                var hash = ComputeHash(vnodeKey);
                _ring[hash] = node;
            }
        }
    }

    // [KYA KARTA HAI] Physical node ko ring se remove karta hai.
    // [KYU ZAROORI HAI] Shard decommissioning ya maintenance ke time node safely unregister karna.
    // [KAISE CHALTA HAI] Removes all virtual node entries matching this physical node from _ring.
    // [OUTPUT / SIDE EFFECT] Removes V virtual nodes from ring.
    // [VARIATIONS TO TRY] Drain connections before removal.
    public void RemoveNode(TNode node)
    {
        if (node == null) return;

        lock (_lock)
        {
            if (!_nodes.Contains(node)) return;
            _nodes.Remove(node);

            var nodeString = node.ToString() ?? string.Empty;
            for (int i = 0; i < VirtualNodeCount; i++)
            {
                var vnodeKey = $"{nodeString}#vnode{i}";
                var hash = ComputeHash(vnodeKey);
                _ring.Remove(hash);
            }
        }
    }

    // [KYA KARTA HAI] Key ke target shard node ko dhoondhta hai (Clockwise traversal on circle).
    // [KYU ZAROORI HAI] Data routing — batana ki yeh order/product kis database shard me belong karta hai.
    // [KAISE CHALTA HAI] Key ka hash compute karta hai; ring me pehla hash dhoondhta hai jo >= keyHash ho. Agar end tak koi nahi mila, toh wrap-around karke first element return karta hai.
    // [OUTPUT / SIDE EFFECT] Target TNode instance.
    // [VARIATIONS TO TRY] Return N replica nodes for replication topology.
    public TNode GetNode(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        lock (_lock)
        {
            if (_ring.Count == 0)
                throw new InvalidOperationException("No database shards registered in Consistent Hash Ring!");

            var keyHash = ComputeHash(key);

            // [KYA KARTA HAI] Clockwise traversal on sorted ring.
            // [KYU ZAROORI HAI] Circle par keyHash ke turant aage wala node dhoondna.
            // [KAISE CHALTA HAI] Iterates through keys; breaks on first key >= keyHash.
            // [OUTPUT / SIDE EFFECT] Matching node found.
            // [VARIATIONS TO TRY] Binary search on Keys array for O(log V) speed.
            foreach (var kvp in _ring)
            {
                if (kvp.Key >= keyHash)
                {
                    return kvp.Value;
                }
            }

            // [KYA KARTA HAI] Ring wrap-around (360 degrees circle).
            // [KYU ZAROORI HAI] Agar keyHash ring ke highest hash se bada hai, toh circle ke first node par wrap karna.
            // [KAISE CHALTA HAI] Returns _ring.First().Value.
            // [OUTPUT / SIDE EFFECT] First node returned.
            // [VARIATIONS TO TRY] None.
            return _ring.First().Value;
        }
    }

    // [KYA KARTA HAI] Saare registered physical nodes return karta hai.
    // [KYU ZAROORI HAI] Scatter-gather query engines ko all shards iterate karne ke liye.
    // [KAISE CHALTA HAI] Returns list of _nodes.
    // [OUTPUT / SIDE EFFECT] List of physical nodes.
    // [VARIATIONS TO TRY] IReadOnlyCollection.
    public IReadOnlyList<TNode> GetAllNodes()
    {
        lock (_lock)
        {
            return _nodes.ToList();
        }
    }
}
