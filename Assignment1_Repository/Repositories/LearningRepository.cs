using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class LearningRepository : ILearningRepository
{
    private readonly RagEduContext _context;

    public LearningRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<Subject?> GetSubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return _context.Subjects
            .AsNoTracking()
            .Include(subject => subject.Chapters)
            .FirstOrDefaultAsync(
                subject => subject.Id == subjectId && subject.IsDeleted != true,
                cancellationToken);
    }

    public Task<List<Document>> GetIndexedDocumentsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Documents
            .AsNoTracking()
            .Include(document => document.Chapter)
            .Where(document =>
                document.SubjectId == subjectId
                && document.IsDeleted != true
                && document.Status == "indexed"
                && document.Chunks.Any())
            .OrderBy(document => document.ChapterId)
            .ThenBy(document => document.OriginalName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Chunk>> GetSourceChunksAsync(
        int subjectId,
        IReadOnlyCollection<int> documentIds,
        int? chapterId,
        int maximumChunks,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Chunks
            .AsNoTracking()
            .Include(chunk => chunk.Document)
                .ThenInclude(document => document.Chapter)
            .Where(chunk =>
                chunk.Document.SubjectId == subjectId
                && chunk.Document.IsDeleted != true
                && chunk.Document.Status == "indexed");

        if (documentIds.Count > 0)
            query = query.Where(chunk => documentIds.Contains(chunk.DocumentId));

        if (chapterId.HasValue)
            query = query.Where(chunk => chunk.Document.ChapterId == chapterId.Value);

        return query
            .OrderBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.ChunkIndex)
            .Take(Math.Max(1, maximumChunks))
            .ToListAsync(cancellationToken);
    }

    public Task<List<QuestionBankItem>> GetQuestionBankAsync(
        int subjectId,
        string? search,
        string? difficulty,
        string? questionType,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _context.QuestionBankItems
            .AsNoTracking()
            .Include(question => question.Chapter)
            .Where(question => question.SubjectId == subjectId);

        if (activeOnly)
            query = query.Where(question => question.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(question =>
                question.Prompt.Contains(keyword)
                || (question.Topic != null && question.Topic.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(question => question.Difficulty == difficulty);

        if (!string.IsNullOrWhiteSpace(questionType))
            query = query.Where(question => question.QuestionType == questionType);

        return query
            .OrderByDescending(question => question.UpdatedAt)
            .ThenByDescending(question => question.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<QuestionBankItem>> GetQuestionBankByIdsAsync(
        int subjectId,
        IReadOnlyCollection<int> questionIds,
        CancellationToken cancellationToken = default)
    {
        return _context.QuestionBankItems
            .AsNoTracking()
            .Include(question => question.Chapter)
            .Where(question =>
                question.SubjectId == subjectId
                && question.IsActive
                && questionIds.Contains(question.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<QuestionBankItem?> GetQuestionAsync(
        int questionId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<QuestionBankItem> query = _context.QuestionBankItems
            .Include(question => question.Chapter);

        if (!tracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(question => question.Id == questionId, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingPromptsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        var prompts = await _context.QuestionBankItems
            .AsNoTracking()
            .Where(question => question.SubjectId == subjectId && question.IsActive)
            .Select(question => question.Prompt)
            .ToListAsync(cancellationToken);

        return prompts
            .Select(NormalizePrompt)
            .Where(prompt => prompt.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task AddQuestionsAsync(
        IReadOnlyCollection<QuestionBankItem> questions,
        CancellationToken cancellationToken = default)
    {
        await _context.QuestionBankItems.AddRangeAsync(questions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<LearningSet>> GetLearningSetsAsync(
        int subjectId,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LearningSets
            .AsNoTracking()
            .Include(set => set.Subject)
            .Include(set => set.Items)
            .Where(set =>
                set.SubjectId == subjectId
                && !set.IsDeleted);

        if (!includeUnpublished)
            query = query.Where(set => set.IsPublished);

        return query
            .OrderByDescending(set => set.UpdatedAt)
            .ThenByDescending(set => set.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LearningSet>> GetPublishedLearningSetsAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.LearningSets
            .AsNoTracking()
            .Include(set => set.Subject)
            .Include(set => set.Items)
            .Where(set =>
                set.IsPublished
                && !set.IsDeleted
                && set.Subject.IsDeleted != true)
            .OrderBy(set => set.Subject.Code)
            .ThenByDescending(set => set.UpdatedAt)
            .ThenByDescending(set => set.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<LearningSet?> GetLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LearningSet> query = _context.LearningSets
            .Include(set => set.Subject)
            .Include(set => set.CreatedByUser)
            .Include(set => set.Items)
                .ThenInclude(item => item.QuestionBankItem)
                    .ThenInclude(question => question.Chapter);
        if (tracking)
        {
            query = query
                .Include(set => set.Items)
                    .ThenInclude(item => item.QuestionBankItem)
                        .ThenInclude(question => question.LearningSetItems)
                .Include(set => set.Items)
                    .ThenInclude(item => item.QuestionBankItem)
                        .ThenInclude(question => question.AttemptAnswers);
            query = query.AsSplitQuery();
        }
        else
        {
            query = query.AsNoTracking().AsSplitQuery();
        }

        return query.FirstOrDefaultAsync(
            set => set.Id == learningSetId && !set.IsDeleted,
            cancellationToken);
    }

    public Task<List<LearningSet>> GetDeletedLearningSetsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return _context.LearningSets
            .AsNoTracking()
            .Include(set => set.Subject)
            .Include(set => set.Items)
            .Where(set =>
                set.SubjectId == subjectId
                && set.IsDeleted)
            .OrderByDescending(set => set.DeletedAt)
            .ThenByDescending(set => set.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<LearningSet?> GetDeletedLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LearningSet> query = _context.LearningSets
            .Include(set => set.Subject)
            .Include(set => set.CreatedByUser)
            .Include(set => set.Items)
                .ThenInclude(item => item.QuestionBankItem)
                    .ThenInclude(question => question.Chapter);

        query = tracking
            ? query.AsSplitQuery()
            : query.AsNoTracking().AsSplitQuery();

        return query.FirstOrDefaultAsync(
            set => set.Id == learningSetId && set.IsDeleted,
            cancellationToken);
    }

    public async Task AddLearningSetAsync(
        LearningSet learningSet,
        CancellationToken cancellationToken = default)
    {
        await _context.LearningSets.AddAsync(learningSet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveLearningSetItems(IReadOnlyCollection<LearningSetItem> items)
    {
        _context.LearningSetItems.RemoveRange(items);
    }

    public async Task DeleteLearningSetAsync(
        LearningSet learningSet,
        CancellationToken cancellationToken = default)
    {
        var attempts = await _context.LearningAttempts
            .Include(attempt => attempt.Answers)
            .Where(attempt => attempt.LearningSetId == learningSet.Id)
            .ToListAsync(cancellationToken);

        _context.LearningAttemptAnswers.RemoveRange(attempts.SelectMany(attempt => attempt.Answers));
        _context.LearningAttempts.RemoveRange(attempts);
        _context.LearningSetItems.RemoveRange(learningSet.Items);
        _context.LearningSets.Remove(learningSet);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttemptAsync(
        LearningAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        await _context.LearningAttempts.AddAsync(attempt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<LearningAttempt>> GetAttemptsAsync(
        int userId,
        int? subjectId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LearningAttempts
            .AsNoTracking()
            .Include(attempt => attempt.LearningSet)
                .ThenInclude(set => set.Subject)
            .Where(attempt =>
                attempt.UserId == userId
                && !attempt.LearningSet.IsDeleted);

        if (subjectId.HasValue)
        {
            query = query.Where(attempt =>
                attempt.LearningSet.SubjectId == subjectId.Value);
        }

        return query
            .OrderByDescending(attempt => attempt.CompletedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LearningAttempt>> GetSubjectAttemptsAsync(
        int subjectId,
        int? learningSetId,
        DateTime? completedFromUtc,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LearningAttempts
            .AsNoTracking()
            .Include(attempt => attempt.User)
            .Include(attempt => attempt.LearningSet)
                .ThenInclude(set => set.Subject)
            .Include(attempt => attempt.Answers)
                .ThenInclude(answer => answer.QuestionBankItem)
            .Where(attempt =>
                attempt.LearningSet.SubjectId == subjectId
                && !attempt.LearningSet.IsDeleted);

        if (learningSetId.HasValue)
            query = query.Where(attempt => attempt.LearningSetId == learningSetId.Value);
        if (completedFromUtc.HasValue)
            query = query.Where(attempt => attempt.CompletedAt >= completedFromUtc.Value);

        return query
            .OrderByDescending(attempt => attempt.CompletedAt)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LearningSetVersion>> GetLearningSetVersionsAsync(
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        return _context.LearningSetVersions
            .AsNoTracking()
            .Include(version => version.CreatedByUser)
            .Where(version => version.LearningSetId == learningSetId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public Task<LearningSetVersion?> GetLearningSetVersionAsync(
        int learningSetId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        return _context.LearningSetVersions
            .AsNoTracking()
            .Include(version => version.CreatedByUser)
            .FirstOrDefaultAsync(
                version =>
                    version.Id == versionId
                    && version.LearningSetId == learningSetId,
                cancellationToken);
    }

    public Task<LearningSetVersion?> GetLatestLearningSetVersionAsync(
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        return _context.LearningSetVersions
            .AsNoTracking()
            .Where(version => version.LearningSetId == learningSetId)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddLearningSetVersionAsync(
        LearningSetVersion version,
        CancellationToken cancellationToken = default)
    {
        await _context.LearningSetVersions.AddAsync(version, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizePrompt(string prompt)
    {
        return string.Join(
            ' ',
            prompt.Trim().ToLowerInvariant().Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries));
    }
}
