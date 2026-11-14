namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Trie (Prefix Tree) data structure for ultra-fast grocery search autocomplete.
//
// [KYU ZAROORI HAI]:
// Agar search box me 50,000 grocery items hain, toh har keystroke par `item.Name.StartsWith(query)`
// chalane par O(N * L) time lagta hai jo bohot slow hai.
// Trie me search time O(L) hota hai (jahan L search term ki length hai, e.g. 3 characters = 3 hops),
// chahe database me 10 lakh products kyu na hon!
//
// [KAISE CHALTA HAI]:
// Har Node ke paas children dictionary hoti hai jo agle character ko point karti hai.
// Word ke end par `IsEndOfWord = true` flag hota hai.
//
// [OUTPUT EXPECTATION]:
// "am" type karne par ["Amul Milk", "Amul Butter", "Amul Cheese", "Amul Curd"] instant suggestions aayenge.
//
// [VARIATIONS TO TRY]:
// Har node par product frequency/rating add karke top-3 most popular products return karein.
// =========================================================================================
public class Trie
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public bool IsEndOfWord { get; set; }
        public string? FullWord { get; set; }
    }

    private readonly TrieNode _root = new();

    // O(L) Insert
    public void Insert(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        var current = _root;
        string lower = word.Trim().ToLowerInvariant();

        foreach (char ch in lower)
        {
            if (!current.Children.TryGetValue(ch, out var nextNode))
            {
                nextNode = new TrieNode();
                current.Children[ch] = nextNode;
            }
            current = nextNode;
        }

        current.IsEndOfWord = true;
        current.FullWord = word.Trim(); // Store original casing
    }

    // O(L) Exact Search
    public bool Search(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;

        var current = _root;
        string lower = word.Trim().ToLowerInvariant();

        foreach (char ch in lower)
        {
            if (!current.Children.TryGetValue(ch, out var nextNode))
            {
                return false;
            }
            current = nextNode;
        }

        return current.IsEndOfWord;
    }

    // O(L + K) Autocomplete Suggestions for Prefix
    public List<string> GetAutocompleteSuggestions(string prefix, int maxSuggestions = 5)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(prefix)) return results;

        var current = _root;
        string lower = prefix.Trim().ToLowerInvariant();

        // 1. Navigate to end of prefix
        foreach (char ch in lower)
        {
            if (!current.Children.TryGetValue(ch, out var nextNode))
            {
                return results; // Prefix not found
            }
            current = nextNode;
        }

        // 2. DFS to collect all words starting from prefix node
        CollectWordsDfs(current, results, maxSuggestions);
        return results;
    }

    private void CollectWordsDfs(TrieNode node, List<string> results, int maxSuggestions)
    {
        if (results.Count >= maxSuggestions) return;

        if (node.IsEndOfWord && node.FullWord != null)
        {
            results.Add(node.FullWord);
        }

        foreach (var child in node.Children.Values)
        {
            CollectWordsDfs(child, results, maxSuggestions);
            if (results.Count >= maxSuggestions) break;
        }
    }
}

