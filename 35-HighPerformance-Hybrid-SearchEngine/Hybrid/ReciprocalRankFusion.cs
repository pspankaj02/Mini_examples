namespace KiranaHub.HybridSearch.Hybrid;

using KiranaHub.HybridSearch.Indexing;
using KiranaHub.HybridSearch.Ranking;

// [KYA KARTA HAI]: Reciprocal Rank Fusion (RRF) algorithm ke zariye Lexical BM25 Keyword Search aur Dense Semantic Vector Search ko fuse karta hai.
// [KYU ZAROORI HAI]: E-commerce search me pure keyword match (BM25) exact brand names ke liye best hota hai jabki dense vectors conceptual queries (e.g. "healthy breakfast grain") ke liye best hote hain; RRF dono ki strengths ko bina score-calibration issue ke mathematically merge karta hai.
// [KAISE CHALTA HAI]: BM25 aur Vector search dono se top-K results nikalta hai, har document ke ranks ka inverse reciprocal sum compute karta hai ($RRF(d) = \sum \frac{1}{60 + \text{rank}(d)}$), aur final hybrid ranking return karta hai.
// [OUTPUT / SIDE EFFECT]: Unified hybrid search results combining precision and recall.
// [VARIATIONS TO TRY]: Weighted RRF jahan BM25 ya Vector ranker ko custom weight multiplier diya jaye.
public class ReciprocalRankFusion
{
    // [KYA KARTA HAI]: InvertedIndex reference hold karta hai.
    // [KYU ZAROORI HAI]: Accessing catalog documents.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: Bound index.
    // [VARIATIONS TO TRY]: Multi-index union.
    private readonly InvertedIndex _index;

    // [KYA KARTA HAI]: Bm25Scorer instance.
    // [KYU ZAROORI HAI]: Lexical search engine.
    // [KAISE CHALTA HAI]: Constructor initialization.
    // [OUTPUT / SIDE EFFECT]: Bound BM25 engine.
    // [VARIATIONS TO TRY]: Configurable parameters.
    private readonly Bm25Scorer _bm25Scorer;

    // [KYA KARTA HAI]: RRF smoothing constant $k$.
    // [KYU ZAROORI HAI]: High-ranked items ke disproportionate dominance ko soften karna.
    // [KAISE CHALTA HAI]: Standard information retrieval value 60 (TREC benchmark standard).
    // [OUTPUT / SIDE EFFECT]: Constant 60.
    // [VARIATIONS TO TRY]: 20 for aggressive top-rank bias.
    public const int RrfK = 60;

    // [KYA KARTA HAI]: ReciprocalRankFusion instance initialize karta hai.
    // [KYU ZAROORI HAI]: Engine setup.
    // [KAISE CHALTA HAI]: Constructor injection.
    // [OUTPUT / SIDE EFFECT]: Initialized hybrid engine.
    // [VARIATIONS TO TRY]: Custom vector dimension embedding model.
    public ReciprocalRankFusion(InvertedIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
        _bm25Scorer = new Bm25Scorer(index);
    }

