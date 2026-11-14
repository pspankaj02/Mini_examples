using KiranaHub.GpuMatrixCompute.Compute;

namespace KiranaHub.GpuMatrixCompute.Recommender;

/// <summary>
/// Real-Time Grocery Collaborative Filtering Recommender Engine.
/// Computes personalized customer affinity scores across large product catalogs
/// by multiplying User Latent Factor Embeddings (M x K) with Item Factor Embeddings (K x N).
/// </summary>
public sealed class CollaborativeFilteringModel
{
    public int UserCount { get; }
    public int ItemCount { get; }
    public int LatentFactors { get; }

    // User Embeddings: (M x K) flattened array
    private readonly float[] _userMatrix;

    // Item Embeddings Transposed: (K x N) flattened array
    private readonly float[] _itemMatrixTransposed;

    // Output Prediction Matrix: (M x N) flattened array (Affinity Scores)
    private readonly float[] _predictionMatrix;

    // Item catalog metadata (SKUs and Human-Readable Names)
    private readonly string[] _itemCatalog;

    public CollaborativeFilteringModel(int userCount = 1000, int itemCount = 2000, int latentFactors = 64)
    {
        UserCount = userCount;
        ItemCount = itemCount;
        LatentFactors = latentFactors;

        _userMatrix = new float[userCount * latentFactors];
        _itemMatrixTransposed = new float[latentFactors * itemCount];
        _predictionMatrix = new float[userCount * itemCount];
        _itemCatalog = GenerateCatalog(itemCount);

        InitializeSyntheticEmbeddings();
    }

    // [KYA KARTA HAI]: Reproducible pseudo-random latent factor embeddings initialize karta hai.
    // [KYU ZAROORI HAI]: Realistic user preferences aur grocery item attributes simulate karna.
    // [KAISE CHALTA HAI]: Normalized random floats in [-1.0, 1.0].
    // [OUTPUT / SIDE EFFECT]: _userMatrix and _itemMatrixTransposed populated.
    // [VARIATIONS TO TRY]: Pre-trained PyTorch / ONNX embedding vectors ingestion.
    private void InitializeSyntheticEmbeddings()
    {
        var rng = new Random(42);

        for (int i = 0; i < _userMatrix.Length; i++)
        {
            _userMatrix[i] = (float)((rng.NextDouble() * 2.0) - 1.0);
        }

        for (int i = 0; i < _itemMatrixTransposed.Length; i++)
        {
            _itemMatrixTransposed[i] = (float)((rng.NextDouble() * 2.0) - 1.0);
        }
    }

    // [KYA KARTA HAI]: Grocery catalog item names generate karta hai.
    // [KYU ZAROORI HAI]: Top recommendations me readable product titles display karna.
    // [KAISE CHALTA HAI]: String formatting array.
    // [OUTPUT / SIDE EFFECT]: Product catalog array populated.
    // [VARIATIONS TO TRY]: Live MongoDB product catalog extraction.
    private static string[] GenerateCatalog(int count)
    {
        string[] sampleCategories = ["Organic Milk", "Whole Wheat Bread", "Farm Fresh Eggs", "Amul Salted Butter", "Taj Mahal Tea", "Filter Coffee", "Aashirvaad Atta", "Basmati Rice", "Maggi 2-Min Noodles", "Alphonso Mangoes"];
        var catalog = new string[count];

        for (int i = 0; i < count; i++)
        {
            string cat = sampleCategories[i % sampleCategories.Length];
            catalog[i] = $"{cat} (SKU-{i:D5})";
        }

        return catalog;
    }

    // [KYA KARTA HAI]: Naive baseline se saare user-item affinity scores compute karta hai.
    // [KYU ZAROORI HAI]: Speedup comparison benchmark ke liye scalar baseline execution.
    // [KAISE CHALTA HAI]: TiledMatrixMultiplier.MultiplyNaive call karta hai.
    // [OUTPUT / SIDE EFFECT]: Destination array populated with scores.
    // [VARIATIONS TO TRY]: SIMD scalar loop.
    public void ComputeAffinityScoresNaive(float[] destOutput)
    {
        TiledMatrixMultiplier.MultiplyNaive(_userMatrix, _itemMatrixTransposed, destOutput, UserCount, LatentFactors, ItemCount);
    }

    // [KYA KARTA HAI]: GPU-inspired Cache-Tiled SIMT parallel kernel se affinity scores compute karta hai.
    // [KYU ZAROORI HAI]: 20,00,000 predictions sub-second latency me produce karna.
    // [KAISE CHALTA HAI]: TiledMatrixMultiplier.MultiplyTiledParallel call karta hai.
    // [OUTPUT / SIDE EFFECT]: _predictionMatrix populated with high-speed predictions.
    // [VARIATIONS TO TRY]: Half-precision float16 (FP16) tensor cores.
    public void ComputeAffinityScoresParallel()
    {
        TiledMatrixMultiplier.MultiplyTiledParallel(_userMatrix, _itemMatrixTransposed, _predictionMatrix, UserCount, LatentFactors, ItemCount);
    }

    // [KYA KARTA HAI]: Specific user ke liye Top-K highest affinity grocery recommendations extract karta hai.
    // [KYU ZAROORI HAI]: Customer app homepage par personalized recommendations display karna.
    // [KAISE CHALTA HAI]: User row ke N predictions sort karke top K items nikalta hai.
    // [OUTPUT / SIDE EFFECT]: Array of TopRecommendation items.
    // [VARIATIONS TO TRY]: Min-heap priority queue for O(N log K) selection.
    public TopRecommendation[] GetTopRecommendations(int userId, int k = 5)
    {
        if (userId < 0 || userId >= UserCount)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        int rowOffset = userId * ItemCount;
        var scores = new (int ItemIdx, float Score)[ItemCount];

        for (int j = 0; j < ItemCount; j++)
        {
            scores[j] = (j, _predictionMatrix[rowOffset + j]);
        }

        // Top-K highest scores
        return scores.OrderByDescending(s => s.Score)
                     .Take(k)
                     .Select(s => new TopRecommendation(s.ItemIdx, _itemCatalog[s.ItemIdx], s.Score))
                     .ToArray();
    }

    public float[] RawUserMatrix => _userMatrix;
    public float[] RawItemMatrix => _itemMatrixTransposed;
    public float[] RawPredictionMatrix => _predictionMatrix;
}

/// <summary>
/// Personalized recommendation result item.
/// </summary>
public sealed record TopRecommendation(int ItemIndex, string ProductName, float AffinityScore);
