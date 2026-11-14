// [KYA KARTA HAI]: Model and RPC namespaces import karta hai
// [KYU ZAROORI HAI]: RaftNodeRole, RaftLogEntry, RequestVoteArgs, AppendEntriesArgs use karne ke liye
// [KAISE CHALTA HAI]: Namespaces mapped
// [OUTPUT / SIDE EFFECT]: Protocol types available
// [VARIATIONS TO TRY]: Alias imports
using KiranaHub.RaftConsensus.Model;
using KiranaHub.RaftConsensus.Rpc;

// [KYA KARTA HAI]: Cluster management namespace define karta hai
// [KYU ZAROORI HAI]: Distributed consensus node and mesh primitives ko encapsulate karne ke liye
// [KAISE CHALTA HAI]: Project cluster directory map karta hai
// [OUTPUT / SIDE EFFECT]: RaftNode registered in KiranaHub.RaftConsensus.Cluster
// [VARIATIONS TO TRY]: KiranaHub.Consensus.Nodes
namespace KiranaHub.RaftConsensus.Cluster;

// [KYA KARTA HAI]: Autonomous Raft Consensus Protocol Node State Machine
// [KYU ZAROORI HAI]: High-availability dark-store metadata cluster me leader election aur fault-tolerant log consensus maintain karne ke liye
// [KAISE CHALTA HAI]: Follower, Candidate, aur Leader roles ke beech state transitions karta hai, randomized election timers run karta hai aur quorum commitments enforce karta hai
// [OUTPUT / SIDE EFFECT]: Zero split-brain consistency guarantee across node crashes and network partitions
// [VARIATIONS TO TRY]: Persistent disk Write-Ahead Logging (WAL) integration
public class RaftNode
{
    // [KYA KARTA HAI]: Synchronization lock object
    // [KYU ZAROORI HAI]: Node state mutations ko thread-safe rakhne ke liye
    // [KAISE CHALTA HAI]: lock (_lock) statement protects critical state
    // [OUTPUT / SIDE EFFECT]: Mutex synchronization
    // [VARIATIONS TO TRY]: ReaderWriterLockSlim
    private readonly object _lock = new();

    // [KYA KARTA HAI]: Node ka unique string identifier (e.g. "Node-1")
    // [KYU ZAROORI HAI]: Cluster me node identity maintain karne ke liye
    // [KAISE CHALTA HAI]: Readonly string field
    // [OUTPUT / SIDE EFFECT]: Node ID set
    // [VARIATIONS TO TRY]: URI identifier
    public string NodeId { get; }

    // [KYA KARTA HAI]: Cluster transport mesh reference
    // [KYU ZAROORI HAI]: Peers ke sath RPC messages exchange karne ke liye
    // [KAISE CHALTA HAI]: RaftClusterMesh instance
    // [OUTPUT / SIDE EFFECT]: Peer communication enabled
    // [VARIATIONS TO TRY]: gRPC network channel
    public RaftClusterMesh Mesh { get; set; } = null!;

    // [KYA KARTA HAI]: Current Raft term (latest term node has seen)
    // [KYU ZAROORI HAI]: Logical clock consensus maintain karne ke liye
    // [KAISE CHALTA HAI]: Monotonically increasing long integer
    // [OUTPUT / SIDE EFFECT]: Term counter maintained
    // [VARIATIONS TO TRY]: Atomic long
    public long CurrentTerm { get; private set; }

    // [KYA KARTA HAI]: CandidateId jo iss term me vote receive kiya (null if none)
    // [KYU ZAROORI HAI]: Per-term single vote rule enforce karne ke liye
    // [KAISE CHALTA HAI]: String candidate ID
    // [OUTPUT / SIDE EFFECT]: Voted candidate tracked
    // [VARIATIONS TO TRY]: Persistent store in file
    public string? VotedFor { get; private set; }

    // [KYA KARTA HAI]: Node ka current role (Follower, Candidate, Leader)
    // [KYU ZAROORI HAI]: Behavior decide karne ke liye
    // [KAISE CHALTA HAI]: RaftNodeRole enum
    // [OUTPUT / SIDE EFFECT]: Role updated
    // [VARIATIONS TO TRY]: State pattern with discrete role classes
    public RaftNodeRole Role { get; private set; } = RaftNodeRole.Follower;

