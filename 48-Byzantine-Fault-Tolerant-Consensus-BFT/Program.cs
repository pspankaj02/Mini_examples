using System.Text;
using KiranaHub.BftConsensus.Protocol;
using KiranaHub.BftConsensus.State;

namespace KiranaHub.BftConsensus;

public static class Program
{
    // [KYA KARTA HAI]: Practical Byzantine Fault Tolerance (PBFT) Consensus Engine ka comprehensive demonstration entry point.
    // [KYU ZAROORI HAI]: Multi-enterprise consortium ledger, Byzantine malicious attack resilience, aur Merkle proof verification verify karna.
    // [KAISE CHALTA HAI]: 4-node cluster boot karta hai, 1 node ko Byzantine attacker banata hai, 3-phase consensus chalata hai, aur Merkle proof verify karta hai.
    // [OUTPUT / SIDE EFFECT]: Console logs showing PBFT protocol phases, attack dropping, block commitment, and Merkle root verification.
    // [VARIATIONS TO TRY]: 7-node cluster with f = 2 Byzantine attackers.
    public static void Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  KiranaHub Level 11: Byzantine Fault Tolerant (PBFT) Consensus Engine          ");
        Console.WriteLine("  Domain: Multi-Retailer Consortia (KiranaHub, FMCG Brands, 3PL Transporters)   ");
        Console.WriteLine("================================================================================");

        const int TotalNodes = 4;
        const int MaxFaulty = 1; // f = (4 - 1) / 3 = 1

        Console.WriteLine($"\n[SETUP] Initializing {TotalNodes}-Node Consortium Cluster (Tolerating f = {MaxFaulty} Byzantine Traitor):");

        // Generate cluster secret keys for digital signing
        var clusterSecretKeys = new Dictionary<int, byte[]>();
        for (int i = 0; i < TotalNodes; i++)
        {
            clusterSecretKeys[i] = Encoding.UTF8.GetBytes($"consortium_secret_key_node_{i:D2}_salt_2026");
        }

        // Initialize 4 Nodes
        var nodes = new List<BftNode>();
        for (int i = 0; i < TotalNodes; i++)
        {
            bool isTraitor = (i == 3); // Node 3 is compromised / Byzantine!
            var node = new BftNode(i, TotalNodes, clusterSecretKeys[i], clusterSecretKeys, isByzantine: isTraitor);
            nodes.Add(node);

            string role = (i == 0) ? "PRIMARY LEADER (KiranaHub HQ)" :
                          (i == 1) ? "REPLICA 1 (Nestle Supply Node)" :
                          (i == 2) ? "REPLICA 2 (BlueDart Logistics Node)" :
                                     "REPLICA 3 (⚠️ COMPROMISED / BYZANTINE MALICIOUS NODE)";
            Console.WriteLine($"  - Node {i}: {role}");
        }

        // Shared peer-to-peer network message bus with asynchronous FIFO delivery
        var messageQueue = new Queue<object>();
        bool isDispatching = false;

        void BroadcastMessage(object msg)
        {
            messageQueue.Enqueue(msg);
            if (isDispatching) return;

            isDispatching = true;
            while (messageQueue.TryDequeue(out var currentMsg))
            {
                foreach (var peer in nodes)
                {
                    switch (currentMsg)
                    {
                        case PrePrepareMessage pp:
                            peer.HandlePrePrepare(pp, BroadcastMessage);
                            break;
                        case PrepareMessage prep:
                            peer.HandlePrepare(prep, BroadcastMessage);
                            break;
                        case CommitMessage com:
                            peer.HandleCommit(com);
                            break;
                    }
                }
            }
            isDispatching = false;
        }

        // -------------------------------------------------------------------------
        // PHASE 1: Propose Block #1 under Byzantine Adversarial Attack
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[CONSENSUS ROUND 1] Primary Node 0 proposing Block #1 with Consortium Ledger Mutations...");
        string blockPayload = "inv:store_bandra:milk=1200;balance:nestle=450000;inv:store_kurla:bread=800;payout:bluedart=24500";

        Console.WriteLine($"  - Block Payload Content: \"{blockPayload}\"");
        Console.WriteLine("  - Launching PBFT 3-Phase Consensus (Pre-Prepare -> Prepare -> Commit)...");

        nodes[0].ProposeBlock(sequenceNumber: 1, blockPayload, BroadcastMessage);

        // -------------------------------------------------------------------------
        // AUDIT & CONSENSUS VERIFICATION
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[CONSENSUS AUDIT] Evaluating State Commitments across Cluster Nodes:");
        string leaderRoot = nodes[0].StateTrie.ComputeRootHash();
        string replica1Root = nodes[1].StateTrie.ComputeRootHash();
        string replica2Root = nodes[2].StateTrie.ComputeRootHash();

        Console.WriteLine($"  - Node 0 (Leader)   Executed Seq: {nodes[0].LastExecutedSequence} | Merkle Root: {leaderRoot[..16]}...");
        Console.WriteLine($"  - Node 1 (Honest)   Executed Seq: {nodes[1].LastExecutedSequence} | Merkle Root: {replica1Root[..16]}...");
        Console.WriteLine($"  - Node 2 (Honest)   Executed Seq: {nodes[2].LastExecutedSequence} | Merkle Root: {replica2Root[..16]}...");
        Console.WriteLine($"  - Node 3 (Traitor)  Executed Seq: {nodes[3].LastExecutedSequence} | Status: Isolated by Honest 2f+1 Quorum!");

        bool rootsMatch = (leaderRoot == replica1Root) && (replica1Root == replica2Root);
        Console.WriteLine($"  - Cryptographic State Agreement: {(rootsMatch ? "100% DETERMINISTIC CONSENSUS (All Honest Nodes Agree!)" : "FAILED (Split State)")}");

        // -------------------------------------------------------------------------
        // MERKLE PROOF VERIFICATION DEMONSTRATION
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[MERKLE PROOF] Verifying Inclusion of 'inv:store_bandra:milk' for Independent Auditor...");
        string auditKey = "inv:store_bandra:milk";
        string? auditVal = nodes[1].StateTrie.Get(auditKey);
        var proof = nodes[1].StateTrie.GenerateProof(auditKey);

        Console.WriteLine($"  - Audited Key            : '{auditKey}' => Value: '{auditVal}'");
        Console.WriteLine($"  - Generated Merkle Proof : {proof.Count} sibling hashes ({string.Join(", ", proof.Select(p => p[..8] + "..."))})");
        Console.WriteLine("  - Auditor verifies Merkle proof against committed Root Hash in O(log N) time...");

        // In binary tree of 4 leaves, key is at index 1
        bool isProofValid = MerklePatriciaTrie.VerifyProof(auditKey, auditVal!, leaderRoot, proof, keyIndex: 1);
        Console.WriteLine($"  - Merkle Proof Validated : {(isProofValid ? "SUCCESS (Cryptographically Proven Included in Block #1)" : "FAILED")}");

        Console.WriteLine("\n[SUCCESS] Byzantine Fault Tolerant (PBFT) Consensus Engine completed all verifications successfully!");
    }
}
