namespace Assignment1_Service.Helpers;

public static class TextChunker
{
    public static List<(int Index, string Content, int CharStart, int CharEnd)> Chunk(
        string text,
        int wordsPerChunk = 450,
        int overlapWords = 0)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var results = new List<(int, string, int, int)>();

        if (words.Length == 0)
            return results;

        var step = Math.Max(1, wordsPerChunk - Math.Max(0, overlapWords));
        var index = 0;
        var charOffset = 0;

        for (var start = 0; start < words.Length; start += step)
        {
            var count = Math.Min(wordsPerChunk, words.Length - start);
            var content = string.Join(' ', words, start, count);

            if (content.Length == 0)
                continue;

            results.Add((index, content, charOffset, charOffset + content.Length));
            index++;
            charOffset += content.Length + 1;

            if (start + wordsPerChunk >= words.Length)
                break;
        }

        return results;
    }
}
