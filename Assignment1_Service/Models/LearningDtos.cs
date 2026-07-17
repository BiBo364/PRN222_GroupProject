using Assignment1_Repository.Models;

namespace Assignment1_Service.Models;

public sealed class LearningGenerationOptionsDto
{
    public SubjectDto Subject { get; init; } = new();
    public IReadOnlyList<LearningDocumentOptionDto> Documents { get; init; } = [];
    public IReadOnlyList<ChapterDto> Chapters { get; init; } = [];
}

public sealed class LearningDocumentOptionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public int? ChapterId { get; init; }
    public string? ChapterName { get; init; }
}

public sealed class GenerateQuestionBankRequest
{
    public IReadOnlyCollection<int> DocumentIds { get; init; } = [];
    public int? ChapterId { get; init; }
    public int QuestionCount { get; init; } = 10;
    public IReadOnlyCollection<string> QuestionTypes { get; init; } = [];
    public string Difficulty { get; init; } = "mixed";
    public string? Focus { get; init; }
}

public sealed class GenerateQuestionBankResult
{
    public int RequestedCount { get; init; }
    public int CreatedCount { get; init; }
    public int SkippedCount { get; init; }
}

public sealed class QuestionBankPageDto
{
    public SubjectDto Subject { get; init; } = new();
    public IReadOnlyList<QuestionBankItemDto> Questions { get; init; } = [];
}

