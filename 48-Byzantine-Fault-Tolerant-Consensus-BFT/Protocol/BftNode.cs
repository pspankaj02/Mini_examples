using System.Text;
using KiranaHub.BftConsensus.Crypto;
using KiranaHub.BftConsensus.State;

namespace KiranaHub.BftConsensus.Protocol;

/// <summary>
/// Practical Byzantine Fault Tolerant (PBFT) Cluster Node.
/// Participates in 3-phase consensus (Pre-Prepare -> Prepare -> Commit)
/// and updates its local Merkle State Trie upon forming committed certificates.
/// Can operate as an honest validator or simulate an adversarial Byzantine traitor.
/// </summary>
public sealed class BftNode
{
    public int NodeId { get; }
    public int TotalNodes { get; }
    public int MaxFaultyNodes { get; } // f = (N - 1) / 3
    public bool IsByzantine { get; set; }
    public MerklePatriciaTrie StateTrie { get; } = new();

    private readonly byte[] _secretKey;
    private readonly Dictionary<int, byte[]> _clusterKeys;
    private int _currentView = 0;
    private long _lastExecutedSequence = 0;

    // Prepared certificates store: seqNumber -> PreparedCertificate
    private readonly Dictionary<long, PreparedCertificate> _preparedCerts = new();

    // Committed certificates store: seqNumber -> CommittedCertificate
    private readonly Dictionary<long, CommittedCertificate> _committedCerts = new();

    public BftNode(
        int nodeId,
        int totalNodes,
        byte[] secretKey,
        Dictionary<int, byte[]> clusterKeys,
        bool isByzantine = false)
    {
        NodeId = nodeId;
        TotalNodes = totalNodes;
        MaxFaultyNodes = (totalNodes - 1) / 3;
        _secretKey = secretKey;
        _clusterKeys = clusterKeys;
        IsByzantine = isByzantine;
    }

    public bool IsPrimary => (_currentView % TotalNodes) == NodeId;

    // [KYA KARTA HAI]: Primary leader ke roop me naya block propose karta hai (Pre-Prepare phase).
    // [KYU ZAROORI HAI]: Consortium transactions ko sequence number assign karke consensus initiate karna.
    // [KAISE CHALTA HAI]: Digest calculate karta hai, digital signature banata hai, aur PrePrepareMessage emit karta hai.
    // [OUTPUT / SIDE EFFECT]: PrePrepareMessage broadcasted to all cluster nodes.
    // [VARIATIONS TO TRY]: Batching multiple pending transactions into single block.
    public void ProposeBlock(long sequenceNumber, string blockPayload, Action<object> broadcast)
    {
        if (!IsPrimary)
        {
            throw new InvalidOperationException($"Node {NodeId} is not the primary leader for view {_currentView}.");
        }

        string digest = BftCrypto.ComputeDigest(blockPayload);
        string signable = $"{_currentView}:{sequenceNumber}:{digest}";
        string sig = BftCrypto.Sign(signable, _secretKey);

        var prePrepare = new PrePrepareMessage(
            View: _currentView,
            SequenceNumber: sequenceNumber,
            Digest: digest,
            BlockPayload: blockPayload,
            SenderNodeId: NodeId,
            Signature: sig);

        Console.WriteLine($"[NODE {NodeId} (PRIMARY)] 📜 Pre-Prepare Proposed Block #{sequenceNumber} (Digest: {digest[..8]}...)");
        broadcast(prePrepare);

        // Leader also creates prepared certificate locally
        _preparedCerts[sequenceNumber] = new PreparedCertificate(prePrepare);
    }

