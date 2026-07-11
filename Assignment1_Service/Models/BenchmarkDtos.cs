namespace Assignment1_Service.Models;

public class TestQuestionDto
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public int? ChapterId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? GroundTruthChunks { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateTestQuestionDto
{
    public int? SubjectId { get; set; }
    public int? ChapterId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? GroundTruthChunks { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
}

public class BenchmarkRunListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? LlmModel { get; set; }
    public int? TopK { get; set; }
    public bool? UseReranker { get; set; }
    public string? EmbeddingModelName { get; set; }
    public string? ChunkingConfigName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public BenchmarkSummaryDto? Summary { get; set; }
}

public class BenchmarkRunDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? LlmModel { get; set; }
    public int? TopK { get; set; }
    public bool? UseReranker { get; set; }
    public string? EmbeddingModelName { get; set; }
    public string? ChunkingConfigName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public BenchmarkSummaryDto? Summary { get; set; }
    public List<BenchmarkResultDto> Results { get; set; } = new();
}

public class BenchmarkResultDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? SubjectCode { get; set; }
    public string? GeneratedAnswer { get; set; }
    public string? RetrievedChunkIds { get; set; }
    public int? LatencyMs { get; set; }
    public double? Faithfulness { get; set; }
    public double? AnswerRelevancy { get; set; }
    public double? ContextPrecision { get; set; }
    public double? ContextRecall { get; set; }
    public double? AnswerCorrectness { get; set; }
    public string? ErrorMsg { get; set; }
}

public class BenchmarkSummaryDto
{
    public int TotalQuestions { get; set; }
    public double? AvgFaithfulness { get; set; }
    public double? AvgAnswerRelevancy { get; set; }
    public double? AvgContextPrecision { get; set; }
    public double? AvgContextRecall { get; set; }
    public double? AvgAnswerCorrectness { get; set; }
    public double? AvgLatencyMs { get; set; }
    public double? P95LatencyMs { get; set; }
    public DateTime? ComputedAt { get; set; }
}

public class CreateBenchmarkRunDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SubjectId { get; set; }
    public int? EmbeddingModelId { get; set; }
    public int? ChunkingConfigId { get; set; }
    public int TopK { get; set; } = 4;
    public bool UseReranker { get; set; } = true;
}

public class BenchmarkConfigOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RagMetricsResult
{
    public double Faithfulness { get; set; }
    public double AnswerRelevancy { get; set; }
    public double ContextPrecision { get; set; }
    public double ContextRecall { get; set; }
    public double AnswerCorrectness { get; set; }
}