public sealed class QuestionBankItemDto
{
    public int Id { get; init; }
    public int SubjectId { get; init; }
    public int? ChapterId { get; init; }
    public string? ChapterName { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string? Topic { get; init; }
    public string? LearningObjective { get; init; }
    public IReadOnlyList<string> SourceReferences { get; init; } = [];
    public bool IsAiGenerated { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class UpdateQuestionBankItemRequest
{
    public int Id { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string? Explanation { get; init; }
    public string Difficulty { get; init; } = string.Empty;
    public string? Topic { get; init; }
    public string? LearningObjective { get; init; }
}

public sealed class ComposeLearningSetOptionsDto
{
    public SubjectDto Subject { get; init; } = new();
    public int AvailableQuestionCount { get; init; }
    public IReadOnlyDictionary<string, int> CountByDifficulty { get; init; } =
        new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> CountByQuestionType { get; init; } =
        new Dictionary<string, int>();
}

public sealed class ComposeLearningSetRequest
{
    public string ActivityType { get; init; } = string.Empty;
    public int QuestionCount { get; init; } = 10;
    public string Difficulty { get; init; } = "mixed";
    public string? Focus { get; init; }
    public bool PublishImmediately { get; init; }
}

public sealed class LearningDashboardDto
{
    public SubjectDto? Subject { get; init; }
    public bool CanManage { get; init; }
    public bool IsGlobalCatalog { get; init; }
    public int SubjectCount { get; init; }
    public int ActiveQuestionCount { get; init; }
    public IReadOnlyList<LearningSetSummaryDto> LearningSets { get; init; } = [];
    public IReadOnlyList<LearningAttemptSummaryDto> RecentAttempts { get; init; } = [];
}

public sealed class LearningSetSummaryDto
{
    public int Id { get; init; }
    public int SubjectId { get; init; }
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
    public int? DurationMinutes { get; init; }
    public bool IsPublished { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class LearningSetDetailDto
{
    public int Id { get; init; }
    public int SubjectId { get; init; }
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public int? DurationMinutes { get; init; }
    public bool IsPublished { get; init; }
    public bool ShuffleQuestions { get; init; }
    public bool ShuffleOptions { get; init; }
    public bool CanManage { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<LearningSetQuestionDto> Questions { get; init; } = [];
}

public sealed class LearningSetQuestionDto
{
    public int Id { get; init; }
    public int OrderIndex { get; init; }
    public decimal Points { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string? Topic { get; init; }
    public string? SourceReference { get; init; }
}

public sealed class SubmitLearningAttemptRequest
{
    public int LearningSetId { get; init; }
    public IReadOnlyDictionary<int, string?> Answers { get; init; } =
        new Dictionary<int, string?>();
    public DateTime StartedAt { get; init; }
}

public sealed class LearningAttemptResultDto
{
    public int AttemptId { get; init; }
    public decimal Score { get; init; }
    public decimal TotalPoints { get; init; }
    public int CorrectCount { get; init; }
    public int TotalQuestions { get; init; }
    public decimal Percentage { get; init; }
    public IReadOnlyList<LearningAnswerResultDto> Answers { get; init; } = [];
}

public sealed class LearningAnswerResultDto
{
    public int QuestionId { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? SelectedAnswer { get; init; }
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public sealed class LearningAttemptSummaryDto
{
    public int Id { get; init; }
    public int LearningSetId { get; init; }
    public string SubjectCode { get; init; } = string.Empty;
    public string LearningSetTitle { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public int CorrectCount { get; init; }
    public int TotalQuestions { get; init; }
    public DateTime CompletedAt { get; init; }
}

public sealed class ManualQuizEditorDto
{
    public int? Id { get; init; }
    public SubjectDto Subject { get; init; } = new();
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public int DurationMinutes { get; init; } = 15;
    public bool IsPublished { get; init; }
    public bool ShuffleQuestions { get; init; } = true;
    public bool ShuffleOptions { get; init; } = true;
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<ManualQuizQuestionDto> Questions { get; init; } = [];
}

public sealed class ManualQuizQuestionDto
{
    public int? Id { get; init; }
    public string ClientKey { get; init; } = Guid.NewGuid().ToString("N");
    public string QuestionType { get; init; } = LearningQuestionTypes.MultipleChoice;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string? Explanation { get; init; }
    public string Difficulty { get; init; } = LearningDifficultyLevels.Medium;
    public string? Topic { get; init; }
    public decimal Points { get; init; } = 1m;
}

public sealed class SaveManualQuizRequest
{
    public int? Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public int DurationMinutes { get; init; } = 15;
    public bool IsPublished { get; init; }
    public bool ShuffleQuestions { get; init; } = true;
    public bool ShuffleOptions { get; init; } = true;
    public IReadOnlyList<SaveManualQuizQuestionRequest> Questions { get; init; } = [];
}

public sealed class SaveManualQuizQuestionRequest
{
    public int? Id { get; init; }
    public string ClientKey { get; init; } = string.Empty;
    public string QuestionType { get; init; } = LearningQuestionTypes.MultipleChoice;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string? Explanation { get; init; }
    public string Difficulty { get; init; } = LearningDifficultyLevels.Medium;
    public string? Topic { get; init; }
    public decimal Points { get; init; } = 1m;
}

public sealed class SaveManualQuizResult
{
    public int Id { get; init; }
    public bool IsPublished { get; init; }
    public DateTime SavedAt { get; init; }
    public IReadOnlyDictionary<string, int> QuestionIds { get; init; } =
        new Dictionary<string, int>();
}

public sealed class LearningRecycleBinDto
{
    public SubjectDto Subject { get; init; } = new();
    public IReadOnlyList<DeletedLearningSetDto> Items { get; init; } = [];
}

public sealed class DeletedLearningSetDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int QuestionCount { get; init; }
    public bool WasPublished { get; init; }
    public DateTime? DeletedAt { get; init; }
}

public sealed class QuizAnalyticsDashboardDto
{
    public SubjectDto Subject { get; init; } = new();
    public int? SelectedLearningSetId { get; init; }
    public int SelectedDays { get; init; }
    public IReadOnlyList<QuizAnalyticsFilterOptionDto> LearningSets { get; init; } = [];
    public int TotalAttempts { get; init; }
    public int UniqueStudents { get; init; }
    public decimal AveragePercentage { get; init; }
    public decimal PassRate { get; init; }
    public decimal AverageDurationMinutes { get; init; }
    public IReadOnlyList<QuizTrendPointDto> Trend { get; init; } = [];
    public IReadOnlyList<QuizPerformanceDto> QuizPerformance { get; init; } = [];
    public IReadOnlyList<QuestionAnalyticsDto> Questions { get; init; } = [];
}

public sealed class QuizAnalyticsFilterOptionDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
}

public sealed class QuizTrendPointDto
{
    public DateTime Date { get; init; }
    public int AttemptCount { get; init; }
    public decimal AveragePercentage { get; init; }
}

public sealed class QuizPerformanceDto
{
    public int LearningSetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public int UniqueStudents { get; init; }
    public decimal AveragePercentage { get; init; }
    public decimal PassRate { get; init; }
    public DateTime? LastAttemptAt { get; init; }
}

public sealed class QuestionAnalyticsDto
{
    public int QuestionId { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string QuestionType { get; init; } = string.Empty;
    public string CorrectAnswer { get; init; } = string.Empty;
    public int AnswerCount { get; init; }
    public int CorrectCount { get; init; }
    public decimal CorrectRate { get; init; }
    public string DifficultyBand { get; init; } = string.Empty;
    public string DifficultyLabel { get; init; } = string.Empty;
    public IReadOnlyList<OptionSelectionAnalyticsDto> Options { get; init; } = [];
}

public sealed class OptionSelectionAnalyticsDto
{
    public string Option { get; init; } = string.Empty;
    public int SelectionCount { get; init; }
    public decimal SelectionRate { get; init; }
    public bool IsCorrect { get; init; }
}

public sealed class QuizVersionHistoryDto
{
    public int LearningSetId { get; init; }
    public string LearningSetTitle { get; init; } = string.Empty;
    public DateTime CurrentUpdatedAt { get; init; }
    public IReadOnlyList<QuizVersionDto> Versions { get; init; } = [];
}

public sealed class QuizVersionDto
{
    public int Id { get; init; }
    public int VersionNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int QuestionCount { get; init; }
    public bool IsPublished { get; init; }
    public int DurationMinutes { get; init; }
    public string ChangeSummary { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
