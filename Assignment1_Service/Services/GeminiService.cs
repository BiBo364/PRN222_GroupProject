using System.Net.Http.Json;
using System.Text.Json;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        IReadOnlyCollection<ChatMessageDto> recentHistory,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return "Mình không tìm thấy nội dung phù hợp trong tài liệu đã index để trả lời câu hỏi này.";

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return "Gemini API key chưa được cấu hình. Vui lòng thêm Gemini:ApiKey vào appsettings hoặc user-secrets.";

        var systemPrompt = BuildSystemPrompt();
        var contents = BuildMessages(question, chunks, recentHistory);
        var request = new
        {
            systemInstruction = new
            {
                parts = new[]
                { new { text = systemPrompt } }
            },
            contents = contents,
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 1200
            }
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Gemini answer generation failed with HTTP {StatusCode}; using generic extractive fallback.",
                (int)response.StatusCode);
            return BuildExtractiveFallback(question, chunks);
        }

        var answer = ExtractAnswer(payload);
        if (string.IsNullOrWhiteSpace(answer) || LooksInsufficient(answer))
        {
            _logger.LogWarning("Gemini answer was empty or insufficient; using generic extractive fallback.");
            return BuildExtractiveFallback(question, chunks);
        }

        return answer.Trim();
    }

    private static string BuildSystemPrompt()
    {
        return "Bạn là trợ lý học tập cho môn học. Chỉ trả lời dựa trên ngữ cảnh được cung cấp, không bịa đặt, trả lời bằng tiếng Việt, rõ ràng, đầy đủ nhưng không dài dòng không cần thiết, và ưu tiên nêu nguồn/trang khi có thể.";
    }

    private static object BuildMessages(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        IReadOnlyCollection<ChatMessageDto> recentHistory)
    {
        var contents = new List<object>();

        foreach (var message in recentHistory)
        {
            contents.Add(new
            {
                role = message.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = message.Content } }
            });
        }

        var context = BuildContextBlock(chunks);
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = context } }
        });

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = $"Câu hỏi: {question}" } }
        });

        return contents.ToArray();
    }

    private static string BuildContextBlock(IReadOnlyCollection<RetrievedChunk> chunks)
    {
        return string.Join("\n\n", chunks.Select((chunk, index) =>
        {
            var slideMeta = SlideChunkMetadata.FromJson(chunk.Chunk.Metadata);
            var pageLabel = chunk.Chunk.PageNumber is int pageNumber
                ? $"page {pageNumber}"
                : slideMeta?.SlideNumber is int slideNumber
                    ? $"slide {slideNumber}"
                    : "page unknown";

            var sourceLabel = $"Source {index + 1} | {chunk.Document.OriginalName} | {pageLabel} | score {Math.Round(chunk.Score, 3)} | vector {Math.Round(chunk.VectorScore, 3)}";
            return $"[{sourceLabel}]\n{chunk.Chunk.Content}";
        }));
    }

    private static string BuildExtractiveFallback(string question, IReadOnlyCollection<RetrievedChunk> chunks)
    {
        var questionTokens = TokenizeQuestion(question)
            .Where(token => token.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = chunks
            .SelectMany(chunk => SplitSentences(chunk.Chunk.Content)
                .Select(sentence => new
                {
                    Sentence = sentence.Trim(),
                    Score = ScoreSentence(sentence, questionTokens)
                }))
            .Where(x => x.Sentence.Length >= 20 && x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Sentence.Length)
            .Select(x => x.Sentence)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (candidates.Count == 0)
            return "Mình chưa thấy đủ thông tin rõ ràng trong tài liệu đã index để trả lời câu này.";

        if (candidates.Count == 1)
            return $"Dựa trên tài liệu, {NormalizeSentence(candidates[0])}";

        if (candidates.Count == 2)
            return $"Dựa trên tài liệu, {NormalizeSentence(candidates[0])} {NormalizeSentence(candidates[1])}";

        return $"Dựa trên tài liệu, {NormalizeSentence(candidates[0])} {NormalizeSentence(candidates[1])} {NormalizeSentence(candidates[2])}";
    }

    private static double ScoreSentence(string sentence, ISet<string> questionTokens)
    {
        if (questionTokens.Count == 0)
            return 0;

        var sentenceTokens = TokenizeQuestion(sentence)
            .Where(token => token.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var overlap = questionTokens.Count(token => sentenceTokens.Contains(token));
        var overlapScore = (double)overlap / questionTokens.Count;

        return overlapScore;
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var buffer = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            buffer.Append(ch);

            if (ch is '.' or '!' or '?' or ';' or '\n')
            {
                var sentence = buffer.ToString().Trim();
                buffer.Clear();

                if (!string.IsNullOrWhiteSpace(sentence))
                    yield return sentence.TrimEnd('.', '!', '?', ';');
            }
        }

        var tail = buffer.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(tail))
            yield return tail;
    }

    private static string NormalizeSentence(string sentence)
    {
        return sentence.Trim().TrimEnd('.', ';', ':');
    }

    private static bool LooksInsufficient(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return true;

        var lower = answer.ToLowerInvariant();
        return lower.Contains("do not contain enough information")
            || lower.Contains("does not contain enough information")
            || lower.Contains("not enough information")
            || lower.Contains("insufficient information")
            || lower.Contains("không đủ thông tin")
            || lower.Contains("không tìm thấy nội dung phù hợp")
            || lower.Contains("chưa thấy đủ thông tin");
    }

    private static IEnumerable<string> TokenizeQuestion(string text)
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
                yield return token.ToString();
                token.Clear();
            }
        }

        if (token.Length > 0)
            yield return token.ToString();
    }

    private static bool IsDefinitionQuestion(string question)
    {
        var lower = question.Trim().ToLowerInvariant();
        return lower.StartsWith("what is ")
            || lower.StartsWith("what are ")
            || lower.StartsWith("define ")
            || lower.StartsWith("explain ")
            || lower.StartsWith("what does ");
    }

    private static string? ExtractAnswer(string payload)
    {
        using var document = JsonDocument.Parse(payload);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return null;

        var parts = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")
            .EnumerateArray();

        var texts = parts
            .Select(part => part.TryGetProperty("text", out var text) ? text.GetString() : null)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return texts.Count == 0 ? null : string.Join(string.Empty, texts);
    }
}