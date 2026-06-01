using System.Text;

namespace Assignment1_Service.Helpers;

public static class TextChunker
{
    public static List<(int Index, string Content, int CharStart, int CharEnd)> Chunk(
        string text,
        int chunkSize = 800,
        int overlap = 100)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        var results = new List<(int, string, int, int)>();

        if (string.IsNullOrWhiteSpace(normalized))
            return results;

        var index = 0;
        var start = 0;

        while (start < normalized.Length)
        {
            var windowEnd = Math.Min(normalized.Length, start + chunkSize);
            var end = FindBestBreak(normalized, start, windowEnd);

            if (end <= start)
                end = windowEnd;

            var content = normalized.Substring(start, end - start).Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                results.Add((index, content, start, end));
                index++;
            }

            if (end >= normalized.Length)
                break;

            var nextStart = Math.Max(0, end - overlap);
            if (nextStart <= start)
                nextStart = end;

            start = nextStart;
        }

        return results;
    }

    private static int FindBestBreak(string text, int start, int windowEnd)
    {
        var searchLength = windowEnd - start;
        if (searchLength <= 0)
            return windowEnd;

        var separators = new[] { "\n\n", "\n", ". ", ".", " ", "" };
        foreach (var separator in separators)
        {
            if (separator.Length == 0)
                continue;

            var idx = text.LastIndexOf(separator, windowEnd - 1, searchLength, StringComparison.Ordinal);
            if (idx > start)
                return idx + separator.Length;
        }

        return windowEnd;
    }
}
