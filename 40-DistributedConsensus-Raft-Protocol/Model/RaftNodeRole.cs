// [KYA KARTA HAI]: Raft consensus model namespace define karta hai
// [KYU ZAROORI HAI]: Raft state machine roles aur log entry data structures ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project ke model directory ko organize karta hai
// [OUTPUT / SIDE EFFECT]: Raft models registered in KiranaHub.RaftConsensus.Model
// [VARIATIONS TO TRY]: KiranaHub.Consensus.Raft.Types
namespace KiranaHub.RaftConsensus.Model;

// [KYA KARTA HAI]: Raft consensus protocol node roles enumeration
// [KYU ZAROORI HAI]: Cluster me har node ka exact operational state track karne ke liye
// [KAISE CHALTA HAI]: Follower -> Candidate -> Leader transitions maintain karta hai
// [OUTPUT / SIDE EFFECT]: Determines node behaviour in RPC processing
// [VARIATIONS TO TRY]: Add PreCandidate role for Raft Pre-Vote extension
public enum RaftNodeRole
{
    // [KYA KARTA HAI]: Follower role (default initial state)
    // [KYU ZAROORI HAI]: Leader se heartbeats receive karne aur log entries replicate karne ke liye
    // [KAISE CHALTA HAI]: Passive mode; election timer monitor karta hai
    // [OUTPUT / SIDE EFFECT]: Responds to leader and candidate RPCs
    // [VARIATIONS TO TRY]: Read-only follower
    Follower = 0,

    // [KYA KARTA HAI]: Candidate role
    // [KYU ZAROORI HAI]: Leader crash hone par election initiate karne ke liye
    // [KAISE CHALTA HAI]: Increments term, votes for self, and requests votes from peers
    // [OUTPUT / SIDE EFFECT]: Solicits cluster quorum votes
    // [VARIATIONS TO TRY]: Pre-candidate state before term increment
    Candidate = 1,

    // [KYA KARTA HAI]: Leader role
    // [KYU ZAROORI HAI]: Cluster state mutations coordinate karne aur log entries replicate karne ke liye
    // [KAISE CHALTA HAI]: Sends periodic heartbeats and coordinates quorum commitments
    // [OUTPUT / SIDE EFFECT]: Single authoritative writer for the cluster
    // [VARIATIONS TO TRY]: Joint consensus leader during configuration changes
    Leader = 2
}

// [KYA KARTA HAI]: Raft replicated log entry class
// [KYU ZAROORI HAI]: State machine mutations (dark store status, stock locks) ko deterministic order me record karne ke liye
// [KAISE CHALTA HAI]: Holds Index, Term, Key and Value for state machine application
// [OUTPUT / SIDE EFFECT]: Replicated across all cluster nodes
// [VARIATIONS TO TRY]: Generic payload command type
public class RaftLogEntry
{
    // [KYA KARTA HAI]: 1-based sequential log index
    // [KYU ZAROORI HAI]: Monotonic log ordering maintain karne ke liye
    // [KAISE CHALTA HAI]: Long integer index (1, 2, 3...)
    // [OUTPUT / SIDE EFFECT]: Position in log identified
    // [VARIATIONS TO TRY]: 64-bit ULID index
    public long Index { get; set; }

    // [KYA KARTA HAI]: Raft election term number jab ye entry record hui thi
    // [KYU ZAROORI HAI]: Log matching property aur leader completeness guarantee karne ke liye
    // [KAISE CHALTA HAI]: Long integer term count
    // [OUTPUT / SIDE EFFECT]: Term association recorded
    // [VARIATIONS TO TRY]: Term validation check
    public long Term { get; set; }

    // [KYA KARTA HAI]: State machine key (e.g. "DarkStore:101:Status")
    // [KYU ZAROORI HAI]: Target entity identify karne ke liye
    // [KAISE CHALTA HAI]: String key
    // [OUTPUT / SIDE EFFECT]: Key mapped in state machine
    // [VARIATIONS TO TRY]: UTF-8 byte array key
    public string Key { get; set; } = string.Empty;

    // [KYA KARTA HAI]: State machine value (e.g. "OPEN", "LOCKED", "REPLENISHING")
    // [KYU ZAROORI HAI]: Target state value store karne ke liye
    // [KAISE CHALTA HAI]: String value
    // [OUTPUT / SIDE EFFECT]: Value applied to state machine
    // [VARIATIONS TO TRY]: Serialized JSON payload
    public string Value { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Ingestion timestamp tick
    // [KYU ZAROORI HAI]: Latency tracking and auditing ke liye
    // [KAISE CHALTA HAI]: DateTime.UtcNow.Ticks
    // [OUTPUT / SIDE EFFECT]: Timestamp recorded
    // [VARIATIONS TO TRY]: High-resolution stopwatch tick
    public long TimestampTicks { get; set; } = DateTime.UtcNow.Ticks;
}
