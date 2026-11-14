// [KYA KARTA HAI]: Model namespace import karta hai
// [KYU ZAROORI HAI]: RaftLogEntry use karne ke liye
// [KAISE CHALTA HAI]: Namespace mapping
// [OUTPUT / SIDE EFFECT]: Model types available
// [VARIATIONS TO TRY]: Alias imports
using KiranaHub.RaftConsensus.Model;

// [KYA KARTA HAI]: RPC contracts namespace define karta hai
// [KYU ZAROORI HAI]: Raft peer-to-peer wire protocol messages ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project RPC directory map karta hai
// [OUTPUT / SIDE EFFECT]: RPC classes registered in KiranaHub.RaftConsensus.Rpc
// [VARIATIONS TO TRY]: Protobuf message definitions
namespace KiranaHub.RaftConsensus.Rpc;

// [KYA KARTA HAI]: RequestVote RPC invocation argument class
// [KYU ZAROORI HAI]: Candidates ko peers se leader election votes request karne ke liye
// [KAISE CHALTA HAI]: Term, CandidateId, LastLogIndex, LastLogTerm pass karta hai
// [OUTPUT / SIDE EFFECT]: Peer nodes vote grant or deny decision lete hain
// [VARIATIONS TO TRY]: Add candidate cluster version
public class RequestVoteArgs
{
    // [KYA KARTA HAI]: Candidate ka current election term
    // [KYU ZAROORI HAI]: Term check karke stale candidates ko reject karne ke liye
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Term identified
    // [VARIATIONS TO TRY]: Monotonic validation
    public long Term { get; set; }

    // [KYA KARTA HAI]: Candidate node ka unique identifier
    // [KYU ZAROORI HAI]: Follower ko pata chale ki kis node ne vote manga hai
    // [KAISE CHALTA HAI]: String node identifier (e.g. "Node-1")
    // [OUTPUT / SIDE EFFECT]: Candidate identified
    // [VARIATIONS TO TRY]: Integer node id
    public string CandidateId { get; set; } = string.Empty;

    // [KYA KARTA HAI]: Candidate ke last log entry ka index
    // [KYU ZAROORI HAI]: Raft election safety rule: candidate ka log voter ke log se kam se kam equally up-to-date hona chahiye
    // [KAISE CHALTA HAI]: Long integer index
    // [OUTPUT / SIDE EFFECT]: Log completeness evaluated
    // [VARIATIONS TO TRY]: Checksum of log
    public long LastLogIndex { get; set; }

    // [KYA KARTA HAI]: Candidate ke last log entry ka term
    // [KYU ZAROORI HAI]: Log freshness compare karne ke liye (higher term is fresher)
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Term freshness evaluated
    // [VARIATIONS TO TRY]: Tie-break comparison logic
    public long LastLogTerm { get; set; }
}

// [KYA KARTA HAI]: RequestVote RPC response reply class
// [KYU ZAROORI HAI]: Voter node ka decision candidate ko return karne ke liye
// [KAISE CHALTA HAI]: Holds current term and boolean voteGranted decision
// [OUTPUT / SIDE EFFECT]: Candidate counts total granted votes towards quorum
// [VARIATIONS TO TRY]: Include reason for vote rejection
public class RequestVoteReply
{
    // [KYA KARTA HAI]: Voter node ka current term
    // [KYU ZAROORI HAI]: Agar voter ka term bada ho to candidate immediately step down karke follower ban sake
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Candidate term update trigger
    // [VARIATIONS TO TRY]: Term monotonicity check
    public long Term { get; set; }

    // [KYA KARTA HAI]: Vote granting outcome boolean flag
    // [KYU ZAROORI HAI]: Check karne ke liye ki candidate ko vote mila ya nahi
    // [KAISE CHALTA HAI]: True if vote granted, false if rejected
    // [OUTPUT / SIDE EFFECT]: Candidate increments its vote tally
    // [VARIATIONS TO TRY]: Enum VoteStatus (Granted, StaleTerm, LogBehind, AlreadyVoted)
    public bool VoteGranted { get; set; }
}

