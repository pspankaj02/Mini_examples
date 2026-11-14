namespace KiranaHub.HybridSearch.Fuzzy;

// [KYA KARTA HAI]: Dynamic programming ke zariye Levenshtein Edit Distance aur Fuzzy Token Matcher implement karta hai.
// [KYU ZAROORI HAI]: Hinglish aur English misspellings (e.g. "magi" -> "maggi", "aaloo" -> "aloo", "choclate" -> "chocolate") ko detect karke auto-correct karna.
// [KAISE CHALTA HAI]: Space-optimized $2 \times N$ dynamic programming matrix se minimum insertions, deletions, aur substitutions calculate karta hai; vocabulary me se lowest distance matches filter karta hai.
// [OUTPUT / SIDE EFFECT]: Closest vocabulary term suggestions and fuzzy matching.
// [VARIATIONS TO TRY]: Damerau-Levenshtein distance (transposition of two adjacent characters as cost 1).
public class LevenshteinMatcher
{
    // [KYA KARTA HAI]: Do strings ke beech Levenshtein distance compute karta hai in $O(M \times N)$ time aur $O(N)$ space.
    // [KYU ZAROORI HAI]: Edit distance nikalna to quantify typo severity.
    // [KAISE CHALTA HAI]: Space-optimized 2-row DP array use karta hai: row0 (previous) aur row1 (current).
    // [OUTPUT / SIDE EFFECT]: Non-negative edit distance integer.
    // [VARIATIONS TO TRY]: Damerau-Levenshtein transposition check.
    public static int ComputeDistance(string source, string target)
    {
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        string s = source.ToLowerInvariant();
        string t = target.ToLowerInvariant();

        int m = s.Length;
        int n = t.Length;

        // [KYA KARTA HAI]: 2-row DP array initialize karta hai.
        // [KYU ZAROORI HAI]: Full $M \times N$ matrix ki memory allocation se bachna.
        // [KAISE CHALTA HAI]: int[] of size n + 1.
        // [OUTPUT / SIDE EFFECT]: Previous and current row arrays.
        // [VARIATIONS TO TRY]: stackalloc int[128] for zero heap allocation on short words.
        int[] prevRow = new int[n + 1];
        int[] currRow = new int[n + 1];

        for (int j = 0; j <= n; j++)
        {
            prevRow[j] = j;
        }

        for (int i = 1; i <= m; i++)
        {
            currRow[0] = i;

            for (int j = 1; j <= n; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                // [KYA KARTA HAI]: Minimum of Deletion, Insertion, aur Substitution calculate karta hai.
                // [KYU ZAROORI HAI]: Optimal substructure property of edit distance.
                // [KAISE CHALTA HAI]: Math.Min(currRow[j - 1] + 1, Math.Min(prevRow[j] + 1, prevRow[j - 1] + cost)).
                // [OUTPUT / SIDE EFFECT]: Min edit operations.
                // [VARIATIONS TO TRY]: Custom weighted substitution costs.
                currRow[j] = Math.Min(
                    currRow[j - 1] + 1,              // Insertion
                    Math.Min(
                        prevRow[j] + 1,              // Deletion
                        prevRow[j - 1] + cost));     // Substitution
            }

            // Swap rows
            Array.Copy(currRow, prevRow, n + 1);
        }

        return prevRow[n];
    }

    // [KYA KARTA HAI]: Misspelled query token ke liye vocabulary me se closest matching term find karta hai.
    // [KYU ZAROORI HAI]: Auto-correcting user typos in grocery search.
    // [KAISE CHALTA HAI]: Vocabulary scan karta hai, ComputeDistance() compare karta hai, aur maxDistance threshold ke andar lowest distance word return karta hai.
    // [OUTPUT / SIDE EFFECT]: Corrected term string, ya original token agar koi close match na mile.
    // [VARIATIONS TO TRY]: BK-Tree (Burkhard-Keller Tree) for $O(\log V)$ sub-linear fuzzy lookups.
    public static string FindClosestMatch(string token, IEnumerable<string> vocabulary, int maxDistance = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        string bestMatch = token;
        int minDistance = int.MaxValue;

        foreach (var word in vocabulary)
        {
            // Early skip if length difference is already greater than minDistance
            if (Math.Abs(word.Length - token.Length) >= minDistance)
            {
                continue;
            }

            int dist = ComputeDistance(token, word);
            if (dist < minDistance && dist <= maxDistance)
            {
                minDistance = dist;
                bestMatch = word;

                // Exact match found
                if (dist == 0) break;
            }
        }

        return bestMatch;
    }
}
