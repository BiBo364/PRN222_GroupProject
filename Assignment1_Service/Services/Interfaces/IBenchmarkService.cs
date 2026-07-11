using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IBenchmarkService
{
    Task<List<TestQuestionDto>> GetTestQuestionsAsync(int? subjectId = null);
    Task<TestQuestionDto?> GetTestQuestionAsync(int id);
    Task<TestQuestionDto> CreateTestQuestionAsync(CreateTestQuestionDto dto, string? createdBy);
    Task UpdateTestQuestionAsync(int id, CreateTestQuestionDto dto);
    Task DeleteTestQuestionAsync(int id);

    Task<List<BenchmarkRunListItemDto>> GetBenchmarkRunsAsync();
    Task<BenchmarkRunDetailDto?> GetBenchmarkRunAsync(int runId);
    Task<int> RunBenchmarkAsync(CreateBenchmarkRunDto dto, CancellationToken cancellationToken = default);

    Task<List<BenchmarkConfigOptionDto>> GetEmbeddingModelOptionsAsync();
    Task<List<BenchmarkConfigOptionDto>> GetChunkingConfigOptionsAsync();
    Task<List<SubjectDto>> GetSubjectsWithTestQuestionsAsync();
}
