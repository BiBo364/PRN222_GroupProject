using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface ILearningRepository
{
    Task<Subject?> GetSubjectAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetIndexedDocumentsAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<List<Chunk>> GetSourceChunksAsync(
        int subjectId,
        IReadOnlyCollection<int> documentIds,
        int? chapterId,
        int maximumChunks,
        CancellationToken cancellationToken = default);
    Task<List<QuestionBankItem>> GetQuestionBankAsync(
        int subjectId,
        string? search,
        string? difficulty,
        string? questionType,
        bool activeOnly,
        CancellationToken cancellationToken = default);
    Task<List<QuestionBankItem>> GetQuestionBankByIdsAsync(
        int subjectId,
        IReadOnlyCollection<int> questionIds,
        CancellationToken cancellationToken = default);
    Task<QuestionBankItem?> GetQuestionAsync(
        int questionId,
        bool tracking,
        CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetExistingPromptsAsync(
        int subjectId,
        CancellationToken cancellationToken = default);
    Task AddQuestionsAsync(
        IReadOnlyCollection<QuestionBankItem> questions,
        CancellationToken cancellationToken = default);
    Task<List<LearningSet>> GetLearningSetsAsync(
        int subjectId,
        bool includeUnpublished,
        CancellationToken cancellationToken = default);
    Task<List<LearningSet>> GetPublishedLearningSetsAsync(
        CancellationToken cancellationToken = default);
    Task<LearningSet?> GetLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default);
    Task<List<LearningSet>> GetDeletedLearningSetsAsync(
        int subjectId,
        CancellationToken cancellationToken = default);
    Task<LearningSet?> GetDeletedLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default);
    Task AddLearningSetAsync(LearningSet learningSet, CancellationToken cancellationToken = default);
    void RemoveLearningSetItems(IReadOnlyCollection<LearningSetItem> items);
    Task DeleteLearningSetAsync(LearningSet learningSet, CancellationToken cancellationToken = default);
    Task AddAttemptAsync(LearningAttempt attempt, CancellationToken cancellationToken = default);
    Task<List<LearningAttempt>> GetAttemptsAsync(
        int userId,
        int? subjectId,
        CancellationToken cancellationToken = default);
    Task<List<LearningAttempt>> GetSubjectAttemptsAsync(
        int subjectId,
        int? learningSetId,
        DateTime? completedFromUtc,
        CancellationToken cancellationToken = default);
    Task<List<LearningSetVersion>> GetLearningSetVersionsAsync(
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task<LearningSetVersion?> GetLearningSetVersionAsync(
        int learningSetId,
        int versionId,
        CancellationToken cancellationToken = default);
    Task<LearningSetVersion?> GetLatestLearningSetVersionAsync(
        int learningSetId,
        CancellationToken cancellationToken = default);
    Task AddLearningSetVersionAsync(
        LearningSetVersion version,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