    // [KYA KARTA HAI]: Known leader identifier
    // [KYU ZAROORI HAI]: Client writes ko leader par redirect karne ke liye
    // [KAISE CHALTA HAI]: String leader ID
    // [OUTPUT / SIDE EFFECT]: Leader tracked
    // [VARIATIONS TO TRY]: Cached leader endpoint
    public string? LeaderId { get; private set; }

    // [KYA KARTA HAI]: In-memory replicated log entries list
    // [KYU ZAROORI HAI]: Dark-store mutations ko sequentially store karne ke liye
    // [KAISE CHALTA HAI]: List of RaftLogEntry objects
    // [OUTPUT / SIDE EFFECT]: Log maintained
    // [VARIATIONS TO TRY]: Memory-mapped file log
    private readonly List<RaftLogEntry> _log = [];

    // [KYA KARTA HAI]: Highest log entry known to be committed
    // [KYU ZAROORI HAI]: State machine application point track karne ke liye
    // [KAISE CHALTA HAI]: Long integer index
    // [OUTPUT / SIDE EFFECT]: Commit progress tracked
    // [VARIATIONS TO TRY]: Monotonic assertion
    public long CommitIndex { get; private set; }

    // [KYA KARTA HAI]: Highest log entry applied to state machine
    // [KYU ZAROORI HAI]: Deterministic state machine execution ke liye
    // [KAISE CHALTA HAI]: Long integer index
    // [OUTPUT / SIDE EFFECT]: State machine applied index
    // [VARIATIONS TO TRY]: Check equality with CommitIndex
    public long LastApplied { get; private set; }

    // [KYA KARTA HAI]: Replicated key-value state machine
    // [KYU ZAROORI HAI]: Committed dark-store metadata (store status, inventory locks) hold karne ke liye
    // [KAISE CHALTA HAI]: Dictionary<string, string>
    // [OUTPUT / SIDE EFFECT]: Business state storage
    // [VARIATIONS TO TRY]: ConcurrentDictionary
    private readonly Dictionary<string, string> _stateMachine = new();

    // [KYA KARTA HAI]: Leader specific state: nextIndex for each peer
    // [KYU ZAROORI HAI]: Next log entry index to send to that peer track karne ke liye
    // [KAISE CHALTA HAI]: Map of peerId -> nextIndex
    // [OUTPUT / SIDE EFFECT]: Replication target tracking
    // [VARIATIONS TO TRY]: Initialized to leader's last log index + 1
    private readonly Dictionary<string, long> _nextIndex = new();

    // [KYA KARTA HAI]: Leader specific state: matchIndex for each peer
    // [KYU ZAROORI HAI]: Highest log entry known to be replicated on peer track karne ke liye
    // [KAISE CHALTA HAI]: Map of peerId -> matchIndex
    // [OUTPUT / SIDE EFFECT]: Quorum commitment calculation
    // [VARIATIONS TO TRY]: Initialized to 0
    private readonly Dictionary<string, long> _matchIndex = new();

    // [KYA KARTA HAI]: Last heartbeat / RPC timestamp tick
    // [KYU ZAROORI HAI]: Election timeout calculate karne ke liye
    // [KAISE CHALTA HAI]: DateTime.UtcNow.Ticks
    // [OUTPUT / SIDE EFFECT]: Timestamp updated on valid leader RPC
    // [VARIATIONS TO TRY]: Stopwatch ticks
    private long _lastHeartbeatTicks = DateTime.UtcNow.Ticks;

    // [KYA KARTA HAI]: Randomized election timeout in milliseconds
    // [KYU ZAROORI HAI]: Split-vote livelock prevent karne ke liye (150ms - 300ms)
    // [KAISE CHALTA HAI]: Random integer within range
    // [OUTPUT / SIDE EFFECT]: Jittered timeout threshold
    // [VARIATIONS TO TRY]: Seed with node ID hash
    private int _electionTimeoutMs;

    // [KYA KARTA HAI]: Node online/alive status flag
    // [KYU ZAROORI HAI]: Simulated crash aur network partition testing ke liye
    // [KAISE CHALTA HAI]: Boolean flag checked on incoming and outgoing RPCs
    // [OUTPUT / SIDE EFFECT]: Controls whether node responds
    // [VARIATIONS TO TRY]: Packet loss rate percentage
    public bool IsAlive { get; private set; } = true;

