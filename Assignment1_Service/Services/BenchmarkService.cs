using System.Diagnostics;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class BenchmarkService : IBenchmarkService
{
    private const int RetrievalBatchSize = 120;
    private const int RetrievalCandidateBuffer = 24;

    private readonly IBenchmarkRepository _benchmarkRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiService _geminiService;
    private readonly GeminiOptions _geminiOptions;
    private readonly ILogger<BenchmarkService> _logger;

    public BenchmarkService(
        IBenchmarkRepository benchmarkRepository,
        IChatRepository chatRepository,
        IEmbeddingService embeddingService,
        IGeminiService geminiService,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<BenchmarkService> logger)
    {
        _benchmarkRepository = benchmarkRepository;
        _chatRepository = chatRepository;
        _embeddingService = embeddingService;
        _geminiService = geminiService;
        _geminiOptions = geminiOptions.Value;
        _logger = logger;
    }

    public async Task<List<TestQuestionDto>> GetTestQuestionsAsync(int? subjectId = null)
    {
        var questions = await _benchmarkRepository.GetTestQuestionsAsync(subjectId);
        return questions.Select(ToDto).ToList();
    }

    public async Task<TestQuestionDto?> GetTestQuestionAsync(int id)
    {
        var question = await _benchmarkRepository.GetTestQuestionByIdAsync(id);
        return question is null ? null : ToDto(question);
    }

    public async Task<TestQuestionDto> CreateTestQuestionAsync(CreateTestQuestionDto dto, string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(dto.Question))
            throw new ArgumentException("Question is required.");
        if (string.IsNullOrWhiteSpace(dto.GroundTruth))
            throw new ArgumentException("Ground truth answer is required.");

        var question = new TestQuestion
        {
            SubjectId = dto.SubjectId,
            ChapterId = dto.ChapterId,
            Question = dto.Question.Trim(),
            GroundTruth = dto.GroundTruth.Trim(),
            GroundTruthChunks = string.IsNullOrWhiteSpace(dto.GroundTruthChunks) ? null : dto.GroundTruthChunks.Trim(),
            Difficulty = string.IsNullOrWhiteSpace(dto.Difficulty) ? null : dto.Difficulty.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        question = await _benchmarkRepository.AddTestQuestionAsync(question);
        return ToDto(question);
    }

    public async Task UpdateTestQuestionAsync(int id, CreateTestQuestionDto dto)
    {
        var existing = await _benchmarkRepository.GetTestQuestionByIdAsync(id)
            ?? throw new InvalidOperationException("Test question not found.");

        if (string.IsNullOrWhiteSpace(dto.Question))
            throw new ArgumentException("Question is required.");
        if (string.IsNullOrWhiteSpace(dto.GroundTruth))
            throw new ArgumentException("Ground truth answer is required.");

        existing.SubjectId = dto.SubjectId;
        existing.ChapterId = dto.ChapterId;
        existing.Question = dto.Question.Trim();
        existing.GroundTruth = dto.GroundTruth.Trim();
        existing.GroundTruthChunks = string.IsNullOrWhiteSpace(dto.GroundTruthChunks) ? null : dto.GroundTruthChunks.Trim();
        existing.Difficulty = string.IsNullOrWhiteSpace(dto.Difficulty) ? null : dto.Difficulty.Trim();
        existing.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim();

        await _benchmarkRepository.UpdateTestQuestionAsync(existing);
    }

    public Task DeleteTestQuestionAsync(int id)
        => _benchmarkRepository.DeleteTestQuestionAsync(id);

    public async Task<List<BenchmarkRunListItemDto>> GetBenchmarkRunsAsync()
    {
        var runs = await _benchmarkRepository.GetBenchmarkRunsAsync();
        return runs.Select(ToListItemDto).ToList();
    }

    public async Task<BenchmarkRunDetailDto?> GetBenchmarkRunAsync(int runId)
    {
        var run = await _benchmarkRepository.GetBenchmarkRunDetailAsync(runId);
        return run is null ? null : ToDetailDto(run);
    }

    public async Task<List<BenchmarkConfigOptionDto>> GetEmbeddingModelOptionsAsync()
    {
        var models = await _benchmarkRepository.GetEmbeddingModelsAsync();
        return models.Select(model => new BenchmarkConfigOptionDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = $"{model.Provider} | {model.ModelId}"
        }).ToList();
    }

    public async Task<List<BenchmarkConfigOptionDto>> GetChunkingConfigOptionsAsync()
    {
        var configs = await _benchmarkRepository.GetChunkingConfigsAsync();
        return configs.Select(config => new BenchmarkConfigOptionDto
        {
            Id = config.Id,
            Name = config.Name,
            Description = $"{config.Strategy} | size {config.ChunkSize} | overlap {config.ChunkOverlap}"
        }).ToList();
    }

    public async Task<List<SubjectDto>> GetSubjectsWithTestQuestionsAsync()
    {
        var subjects = await _benchmarkRepository.GetSubjectsWithTestQuestionsAsync();
        return subjects.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<int> RunBenchmarkAsync(CreateBenchmarkRunDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Benchmark name is required.");

        var questions = await _benchmarkRepository.GetTestQuestionsAsync(dto.SubjectId);
        if (questions.Count == 0)
            throw new InvalidOperationException("No test questions found for this benchmark.");

        var run = await _benchmarkRepository.CreateBenchmarkRunAsync(new BenchmarkRun
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            EmbeddingModelId = dto.EmbeddingModelId,
            ChunkingConfigId = dto.ChunkingConfigId,
            LlmModel = _geminiOptions.Model,
            TopK = dto.TopK > 0 ? dto.TopK : 4,
            UseReranker = dto.UseReranker,
            Status = "running",
            StartedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        });

        var embeddingModels = await _chatRepository.GetEmbeddingModelsAsync();
        if (embeddingModels.Count == 0)
            throw new InvalidOperationException("No embedding model configured.");

        if (dto.EmbeddingModelId.HasValue)
        {
            embeddingModels = embeddingModels
                .Where(model => model.Id == dto.EmbeddingModelId.Value)
                .ToList();

            if (embeddingModels.Count == 0)
                throw new InvalidOperationException("Selected embedding model was not found.");
        }

        var modelIds = embeddingModels.Select(model => model.Id).ToArray();
        var results = new List<BenchmarkResult>();

        foreach (var question in questions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!question.SubjectId.HasValue)
            {
                results.Add(await SaveFailedResultAsync(run.Id, question, "Test question has no subject."));
                continue;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var queryVectors = await _embeddingService.GenerateQueryEmbeddingsAsync(
                    question.Question,
                    embeddingModels);

                if (queryVectors.Count == 0)
                {
                    results.Add(await SaveFailedResultAsync(run.Id, question, "Unable to generate embeddings."));
                    continue;
                }

                var retrieved = await RetrieveChunksAsync(
                    question.Question,
                    question.SubjectId.Value,
                    queryVectors,
                    embeddingModels,
                    dto.TopK > 0 ? dto.TopK : 4,
                    dto.UseReranker);

                string answer;
                if (retrieved.Count == 0)
                {
                    answer = "Khong tim thay noi dung phu hop trong tai lieu da index.";
                }
                else
                {
                    answer = await _geminiService.GenerateAnswerAsync(
                        question.Question,
                        retrieved,
                        Array.Empty<ChatMessageDto>(),
                        cancellationToken);
                }

                stopwatch.Stop();

                var retrievedIds = retrieved.Select(chunk => chunk.Chunk.Id).ToList();
                var metrics = RagMetricsEvaluator.Evaluate(
                    question.Question,
                    question.GroundTruth,
                    answer,
                    retrievedIds,
                    question.GroundTruthChunks,
                    retrieved.Select(chunk => chunk.Chunk.Content).ToList());

                var result = new BenchmarkResult
                {
                    RunId = run.Id,
                    QuestionId = question.Id,
                    GeneratedAnswer = answer,
                    RetrievedChunkIds = retrievedIds.Count == 0 ? null : string.Join(",", retrievedIds),
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    Faithfulness = metrics.Faithfulness,
                    AnswerRelevancy = metrics.AnswerRelevancy,
                    ContextPrecision = metrics.ContextPrecision,
                    ContextRecall = metrics.ContextRecall,
                    AnswerCorrectness = metrics.AnswerCorrectness,
                    CreatedAt = DateTime.Now
                };

                await _benchmarkRepository.AddBenchmarkResultAsync(result);
                results.Add(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Benchmark question {QuestionId} failed in run {RunId}.", question.Id, run.Id);
                results.Add(await SaveFailedResultAsync(run.Id, question, ex.Message, (int)stopwatch.ElapsedMilliseconds));
            }
        }

        var successfulResults = results.Where(result => string.IsNullOrWhiteSpace(result.ErrorMsg)).ToList();
        var latencies = successfulResults
            .Where(result => result.LatencyMs.HasValue)
            .Select(result => (double)result.LatencyMs!.Value)
            .OrderBy(value => value)
            .ToList();

        await _benchmarkRepository.SaveBenchmarkSummaryAsync(new BenchmarkSummary
        {
            RunId = run.Id,
            TotalQuestions = results.Count,
            AvgFaithfulness = Average(successfulResults.Select(result => result.Faithfulness)),
            AvgAnswerRelevancy = Average(successfulResults.Select(result => result.AnswerRelevancy)),
            AvgContextPrecision = Average(successfulResults.Select(result => result.ContextPrecision)),
            AvgContextRecall = Average(successfulResults.Select(result => result.ContextRecall)),
            AvgAnswerCorrectness = Average(successfulResults.Select(result => result.AnswerCorrectness)),
            AvgLatencyMs = Average(latencies),
            P95LatencyMs = Percentile(latencies, 0.95),
            ComputedAt = DateTime.Now
        });

        run.Status = "completed";
        run.FinishedAt = DateTime.Now;
        await _benchmarkRepository.UpdateBenchmarkRunAsync(run);

        _logger.LogInformation(
            "Benchmark run {RunId} completed with {QuestionCount} question(s).",
            run.Id,
            results.Count);

        return run.Id;
    }

    private async Task<BenchmarkResult> SaveFailedResultAsync(
        int runId,
        TestQuestion question,
        string error,
        int? latencyMs = null)
    {
        var result = new BenchmarkResult
        {
            RunId = runId,
            QuestionId = question.Id,
            ErrorMsg = error,
            LatencyMs = latencyMs,
            CreatedAt = DateTime.Now
        };

        await _benchmarkRepository.AddBenchmarkResultAsync(result);
        return result;
    }

    private async Task<List<RetrievedChunk>> RetrieveChunksAsync(
        string question,
        int subjectId,
        IReadOnlyDictionary<int, float[]> queryVectors,
        IReadOnlyCollection<EmbeddingModel> embeddingModels,
        int topK,
        bool useHybridRerank)
    {
        var merged = new Dictionary<int, RetrievedChunk>();
        var modelIds = embeddingModels.Select(model => model.Id).ToArray();
        int? lastChunkId = null;

        while (true)
        {
            var batch = await _chatRepository.GetIndexedChunkBatchBySubjectAsync(
                subjectId,
                modelIds,
                lastChunkId,
                RetrievalBatchSize);

            if (batch.Count == 0)
                break;

            lastChunkId = batch[^1].Id;

            var candidates = ChunkRetriever.Retrieve(
                question,
                batch,
                queryVectors,
                RetrievalCandidateBuffer,
                useHybridRerank);

            foreach (var candidate in candidates)
            {
                if (!merged.TryGetValue(candidate.Chunk.Id, out var current)
                    || candidate.Score > current.Score)
                {
                    merged[candidate.Chunk.Id] = candidate;
                }
            }

            TrimRetrievedChunks(merged, RetrievalCandidateBuffer);

            if (batch.Count < RetrievalBatchSize)
                break;
        }

        return merged.Values
            .OrderByDescending(chunk => chunk.Score)
            .ThenByDescending(chunk => chunk.VectorScore)
            .Take(topK)
            .ToList();
    }

    private static void TrimRetrievedChunks(IDictionary<int, RetrievedChunk> candidates, int keepCount)
    {
        if (candidates.Count <= keepCount)
            return;

        var retained = candidates.Values
            .OrderByDescending(chunk => chunk.Score)
            .ThenByDescending(chunk => chunk.VectorScore)
            .Take(keepCount)
            .ToDictionary(chunk => chunk.Chunk.Id);

        candidates.Clear();
        foreach (var entry in retained)
            candidates[entry.Key] = entry.Value;
    }

    private static double? Average(IEnumerable<double?> values)
    {
        var items = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return items.Count == 0 ? null : Math.Round(items.Average(), 4);
    }

    private static double? Average(IEnumerable<double> values)
    {
        var items = values.ToList();
        return items.Count == 0 ? null : Math.Round(items.Average(), 4);
    }

    private static double? Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
            return null;

        var index = (int)Math.Ceiling(percentile * values.Count) - 1;
        index = Math.Clamp(index, 0, values.Count - 1);
        return Math.Round(values[index], 2);
    }

    private static TestQuestionDto ToDto(TestQuestion question) => new()
    {
        Id = question.Id,
        SubjectId = question.SubjectId,
        SubjectCode = question.Subject?.Code,
        SubjectName = question.Subject?.Name,
        ChapterId = question.ChapterId,
        Question = question.Question,
        GroundTruth = question.GroundTruth,
        GroundTruthChunks = question.GroundTruthChunks,
        Difficulty = question.Difficulty,
        Category = question.Category,
        CreatedBy = question.CreatedBy,
        CreatedAt = question.CreatedAt
    };

    private static BenchmarkRunListItemDto ToListItemDto(BenchmarkRun run) => new()
    {
        Id = run.Id,
        Name = run.Name,
        Description = run.Description,
        Status = run.Status,
        LlmModel = run.LlmModel,
        TopK = run.TopK,
        UseReranker = run.UseReranker,
        EmbeddingModelName = run.EmbeddingModel?.Name,
        ChunkingConfigName = run.ChunkingConfig?.Name,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        CreatedAt = run.CreatedAt,
        Summary = run.BenchmarkSummary is null ? null : ToSummaryDto(run.BenchmarkSummary)
    };

    private static BenchmarkRunDetailDto ToDetailDto(BenchmarkRun run) => new()
    {
        Id = run.Id,
        Name = run.Name,
        Description = run.Description,
        Status = run.Status,
        LlmModel = run.LlmModel,
        TopK = run.TopK,
        UseReranker = run.UseReranker,
        EmbeddingModelName = run.EmbeddingModel?.Name,
        ChunkingConfigName = run.ChunkingConfig?.Name,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        CreatedAt = run.CreatedAt,
        Summary = run.BenchmarkSummary is null ? null : ToSummaryDto(run.BenchmarkSummary),
        Results = run.BenchmarkResults
            .OrderBy(result => result.Id)
            .Select(result => new BenchmarkResultDto
            {
                Id = result.Id,
                QuestionId = result.QuestionId,
                Question = result.Question.Question,
                GroundTruth = result.Question.GroundTruth,
                SubjectCode = result.Question.Subject?.Code,
                GeneratedAnswer = result.GeneratedAnswer,
                RetrievedChunkIds = result.RetrievedChunkIds,
                LatencyMs = result.LatencyMs,
                Faithfulness = result.Faithfulness,
                AnswerRelevancy = result.AnswerRelevancy,
                ContextPrecision = result.ContextPrecision,
                ContextRecall = result.ContextRecall,
                AnswerCorrectness = result.AnswerCorrectness,
                ErrorMsg = result.ErrorMsg
            })
            .ToList()
    };

    private static BenchmarkSummaryDto ToSummaryDto(BenchmarkSummary summary) => new()
    {
        TotalQuestions = summary.TotalQuestions ?? 0,
        AvgFaithfulness = summary.AvgFaithfulness,
        AvgAnswerRelevancy = summary.AvgAnswerRelevancy,
        AvgContextPrecision = summary.AvgContextPrecision,
        AvgContextRecall = summary.AvgContextRecall,
        AvgAnswerCorrectness = summary.AvgAnswerCorrectness,
        AvgLatencyMs = summary.AvgLatencyMs,
        P95LatencyMs = summary.P95LatencyMs,
        ComputedAt = summary.ComputedAt
    };
}
