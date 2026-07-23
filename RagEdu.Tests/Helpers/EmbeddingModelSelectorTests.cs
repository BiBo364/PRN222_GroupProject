using Assignment1_Repository.Models;
using Assignment1_Service.Helpers;

namespace RagEdu.Tests.Helpers;

public sealed class EmbeddingModelSelectorTests
{
    [Fact]
    public void SelectForQueryAndRetrieval_UnsupportedModels_UsesOnlyHighestDimensionFallback()
    {
        var selected = EmbeddingModelSelector.SelectForQueryAndRetrieval(
        [
            new EmbeddingModel { Id = 1, Provider = "huggingface", Name = "e5", ModelId = "e5", Dimension = 768 },
            new EmbeddingModel { Id = 2, Provider = "openai", Name = "text-embedding-3-small", ModelId = "text-embedding-3-small", Dimension = 1536 },
            new EmbeddingModel { Id = 3, Provider = "huggingface", Name = "bge", ModelId = "bge", Dimension = 1024 }
        ]);

        var model = Assert.Single(selected);
        Assert.Equal(2, model.Id);
    }

    [Fact]
    public void SelectForQueryAndRetrieval_SupportedLocalOrGeminiModels_KeepsConfiguredModels()
    {
        var selected = EmbeddingModelSelector.SelectForQueryAndRetrieval(
        [
            new EmbeddingModel { Id = 1, Provider = "local", Name = "simple", ModelId = "simple", Dimension = 768 },
            new EmbeddingModel { Id = 2, Provider = "google", Name = "gemini", ModelId = "gemini-embedding-001", Dimension = 768 }
        ]);

        Assert.Equal([1, 2], selected.Select(model => model.Id));
    }
}
