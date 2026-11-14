# Project 40: Distributed Consensus Protocol (Raft Leader Election, Log Replication & State Machine Recovery)

## 1. Reading Roadmap ("Iss project ko scratch se kaise padhein")
Agar aap Paxos, Raft, etcd, ya Apache ZooKeeper-style distributed consensus algorithms पहली बार padh rahe hain, toh files ko iss sequence me padhein:
1. **`Model/RaftNodeRole.cs`**: Dekhein ki Raft me nodes ke 3 core roles hote hain: `Follower`, `Candidate`, aur `Leader`, aur log entries me `Index` aur `Term` ka kya role hai.
2. **`Rpc/RequestVoteRpc.cs`**: Samjhein ki candidates peers se vote mangne ke liye `RequestVote` RPC kaise bhejte hain aur voter nodes candidate ke log completeness ko kaise verify karte hain.
3. **`Rpc/AppendEntriesRpc.cs`**: Padhein ki leader periodic heartbeats aur log entry batches followers ko replicate karne ke liye `AppendEntries` RPC kaise use karta hai.
4. **`Cluster/RaftNode.cs`**: Dekhein ki randomized election timers (150ms-300ms) split-vote livelock ko kaise prevent karte hain, quorum commitment ($Q = \lfloor n/2 \rfloor + 1$) kaise calculate hoti hai, aur committed logs deterministic state machine par kaise apply hote hain.
5. **`Cluster/RaftClusterMesh.cs`**: Samjhein ki in-memory P2P network transport kaise simulated network ticks aur message dispatching handle karta hai.
6. **`Program.cs`**: Run karke complete 5-stage cluster lifecycle verify karein (Election $\to$ Replication $\to$ Leader Crash $\to$ Failover $\to$ Reconnect & Catch-up).

---

## 2. Foundations ("Kya pehle bna, kaise bna")
Distributed systems me high availability achieve karne ke liye pehle manual primary-replica failovers ya Paxos use hota tha:
- **Manual Primary-Replica (e.g. MySQL/Postgres active-passive)**:
  - Agar primary database crash ho jaye, toh human operator ko naya primary promote karna padta tha (downtime).
  - Agar network split-brain ho jaye aur dono datacenters khud ko primary samajh kar writes accept karne lagein, toh data permanently corrupt ho jata tha.
- **Classic Paxos (Leslie Lamport)**:
  - Paxos mathematically correct tha par practical software me implement karna notoriously difficult tha ("Chubby Paxos Made Live"). Paxos me leader election aur log replication unified nahi the.

---

## 3. Evolution & Mechanics ("Phir kya aaya, kyu aaya, kaise aaya, kya karta hai")

### A. Raft Consensus Protocol (Ongaro & Ousterhout)
Raft algorithm understandability aur implementation simplicity ke liye design kiya gaya:
1. **Randomized Election Timers (Split-Vote Livelock Prevention)**:
   - Agar saare nodes ka timeout same ho (e.g. exactly 200ms), toh leader crash hote hi sabhi followers ek sath Candidate ban jayenge aur apne aap ko vote de denge, jisse kisi ko bhi majority nahi milegi (split-vote).
   - Raft har node ko randomized timeout deta hai (150ms to 300ms). Jiska timer pehle expire hota hai, wo Candidate banta hai aur dusro se votes maang kar instant leader ban jata hai.
2. **Authoritative Strong Leader**:
   - Cluster me sirf Leader client writes accept karta hai. Follower clients ko leader par redirect karte hain.
   - Leader append-only entries create karta hai aur followers par replicate karta hai.
3. **Quorum Commitment Rule**:
   - Kisi bhi 5-node cluster me quorum hota hai $\lfloor 5/2 \rfloor + 1 = 3$ nodes.
   - Jab tak koi log entry majority (3 nodes) par replicate nahi hoti, leader use commit nahi karta.
4. **Log Matching & Partition Healing**:
   - Agar crashed leader (Node-1) re-awaken hota hai, toh wo naye leader (Node-2) se aane wale higher Term ke heartbeats accept karta hai, gracefully Follower ban jata hai, aur apni missing entries catch-up karke state machine ko 100% sync kar leta hai.

---

## 4. Counter-Factual Risk Analysis ("⚠️ Uske na hone se kya hoga")
1. **Agar Randomized Timers na hon (Fixed Timer)**:
   - Split-vote deadlock hoga; cluster kabhi bhi leader elect nahi kar payega aur continuous candidate timeouts me fas jayega.
