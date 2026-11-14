// [KYA KARTA HAI]: Diagnostic and Threading namespaces import karta hai
// [KYU ZAROORI HAI]: CancellationTokenSource aur delay timers use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Async control primitives ready
// [VARIATIONS TO TRY]: BenchmarkDotNet harness
using System.Diagnostics;
using KiranaHub.RaftConsensus.Cluster;
using KiranaHub.RaftConsensus.Model;

// [KYA KARTA HAI]: RaftConsensus main namespace
// [KYU ZAROORI HAI]: Program entrypoint wrap karne ke liye
// [KAISE CHALTA HAI]: Namespace wrapping
// [OUTPUT / SIDE EFFECT]: Program registered
// [VARIATIONS TO TRY]: KiranaHub.Consensus.Main
namespace KiranaHub.RaftConsensus;

public static class Program
{
    // [KYA KARTA HAI]: Async main entry point
    // [KYU ZAROORI HAI]: Raft distributed consensus 5-node cluster simulation run karne ke liye
    // [KAISE CHALTA HAI]: Coordinates election, log replication, leader crash failover, and healing
    // [OUTPUT / SIDE EFFECT]: Console output with cluster status tables
    // [VARIATIONS TO TRY]: Interactive CLI console
    public static async Task Main(string[] args)
    {
        // [KYA KARTA HAI]: Console header banner print karta hai
        // [KYU ZAROORI HAI]: Scenario identification ke liye
        // [KAISE CHALTA HAI]: Console.WriteLine
        // [OUTPUT / SIDE EFFECT]: Banner printed
        // [VARIATIONS TO TRY]: ANSI color formatting
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 9: Distributed Consensus Engine (Raft Protocol Implementation)");
        Console.WriteLine("  Features: Leader Election, Monotonic Log Replication, Quorum Commitments, Failover");
        Console.WriteLine("================================================================================\n");

        // [KYA KARTA HAI]: Network mesh instantiate karta hai
        // [KYU ZAROORI HAI]: Nodes ke beech message passing bridge provide karne ke liye
        // [KAISE CHALTA HAI]: new RaftClusterMesh()
        // [OUTPUT / SIDE EFFECT]: Mesh ready
        // [VARIATIONS TO TRY]: Configurable network packet drop rate
        RaftClusterMesh mesh = new();

        // [KYA KARTA HAI]: 5-node cluster create aur register karta hai
        // [KYU ZAROORI HAI]: Distributed consensus quorum ($Q = 3/5$) test karne ke liye
        // [KAISE CHALTA HAI]: Creates Node-1 through Node-5 and registers in mesh
        // [OUTPUT / SIDE EFFECT]: 5 nodes initialized
        // [VARIATIONS TO TRY]: 7-node or 9-node cluster
        Console.WriteLine("[INIT] Creating 5-Node Raft Cluster (Node-1, Node-2, Node-3, Node-4, Node-5)...");
        for (int i = 1; i <= 5; i++)
        {
            RaftNode node = new($"Node-{i}");
            mesh.RegisterNode(node);
        }

        // [KYA KARTA HAI]: Cancellation token source for background cluster ticker
        // [KYU ZAROORI HAI]: Program exit par ticker gracefully stop karne ke liye
        // [KAISE CHALTA HAI]: new CancellationTokenSource()
        // [OUTPUT / SIDE EFFECT]: CTS initialized
        // [VARIATIONS TO TRY]: Link with Console.CancelKeyPress
        using CancellationTokenSource cts = new();

        // [KYA KARTA HAI]: Cluster background ticker task launch karta hai
        // [KYU ZAROORI HAI]: Randomized election timeouts aur heartbeats run karne ke liye
        // [KAISE CHALTA HAI]: mesh.StartTickerAsync(cts.Token)
        // [OUTPUT / SIDE EFFECT]: Ticker loop running every 25ms
        // [VARIATIONS TO TRY]: Isolated timer per node
        _ = mesh.StartTickerAsync(cts.Token);

        // ---------------------------------------------------------------------------------
        // PHASE 1: Initial Leader Election
        // ---------------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 1] Waiting for Initial Leader Election (Randomized Timers 150-300ms)...");
        RaftNode? leader = null;
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(50);
            leader = mesh.GetLeader();
            if (leader != null) break;
        }

        if (leader == null)
        {
            Console.WriteLine("[ERROR] Leader election timed out!");
            return;
        }

        Console.WriteLine($"  [LEADER ELECTED] -> Node: '{leader.NodeId}' in Term {leader.CurrentTerm}");
        PrintClusterStatus(mesh);

        // ---------------------------------------------------------------------------------
        // PHASE 2: Log Replication & Quorum Commitment
        // ---------------------------------------------------------------------------------
        Console.WriteLine("\n[PHASE 2] Client Proposing 3 Dark-Store Operational Metadata Entries to Leader...");
        leader.Propose("DarkStore:101:Status", "OPEN");
        leader.Propose("DarkStore:102:Status", "OPEN");
        leader.Propose("DarkStore:103:Status", "REPLENISHING");

        // Wait for replication across quorum
        await Task.Delay(250);

