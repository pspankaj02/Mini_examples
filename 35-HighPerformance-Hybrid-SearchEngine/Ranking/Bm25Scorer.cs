namespace KiranaHub.HybridSearch.Ranking;

using KiranaHub.HybridSearch.Indexing;

// [KYA KARTA HAI]: Okapi BM25 (Best Matching 25) probabilistic relevance ranking algorithm implement karta hai.
// [KYU ZAROORI HAI]: Full-text search me TF-IDF se superior ranking provide karna jisme term frequency saturation ($k_1$) aur document length normalization ($b$) handle hoti hai.
// [KAISE CHALTA HAI]: Query terms ke liye IDF compute karta hai, inverted index se matching documents fetch karta hai, BM25 formula evaluate karta hai, aur scores aggregate karke descending order me sort karta hai.
// [OUTPUT / SIDE EFFECT]: Ranked list of SearchResult records with relevance scores.
// [VARIATIONS TO TRY]: BM25F for multi-field weighted scoring (Title, Category, Description).
public class Bm25Scorer
{
    // [KYA KARTA HAI]: InvertedIndex reference hold karta hai.
    // [KYU ZAROORI HAI]: Postings, document lengths, aur corpus statistics read karna.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: InvertedIndex bound.
    // [VARIATIONS TO TRY]: Sharded index aggregation.
    private readonly InvertedIndex _index;

    // [KYA KARTA HAI]: Term frequency saturation constant ($k_1$).
    // [KYU ZAROORI HAI]: Term frequency ka diminishing return control karna (agar word 10 baar aaye toh score 10x na ho).
    // [KAISE CHALTA HAI]: Default standard value 1.2.
    // [OUTPUT / SIDE EFFECT]: Parameter float/double.
    // [VARIATIONS TO TRY]: E-commerce fine-tuning between 1.2 and 2.0.
    public double K1 { get; set; } = 1.2;

    // [KYA KARTA HAI]: Document length normalization parameter ($b$).
    // [KYU ZAROORI HAI]: Lambe documents (jinme naturally zyada words hote hain) par length penalty lagana.
    // [KAISE CHALTA HAI]: Default standard value 0.75 ($0 \le b \le 1$).
    // [OUTPUT / SIDE EFFECT]: Parameter double.
    // [VARIATIONS TO TRY]: 0.5 for short catalog titles.
    public double B { get; set; } = 0.75;

