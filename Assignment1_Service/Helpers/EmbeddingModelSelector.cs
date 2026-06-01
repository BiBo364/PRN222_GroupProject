using Assignment1_Repository.Models;

namespace Assignment1_Service.Helpers;

public static class EmbeddingModelSelector
{
    public static EmbeddingModelPlan Resolve(IReadOnlyCollection<EmbeddingModel> models)
    {
        var ordered = models
            .Where(model => model is not null)
            .OrderBy(model => model.Id)
            .ToList();

        var geminiModels = ordered
            .Where(IsGeminiModel)
            .ToList();

        var localFallbackModels = ordered
            .Where(IsLocalFallbackModel)
            .ToList();

        var unsupportedModels = ordered
            .Where(model => !IsGeminiModel(model) && !IsLocalFallbackModel(model))
            .ToList();

        var localModelIds = localFallbackModels.Select(model => model.Id).ToHashSet();
        var dedicatedGeminiModels = geminiModels
            .Where(model => !localModelIds.Contains(model.Id))
            .ToList();

        return new EmbeddingModelPlan(localFallbackModels, dedicatedGeminiModels, unsupportedModels);
    }

    public static EmbeddingModelPlan ResolveForExecution(IReadOnlyCollection<EmbeddingModel> models, out bool usedDegradedFallback)
    {
        var plan = Resolve(models);
        if (plan.GeminiModels.Count == 0
            && plan.LocalFallbackModels.Count == 0
            && plan.UnsupportedModels.Count > 0)
        {
            usedDegradedFallback = true;
            return new EmbeddingModelPlan(plan.UnsupportedModels, Array.Empty<EmbeddingModel>(), Array.Empty<EmbeddingModel>());
        }

        usedDegradedFallback = false;
        return plan;
    }

    public static bool IsGeminiModel(EmbeddingModel model)
    {
        var provider = model.Provider?.Trim() ?? string.Empty;
        var name = model.Name?.Trim() ?? string.Empty;
        var modelId = model.ModelId?.Trim() ?? string.Empty;
        var apiKeyEnv = model.ApiKeyEnv?.Trim() ?? string.Empty;
        var description = model.Description?.Trim() ?? string.Empty;

        if (provider.Equals("google", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
            return true;

        var haystack = string.Join(" ", name, modelId, apiKeyEnv, description);
        return haystack.Contains("gemini", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("google", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("embedding-001", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLocalFallbackModel(EmbeddingModel model)
    {
        var provider = model.Provider?.Trim() ?? string.Empty;
        var name = model.Name?.Trim() ?? string.Empty;
        var modelId = model.ModelId?.Trim() ?? string.Empty;
        var description = model.Description?.Trim() ?? string.Empty;

        if (provider.Equals("local", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("simple", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("internal", StringComparison.OrdinalIgnoreCase))
            return true;

        var haystack = string.Join(" ", name, modelId, description);
        return haystack.Contains("local", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("simple", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("fallback", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record EmbeddingModelPlan(
    IReadOnlyList<EmbeddingModel> LocalFallbackModels,
    IReadOnlyList<EmbeddingModel> GeminiModels,
    IReadOnlyList<EmbeddingModel> UnsupportedModels);
