namespace Assignment1_Service.Helpers;

public static class TextChunker
{
    public static List<(int Index, string Content, int CharStart, int CharEnd)> Chunk(
        string text,
        int chunkSize = 500,
        int overlap = 50)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        var results = new List<(int, string, int, int)>();

        if (string.IsNullOrWhiteSpace(normalized))
            return results;

        var index = 0;
        var start = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            var content = normalized.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                results.Add((index, content, start, start + length));
                index++;
            }

            if (start + length >= normalized.Length)
                break;

            start += Math.Max(1, chunkSize - overlap);
        }

        return results;
    }
}
