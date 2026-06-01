using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Assignment1_Repository.Models;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class EmbeddingService : IEmbeddingService
{
    private const int BatchSize = 100;
    private const int MaxAttempts = 3;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(8);

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Dictionary<int, List<float[]>>> GenerateDocumentEmbeddingsAsync(
        IReadOnlyList<string> texts,
        IReadOnlyCollection<EmbeddingModel> models,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, List<float[]>>();
        if (texts.Count == 0 || models.Count == 0)
            return result;

        var plan = EmbeddingModelSelector.ResolveForExecution(models, out var usedDegradedFallback);

        EnsureSupported(plan);

        if (usedDegradedFallback)
        {
            _logger.LogWarning(
                "No Gemini or explicit local embedding models were configured. Treating {Count} configured model(s) as local simple embeddings.",
                plan.LocalFallbackModels.Count);
        }

        _logger.LogInformation(
            "Generating document embeddings with {GeminiCount} Gemini model(s) and {LocalCount} local fallback model(s).",
            plan.GeminiModels.Count,
            plan.LocalFallbackModels.Count);

        foreach (var localModel in plan.LocalFallbackModels)
        {
            _logger.LogInformation(
                "Using local fallback embedding for model {ModelId} ({ModelName}) with dimension {Dimension}.",
                localModel.ModelId,
                localModel.Name,
                localModel.Dimension);
            result[localModel.Id] = texts
                .Select(text => SimpleEmbedder.GenerateVectorArray(text, localModel.Dimension))
                .ToList();
        }

        foreach (var geminiModel in plan.GeminiModels)
        {
            result[geminiModel.Id] = await GenerateGeminiEmbeddingsWithFallbackAsync(
                texts,
                geminiModel,
                cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<int, float[]>> GenerateQueryEmbeddingsAsync(
        string text,
        IReadOnlyCollection<EmbeddingModel> models,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, float[]>();
        if (string.IsNullOrWhiteSpace(text) || models.Count == 0)
            return result;

        var plan = EmbeddingModelSelector.ResolveForExecution(models, out var usedDegradedFallback);

        EnsureSupported(plan);

        if (usedDegradedFallback)
        {
            _logger.LogWarning(
                "No Gemini or explicit local embedding models were configured. Treating {Count} configured model(s) as local simple embeddings.",
                plan.LocalFallbackModels.Count);
        }

        _logger.LogInformation(
            "Generating query embeddings with {GeminiCount} Gemini model(s) and {LocalCount} local fallback model(s).",
            plan.GeminiModels.Count,
            plan.LocalFallbackModels.Count);

        foreach (var localModel in plan.LocalFallbackModels)
        {
            _logger.LogInformation(
                "Using local fallback embedding for query model {ModelId} ({ModelName}).",
                localModel.ModelId,
                localModel.Name);
            result[localModel.Id] = SimpleEmbedder.GenerateVectorArray(text, localModel.Dimension);
        }

        foreach (var geminiModel in plan.GeminiModels)
        {
            var vectors = await GenerateGeminiEmbeddingsWithFallbackAsync(
                new[] { text },
                geminiModel,
                cancellationToken);
            if (vectors.Count > 0)
                result[geminiModel.Id] = vectors[0];
        }

        return result;
    }

    private void EnsureSupported(EmbeddingModelPlan plan)
    {
        if (plan.UnsupportedModels.Count == 0)
            return;

        var details = string.Join(
            ", ",
            plan.UnsupportedModels.Select(model =>
                $"{model.Id}:{model.Provider}/{model.Name}/{model.ModelId}"));

        throw new InvalidOperationException(
            $"Unsupported embedding model configuration detected: {details}. Configure Gemini models or an explicit local fallback model.");
    }

    private async Task<List<float[]>> GenerateGeminiEmbeddingsAsync(
        IReadOnlyList<string> texts,
        EmbeddingModel model,
        CancellationToken cancellationToken)
    {
        var vectors = new List<float[]>();
        if (texts.Count == 0 || string.IsNullOrWhiteSpace(_options.ApiKey))
            return vectors;

        var configuredModel = string.IsNullOrWhiteSpace(model.ModelId)
            ? _options.EmbeddingModel
            : model.ModelId.Trim();
        var apiModel = configuredModel.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? configuredModel
            : $"models/{configuredModel}";
        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/{apiModel}:batchEmbedContents";

        for (var index = 0; index < texts.Count; index += BatchSize)
        {
            var batch = texts.Skip(index).Take(BatchSize).ToList();
            var batchVectors = await SendBatchAsync(endpoint, apiModel, batch, cancellationToken);
            vectors.AddRange(batchVectors);
        }

        return vectors;
    }

    private async Task<List<float[]>> GenerateGeminiEmbeddingsWithFallbackAsync(
        IReadOnlyList<string> texts,
        EmbeddingModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var geminiVectors = await GenerateGeminiEmbeddingsAsync(texts, model, cancellationToken);
            if (geminiVectors.Count == texts.Count)
                return geminiVectors;
        }
        catch
        {
            _logger.LogWarning(
                "Gemini embedding failed for model {ModelId} ({ModelName}); falling back to local simple embedding.",
                model.ModelId,
                model.Name);
        }

        return texts
            .Select(text => SimpleEmbedder.GenerateVectorArray(text, model.Dimension))
            .ToList();
    }

    private async Task<List<float[]>> SendBatchAsync(
        string endpoint,
        string apiModel,
        IReadOnlyList<string> batch,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = JsonContent.Create(new
            {
                requests = batch.Select(text => new
                {
                    model = apiModel,
                    content = new
                    {
                        parts = new[]
                        {
                            new { text }
                        }
                    }
                })
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return ParseEmbeddingBatch(payload);

            if (ShouldRetry(response.StatusCode) && attempt < MaxAttempts - 1)
            {
                await Task.Delay(GetRetryDelay(response, attempt), cancellationToken);
                continue;
            }

            if (IsQuotaExhausted(response.StatusCode, payload)
                || IsUnsupportedModel(response.StatusCode, payload))
                return new List<float[]>();

            throw new InvalidOperationException(
                $"Gemini embedding request failed with {(int)response.StatusCode} {response.StatusCode}: {TrimPayload(payload)}");
        }

        return new List<float[]>();
    }

    private static List<float[]> ParseEmbeddingBatch(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("embeddings", out var embeddingsElement))
            return new List<float[]>();

        var vectors = new List<float[]>();
        foreach (var item in embeddingsElement.EnumerateArray())
        {
            if (!item.TryGetProperty("values", out var valuesElement))
                continue;

            var vector = valuesElement
                .EnumerateArray()
                .Select(value => value.GetSingle())
                .ToArray();

            if (vector.Length > 0)
                vectors.Add(vector);
        }

        return vectors;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests
               || statusCode == HttpStatusCode.ServiceUnavailable;
    }

    private static bool IsQuotaExhausted(HttpStatusCode statusCode, string payload)
    {
        if (statusCode != HttpStatusCode.TooManyRequests)
            return false;

        return payload.Contains("resource_exhausted", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("embed_content_free_tier_requests", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("quota", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsupportedModel(HttpStatusCode statusCode, string payload)
    {
        if (statusCode != HttpStatusCode.NotFound
            && statusCode != HttpStatusCode.BadRequest)
            return false;

        return payload.Contains("not found", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("not supported", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("ModelService.ListModels", StringComparison.OrdinalIgnoreCase)
               || payload.Contains("embedContent", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
            return delta <= MaxRetryDelay ? delta : MaxRetryDelay;

        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryAt)
        {
            var calculated = retryAt - DateTimeOffset.UtcNow;
            if (calculated > TimeSpan.Zero)
                return calculated <= MaxRetryDelay ? calculated : MaxRetryDelay;
        }

        var fallback = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt + 1), MaxRetryDelay.TotalSeconds));
        return fallback;
    }

    private static string TrimPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return "Empty response body.";

        var compact = payload.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 300 ? compact : compact[..300] + "...";
    }
}
