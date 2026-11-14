namespace KiranaHub.BftConsensus.Protocol;

/// <summary>
/// Phase 1: Pre-Prepare Message broadcast by the Primary Leader.
/// Proposes a new block of transactions with sequence number n and view v.
/// </summary>
public sealed record PrePrepareMessage(
    int View,
    long SequenceNumber,
    string Digest,
    string BlockPayload,
    int SenderNodeId,
    string Signature);

/// <summary>
/// Phase 2: Prepare Message broadcast by replicas upon validating the Pre-Prepare.
/// Attests that node agrees with sequence number n and digest d for view v.
/// </summary>
public sealed record PrepareMessage(
    int View,
    long SequenceNumber,
    string Digest,
    int SenderNodeId,
    string Signature);

/// <summary>
/// Phase 3: Commit Message broadcast by replicas once they collect 2f Prepare messages.
/// Declares that node has formed a Prepared Certificate and is ready to commit to ledger.
/// </summary>
public sealed record CommitMessage(
    int View,
    long SequenceNumber,
    string Digest,
    int SenderNodeId,
    string Signature);

/// <summary>
/// Prepared Certificate: Pre-Prepare proposal + 2f distinct matching Prepare votes.
/// </summary>
public sealed class PreparedCertificate
{
    public PrePrepareMessage PrePrepare { get; }
    public Dictionary<int, PrepareMessage> PrepareVotes { get; } = new();

    public PreparedCertificate(PrePrepareMessage prePrepare)
    {
        PrePrepare = prePrepare;
    }

    public bool HasQuorum(int requiredQuorum) => PrepareVotes.Count >= requiredQuorum;
}

/// <summary>
/// Committed Certificate: 2f + 1 distinct matching Commit votes.
/// </summary>
public sealed class CommittedCertificate
{
    public string Digest { get; }
    public long SequenceNumber { get; }
    public Dictionary<int, CommitMessage> CommitVotes { get; } = new();

    public CommittedCertificate(string digest, long sequenceNumber)
    {
        Digest = digest;
        SequenceNumber = sequenceNumber;
    }

    public bool HasQuorum(int requiredQuorum) => CommitVotes.Count >= requiredQuorum;
}
