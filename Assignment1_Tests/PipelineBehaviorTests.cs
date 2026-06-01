using Assignment1_Repository.Models;
using Assignment1_Service.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using RepoDocument = Assignment1_Repository.Models.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace Assignment1_Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public void Resolve_ClassifiesModelsWithoutPromotingFirstModel()
    {
        var models = new List<EmbeddingModel>
        {
            new() { Id = 1, Name = "local-simple", Provider = "local", ModelId = "simple", Description = "fallback" },
            new() { Id = 2, Name = "openai-embedding", Provider = "openai", ModelId = "text-embedding-3-small", Description = "external" },
            new() { Id = 3, Name = "gemini-embedding", Provider = "google", ModelId = "embedding-001", Description = "gemini" }
        };

        var plan = EmbeddingModelSelector.Resolve(models);

        Assert.Single(plan.LocalFallbackModels);
        Assert.Single(plan.GeminiModels);
        Assert.Single(plan.UnsupportedModels);
        Assert.Equal(1, plan.LocalFallbackModels[0].Id);
        Assert.Equal(3, plan.GeminiModels[0].Id);
        Assert.Equal(2, plan.UnsupportedModels[0].Id);
    }

    [Fact]
    public void ResolveForExecution_FallsBackToLocalWhenOnlyUnsupportedModelsExist()
    {
        var models = new List<EmbeddingModel>
        {
            new() { Id = 1, Name = "huggingface/multilingual-e5-base", Provider = "huggingface", ModelId = "intfloat/multilingual-e5-base", Description = "external" },
            new() { Id = 2, Name = "openai/text-embedding-3-small", Provider = "openai", ModelId = "text-embedding-3-small", Description = "external" }
        };

        var plan = EmbeddingModelSelector.ResolveForExecution(models, out var usedDegradedFallback);

        Assert.True(usedDegradedFallback);
        Assert.Equal(2, plan.LocalFallbackModels.Count);
        Assert.Empty(plan.GeminiModels);
        Assert.Empty(plan.UnsupportedModels);
    }

    [Fact]
    public void Retrieve_ReturnsHighestCosineMatchesAndLimitsTopK()
    {
        var document = new RepoDocument { Id = 10, OriginalName = "test.docx", Filename = "test.docx", FileType = "docx", StoragePath = "mem://test" };

        var chunks = new List<Chunk>
        {
            CreateChunk(1, document, [1f, 0f], "alpha"),
            CreateChunk(2, document, [0.8f, 0.2f], "beta"),
            CreateChunk(3, document, [0f, 1f], "gamma")
        };

        var queryVectors = new Dictionary<int, float[]>
        {
            [100] = [1f, 0f]
        };

        var results = ChunkRetriever.Retrieve("alpha question", chunks, queryVectors, topK: 2, useHybridRerank: false);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Chunk.Id);
        Assert.Equal(2, results[1].Chunk.Id);
        Assert.True(results[0].Score >= results[1].Score);
    }

    [Fact]
    public void ExtractPages_ReturnsDocxContentAsSinglePage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");

        try
        {
            CreateDocx(path, "Hello from DOCX");

            var pages = TextExtractor.ExtractPages(path, "docx");

            Assert.Single(pages);
            Assert.Equal(1, pages[0].PageNumber);
            Assert.Contains("Hello from DOCX", pages[0].Content);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static Chunk CreateChunk(int id, RepoDocument document, float[] vector, string content)
    {
        var chunk = new Chunk
        {
            Id = id,
            DocumentId = document.Id,
            Document = document,
            ChunkIndex = id,
            Content = content,
            PageNumber = 1,
            CreatedAt = DateTime.UtcNow
        };

        chunk.Embeddings.Add(new Embedding
        {
            ChunkId = id,
            EmbeddingModelId = 100,
            Vector = VectorMath.SerializeVector(vector),
            CreatedAt = DateTime.UtcNow,
            Chunk = chunk,
            EmbeddingModel = new EmbeddingModel { Id = 100, Name = "test", Provider = "local", ModelId = "simple", Dimension = 2 }
        });

        return chunk;
    }

    private static void CreateDocx(string path, string text)
    {
        using var document = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new WordDocument(new Body(new Paragraph(new Run(new Text(text)))));
        mainPart.Document.Save();
    }
}
