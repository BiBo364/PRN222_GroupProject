using System.Diagnostics;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace RagEdu.Tests.Performance;

public sealed class LiveChatLatencyBenchmarkTests
{
    private const int BatchSize = 120;
    private const int CandidateBuffer = 24;
    private const int TopK = 4;
    private const string Question = "Serialization là gì? Bạn hãy giải thích cho tôi";

    private readonly ITestOutputHelper _output;

    public LiveChatLatencyBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "LiveBenchmark")]
    public async Task Measures_live_retrieval_and_answer_generation_when_explicitly_enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_CHAT_BENCHMARK"), "1", StringComparison.Ordinal))
        {
            _output.WriteLine("Skipped: set RUN_LIVE_CHAT_BENCHMARK=1 to call the configured Gemini service.");
            return;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(repositoryRoot)
            .AddJsonFile("Assignmet1_Presentation/appsettings.json", optional: false)
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var contextOptions = new DbContextOptionsBuilder<RagEduContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new RagEduContext(contextOptions);
        var repository = new ChatRepository(context);
        var configuredEmbeddingModels = await repository.GetEmbeddingModelsAsync();
        Assert.NotEmpty(configuredEmbeddingModels);
        var embeddingModels = EmbeddingModelSelector.SelectForQueryAndRetrieval(configuredEmbeddingModels);

        var subject = await context.Subjects
            .Where(item => item.Code == "PRN222" && item.Documents.Any(document => document.Status == "indexed"))
            .SingleOrDefaultAsync();
        Assert.NotNull(subject);

        var geminiOptions = configuration.GetSection("Gemini").Get<GeminiOptions>() ?? new GeminiOptions();
        using var httpClient = new HttpClient();
        var embeddingService = new EmbeddingService(httpClient, Options.Create(geminiOptions), NullLogger<EmbeddingService>.Instance);
        var geminiService = new GeminiService(
            new GeminiClient(httpClient, Options.Create(geminiOptions), NullLogger<GeminiClient>.Instance),
            NullLogger<GeminiService>.Instance);

        var embeddingTimer = Stopwatch.StartNew();
        var queryVectors = await embeddingService.GenerateQueryEmbeddingsAsync(Question, embeddingModels);
        embeddingTimer.Stop();
        Assert.NotEmpty(queryVectors);

        var retrievalTimer = Stopwatch.StartNew();
        var chunks = await RetrieveAsync(repository, subject!.Id, queryVectors, embeddingModels);
        retrievalTimer.Stop();
        Assert.NotEmpty(chunks);

        var answerTimer = Stopwatch.StartNew();
        var answer = await geminiService.GenerateAnswerAsync(Question, chunks, []);
        answerTimer.Stop();
        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.False(answer.StartsWith("Dựa trên tài liệu,", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(answer, character => "ăâđêôơưĂÂĐÊÔƠƯ".Contains(character));

        var total = embeddingTimer.Elapsed + retrievalTimer.Elapsed + answerTimer.Elapsed;
        _output.WriteLine($"Subject: {subject.Code}; retrieved chunks: {chunks.Count}");
        _output.WriteLine($"Query embedding: {embeddingTimer.Elapsed.TotalMilliseconds:F0} ms");
        _output.WriteLine($"Chunk retrieval: {retrievalTimer.Elapsed.TotalMilliseconds:F0} ms");
        _output.WriteLine($"Gemini answer generation: {answerTimer.Elapsed.TotalMilliseconds:F0} ms");
        _output.WriteLine($"RAG total before saving chat: {total.TotalMilliseconds:F0} ms");
    }

    private static async Task<List<RetrievedChunk>> RetrieveAsync(
        ChatRepository repository,
        int subjectId,
        IReadOnlyDictionary<int, float[]> queryVectors,
        IReadOnlyCollection<EmbeddingModel> embeddingModels)
    {
        var candidates = new Dictionary<int, RetrievedChunk>();
        var modelIds = embeddingModels.Select(model => model.Id).ToArray();
        int? lastChunkId = null;

        while (true)
        {
            var batch = await repository.GetIndexedChunkBatchBySubjectAsync(subjectId, modelIds, lastChunkId, BatchSize);
            if (batch.Count == 0)
                break;

            lastChunkId = batch[^1].Id;
            foreach (var chunk in ChunkRetriever.Retrieve(Question, batch, queryVectors, CandidateBuffer, useHybridRerank: true))
            {
                if (!candidates.TryGetValue(chunk.Chunk.Id, out var current) || chunk.Score > current.Score)
                    candidates[chunk.Chunk.Id] = chunk;
            }

            if (candidates.Count > CandidateBuffer)
            {
                var retained = candidates.Values
                    .OrderByDescending(chunk => chunk.Score)
                    .ThenByDescending(chunk => chunk.VectorScore)
                    .Take(CandidateBuffer)
                    .ToDictionary(chunk => chunk.Chunk.Id);
                candidates.Clear();
                foreach (var retainedChunk in retained)
                    candidates[retainedChunk.Key] = retainedChunk.Value;
            }

            if (batch.Count < BatchSize)
                break;
        }

        return candidates.Values
            .OrderByDescending(chunk => chunk.Score)
            .ThenByDescending(chunk => chunk.VectorScore)
            .Take(TopK)
            .ToList();
    }
}
