namespace KiranaHub.HybridSearch;

using KiranaHub.HybridSearch.Fuzzy;
using KiranaHub.HybridSearch.Hybrid;
using KiranaHub.HybridSearch.Indexing;
using KiranaHub.HybridSearch.Ranking;

// [KYA KARTA HAI]: Project 35 ka executable entry point aur automated search relevance test suite provide karta hai.
// [KYU ZAROORI HAI]: Inverted Index construction, Okapi BM25 scoring, Levenshtein typo-tolerant fuzzy correction, aur Reciprocal Rank Fusion (RRF) hybrid search ko end-to-end execute aur assert karna.
// [KAISE CHALTA HAI]: 10 grocery products index karta hai, exact keyword queries run karta hai, misspelled queries auto-correct karta hai, aur BM25 + Vector hybrid fusion scores verify karta hai.
// [OUTPUT / SIDE EFFECT]: Console test reports, assertions verified, zero errors.
// [VARIATIONS TO TRY]: Benchmark query execution time with Stopwatch across 10,000 queries.
public class Program
{
    // [KYA KARTA HAI]: Main execution method.
    // [KYU ZAROORI HAI]: Application launch point.
    // [KAISE CHALTA HAI]: Synchronous/async entry execution.
    // [OUTPUT / SIDE EFFECT]: Returns 0 on success.
    // [VARIATIONS TO TRY]: Interactive search prompt loop for user.
    public static Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================================================");
        Console.WriteLine("  🔍 KiranaHub Project 35: High-Performance Hybrid Search Engine 🔍");
        Console.WriteLine("==================================================================================");

        var index = new InvertedIndex();