2. **Agar Quorum Commitment Rule ($Q > n/2$) na ho**:
   - Network partition ke time split-brain ho jayega. 2 nodes ek partition me alag leader elect kar lenge aur 3 nodes doosre partition me alag leader elect kar lenge, leading to conflicting ledger states and double spending!
3. **Agar Log Completeness Check na ho RequestVote me**:
   - Ek aesa node leader ban sakta hai jiska log stale ho, aur wo followers ke committed orders ko overwrite kar dega (silent permanent data loss).
4. **Agar Monotonic Term Numbers na hon**:
   - Purane zombified leaders network delay ke baad wapas aakar stale entries inject kar denge.

---

## 5. How to Run & Verify
Run the standalone benchmark using .NET 10:
```powershell
dotnet run --project 40-DistributedConsensus-Raft-Protocol/KiranaHub.RaftConsensus.csproj
```

### Expected Output
```text
================================================================================
  KiranaHub Level 9: Distributed Consensus Engine (Raft Protocol Implementation)
  Features: Leader Election, Monotonic Log Replication, Quorum Commitments, Failover
================================================================================

[INIT] Creating 5-Node Raft Cluster (Node-1, Node-2, Node-3, Node-4, Node-5)...

[PHASE 1] Waiting for Initial Leader Election (Randomized Timers 150-300ms)...
  [LEADER ELECTED] -> Node: 'Node-1' in Term 1
  +----------+---------+------+-------------+--------------+------------------+
  | Node ID  | Alive?  | Term | Role        | Commit Index | Replicated Logs  |
  +----------+---------+------+-------------+--------------+------------------+
  | Node-1   | YES     |    1 | Leader      |            0 |                0 |
  | Node-2   | YES     |    1 | Follower    |            0 |                0 |
  | Node-3   | YES     |    1 | Follower    |            0 |                0 |
  | Node-4   | YES     |    1 | Follower    |            0 |                0 |
  | Node-5   | YES     |    1 | Follower    |            0 |                0 |
  +----------+---------+------+-------------+--------------+------------------+

[PHASE 2] Client Proposing 3 Dark-Store Operational Metadata Entries to Leader...
  [STATUS] Verifying Cluster-Wide Log Commitment across all nodes...
  +----------+---------+------+-------------+--------------+------------------+
  | Node ID  | Alive?  | Term | Role        | Commit Index | Replicated Logs  |
  +----------+---------+------+-------------+--------------+------------------+
  | Node-1   | YES     |    1 | Leader      |            3 |                3 |
  | Node-2   | YES     |    1 | Follower    |            3 |                3 |
  | Node-3   | YES     |    1 | Follower    |            3 |                3 |
  | Node-4   | YES     |    1 | Follower    |            3 |                3 |
  | Node-5   | YES     |    1 | Follower    |            3 |                3 |
  +----------+---------+------+-------------+--------------+------------------+

[PHASE 3] Simulating Fatal Node Crash on Current Leader 'Node-1'...
  - 'Node-1' is now OFFLINE (Stopped heartbeats and RPCs)
  - Waiting for remaining 4 nodes to detect heartbeat timeout and elect NEW Leader...
  [NEW LEADER ELECTED] -> Node: 'Node-2' in Term 2 (Higher Term)

[PHASE 4] Client Proposing Entry 4 to New Leader 'Node-2' with 1 node dead...
  [STATUS] Verifying Entry 4 Committed by Quorum (3 of 4 active nodes)...

[PHASE 5] Recovering & Re-awakening Old Crashed Leader 'Node-1'...
  - 'Node-1' is back ONLINE as Follower
  - Waiting for authoritative log synchronization from New Leader...

[FINAL VERIFICATION] Final Cluster Consistency Table across ALL 5 Nodes:
  +----------+---------+------+-------------+--------------+------------------+
  | Node ID  | Alive?  | Term | Role        | Commit Index | Replicated Logs  |
  +----------+---------+------+-------------+--------------+------------------+
  | Node-1   | YES     |    2 | Follower    |            4 |                4 |
  | Node-2   | YES     |    2 | Leader      |            4 |                4 |
  | Node-3   | YES     |    2 | Follower    |            4 |                4 |
  | Node-4   | YES     |    2 | Follower    |            4 |                4 |
  | Node-5   | YES     |    2 | Follower    |            4 |                4 |
  +----------+---------+------+-------------+--------------+------------------+

[SUCCESS] Raft consensus protocol passed all leader election, log replication, failover and partition healing verifications!
```
