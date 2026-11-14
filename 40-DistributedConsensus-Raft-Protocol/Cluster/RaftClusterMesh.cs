// [KYA KARTA HAI]: Collections namespace import karta hai
// [KYU ZAROORI HAI]: Dictionary aur List data structures use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Collections available
// [VARIATIONS TO TRY]: Immutable collections
using System.Collections.Concurrent;

// [KYA KARTA HAI]: Cluster namespace define karta hai
// [KYU ZAROORI HAI]: RaftClusterMesh transport implementation ko organize karne ke liye
// [KAISE CHALTA HAI]: Project cluster directory map karta hai
// [OUTPUT / SIDE EFFECT]: RaftClusterMesh registered in KiranaHub.RaftConsensus.Cluster
// [VARIATIONS TO TRY]: KiranaHub.Consensus.Mesh
namespace KiranaHub.RaftConsensus.Cluster;

// [KYA KARTA HAI]: Simulated In-Memory Peer-to-Peer Network Transport Mesh
// [KYU ZAROORI HAI]: Raft cluster nodes ke beech network messages deliver karne, network splits create karne aur node failures simulate karne ke liye
// [KAISE CHALTA HAI]: Maintains registered nodes map and coordinates periodic timer ticks across all nodes
// [OUTPUT / SIDE EFFECT]: Self-contained distributed cluster simulation without real socket port bindings
// [VARIATIONS TO TRY]: Real TCP/gRPC multiplexed transport
public class RaftClusterMesh
{
    // [KYA KARTA HAI]: Thread-safe dictionary holding all active cluster nodes
    // [KYU ZAROORI HAI]: Node lookup by nodeId ke liye
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, RaftNode>
    // [OUTPUT / SIDE EFFECT]: Node registry maintained
    // [VARIATIONS TO TRY]: Dynamic node addition/removal during membership changes
    private readonly ConcurrentDictionary<string, RaftNode> _nodes = new();

    // [KYA KARTA HAI]: Cluster node ko mesh me register karta hai
    // [KYU ZAROORI HAI]: Node ko peer communication capabilities provide karne ke liye
    // [KAISE CHALTA HAI]: Adds node to map and sets node.Mesh = this
    // [OUTPUT / SIDE EFFECT]: Node attached to network mesh
    // [VARIATIONS TO TRY]: Validate unique node IDs
    public void RegisterNode(RaftNode node)
    {
        node.Mesh = this;
        _nodes[node.NodeId] = node;
    }

    // [KYA KARTA HAI]: Given node ke saare peer nodes return karta hai
    // [KYU ZAROORI HAI]: RequestVote aur AppendEntries broadcast karne ke liye
    // [KAISE CHALTA HAI]: Filters out self node ID and returns list
    // [OUTPUT / SIDE EFFECT]: List of peer nodes
    // [VARIATIONS TO TRY]: Network partition filtering
    public List<RaftNode> GetPeers(string nodeId)
    {
        return _nodes.Values.Where(n => n.NodeId != nodeId).ToList();
    }

    // [KYA KARTA HAI]: Cluster me currently elected leader find karta hai
    // [KYU ZAROORI HAI]: Client proposal dispatch karne aur status monitor karne ke liye
    // [KAISE CHALTA HAI]: Finds node with Role == RaftNodeRole.Leader and IsAlive == true
    // [OUTPUT / SIDE EFFECT]: Returns current leader node or null
    // [VARIATIONS TO TRY]: Check term freshness
    public RaftNode? GetLeader()
    {
        return _nodes.Values.FirstOrDefault(n => n.IsAlive && n.Role == Model.RaftNodeRole.Leader);
    }

    // [KYA KARTA HAI]: Saare registered nodes list return karta hai
    // [KYU ZAROORI HAI]: Cluster-wide state inspection ke liye
    // [KAISE CHALTA HAI]: Returns _nodes.Values.ToList()
    // [OUTPUT / SIDE EFFECT]: All nodes list
    // [VARIATIONS TO TRY]: Readonly collection
    public List<RaftNode> GetAllNodes()
    {
        return _nodes.Values.ToList();
    }

    // [KYA KARTA HAI]: High-frequency periodic tick background worker loop launch karta hai
    // [KYU ZAROORI HAI]: Randomized election timeouts aur leader heartbeats ko synchronize karne ke liye
    // [KAISE CHALTA HAI]: Har 25ms par saare nodes par node.OnTick() invoke karta hai
    // [OUTPUT / SIDE EFFECT]: Cluster event loop running in background
    // [VARIATIONS TO TRY]: Per-node independent timer threads
    public Task StartTickerAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (RaftNode node in _nodes.Values)
                {
                    try
                    {
                        node.OnTick();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RaftMesh ERROR] Node {node.NodeId} tick exception: {ex.Message}");
                    }
                }
                await Task.Delay(25, cancellationToken);
            }
        }, cancellationToken);
    }
}
