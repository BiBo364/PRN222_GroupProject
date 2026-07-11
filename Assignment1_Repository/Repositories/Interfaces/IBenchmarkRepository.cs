using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IBenchmarkRepository
{
    Task<List<TestQuestion>> GetTestQuestionsAsync(int? subjectId = null);
    Task<TestQuestion?> GetTestQuestionByIdAsync(int id);
    Task<TestQuestion> AddTestQuestionAsync(TestQuestion question);
    Task UpdateTestQuestionAsync(TestQuestion question);
    Task DeleteTestQuestionAsync(int id);

    Task<List<BenchmarkRun>> GetBenchmarkRunsAsync(int take = 50);
    Task<BenchmarkRun?> GetBenchmarkRunDetailAsync(int runId);
    Task<BenchmarkRun> CreateBenchmarkRunAsync(BenchmarkRun run);
    Task UpdateBenchmarkRunAsync(BenchmarkRun run);
    Task AddBenchmarkResultAsync(BenchmarkResult result);
    Task SaveBenchmarkSummaryAsync(BenchmarkSummary summary);

    Task<List<ChunkingConfig>> GetChunkingConfigsAsync();
    Task<List<EmbeddingModel>> GetEmbeddingModelsAsync();
    Task<List<Subject>> GetSubjectsWithTestQuestionsAsync();
}
