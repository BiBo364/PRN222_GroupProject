using Assignment1_Service.Models;

namespace Assignmet1_Presentation.Models;

public class BenchmarkIndexViewModel
{
    public List<BenchmarkRunListItemViewModel> Runs { get; set; } = new();
    public int TotalRuns { get; set; }
    public int CompletedRuns { get; set; }
    public int TotalQuestions { get; set; }
    public double? LatestAvgFaithfulness { get; set; }
    public double? LatestAvgAnswerCorrectness { get; set; }
    public double? LatestAvgLatencyMs { get; set; }
}

public class BenchmarkRunListItemViewModel
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
    public BenchmarkSummaryViewModel? Summary { get; set; }
}

public class BenchmarkRunDetailViewModel
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
    public BenchmarkSummaryViewModel? Summary { get; set; }
    public List<BenchmarkResultViewModel> Results { get; set; } = new();
}

public class BenchmarkResultViewModel
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
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMsg);
}

public class BenchmarkSummaryViewModel
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

public class BenchmarkQuestionsViewModel
{
    public List<TestQuestionViewModel> Questions { get; set; } = new();
    public List<SubjectListItemViewModel> Subjects { get; set; } = new();
    public int? FilterSubjectId { get; set; }
    public TestQuestionFormViewModel Form { get; set; } = new();
}

public class TestQuestionViewModel
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? GroundTruthChunks { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class TestQuestionFormViewModel
{
    public int? Id { get; set; }
    public int? SubjectId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? GroundTruthChunks { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
}

public class BenchmarkRunFormViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SubjectId { get; set; }
    public int? EmbeddingModelId { get; set; }
    public int? ChunkingConfigId { get; set; }
    public int TopK { get; set; } = 4;
    public bool UseReranker { get; set; } = true;
    public List<SubjectListItemViewModel> Subjects { get; set; } = new();
    public List<BenchmarkConfigOptionViewModel> EmbeddingModels { get; set; } = new();
    public List<BenchmarkConfigOptionViewModel> ChunkingConfigs { get; set; } = new();
    public int QuestionCount { get; set; }
}

public class BenchmarkConfigOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