        // -----------------------------------------------------------------------------------------------------
        // STAGE 1: SEEDING GROCERY CATALOG INTO INVERTED INDEX
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 1] Seeding 10 Grocery Products into Inverted Index...");

        index.IndexDocument("PROD-01", "Amul Pasteurised Butter 500g", "Rich creamy pure butter made from fresh cow and buffalo milk", "Dairy", 275.00m);
        index.IndexDocument("PROD-02", "Harvest Gold 100% Brown Bread 400g", "Healthy whole wheat brown bread high fiber for morning breakfast", "Bakery", 45.00m);
        index.IndexDocument("PROD-03", "Maggi 2-Minute Masala Noodles 70g", "Instant delicious spiced wheat noodles quick snack for hunger", "Instant Foods", 14.00m);
        index.IndexDocument("PROD-04", "Cadbury Dairy Milk Silk Chocolate Bar 150g", "Smooth rich creamy milk chocolate sweet confectionery delight", "Chocolates", 175.00m);
        index.IndexDocument("PROD-05", "Aashirvaad Shudh Chakki Atta 5kg", "100% whole wheat flour traditional stone ground soft rotis", "Atta & Flours", 245.00m);
        index.IndexDocument("PROD-06", "Fortune Sunlite Refined Sunflower Oil 1L", "Light healthy cooking oil enriched with vitamins for frying", "Edible Oils", 135.00m);
        index.IndexDocument("PROD-07", "Tata Salt Vacuum Evaporated Iodized Salt 1kg", "Pure vacuum evaporated table salt with essential iodine", "Spices & Salt", 28.00m);
        index.IndexDocument("PROD-08", "Fresh Potato Aloo 1kg", "Farm fresh organic potatoes ideal for curries, fries, and cooking", "Vegetables", 32.00m);
        index.IndexDocument("PROD-09", "Fresh Red Tomato Hybrid 500g", "Juicy ripe red tomatoes rich in vitamin C for salads and gravies", "Vegetables", 25.00m);
        index.IndexDocument("PROD-10", "Mother Dairy Toned Milk 1L", "Pasteurised homogenized fresh toned milk 3% fat for daily tea", "Dairy", 54.00m);

        Console.WriteLine($" -> Indexed Total Documents: {index.TotalDocuments}");
        Console.WriteLine($" -> Average Document Length: {index.AverageDocumentLength:F1} tokens");
        Console.WriteLine($" -> Inverted Index Vocabulary Size: {index.GetVocabulary().Count()} unique terms");

        if (index.TotalDocuments != 10)
        {
            throw new InvalidOperationException($"Expected 10 documents, got {index.TotalDocuments}");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 2: OKAPI BM25 LEXICAL RELEVANCE SEARCH
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 2] Testing Okapi BM25 Lexical Keyword Search ('amul butter')...");

        var bm25Scorer = new Bm25Scorer(index);
        var bm25Results = bm25Scorer.Search("amul butter", topK: 3);

        Console.WriteLine($" -> Query: 'amul butter' returned {bm25Results.Count} matches:");
        foreach (var r in bm25Results)
        {
            Console.WriteLine($"    ⭐ [{r.Score:F4}] {r.Document.Title} (Category: {r.Document.Category})");
        }

        if (bm25Results.Count == 0 || bm25Results[0].Document.Id != "PROD-01")
        {
            throw new InvalidOperationException("BM25 top hit for 'amul butter' must be Amul Pasteurised Butter!");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 3: LEVENSHTEIN FUZZY MATCHING (TYPO AUTO-CORRECTION)
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 3] Testing Levenshtein Dynamic Programming Typo Correction...");

        string misspelledQuery = "magi masla";
        var vocab = index.GetVocabulary().ToList();

        var queryTokens = misspelledQuery.Split(' ');
        var correctedTokens = new List<string>();

        foreach (var token in queryTokens)
        {
            string closest = LevenshteinMatcher.FindClosestMatch(token, vocab, maxDistance: 2);
            int distance = LevenshteinMatcher.ComputeDistance(token, closest);
            Console.WriteLine($" -> Input Token: '{token}' -> Auto-Corrected: '{closest}' (Edit Distance = {distance})");
            correctedTokens.Add(closest);
        }

        string correctedQuery = string.Join(" ", correctedTokens);
        Console.WriteLine($" -> Final Corrected Query: '{correctedQuery}'");

        var fuzzyResults = bm25Scorer.Search(correctedQuery, topK: 1);
        Console.WriteLine($" -> Fuzzy Auto-Corrected Top Match: {fuzzyResults[0].Document.Title}");

        if (fuzzyResults[0].Document.Id != "PROD-03")
        {
            throw new InvalidOperationException("Fuzzy search failed to find Maggi Noodles for misspelled 'magi masla'!");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 4: RECIPROCAL RANK FUSION (RRF) HYBRID SEARCH
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 4] Testing Reciprocal Rank Fusion (RRF) Hybrid Search (BM25 + Dense Vectors)...");

        var rrfEngine = new ReciprocalRankFusion(index);
        var hybridResults = rrfEngine.Search("dairy milk chocolate sweet", topK: 3);

        Console.WriteLine(" -> Query: 'dairy milk chocolate sweet' -> Top Hybrid Ranked Results:");
        foreach (var hr in hybridResults)
        {
            Console.WriteLine($"    🏆 [RRF Score: {hr.RrfScore:F6}] {hr.Document.Title} (BM25 Rank: {hr.Bm25Rank}, Vector Rank: {hr.VectorRank})");
        }

        // Cadbury Dairy Milk should dominate top 1 due to being rank 1 in both BM25 and Vector!
        if (hybridResults.Count == 0 || hybridResults[0].Document.Id != "PROD-04")
        {
            throw new InvalidOperationException("Hybrid RRF top hit must be Cadbury Dairy Milk Chocolate!");
        }

        Console.WriteLine("\n==================================================================================");
        Console.WriteLine("  ✅ [SUCCESS] ALL BM25, LEVENSHTEIN FUZZY & RRF HYBRID SEARCH TESTS PASSED! ✅");
        Console.WriteLine("==================================================================================");

        return Task.FromResult(0);
    }
}
