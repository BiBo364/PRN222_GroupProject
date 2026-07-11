using System.Text;

namespace Assignment1_Service.Helpers;

/// <summary>
/// Chia văn bản thành các chunk theo số từ (word-based chunking).
/// Mỗi chunk chứa ĐÚNG MaxWordsPerChunk từ (chunk cuối có thể ít hơn).
/// Tham số được đọc từ appsettings.json (section "Chunking") và lưu vào DB khi khởi động.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Chia văn bản thành các chunk, mỗi chunk chứa ĐÚNG <paramref name="maxWordsPerChunk"/> từ.
    /// Chunk cuối cùng có thể chứa ít hơn nếu văn bản không đủ từ.
    /// </summary>
    /// <param name="text">Văn bản cần chia.</param>
    /// <param name="maxWordsPerChunk">Số từ tối đa (chính xác) mỗi chunk.</param>
    /// <param name="overlapWords">Số từ lặp lại ở đầu chunk tiếp theo (overlap).</param>
    /// <returns>Danh sách chunk: (chỉ số, nội dung, vị trí ký tự bắt đầu, vị trí ký tự kết thúc).</returns>
    public static List<(int Index, string Content, int CharStart, int CharEnd)> Chunk(
        string text,
        int maxWordsPerChunk = 250,
        int overlapWords = 40)
    {
        var results = new List<(int, string, int, int)>();

        var normalized = text.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return results;

        // Đảm bảo tham số hợp lệ
        maxWordsPerChunk = Math.Max(1, maxWordsPerChunk);
        overlapWords     = Math.Clamp(overlapWords, 0, maxWordsPerChunk - 1);

        // Tách thành danh sách token (từ) kèm vị trí ký tự bắt đầu trong văn bản gốc
        var tokens = TokenizeWithPositions(normalized);
        if (tokens.Count == 0)
            return results;

        var chunkIndex = 0;
        var wordStart  = 0; // vị trí từ đầu tiên của chunk hiện tại

        while (wordStart < tokens.Count)
        {
            // Lấy ĐÚNG maxWordsPerChunk từ (hoặc phần còn lại nếu không đủ)
            var wordEnd = Math.Min(tokens.Count, wordStart + maxWordsPerChunk);

            // Tính vị trí ký tự bắt đầu và kết thúc
            var charStart = tokens[wordStart].CharStart;
            var charEnd   = wordEnd < tokens.Count
                ? tokens[wordEnd].CharStart   // bắt đầu của từ TIẾP THEO = kết thúc của chunk này
                : normalized.Length;

            var content = normalized.Substring(charStart, charEnd - charStart).Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                results.Add((chunkIndex, content, charStart, charEnd));
                chunkIndex++;
            }

            if (wordEnd >= tokens.Count)
                break;

            // Bước sang chunk tiếp theo có overlap
            // wordStart mới = wordEnd - overlapWords (số từ cuối chunk hiện tại được lặp lại)
            var nextStart = wordEnd - overlapWords;
            if (nextStart <= wordStart)
                nextStart = wordStart + 1; // tránh vòng lặp vô tận

            wordStart = nextStart;
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────

    private readonly record struct Token(string Word, int CharStart);

    /// <summary>
    /// Tách văn bản thành danh sách token, mỗi token lưu từ và vị trí ký tự bắt đầu.
    /// Các ký tự khoảng trắng bị bỏ qua khi đếm từ.
    /// </summary>
    private static List<Token> TokenizeWithPositions(string text)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < text.Length)
        {
            // Bỏ qua khoảng trắng
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            if (i >= text.Length)
                break;

            var start = i;

            // Đọc từ (đến khi gặp khoảng trắng)
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;

            tokens.Add(new Token(text[start..i], start));
        }

        return tokens;
    }
}