    // [KYA KARTA HAI]: Pre-Prepare message handle karta hai aur validation pass hone par Prepare vote broadcast karta hai.
    // [KYU ZAROORI HAI]: Replicas verify karein ki proposal leader se aayi hai aur digest genuine hai.
    // [KAISE CHALTA HAI]: Signature verify karta hai; genuine hone par PrepareMessage broadcast karta hai.
    // [OUTPUT / SIDE EFFECT]: PrepareMessage sent to all peers.
    // [VARIATIONS TO TRY]: View-change initiation if primary is silent.
    public void HandlePrePrepare(PrePrepareMessage msg, Action<object> broadcast)
    {
        // 1. Verify primary authority for view
        int expectedPrimary = msg.View % TotalNodes;
        if (msg.SenderNodeId != expectedPrimary)
        {
            Console.WriteLine($"[NODE {NodeId}] ❌ REJECTED Pre-Prepare from unauthorized Node {msg.SenderNodeId} (Expected {expectedPrimary})");
            return;
        }

        // 2. Verify signature
        string signable = $"{msg.View}:{msg.SequenceNumber}:{msg.Digest}";
        if (!BftCrypto.Verify(signable, msg.Signature, _clusterKeys[msg.SenderNodeId]))
        {
            Console.WriteLine($"[NODE {NodeId}] ❌ REJECTED Pre-Prepare: Invalid cryptographic signature!");
            return;
        }

        // 3. Verify digest matches actual block payload
        string computedDigest = BftCrypto.ComputeDigest(msg.BlockPayload);
        if (!string.Equals(computedDigest, msg.Digest, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[NODE {NodeId}] ❌ REJECTED Pre-Prepare: Digest mismatch!");
            return;
        }

        // Initialize local prepared certificate
        _preparedCerts[msg.SequenceNumber] = new PreparedCertificate(msg);

        // Primary leader already proposed and does not need to send a prepare message
        if (NodeId == expectedPrimary)
        {
            return;
        }

        // Broadcast Prepare vote (Unless this node is acting as an adversarial Byzantine liar)
        if (IsByzantine)
        {
            // Adversary behavior: send corrupted fake prepare vote with altered digest
            string fakeDigest = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
            string fakeSig = BftCrypto.Sign($"{msg.View}:{msg.SequenceNumber}:{fakeDigest}", _secretKey);
            var badPrepare = new PrepareMessage(msg.View, msg.SequenceNumber, fakeDigest, NodeId, fakeSig);
            Console.WriteLine($"[NODE {NodeId} (BYZANTINE ATTACK)] 😈 Broadcasted corrupted Prepare vote with fake digest!");
            broadcast(badPrepare);
            return;
        }

        string prepSig = BftCrypto.Sign(signable, _secretKey);
        var prepare = new PrepareMessage(msg.View, msg.SequenceNumber, msg.Digest, NodeId, prepSig);

        Console.WriteLine($"[NODE {NodeId}] ✅ Validated Pre-Prepare #{msg.SequenceNumber}. Broadcasting Prepare vote...");
        broadcast(prepare);
    }

    // [KYA KARTA HAI]: Incoming Prepare votes collect karta hai; 2f votes milte hi Commit phase trigger karta hai.
    // [KYU ZAROORI HAI]: Ensure that a 2f quorum of replicas has acknowledged and prepared the proposal.
    // [KAISE CHALTA HAI]: Validates signature and digest; records vote; checks if 2f quorum reached.
    // [OUTPUT / SIDE EFFECT]: CommitMessage broadcasted when prepared certificate formed.
    // [VARIATIONS TO TRY]: Dynamic timeout for slow prepare phase.
    public void HandlePrepare(PrepareMessage msg, Action<object> broadcast)
    {
        // 1. Verify signature
        string signable = $"{msg.View}:{msg.SequenceNumber}:{msg.Digest}";
        if (!BftCrypto.Verify(signable, msg.Signature, _clusterKeys[msg.SenderNodeId]))
        {
            Console.WriteLine($"[NODE {NodeId}] 🛡️ DROPPED Prepare vote from Node {msg.SenderNodeId}: Forged signature!");
            return;
        }

        if (!_preparedCerts.TryGetValue(msg.SequenceNumber, out var cert))
        {
            return;
        }

        // 2. Verify digest matches our pre-prepare
        if (!string.Equals(cert.PrePrepare.Digest, msg.Digest, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[NODE {NodeId}] 🛡️ DROPPED Byzantine Prepare vote from Node {msg.SenderNodeId}: Conflicting digest!");
            return;
        }

        // Record vote
        cert.PrepareVotes[msg.SenderNodeId] = msg;

        // Check if Prepared Certificate quorum reached (2f Prepare votes)
        int requiredPrepareQuorum = 2 * MaxFaultyNodes;
        if (cert.HasQuorum(requiredPrepareQuorum) && !_committedCerts.ContainsKey(msg.SequenceNumber))
        {
            Console.WriteLine($"[NODE {NodeId}] 🏅 PREPARED CERTIFICATE FORMED for Block #{msg.SequenceNumber} ({cert.PrepareVotes.Count} votes). Broadcasting Commit...");

            _committedCerts[msg.SequenceNumber] = new CommittedCertificate(msg.Digest, msg.SequenceNumber);

            string commitSig = BftCrypto.Sign(signable, _secretKey);
            var commit = new CommitMessage(msg.View, msg.SequenceNumber, msg.Digest, NodeId, commitSig);
            broadcast(commit);
        }
    }

    // [KYA KARTA HAI]: Commit votes collect karta hai; 2f + 1 votes milne par transaction state commit karta hai.
    // [KYU ZAROORI HAI]: BFT consensus finalization: state machine permanently executes block mutations.
    // [KAISE CHALTA HAI]: Records commit vote, checks 2f+1 quorum, applies block to Merkle State Trie.
    // [OUTPUT / SIDE EFFECT]: State committed; Merkle root updated.
    // [VARIATIONS TO TRY]: Checkpointing state every 100 blocks.
    public void HandleCommit(CommitMessage msg)
    {
        string signable = $"{msg.View}:{msg.SequenceNumber}:{msg.Digest}";
        if (!BftCrypto.Verify(signable, msg.Signature, _clusterKeys[msg.SenderNodeId]))
        {
            Console.WriteLine($"[NODE {NodeId}] 🛡️ DROPPED Commit vote from Node {msg.SenderNodeId}: Forged signature!");
            return;
        }

        if (!_committedCerts.TryGetValue(msg.SequenceNumber, out var cert))
        {
            cert = new CommittedCertificate(msg.Digest, msg.SequenceNumber);
            _committedCerts[msg.SequenceNumber] = cert;
        }

        if (!string.Equals(cert.Digest, msg.Digest, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[NODE {NodeId}] 🛡️ DROPPED Byzantine Commit vote from Node {msg.SenderNodeId}: Conflicting digest!");
            return;
        }

        cert.CommitVotes[msg.SenderNodeId] = msg;

        // Check if Committed Certificate quorum reached (2f + 1 Commit votes)
        int requiredCommitQuorum = (2 * MaxFaultyNodes) + 1;
        if (cert.HasQuorum(requiredCommitQuorum) && _lastExecutedSequence < msg.SequenceNumber)
        {
            _lastExecutedSequence = msg.SequenceNumber;

            // Execute block transactions on Merkle State Trie
            if (_preparedCerts.TryGetValue(msg.SequenceNumber, out var prepCert))
            {
                ApplyBlockPayload(prepCert.PrePrepare.BlockPayload);
            }

            Console.WriteLine($"[NODE {NodeId}] 🔒 COMMITTED & EXECUTED Block #{msg.SequenceNumber}! Quorum: {cert.CommitVotes.Count}/{TotalNodes} votes. Merkle Root: {StateTrie.ComputeRootHash()[..12]}...");
        }
    }

    // [KYA KARTA HAI]: Block payload parse karke key-value state trie par mutations execute karta hai.
    // [KYU ZAROORI HAI]: Ledger state transition application (e.g. "inventory:milk=5000;balance:nestle=120000").
    // [KAISE CHALTA HAI]: Semicolon separated pairs split karke StateTrie.Put() call karta hai.
    // [OUTPUT / SIDE EFFECT]: State trie leaves updated; root hash recalculated.
    // [VARIATIONS TO TRY]: Rollback on invalid transaction format.
    private void ApplyBlockPayload(string payload)
    {
        var items = payload.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                StateTrie.Put(parts[0].Trim(), parts[1].Trim());
            }
        }
    }

    public long LastExecutedSequence => _lastExecutedSequence;
}