// [KYA KARTA HAI]: AppendEntries RPC invocation argument class (Heartbeats and Log Replication)
// [KYU ZAROORI HAI]: Leader dwara heartbeats send karne aur log entries replicate karne ke liye
// [KAISE CHALTA HAI]: Leader log entries aur commit index followers ko send karta hai
// [OUTPUT / SIDE EFFECT]: Followers synchronize logs with leader
// [VARIATIONS TO TRY]: Compressed binary batch
public class AppendEntriesArgs
{
    // [KYA KARTA HAI]: Leader ka current term
    // [KYU ZAROORI HAI]: Follower ko authoritative leader term verify karne ke liye
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Leader authority established
    // [VARIATIONS TO TRY]: Check term validity
    public long Term { get; set; }

    // [KYA KARTA HAI]: Leader node ka identifier
    // [KYU ZAROORI HAI]: Followers clients ko leader ki taraf redirect kar sakein
    // [KAISE CHALTA HAI]: String leader ID
    // [OUTPUT / SIDE EFFECT]: Leader recorded by followers
    // [VARIATIONS TO TRY]: Leader endpoint URI
    public string LeaderId { get; set; } = string.Empty;

    // [KYA KARTA HAI]: New entries se theek pehle wali log entry ka index
    // [KYU ZAROORI HAI]: Log consistency check ke liye (Log Matching Property)
    // [KAISE CHALTA HAI]: Long integer index
    // [OUTPUT / SIDE EFFECT]: Entry alignment verified
    // [VARIATIONS TO TRY]: Sentinel index 0
    public long PrevLogIndex { get; set; }

    // [KYA KARTA HAI]: PrevLogIndex entry ka term
    // [KYU ZAROORI HAI]: Verify karne ke liye ki follower ke paas same term ka log exist karta hai
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Term continuity verified
    // [VARIATIONS TO TRY]: Check mismatch
    public long PrevLogTerm { get; set; }

    // [KYA KARTA HAI]: Log entries array to replicate (empty for heartbeat)
    // [KYU ZAROORI HAI]: State updates deliver karne ke liye
    // [KAISE CHALTA HAI]: List of RaftLogEntry objects
    // [OUTPUT / SIDE EFFECT]: Follower appends entries to its local log
    // [VARIATIONS TO TRY]: Paginated entries batch
    public List<RaftLogEntry> Entries { get; set; } = [];

    // [KYA KARTA HAI]: Leader ka current committed log index
    // [KYU ZAROORI HAI]: Followers ko batane ke liye ki kahan tak entries state machine par safely apply ki ja sakti hain
    // [KAISE CHALTA HAI]: Long integer commitIndex
    // [OUTPUT / SIDE EFFECT]: Follower updates commit index and applies to state machine
    // [VARIATIONS TO TRY]: Monotonic commit assertion
    public long LeaderCommit { get; set; }
}

// [KYA KARTA HAI]: AppendEntries RPC response reply class
// [KYU ZAROORI HAI]: Follower replication result leader ko inform karne ke liye
// [KAISE CHALTA HAI]: Success boolean and MatchIndex return karta hai
// [OUTPUT / SIDE EFFECT]: Leader tracks each follower's matchIndex towards quorum commit
// [VARIATIONS TO TRY]: Conflict index hint for fast log backtracking
public class AppendEntriesReply
{
    // [KYA KARTA HAI]: Follower ka current term
    // [KYU ZAROORI HAI]: Leader ko stale hone par step down karne ke liye
    // [KAISE CHALTA HAI]: Long integer term
    // [OUTPUT / SIDE EFFECT]: Stale leader steps down
    // [VARIATIONS TO TRY]: Term check
    public long Term { get; set; }

    // [KYA KARTA HAI]: Replication success boolean flag
    // [KYU ZAROORI HAI]: Check karne ke liye ki follower log match hua ya nahi
    // [KAISE CHALTA HAI]: True if entries accepted; false if PrevLogIndex/Term mismatch
    // [OUTPUT / SIDE EFFECT]: Leader either advances matchIndex or decrements nextIndex
    // [VARIATIONS TO TRY]: Detailed failure code
    public bool Success { get; set; }

    // [KYA KARTA HAI]: Follower ka highest matching log index
    // [KYU ZAROORI HAI]: Leader ko follower ki exact replication progress batane ke liye
    // [KAISE CHALTA HAI]: Long integer index
    // [OUTPUT / SIDE EFFECT]: Leader updates MatchIndex[follower]
    // [VARIATIONS TO TRY]: Conflict term hint
    public long MatchIndex { get; set; }
}
