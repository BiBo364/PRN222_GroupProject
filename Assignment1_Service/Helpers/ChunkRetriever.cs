using Assignment1_Repository.Models;
using Assignment1_Service.Models;

namespace Assignment1_Service.Helpers;

public static class ChunkRetriever
{
    private const int DefaultTopK = 4;

    public static List<RetrievedChunk> Retrieve(
        string question,
        IEnumerable<Chunk> chunks,
        IReadOnlyDictionary<int, float[]> queryVectors,
        int topK = DefaultTopK,
        bool useHybridRerank = false)
    {
        if (queryVectors.Count == 0)
            return new List<RetrievedChunk>();

        var merged = new Dictionary<int, RetrievedChunk>();

        foreach (var queryVector in queryVectors)
        {
            foreach (var chunk in chunks)
            {
                var embedding = chunk.Embeddings.FirstOrDefault(item => item.EmbeddingModelId == queryVector.Key);
                if (embedding is null)
                    continue;

                var chunkVector = VectorMath.ParseVector(embedding.Vector);
                var vectorScore = VectorMath.CosineSimilarity(queryVector.Value, chunkVector);
                var keywordScore = useHybridRerank ? ComputeKeywordScore(question, chunk.Content) : 0;
                var score = useHybridRerank
                    ? CombineScores(vectorScore, keywordScore)
                    : vectorScore;

                var candidate = new RetrievedChunk
                {
                    Chunk = chunk,
                    Document = chunk.Document,
                    Score = score,
                    VectorScore = vectorScore,
                    KeywordScore = keywordScore,
                    MatchedEmbeddingModelId = queryVector.Key
                };

                if (!merged.TryGetValue(candidate.Chunk.Id, out var current) || candidate.Score > current.Score)
                    merged[candidate.Chunk.Id] = candidate;
            }
        }

        return merged.Values
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.VectorScore)
            .Take(topK)
            .ToList();
    }

    private static double CombineScores(double vectorScore, double keywordScore)
    {
        return (vectorScore * 0.8) + (keywordScore * 0.2);
    }

    private static double ComputeKeywordScore(string question, string chunkContent)
    {
        var questionTokens = Tokenize(question).ToList();
        if (questionTokens.Count == 0)
            return 0;

        var chunkText = Normalize(chunkContent);
        var chunkTokens = Tokenize(chunkText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matchedTokens = questionTokens.Count(token => chunkTokens.Contains(token));

        var overlapScore = (double)matchedTokens / questionTokens.Count;
        return Math.Clamp(overlapScore, 0, 1);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var token = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                token.Append(char.ToLowerInvariant(ch));
                continue;
            }

            if (token.Length > 0)
            {
                var value = token.ToString();
                token.Clear();

                if (value.Length > 1)
                    yield return value;
            }
        }

        if (token.Length > 0)
        {
            var value = token.ToString();
            if (value.Length > 1)
                yield return value;
        }
    }

    private static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().ToLowerInvariant();
    }
}
