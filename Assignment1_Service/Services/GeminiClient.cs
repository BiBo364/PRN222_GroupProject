using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public sealed class GeminiClient : IGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ModelName => _options.Model;

    public Task<string> GenerateTextAsync(
        string systemInstruction,
        IReadOnlyCollection<GeminiMessage> messages,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = messages.Select(message => new
            {
                role = message.Role,
                parts = new[] { new { text = message.Text } }
            }),
            generationConfig = new
            {
                temperature,
                maxOutputTokens = maximumOutputTokens
            }
        };

        return SendAsync(request, cancellationToken);
    }

    public async Task<T> GenerateJsonAsync<T>(
        string systemInstruction,
        string prompt,
        object responseSchema,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature,
                maxOutputTokens = maximumOutputTokens,
                responseMimeType = "application/json",
                responseSchema
            }
        };

        var payload = await SendAsync(request, cancellationToken);
        var normalizedPayload = RemoveCodeFence(payload);
        var result = JsonSerializer.Deserialize<T>(normalizedPayload, JsonOptions);

        return result
            ?? throw new InvalidOperationException("AI không trả về dữ liệu JSON hợp lệ.");
    }

    private async Task<string> SendAsync(object request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình khóa API cho dịch vụ AI. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent";
        const int maximumAttempts = 3;

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
            httpRequest.Content = JsonContent.Create(request);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Gemini request timed out.");
                throw new InvalidOperationException(
                    "AI phản hồi quá lâu. Vui lòng thử lại với ít tài liệu hoặc số câu hỏi nhỏ hơn.");
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Could not connect to Gemini.");
                throw new InvalidOperationException(
                    "Không thể kết nối đến dịch vụ AI. Vui lòng kiểm tra kết nối mạng và thử lại.");
            }

            using (response)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var canRetry = IsTransient(response.StatusCode) && attempt < maximumAttempts;
                    _logger.LogWarning(
                        "Gemini request failed with HTTP {StatusCode} on attempt {Attempt}/{MaximumAttempts}. Response length: {ResponseLength}. Retrying: {CanRetry}.",
                        (int)response.StatusCode,
                        attempt,
                        maximumAttempts,
                        responseBody.Length,
                        canRetry);

                    if (canRetry)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(650 * attempt), cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException(BuildFriendlyError(response.StatusCode));
                }

                var generatedText = ExtractText(responseBody);
                if (string.IsNullOrWhiteSpace(generatedText))
                    throw new InvalidOperationException("AI không trả về nội dung. Vui lòng điều chỉnh yêu cầu và thử lại.");

                return generatedText.Trim();
            }
        }

        throw new InvalidOperationException("AI chưa thể xử lý yêu cầu. Vui lòng thử lại sau.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static string BuildFriendlyError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest =>
                "AI từ chối cấu trúc yêu cầu. Vui lòng giảm số lượng nội dung hoặc thử lại.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Khóa API của dịch vụ AI không hợp lệ hoặc chưa có quyền sử dụng mô hình đã chọn.",
            HttpStatusCode.NotFound =>
                "Mô hình AI đã cấu hình không còn khả dụng. Vui lòng liên hệ quản trị viên.",
            HttpStatusCode.TooManyRequests =>
                "AI đã đạt giới hạn yêu cầu hoặc hạn mức sử dụng. Vui lòng thử lại sau.",
            HttpStatusCode.ServiceUnavailable =>
                "AI đang quá tải tạm thời. Vui lòng chờ một lát rồi thử lại.",
            _ =>
                $"AI không thể xử lý yêu cầu lúc này (HTTP {(int)statusCode}). Vui lòng thử lại sau."
        };
    }

    private static string? ExtractText(string payload)
    {
        using var document = JsonDocument.Parse(payload);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var texts = parts.EnumerateArray()
            .Select(part => part.TryGetProperty("text", out var text) ? text.GetString() : null)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Concat(texts);
    }

    private static string RemoveCodeFence(string payload)
    {
        var text = payload.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var firstLineEnd = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        if (firstLineEnd < 0 || lastFence <= firstLineEnd)
            return text;

        return text[(firstLineEnd + 1)..lastFence].Trim();
    }
}