    // [KYA KARTA HAI]: RaftNode constructor
    // [KYU ZAROORI HAI]: Node identity aur randomized election timer initialize karne ke liye
    // [KAISE CHALTA HAI]: Sets NodeId and resets election timeout
    // [OUTPUT / SIDE EFFECT]: Configured Raft node
    // [VARIATIONS TO TRY]: Load term and vote from disk
    public RaftNode(string nodeId)
    {
        NodeId = nodeId;
        ResetElectionTimeout();
    }

    // [KYA KARTA HAI]: Replicated state machine snapshot dictionary expose karta hai
    // [KYU ZAROORI HAI]: Read queries aur verification tests ke liye
    // [KAISE CHALTA HAI]: Returns read-only copy of _stateMachine
    // [OUTPUT / SIDE EFFECT]: State machine inspected
    // [VARIATIONS TO TRY]: Key lookup method
    public IReadOnlyDictionary<string, string> StateMachine
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_stateMachine);
            }
        }
    }

    // [KYA KARTA HAI]: Total log entries count property
    // [KYU ZAROORI HAI]: Log synchronization verification ke liye
    // [KAISE CHALTA HAI]: Returns _log.Count
    // [OUTPUT / SIDE EFFECT]: Log length returned
    // [VARIATIONS TO TRY]: Log tail inspection
    public int LogCount
    {
        get
        {
            lock (_lock)
            {
                return _log.Count;
            }
        }
    }

    // [KYA KARTA HAI]: Jittered randomized election timeout set karta hai
    // [KYU ZAROORI HAI]: Multiple nodes simultaneously candidate banne se rokne ke liye
    // [KAISE CHALTA HAI]: Generates random 150 to 300 milliseconds
    // [OUTPUT / SIDE EFFECT]: _electionTimeoutMs populated
    // [VARIATIONS TO TRY]: 200 to 400 ms for WAN clusters
    private void ResetElectionTimeout()
    {
        int seed = HashCode.Combine(NodeId, DateTime.UtcNow.Ticks);
        Random rnd = new(seed);
        _electionTimeoutMs = rnd.Next(150, 301);
        _lastHeartbeatTicks = DateTime.UtcNow.Ticks;
    }

    // [KYA KARTA HAI]: Node ko crash simulate karke pause karta hai
    // [KYU ZAROORI HAI]: Leader crash aur partition recovery test karne ke liye
    // [KAISE CHALTA HAI]: IsAlive = false; stops responding to all RPCs
    // [OUTPUT / SIDE EFFECT]: Node disconnected
    // [VARIATIONS TO TRY]: Simulate slow packet delay
    public void SimulateCrash()
    {
        lock (_lock)
        {
            IsAlive = false;
            Role = RaftNodeRole.Follower;
        }
    }

    // [KYA KARTA HAI]: Crashed node ko re-awaken karta hai
    // [KYU ZAROORI HAI]: Partition healing aur log synchronization test karne ke liye
    // [KAISE CHALTA HAI]: IsAlive = true, role becomes Follower, resets election timer
    // [OUTPUT / SIDE EFFECT]: Node reconnects to cluster
    // [VARIATIONS TO TRY]: Replay WAL on recovery
    public void SimulateRecover()
    {
        lock (_lock)
        {
            IsAlive = true;
            Role = RaftNodeRole.Follower;
            ResetElectionTimeout();
        }
    }

    // [KYA KARTA HAI]: Periodic cluster tick handler (called by cluster runner every 25ms)
    // [KYU ZAROORI HAI]: Election timeouts trigger karne aur leader heartbeats send karne ke liye
    // [KAISE CHALTA HAI]: Checks elapsed time against _electionTimeoutMs or sends AppendEntries if leader
    // [OUTPUT / SIDE EFFECT]: Triggers state transitions
    // [VARIATIONS TO TRY]: Dedicated background timer thread
    public void OnTick()
    {
        lock (_lock)
        {
            if (!IsAlive) return;

            long now = DateTime.UtcNow.Ticks;
            long elapsedMs = (now - _lastHeartbeatTicks) / TimeSpan.TicksPerMillisecond;

            if (Role == RaftNodeRole.Leader)
            {
                // Leader broadcasts periodic heartbeat every 50ms (or replications)
                if (elapsedMs >= 50)
                {
                    BroadcastAppendEntries();
                    _lastHeartbeatTicks = now;
                }
            }
            else
            {
                // Follower or Candidate: check if election timeout elapsed
                if (elapsedMs >= _electionTimeoutMs)
                {
                    StartElection();
                }
            }
        }
    }

    // [KYA KARTA HAI]: Candidate election sequence start karta hai
    // [KYU ZAROORI HAI]: Leader timeout hone par naya leader elect karne ke liye
    // [KAISE CHALTA HAI]: Increments term, transitions to Candidate, votes for self, and sends RequestVote RPCs
    // [OUTPUT / SIDE EFFECT]: Election initiated
    // [VARIATIONS TO TRY]: Pre-vote phase
    private void StartElection()
    {
        Role = RaftNodeRole.Candidate;
        CurrentTerm++;
        VotedFor = NodeId;
        ResetElectionTimeout();

        long lastLogIndex = _log.Count;
        long lastLogTerm = _log.Count > 0 ? _log[^1].Term : 0;

        RequestVoteArgs args = new()
        {
            Term = CurrentTerm,
            CandidateId = NodeId,
            LastLogIndex = lastLogIndex,
            LastLogTerm = lastLogTerm
        };

        // Asynchronous election vote collection via Mesh
        Task.Run(() =>
        {
            int votesGranted = 1; // Votes for self
            List<RaftNode> peers = Mesh.GetPeers(NodeId);
            int quorumThreshold = (peers.Count + 1) / 2 + 1;

            foreach (RaftNode peer in peers)
            {
                RequestVoteReply reply = peer.HandleRequestVote(args);
                lock (_lock)
                {
                    if (!IsAlive || Role != RaftNodeRole.Candidate || CurrentTerm != args.Term)
                    {
                        return;
                    }

                    if (reply.Term > CurrentTerm)
                    {
                        CurrentTerm = reply.Term;
                        Role = RaftNodeRole.Follower;
                        VotedFor = null;
                        ResetElectionTimeout();
                        return;
                    }

                    if (reply.VoteGranted)
                    {
                        votesGranted++;
                        if (votesGranted >= quorumThreshold)
                        {
                            BecomeLeader();
                            return;
                        }
                    }
                }
            }
        });
    }

    // [KYA KARTA HAI]: Node ko cluster Leader promote karta hai
    // [KYU ZAROORI HAI]: Majority votes milne ke baad authoritative leader establish karne ke liye
    // [KAISE CHALTA HAI]: Sets Role = Leader, initializes nextIndex and matchIndex, broadcasts immediate heartbeat
    // [OUTPUT / SIDE EFFECT]: Leader active
    // [VARIATIONS TO TRY]: Append no-op entry on election
    private void BecomeLeader()
    {
        Role = RaftNodeRole.Leader;
        LeaderId = NodeId;

        List<RaftNode> peers = Mesh.GetPeers(NodeId);
        long lastIndex = _log.Count;

        _nextIndex.Clear();
        _matchIndex.Clear();
        foreach (RaftNode peer in peers)
        {
            _nextIndex[peer.NodeId] = lastIndex + 1;
            _matchIndex[peer.NodeId] = 0;
        }

        BroadcastAppendEntries();
    }

    // [KYA KARTA HAI]: Peer nodes ko AppendEntries (log replication ya heartbeat) broadcast karta hai
    // [KYU ZAROORI HAI]: Authority maintain karne aur quorum replication check karne ke liye
    // [KAISE CHALTA HAI]: Sends RPC to all peers; carries missing entries if peer is behind
    // [OUTPUT / SIDE EFFECT]: Log replicated and committed
    // [VARIATIONS TO TRY]: Parallel batch sending
    private void BroadcastAppendEntries()
    {
        List<RaftNode> peers = Mesh.GetPeers(NodeId);
        long term = CurrentTerm;
        long leaderCommit = CommitIndex;

        foreach (RaftNode peer in peers)
        {
            long nextIdx = _nextIndex.GetValueOrDefault(peer.NodeId, _log.Count + 1);
            long prevLogIndex = nextIdx - 1;
            long prevLogTerm = prevLogIndex > 0 && prevLogIndex <= _log.Count ? _log[(int)prevLogIndex - 1].Term : 0;

            List<RaftLogEntry> entriesToSend = [];
            // Catch up peer if peer's nextIdx is behind leader's log
            if (nextIdx <= _log.Count)
            {
                entriesToSend = _log.Skip((int)nextIdx - 1).ToList();
            }

            AppendEntriesArgs args = new()
            {
                Term = term,
                LeaderId = NodeId,
                PrevLogIndex = prevLogIndex,
                PrevLogTerm = prevLogTerm,
                Entries = entriesToSend,
                LeaderCommit = leaderCommit
            };

            Task.Run(() =>
            {
                AppendEntriesReply reply = peer.HandleAppendEntries(args);
                lock (_lock)
                {
                    if (!IsAlive || Role != RaftNodeRole.Leader || CurrentTerm != term)
                    {
                        return;
                    }

                    if (reply.Term > CurrentTerm)
                    {
                        CurrentTerm = reply.Term;
                        Role = RaftNodeRole.Follower;
                        VotedFor = null;
                        ResetElectionTimeout();
                        return;
                    }

                    if (reply.Success)
                    {
                        _nextIndex[peer.NodeId] = reply.MatchIndex + 1;
                        _matchIndex[peer.NodeId] = reply.MatchIndex;
                        CheckAndAdvanceCommitIndex();
                    }
                    else
                    {
                        // Decrement nextIndex on failure to find match
                        if (_nextIndex.TryGetValue(peer.NodeId, out long currentNext) && currentNext > 1)
                        {
                            _nextIndex[peer.NodeId] = currentNext - 1;
                        }
                    }
                }
            });
        }
    }

    // [KYA KARTA HAI]: Checks if a log entry has been replicated on a majority of nodes and advances CommitIndex
    // [KYU ZAROORI HAI]: Raft Quorum Commitment rule enforce karne ke liye ($N > \text{CommitIndex}$ and match count > $n/2$)
    // [KAISE CHALTA HAI]: Evaluates median of match indices and applies committed entries to state machine
    // [OUTPUT / SIDE EFFECT]: Commits entries and mutates _stateMachine
    // [VARIATIONS TO TRY]: Log compaction / snapshotting
    private void CheckAndAdvanceCommitIndex()
    {
        List<RaftNode> peers = Mesh.GetPeers(NodeId);
        int totalNodes = peers.Count + 1;
        int quorum = totalNodes / 2 + 1;

        for (long n = _log.Count; n > CommitIndex; n--)
        {
            if (_log[(int)n - 1].Term == CurrentTerm)
            {
                int matchCount = 1; // Self
                foreach (RaftNode peer in peers)
                {
                    if (_matchIndex.GetValueOrDefault(peer.NodeId, 0) >= n)
                    {
                        matchCount++;
                    }
                }

                if (matchCount >= quorum)
                {
                    CommitIndex = n;
                    ApplyCommittedEntries();
                    break;
                }
            }
        }
    }

    // [KYA KARTA HAI]: Committed log entries ko local state machine par apply karta hai
    // [KYU ZAROORI HAI]: LastApplied index se CommitIndex tak saare updates execute karne ke liye
    // [KAISE CHALTA HAI]: Updates _stateMachine dictionary
    // [OUTPUT / SIDE EFFECT]: Local state machine updated
    // [VARIATIONS TO TRY]: State machine snapshotting
    private void ApplyCommittedEntries()
    {
        while (LastApplied < CommitIndex)
        {
            LastApplied++;
            RaftLogEntry entry = _log[(int)LastApplied - 1];
            _stateMachine[entry.Key] = entry.Value;
        }
    }

    // [KYA KARTA HAI]: RequestVote RPC inbound message handler
    // [KYU ZAROORI HAI]: Candidate ke vote request ko evaluate karne ke liye
    // [KAISE CHALTA HAI]: Term and log completeness rules check karta hai
    // [OUTPUT / SIDE EFFECT]: Grants or denies vote
    // [VARIATIONS TO TRY]: Check candidate leader lease
    public RequestVoteReply HandleRequestVote(RequestVoteArgs args)
    {
        lock (_lock)
        {
            if (!IsAlive)
            {
                return new RequestVoteReply { Term = CurrentTerm, VoteGranted = false };
            }

            if (args.Term > CurrentTerm)
            {
                CurrentTerm = args.Term;
                Role = RaftNodeRole.Follower;
                VotedFor = null;
            }

            bool voteGranted = false;
            if (args.Term == CurrentTerm && (VotedFor == null || VotedFor == args.CandidateId))
            {
                // Raft Log Completeness Rule
                long lastLogIndex = _log.Count;
                long lastLogTerm = _log.Count > 0 ? _log[^1].Term : 0;

                bool logOk = args.LastLogTerm > lastLogTerm ||
                             (args.LastLogTerm == lastLogTerm && args.LastLogIndex >= lastLogIndex);

                if (logOk)
                {
                    voteGranted = true;
                    VotedFor = args.CandidateId;
                    ResetElectionTimeout();
                }
            }

            return new RequestVoteReply { Term = CurrentTerm, VoteGranted = voteGranted };
        }
    }

    // [KYA KARTA HAI]: AppendEntries RPC inbound message handler
    // [KYU ZAROORI HAI]: Leader heartbeats aur log replication process karne ke liye
    // [KAISE CHALTA HAI]: PrevLogIndex match check karta hai, log appends karta hai, aur CommitIndex update karta hai
    // [OUTPUT / SIDE EFFECT]: Follower log synchronized
    // [VARIATIONS TO TRY]: Conflict term detection
    public AppendEntriesReply HandleAppendEntries(AppendEntriesArgs args)
    {
        lock (_lock)
        {
            if (!IsAlive)
            {
                return new AppendEntriesReply { Term = CurrentTerm, Success = false, MatchIndex = 0 };
            }

            if (args.Term > CurrentTerm)
            {
                CurrentTerm = args.Term;
                Role = RaftNodeRole.Follower;
                VotedFor = null;
            }

            if (args.Term < CurrentTerm)
            {
                return new AppendEntriesReply { Term = CurrentTerm, Success = false, MatchIndex = 0 };
            }

            // Valid leader recognized
            Role = RaftNodeRole.Follower;
            LeaderId = args.LeaderId;
            ResetElectionTimeout();

            // Log consistency check
            if (args.PrevLogIndex > 0)
            {
                if (args.PrevLogIndex > _log.Count || _log[(int)args.PrevLogIndex - 1].Term != args.PrevLogTerm)
                {
                    return new AppendEntriesReply { Term = CurrentTerm, Success = false, MatchIndex = _log.Count };
                }
            }

            // Append new entries not already in log
            int insertIdx = (int)args.PrevLogIndex;
            foreach (RaftLogEntry newEntry in args.Entries)
            {
                if (insertIdx < _log.Count)
                {
                    if (_log[insertIdx].Term != newEntry.Term)
                    {
                        // Term conflict: truncate diverging tail
                        _log.RemoveRange(insertIdx, _log.Count - insertIdx);
                        _log.Add(newEntry);
                    }
                }
                else
                {
                    _log.Add(newEntry);
                }
                insertIdx++;
            }

            // Update commit index
            if (args.LeaderCommit > CommitIndex)
            {
                CommitIndex = Math.Min(args.LeaderCommit, _log.Count);
                ApplyCommittedEntries();
            }

            return new AppendEntriesReply { Term = CurrentTerm, Success = true, MatchIndex = _log.Count };
        }
    }

    // [KYA KARTA HAI]: Client write proposal method (e.g. updating dark store status)
    // [KYU ZAROORI HAI]: Dark-store mutations ko Raft consensus log me submit karne ke liye
    // [KAISE CHALTA HAI]: Only leader accepts write; appends to local log and triggers replication
    // [OUTPUT / SIDE EFFECT]: Returns true if leader committed entry, false if not leader
    // [VARIATIONS TO TRY]: Forward client request to leader
    public bool Propose(string key, string value)
    {
        lock (_lock)
        {
            if (!IsAlive || Role != RaftNodeRole.Leader)
            {
                return false;
            }

            RaftLogEntry newEntry = new()
            {
                Index = _log.Count + 1,
                Term = CurrentTerm,
                Key = key,
                Value = value
            };

            _log.Add(newEntry);
            BroadcastAppendEntries();
            return true;
        }
    }
}