        Console.WriteLine("  [STATUS] Verifying Cluster-Wide Log Commitment across all nodes...");
        PrintClusterStatus(mesh);

        // Verify state machine values on leader and followers
        foreach (RaftNode n in mesh.GetAllNodes())
        {
            Console.WriteLine($"    - {n.NodeId}: CommitIndex={n.CommitIndex}, StateMachineCount={n.StateMachine.Count}, Store 101={n.StateMachine.GetValueOrDefault("DarkStore:101:Status")}");
        }

        // ---------------------------------------------------------------------------------
        // PHASE 3: Simulated Leader Crash & Automatic Failover
        // ---------------------------------------------------------------------------------
        string crashedLeaderId = leader.NodeId;
        Console.WriteLine($"\n[PHASE 3] Simulating Fatal Node Crash on Current Leader '{crashedLeaderId}'...");
        leader.SimulateCrash();
        Console.WriteLine($"  - '{crashedLeaderId}' is now OFFLINE (Stopped heartbeats and RPCs)");

        Console.WriteLine("  - Waiting for remaining 4 nodes to detect heartbeat timeout and elect NEW Leader...");
        RaftNode? newLeader = null;
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(50);
            RaftNode? current = mesh.GetLeader();
            if (current != null && current.NodeId != crashedLeaderId)
            {
                newLeader = current;
                break;
            }
        }

        if (newLeader == null)
        {
            Console.WriteLine("[ERROR] Failover leader election failed!");
            return;
        }

        Console.WriteLine($"  [NEW LEADER ELECTED] -> Node: '{newLeader.NodeId}' in Term {newLeader.CurrentTerm} (Higher Term)");
        PrintClusterStatus(mesh);

        // ---------------------------------------------------------------------------------
        // PHASE 4: Committing Under Partial Cluster (4 of 5 Nodes Active)
        // ---------------------------------------------------------------------------------
        Console.WriteLine($"\n[PHASE 4] Client Proposing Entry 4 to New Leader '{newLeader.NodeId}' with 1 node dead...");
        newLeader.Propose("DarkStore:104:Status", "OPEN");

        // Wait for quorum replication
        await Task.Delay(250);

        Console.WriteLine("  [STATUS] Verifying Entry 4 Committed by Quorum (3 of 4 active nodes)...");
        PrintClusterStatus(mesh);

        // ---------------------------------------------------------------------------------
        // PHASE 5: Partition Healing & Old Leader Re-awakening
        // ---------------------------------------------------------------------------------
        Console.WriteLine($"\n[PHASE 5] Recovering & Re-awakening Old Crashed Leader '{crashedLeaderId}'...");
        leader.SimulateRecover();
        Console.WriteLine($"  - '{crashedLeaderId}' is back ONLINE as Follower");
        Console.WriteLine("  - Waiting for authoritative log synchronization from New Leader...");

        // Wait for AppendEntries catch-up
        await Task.Delay(500);

        Console.WriteLine("\n[FINAL VERIFICATION] Final Cluster Consistency Table across ALL 5 Nodes:");
        PrintClusterStatus(mesh);

        bool allConsistent = true;
        foreach (RaftNode n in mesh.GetAllNodes())
        {
            bool hasAllKeys = n.StateMachine.ContainsKey("DarkStore:101:Status") &&
                              n.StateMachine.ContainsKey("DarkStore:102:Status") &&
                              n.StateMachine.ContainsKey("DarkStore:103:Status") &&
                              n.StateMachine.ContainsKey("DarkStore:104:Status");

            if (!hasAllKeys || n.CommitIndex != 4)
            {
                allConsistent = false;
            }
        }

        if (allConsistent)
        {
            Console.WriteLine("[SUCCESS] Raft consensus protocol passed all leader election, log replication, failover and partition healing verifications!\n");
        }
        else
        {
            Console.WriteLine("[WARNING] Cluster state divergence detected!\n");
        }

        cts.Cancel();
    }

    // [KYA KARTA HAI]: Prints formatted ASCII table of cluster node states
    // [KYU ZAROORI HAI]: Cluster status clear visualization ke liye
    // [KAISE CHALTA HAI]: Iterates over all registered nodes and prints role, term, commit index
    // [OUTPUT / SIDE EFFECT]: Formatted console table
    // [VARIATIONS TO TRY]: JSON status export
    private static void PrintClusterStatus(RaftClusterMesh mesh)
    {
        Console.WriteLine("  +----------+---------+------+-------------+--------------+------------------+");
        Console.WriteLine("  | Node ID  | Alive?  | Term | Role        | Commit Index | Replicated Logs  |");
        Console.WriteLine("  +----------+---------+------+-------------+--------------+------------------+");
        foreach (RaftNode n in mesh.GetAllNodes().OrderBy(x => x.NodeId))
        {
            string aliveStr = n.IsAlive ? "YES" : "NO (DEAD)";
            Console.WriteLine($"  | {n.NodeId,-8} | {aliveStr,-7} | {n.CurrentTerm,4} | {n.Role,-11} | {n.CommitIndex,12} | {n.LogCount,16} |");
        }
        Console.WriteLine("  +----------+---------+------+-------------+--------------+------------------+\n");
    }
}
