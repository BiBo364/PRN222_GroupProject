using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ILearningService
{
    Task<LearningDashboardDto?> GetDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default);
    Task<LearningGenerationOptionsDto?> GetGenerationOptionsAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default);
    Task<GenerateQuestionBankResult> GenerateQuestionsAsync(
        int lecturerUserId,
        GenerateQuestionBankRequest request,
        CancellationToken cancellationToken = default);
    Task<QuestionBankPageDto?> GetQuestionBankAsync(
        int lecturerUserId,
        string? search,
        string? difficulty,
        string? questionType,
        bool includeInactive,
        CancellationToken cancellationToken = default);
    Task UpdateQuestionAsync(
        int lecturerUserId,
        UpdateQuestionBankItemRequest request,
        CancellationToken cancellationToken = default);
    Task SetQuestionActiveAsync(
        int lecturerUserId,
        int questionId,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task<ComposeLearningSetOptionsDto?> GetComposeOptionsAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default);
    Task<LearningSetDetailDto> ComposeLearningSetAsync(
        int lecturerUserId,
        ComposeLearningSetRequest request,
        CancellationToken cancellationToken = default);
    Task<LearningSetDetailDto?> GetLearningSetAsync(
        int userId,
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task<ManualQuizEditorDto?> GetManualQuizEditorAsync(
        int lecturerUserId,
        int? learningSetId,
        CancellationToken cancellationToken = default);
    Task<SaveManualQuizResult> SaveManualQuizAsync(
        int lecturerUserId,
        SaveManualQuizRequest request,
        bool isAutosave,
        CancellationToken cancellationToken = default);
    Task SetPublishedAsync(
        int lecturerUserId,
        int learningSetId,
        bool isPublished,
        CancellationToken cancellationToken = default);
    Task DeleteLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task<LearningRecycleBinDto?> GetRecycleBinAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default);
    Task RestoreLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task PermanentlyDeleteLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task<LearningAttemptResultDto> SubmitQuizAsync(
        int userId,
        SubmitLearningAttemptRequest request,
        CancellationToken cancellationToken = default);
    Task<QuizAnalyticsDashboardDto?> GetQuizAnalyticsAsync(
        int lecturerUserId,
        int? learningSetId,
        int days,
        CancellationToken cancellationToken = default);
    Task<QuizVersionHistoryDto?> GetQuizVersionHistoryAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task RestoreQuizVersionAsync(
        int lecturerUserId,
        int learningSetId,
        int versionId,
        CancellationToken cancellationToken = default);
}
