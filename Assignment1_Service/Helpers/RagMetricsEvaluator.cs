using Assignment1_Service.Models;

namespace Assignment1_Service.Helpers;

public static class RagMetricsEvaluator
{
    public static RagMetricsResult Evaluate(
        string question,
        string groundTruth,
        string generatedAnswer,
        IReadOnlyCollection<int> retrievedChunkIds,
        string? groundTruthChunks,
        IReadOnlyCollection<string> retrievedChunkContents)
    {
        var groundTruthChunkIds = ParseChunkIds(groundTruthChunks);
        var faithfulness = ComputeFaithfulness(generatedAnswer, retrievedChunkContents);
        var answerRelevancy = TokenOverlapScore(generatedAnswer, question);
        var answerCorrectness = TokenOverlapScore(generatedAnswer, groundTruth);
        var (contextPrecision, contextRecall) = ComputeContextMetrics(
            retrievedChunkIds,
            groundTruthChunkIds,
            retrievedChunkContents,
            groundTruth);

        return new RagMetricsResult
        {
            Faithfulness = Round(faithfulness),
            AnswerRelevancy = Round(answerRelevancy),
            ContextPrecision = Round(contextPrecision),
            ContextRecall = Round(contextRecall),
            AnswerCorrectness = Round(answerCorrectness)
        };
    }

    private static double ComputeFaithfulness(
        string generatedAnswer,
        IReadOnlyCollection<string> retrievedChunkContents)
    {
        if (retrievedChunkContents.Count == 0 || string.IsNullOrWhiteSpace(generatedAnswer))
            return 0;

        return retrievedChunkContents
            .Select(content => TokenOverlapScore(generatedAnswer, content))
            .DefaultIfEmpty(0)
            .Max();
    }

    private static (double Precision, double Recall) ComputeContextMetrics(
        IReadOnlyCollection<int> retrievedChunkIds,
        IReadOnlyCollection<int> groundTruthChunkIds,
        IReadOnlyCollection<string> retrievedChunkContents,
        string groundTruth)
    {
        if (groundTruthChunkIds.Count > 0)
        {
            if (retrievedChunkIds.Count == 0)
                return (0, 0);

            var retrieved = retrievedChunkIds.ToHashSet();
            var expected = groundTruthChunkIds.ToHashSet();
            var intersection = retrieved.Count(id => expected.Contains(id));

            return (
                (double)intersection / retrieved.Count,
                (double)intersection / expected.Count);
        }

        if (retrievedChunkContents.Count == 0 || string.IsNullOrWhiteSpace(groundTruth))
            return (0, 0);

        var contentScore = retrievedChunkContents
            .Select(content => TokenOverlapScore(content, groundTruth))
            .DefaultIfEmpty(0)
            .Max();

        return (contentScore, contentScore);
    }

    public static double TokenOverlapScore(string left, string right)
    {
        var leftTokens = Tokenize(left).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = Tokenize(right).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
            return 0;

        var intersection = leftTokens.Count(token => rightTokens.Contains(token));
        var union = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }

    public static List<int> ParseChunkIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<int>();

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var token = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                token.Append(char.ToLowerInvariant(ch));
                continue;
            }

            if (token.Length > 1)
            {
                yield return token.ToString();
                token.Clear();
            }
            else
            {
                token.Clear();
            }
        }

        if (token.Length > 1)
            yield return token.ToString();
    }

    private static double Round(double value) => Math.Round(value, 4);
}