    // [KYA KARTA HAI]: Text string ke liye in-memory dense character 3-gram vector embedding generate karta hai.
    // [KYU ZAROORI HAI]: Lightweight semantic concept matching without external Python/GPU dependencies.
    // [KAISE CHALTA HAI]: Sliding window of 3 characters se n-grams extract karta hai aur term frequency vector banata hai.
    // [OUTPUT / SIDE EFFECT]: Dictionary<string, double> representing sparse-dense embedding vector.
    // [VARIATIONS TO TRY]: Fast hashing trick (MurmurHash3 to fixed 256-float array).
    public static Dictionary<string, double> GenerateEmbedding(string text)
    {
        var vector = new Dictionary<string, double>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text)) return vector;

        string normalized = text.ToLowerInvariant().Replace(" ", "_");
        for (int i = 0; i <= normalized.Length - 3; i++)
        {
            string trigram = normalized.Substring(i, 3);
            vector[trigram] = vector.GetValueOrDefault(trigram, 0.0) + 1.0;
        }

        // L2 Normalize vector
        double norm = Math.Sqrt(vector.Values.Sum(v => v * v));
        if (norm > 0)
        {
            foreach (var key in vector.Keys.ToList())
            {
                vector[key] /= norm;
            }
        }

        return vector;
    }

    // [KYA KARTA HAI]: Do vectors ke beech Cosine Similarity calculate karta hai: $\frac{\vec{A} \cdot \vec{B}}{\|\vec{A}\| \|\vec{B}\|}$.
    // [KYU ZAROORI HAI]: Semantic relevance score (0.0 to 1.0) nikalna.
    // [KAISE CHALTA HAI]: Dot product over common keys (vectors are already unit normalized).
    // [OUTPUT / SIDE EFFECT]: Cosine similarity score double.
    // [VARIATIONS TO TRY]: SIMD DotProduct for 50x speedup.
    public static double CosineSimilarity(Dictionary<string, double> vecA, Dictionary<string, double> vecB)
    {
        if (vecA.Count == 0 || vecB.Count == 0) return 0.0;

        double dotProduct = 0.0;
        foreach (var (ngram, valA) in vecA)
        {
            if (vecB.TryGetValue(ngram, out var valB))
            {
                dotProduct += valA * valB;
            }
        }

        return Math.Clamp(dotProduct, 0.0, 1.0);
    }

    // [KYA KARTA HAI]: Pure semantic dense vector search perform karta hai across catalog documents.
    // [KYU ZAROORI HAI]: Conceptual query match (e.g. "breakfast snacks" -> "Poha, Cornflakes").
    // [KAISE CHALTA HAI]: Query vector banata hai, sabhi documents ke vectors se cosine similarity nikalta hai, aur top-K sort karta hai.
    // [OUTPUT / SIDE EFFECT]: Sorted list of SearchResult records.
    // [VARIATIONS TO TRY]: HNSW (Hierarchical Navigable Small World) index for sub-linear search.
    public List<SearchResult> SemanticVectorSearch(string query, int topK = 10)
    {
        var queryVec = GenerateEmbedding(query);
        var results = new List<SearchResult>();

        foreach (var doc in _index.GetAllDocuments())
        {
            var docVec = GenerateEmbedding($"{doc.Title} {doc.Description} {doc.Category}");
            double similarity = CosineSimilarity(queryVec, docVec);
            if (similarity > 0.05) // Noise threshold
            {
                results.Add(new SearchResult(doc, Math.Round(similarity, 4), "Vector"));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    // [KYA KARTA HAI]: Lexical (BM25) aur Semantic (Vector) rankings ko Reciprocal Rank Fusion (RRF) se combine karta hai.
    // [KYU ZAROORI HAI]: State-of-the-art hybrid search relevance achieve karna.
    // [KAISE CHALTA HAI]: BM25 aur Vector search run karta hai, document rank positions $r_m(d)$ nikalta hai, RRF score $RRF(d) = \sum \frac{1}{60 + r_m(d)}$ compute karta hai, aur sorted hybrid results return karta hai.
    // [OUTPUT / SIDE EFFECT]: Re-ranked hybrid results.
    // [VARIATIONS TO TRY]: Weight multiplier for BM25 vs Vector.
    public List<HybridSearchResult> Search(string query, int topK = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        // 1. Run BM25 Lexical Search
        var bm25Results = _bm25Scorer.Search(query, topK: 20);

        // 2. Run Semantic Vector Search
        var vectorResults = SemanticVectorSearch(query, topK: 20);

        // Map: DocId -> (RrfScore, Bm25Rank, VectorRank)
        var fusionMap = new Dictionary<string, (double rrfScore, int bm25Rank, int vectorRank)>(StringComparer.OrdinalIgnoreCase);

        // Accumulate BM25 ranks
        for (int rank = 0; rank < bm25Results.Count; rank++)
        {
            int rankPos = rank + 1; // 1-based rank
            string docId = bm25Results[rank].Document.Id;
            double scoreContrib = 1.0 / (RrfK + rankPos);

            fusionMap[docId] = (scoreContrib, rankPos, -1);
        }

        // Accumulate Vector ranks
        for (int rank = 0; rank < vectorResults.Count; rank++)
        {
            int rankPos = rank + 1;
            string docId = vectorResults[rank].Document.Id;
            double scoreContrib = 1.0 / (RrfK + rankPos);

            if (fusionMap.TryGetValue(docId, out var existing))
            {
                fusionMap[docId] = (existing.rrfScore + scoreContrib, existing.bm25Rank, rankPos);
            }
            else
            {
                fusionMap[docId] = (scoreContrib, -1, rankPos);
            }
        }

        // Sort by RRF score descending
        return fusionMap
            .OrderByDescending(kv => kv.Value.rrfScore)
            .Take(topK)
            .Select(kv => new HybridSearchResult(
                Document: _index.GetDocument(kv.Key)!,
                RrfScore: Math.Round(kv.Value.rrfScore, 6),
                Bm25Rank: kv.Value.bm25Rank,
                VectorRank: kv.Value.vectorRank))
            .ToList();
    }
}

// [KYA KARTA HAI]: Hybrid search result model encapsulating document, RRF score, aur individual rank positions.
// [KYU ZAROORI HAI]: Transparency in why a document ranked at its final position.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Hybrid result DTO.
// [VARIATIONS TO TRY]: Add explanation text.
public record HybridSearchResult(
    IndexedDocument Document,
    double RrfScore,
    int Bm25Rank,
    int VectorRank);