    // [KYA KARTA HAI]: Bm25Scorer instance initialize karta hai.
    // [KYU ZAROORI HAI]: Scorer setup.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: Initialized scorer.
    // [VARIATIONS TO TRY]: Custom K1 and B configurations.
    public Bm25Scorer(InvertedIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    // [KYA KARTA HAI]: Term ke liye Inverse Document Frequency (IDF) calculate karta hai.
    // [KYU ZAROORI HAI]: Rare terms (e.g. "basmati") ko common terms se zyada weightage dena.
    // [KAISE CHALTA HAI]: $\text{IDF}(t) = \ln\left(1 + \frac{N - n(t) + 0.5}{n(t) + 0.5}\right)$.
    // [OUTPUT / SIDE EFFECT]: Non-negative IDF score.
    // [VARIATIONS TO TRY]: Lucene style smoothed IDF formula.
    public double CalculateIdf(int docCountWithTerm, int totalDocs)
    {
        if (totalDocs == 0 || docCountWithTerm == 0)
        {
            return 0.0;
        }

        double numerator = totalDocs - docCountWithTerm + 0.5;
        double denominator = docCountWithTerm + 0.5;
        return Math.Log(1.0 + (numerator / denominator));
    }

    // [KYA KARTA HAI]: Query string ke matching documents ko BM25 score ke mutabiq rank karta hai.
    // [KYU ZAROORI HAI]: Customer search input ke relevant products display karna.
    // [KAISE CHALTA HAI]: Query tokenize karta hai, har term ke posting list se documents dhundta hai, score accumulate karta hai, aur top results sort karta hai.
    // [OUTPUT / SIDE EFFECT]: Sorted list of SearchResult records.
    // [VARIATIONS TO TRY]: Pagination with skip and take.
    public List<SearchResult> Search(string query, int topK = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var queryTokens = InvertedIndex.Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return new List<SearchResult>();
        }

        int totalDocs = _index.TotalDocuments;
        double avgdl = _index.AverageDocumentLength;
        if (totalDocs == 0 || avgdl <= 0)
        {
            return new List<SearchResult>();
        }

        // [KYA KARTA HAI]: Accumulated scores map (DocId -> Total BM25 Score).
        // [KYU ZAROORI HAI]: Multi-term queries me scores aggregate karna.
        // [KAISE CHALTA HAI]: Dictionary<string, double>.
        // [OUTPUT / SIDE EFFECT]: Scores dictionary.
        // [VARIATIONS TO TRY]: Sparse vector accumulator.
        var docScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // [KYA KARTA HAI]: Sabhi query tokens ke liye postings evaluate karta hai.
        // [KYU ZAROORI HAI]: Score contribution per token.
        // [KAISE CHALTA HAI]: foreach over queryTokens.
        // [OUTPUT / SIDE EFFECT]: Aggregates BM25 sum.
        // [VARIATIONS TO TRY]: WAND (Weak AND) algorithm for fast top-k pruning.
        foreach (var token in queryTokens)
        {
            var postings = _index.GetPostings(token);
            if (postings.Count == 0)
            {
                continue;
            }

            // [KYA KARTA HAI]: Token ka IDF compute karta hai.
            // [KYU ZAROORI HAI]: Token discrimination weight.
            // [KAISE CHALTA HAI]: CalculateIdf(postings.Count, totalDocs).
            // [OUTPUT / SIDE EFFECT]: IDF value.
            // [VARIATIONS TO TRY]: Pre-computed IDF cache.
            double idf = CalculateIdf(postings.Count, totalDocs);

            // [KYA KARTA HAI]: Token ke sabhi matching documents iterate karta hai.
            // [KYU ZAROORI HAI]: Compute term frequency saturation component.
            // [KAISE CHALTA HAI]: foreach over postings.
            // [OUTPUT / SIDE EFFECT]: Document score increased.
            // [VARIATIONS TO TRY]: Parallel processing across tokens.
            foreach (var posting in postings)
            {
                var doc = _index.GetDocument(posting.DocumentId);
                if (doc == null) continue;

                int tf = posting.TermFrequency;
                int docLen = doc.TokenLength;

                // [KYA KARTA HAI]: BM25 term frequency saturation component calculate karta hai.
                // [KYU ZAROORI HAI]: Non-linear scaling: $\frac{f \cdot (k_1 + 1)}{f + k_1 \cdot (1 - b + b \cdot \frac{|D|}{\text{avgdl}})}$.
                // [KAISE CHALTA HAI]: Formula calculation.
                // [OUTPUT / SIDE EFFECT]: Weighted component.
                // [VARIATIONS TO TRY]: Title boost multiplier.
                double lengthNorm = 1.0 - B + (B * (docLen / avgdl));
                double tfComponent = (tf * (K1 + 1.0)) / (tf + (K1 * lengthNorm));

                double termScore = idf * tfComponent;

                // [KYA KARTA HAI]: Document score update karta hai.
                // [KYU ZAROORI HAI]: Accumulate sum over all query terms.
                // [KAISE CHALTA HAI]: docScores[docId] += termScore.
                // [OUTPUT / SIDE EFFECT]: Accumulated score.
                // [VARIATIONS TO TRY]: Coordination bonus (number of distinct matching terms).
                docScores[posting.DocumentId] = docScores.GetValueOrDefault(posting.DocumentId, 0.0) + termScore;
            }
        }

        // [KYA KARTA HAI]: Documents ko score ke descending order me sort karta hai aur topK leta hai.
        // [KYU ZAROORI HAI]: Most relevant items first.
        // [KAISE CHALTA HAI]: OrderByDescending -> Take(topK).
        // [OUTPUT / SIDE EFFECT]: Sorted list.
        // [VARIATIONS TO TRY]: Min-heap for $O(N \log K)$ sorting.
        return docScores
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => new SearchResult(
                Document: _index.GetDocument(kv.Key)!,
                Score: Math.Round(kv.Value, 4),
                MatchMethod: "BM25"))
            .ToList();
    }
}

// [KYA KARTA HAI]: Search result item encapsulating document, relevance score, aur match method.
// [KYU ZAROORI HAI]: Unified presentation DTO for BM25, Fuzzy, and Vector search results.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Result model.
// [VARIATIONS TO TRY]: Snippet highlight text shamil karein.
public record SearchResult(
    IndexedDocument Document,
    double Score,
    string MatchMethod);
