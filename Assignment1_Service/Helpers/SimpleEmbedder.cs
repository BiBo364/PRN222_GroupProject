using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Assignment1_Service.Helpers;

public static class SimpleEmbedder
{
    private static readonly Regex TokenRegex = new("[a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string GenerateVector(string text, int dimension)
    {
        return JsonSerializer.Serialize(GenerateVectorArray(text, dimension));
    }

    public static float[] GenerateVectorArray(string text, int dimension)
    {
        if (dimension <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimension), "Số chiều biểu diễn ngữ nghĩa phải lớn hơn 0.");

        var vector = new float[dimension];
        var normalized = NormalizeText(text);
        var tokens = TokenRegex.Matches(normalized)
            .Select(match => match.Value)
            .ToArray();

        if (tokens.Length > 0)
        {
            foreach (var token in tokens)
                Accumulate(vector, dimension, $"tok:{token}", 1.0f);

            for (var i = 0; i < tokens.Length - 1; i++)
                Accumulate(vector, dimension, $"bg:{tokens[i]}:{tokens[i + 1]}", 0.75f);
        }

        var compact = tokens.Length > 0
            ? string.Concat(tokens)
            : normalized.Replace(" ", string.Empty, StringComparison.Ordinal);

        if (!string.IsNullOrEmpty(compact))
        {
            for (var i = 0; i <= compact.Length - 3; i++)
                Accumulate(vector, dimension, $"tri:{compact.Substring(i, 3)}", 0.35f);
        }
        else
        {
            Accumulate(vector, dimension, "empty", 1.0f);
        }

        Normalize(vector);
        return vector;
    }

    private static void Accumulate(float[] vector, int dimension, string feature, float weight)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(feature));
        var index = (int)(BitConverter.ToUInt64(hash, 0) % (ulong)dimension);
        vector[index] += weight;
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (char.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static void Normalize(float[] vector)
    {
        double sum = 0;
        foreach (var value in vector)
            sum += value * value;

        var norm = Math.Sqrt(sum);
        if (norm == 0)
        {
            vector[0] = 1.0f;
            return;
        }

        for (var i = 0; i < vector.Length; i++)
            vector[i] = (float)(vector[i] / norm);
    }
}
