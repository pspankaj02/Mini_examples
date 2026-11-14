using System.Security.Cryptography;
using System.Text;

namespace KiranaHub.BftConsensus.State;

/// <summary>
/// Cryptographic Merkle State Trie.
/// Authenticates the entire quick-commerce inventory and financial state into an immutable 32-byte Root Hash.
/// Enables O(log N) lightweight cryptographic inclusion proofs (Merkle Proofs).
/// </summary>
public sealed class MerklePatriciaTrie
{
    // [KYA KARTA HAI]: In-memory sorted key-value store jahan current state store hoti hai.
    // [KYU ZAROORI HAI]: Deterministic root hash calculation ke liye keys sorted order me process honi chahiye.
    // [KAISE CHALTA HAI]: SortedDictionary with StringComparer.Ordinal.
    // [OUTPUT / SIDE EFFECT]: In-memory sorted dictionary.
    // [VARIATIONS TO TRY]: Radix tree with hex-nibble branch nodes.
    private readonly SortedDictionary<string, string> _state = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: Key-Value state mutate karta hai (insert ya update).
    // [KYU ZAROORI HAI]: Ledger transactions execute hone par inventory aur balances update karna.
    // [KAISE CHALTA HAI]: _state[key] = value.
    // [OUTPUT / SIDE EFFECT]: State map updated; root hash will change.
    // [VARIATIONS TO TRY]: Versioned snapshot state.
    public void Put(string key, string value)
    {
        _state[key] = value;
    }

    // [KYA KARTA HAI]: Key ki current value retrieve karta hai.
    // [KYU ZAROORI HAI]: State lookups aur balance checks.
    // [KAISE CHALTA HAI]: Dictionary lookup.
    // [OUTPUT / SIDE EFFECT]: Value string or null if absent.
    // [VARIATIONS TO TRY]: Read-only view.
    public string? Get(string key)
    {
        return _state.TryGetValue(key, out var val) ? val : null;
    }

    // [KYA KARTA HAI]: Poore state ka single deterministic 32-byte Merkle Root Hash calculate karta hai.
    // [KYU ZAROORI HAI]: Consortia blocks me state commitment embed karna: two nodes with same state have exact same root hash.
    // [KAISE CHALTA HAI]: Sorted leaf hashes ko binary tree me pair-wise hash karta hai.
    // [OUTPUT / SIDE EFFECT]: 64-character hex root hash string.
    // [VARIATIONS TO TRY]: Ethereum-style 16-way hexary trie.
    public string ComputeRootHash()
    {
        if (_state.Count == 0)
        {
            return "0000000000000000000000000000000000000000000000000000000000000000";
        }

        // Calculate leaf hashes for every key-value pair
        var currentLevel = new List<string>();
        foreach (var (k, v) in _state)
        {
            string leafContent = $"{k}:{v}";
            currentLevel.Add(HashString(leafContent));
        }

        // Reduce levels pair-wise until single root hash remains (Merkle Tree reduction)
        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();
            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                if (i + 1 < currentLevel.Count)
                {
                    string combined = currentLevel[i] + currentLevel[i + 1];
                    nextLevel.Add(HashString(combined));
                }
                else
                {
                    // Odd node: duplicate or promote
                    string combined = currentLevel[i] + currentLevel[i];
                    nextLevel.Add(HashString(combined));
                }
            }
            currentLevel = nextLevel;
        }

        return currentLevel[0];
    }

    // [KYA KARTA HAI]: Specific key ke liye cryptographic Merkle proof path generate karta hai.
    // [KYU ZAROORI HAI]: Client ko prove karna ki unka balance ledger me committed hai bina poora database bheje!
    // [KAISE CHALTA HAI]: Sibling tree hashes collect karta hai along the path to root.
    // [OUTPUT / SIDE EFFECT]: List of sibling hex hashes.
    // [VARIATIONS TO TRY]: Compact binary proof encoding.
    public List<string> GenerateProof(string key)
    {
        var proof = new List<string>();
        if (!_state.ContainsKey(key)) return proof;

        var keysList = _state.Keys.ToList();
        var leaves = keysList.Select(k => HashString($"{k}:{_state[k]}")).ToList();

        int targetIdx = keysList.IndexOf(key);

        var currentLevel = leaves;
        while (currentLevel.Count > 1)
        {
            int siblingIdx = (targetIdx % 2 == 0) ? targetIdx + 1 : targetIdx - 1;
            if (siblingIdx < currentLevel.Count)
            {
                proof.Add(currentLevel[siblingIdx]);
            }
            else
            {
                proof.Add(currentLevel[targetIdx]); // Odd node duplication
            }

            var nextLevel = new List<string>();
            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                string right = (i + 1 < currentLevel.Count) ? currentLevel[i + 1] : currentLevel[i];
                nextLevel.Add(HashString(currentLevel[i] + right));
            }

            targetIdx /= 2;
            currentLevel = nextLevel;
        }

        return proof;
    }

    // [KYA KARTA HAI]: Sibling proof hashes se expected root hash verify karta hai.
    // [KYU ZAROORI HAI]: Light clients and mobile apps can verify transactions in O(log N) time.
    // [KAISE CHALTA HAI]: Leaf hash se shuru karke proof siblings ke sath hash karte hue root tak jata hai.
    // [OUTPUT / SIDE EFFECT]: True if computed root matches expected root hash.
    // [VARIATIONS TO TRY]: Parallel batch verification.
    public static bool VerifyProof(string key, string value, string expectedRoot, IReadOnlyList<string> proofHashes, int keyIndex)
    {
        string currentHash = HashString($"{key}:{value}");
        int index = keyIndex;

        foreach (var sibling in proofHashes)
        {
            string combined = (index % 2 == 0) ? currentHash + sibling : sibling + currentHash;
            currentHash = HashString(combined);
            index /= 2;
        }

        return string.Equals(currentHash, expectedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashString(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public int EntryCount => _state.Count;
}
