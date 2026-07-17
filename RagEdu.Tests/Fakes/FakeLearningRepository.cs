namespace RagEdu.Tests.Fakes;

internal sealed class FakeLearningRepository : ILearningRepository
{
    public List<Subject> Subjects { get; } = [];
    public List<Document> Documents { get; } = [];
    public List<Chunk> Chunks { get; } = [];
    public List<QuestionBankItem> Questions { get; } = [];
    public List<LearningSet> LearningSets { get; } = [];
    public List<LearningAttempt> Attempts { get; } = [];
    public List<LearningSetVersion> Versions { get; } = [];
    public List<LearningSetItem> RemovedItems { get; } = [];

    public int SaveChangesCallCount { get; private set; }
    public int AddQuestionsCallCount { get; private set; }
    public int AddLearningSetCallCount { get; private set; }
    public int AddAttemptCallCount { get; private set; }
    public int AddVersionCallCount { get; private set; }
    public int PermanentDeleteCallCount { get; private set; }
    public int? LastSubjectId { get; private set; }
    public bool? LastIncludeUnpublished { get; private set; }
    public bool? LastActiveOnly { get; private set; }
    public string? LastSearch { get; private set; }
    public string? LastDifficulty { get; private set; }
    public string? LastQuestionType { get; private set; }
    public int? LastAttemptUserId { get; private set; }
    public int? LastAttemptSubjectId { get; private set; }
    public int? LastAnalyticsLearningSetId { get; private set; }
    public DateTime? LastAnalyticsFromUtc { get; private set; }

    public Task<Subject?> GetSubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        return Task.FromResult(Subjects.FirstOrDefault(subject =>
            subject.Id == subjectId
            && subject.IsDeleted != true));
    }

    public Task<List<Document>> GetIndexedDocumentsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        var result = Documents
            .Where(document =>
                document.SubjectId == subjectId
                && document.IsDeleted != true
                && string.Equals(document.Status, "indexed", StringComparison.OrdinalIgnoreCase))
            .OrderBy(document => document.OriginalName)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<Chunk>> GetSourceChunksAsync(
        int subjectId,
        IReadOnlyCollection<int> documentIds,
        int? chapterId,
        int maximumChunks,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        var selectedIds = documentIds.ToHashSet();
        var result = Chunks
            .Where(chunk =>
                chunk.Document.SubjectId == subjectId
                && (selectedIds.Count == 0 || selectedIds.Contains(chunk.DocumentId))
                && (!chapterId.HasValue || chunk.Document.ChapterId == chapterId))
            .OrderBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.ChunkIndex)
            .Take(maximumChunks)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<QuestionBankItem>> GetQuestionBankAsync(
        int subjectId,
        string? search,
        string? difficulty,
        string? questionType,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        LastSearch = search;
        LastDifficulty = difficulty;
        LastQuestionType = questionType;
        LastActiveOnly = activeOnly;

        IEnumerable<QuestionBankItem> query = Questions.Where(question => question.SubjectId == subjectId);
        if (activeOnly)
            query = query.Where(question => question.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(question =>
                question.Prompt.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (question.Topic?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            query = query.Where(question =>
                string.Equals(question.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(questionType))
        {
            query = query.Where(question =>
                string.Equals(question.QuestionType, questionType, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query.OrderBy(question => question.Id).ToList());
    }

    public Task<List<QuestionBankItem>> GetQuestionBankByIdsAsync(
        int subjectId,
        IReadOnlyCollection<int> questionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        var ids = questionIds.ToHashSet();
        return Task.FromResult(Questions
            .Where(question => question.SubjectId == subjectId && ids.Contains(question.Id))
            .OrderBy(question => question.Id)
            .ToList());
    }

    public Task<QuestionBankItem?> GetQuestionAsync(
        int questionId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Questions.FirstOrDefault(question => question.Id == questionId));
    }

    public Task<HashSet<string>> GetExistingPromptsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Questions
            .Where(question => question.SubjectId == subjectId)
            .Select(question => LearningTextNormalizer.NormalizeForComparison(question.Prompt))
            .ToHashSet(StringComparer.Ordinal));
    }

    public Task AddQuestionsAsync(
        IReadOnlyCollection<QuestionBankItem> questions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddQuestionsCallCount++;
        foreach (var question in questions)
            AttachQuestion(question);
        return Task.CompletedTask;
    }

    public Task<List<LearningSet>> GetLearningSetsAsync(
        int subjectId,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        LastIncludeUnpublished = includeUnpublished;
        var result = LearningSets
            .Where(set =>
                set.SubjectId == subjectId
                && !set.IsDeleted
                && (includeUnpublished || set.IsPublished))
            .OrderByDescending(set => set.UpdatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<LearningSet>> GetPublishedLearningSetsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LearningSets
            .Where(set => set.IsPublished && !set.IsDeleted)
            .OrderBy(set => set.Subject.Code)
            .ThenBy(set => set.Title)
            .ToList());
    }

    public Task<LearningSet?> GetLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LearningSets.FirstOrDefault(set =>
            set.Id == learningSetId
            && !set.IsDeleted));
    }

    public Task<List<LearningSet>> GetDeletedLearningSetsAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        return Task.FromResult(LearningSets
            .Where(set => set.SubjectId == subjectId && set.IsDeleted)
            .OrderByDescending(set => set.DeletedAt)
            .ToList());
    }

    public Task<LearningSet?> GetDeletedLearningSetAsync(
        int learningSetId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LearningSets.FirstOrDefault(set =>
            set.Id == learningSetId
            && set.IsDeleted));
    }

    public Task AddLearningSetAsync(
        LearningSet learningSet,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddLearningSetCallCount++;
        AttachLearningSet(learningSet);
        return Task.CompletedTask;
    }

    public void RemoveLearningSetItems(IReadOnlyCollection<LearningSetItem> items)
    {
        RemovedItems.AddRange(items);
    }

    public Task DeleteLearningSetAsync(
        LearningSet learningSet,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PermanentDeleteCallCount++;
        LearningSets.Remove(learningSet);
        Versions.RemoveAll(version => version.LearningSetId == learningSet.Id);
        Attempts.RemoveAll(attempt => attempt.LearningSetId == learningSet.Id);
        return Task.CompletedTask;
    }

    public Task AddAttemptAsync(
        LearningAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddAttemptCallCount++;
        if (attempt.Id == 0)
            attempt.Id = Attempts.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
        attempt.LearningSet = LearningSets.First(set => set.Id == attempt.LearningSetId);
        var nextAnswerId = Attempts
            .SelectMany(item => item.Answers)
            .Select(answer => answer.Id)
            .DefaultIfEmpty()
            .Max() + 1;
        foreach (var answer in attempt.Answers)
        {
            if (answer.Id == 0)
                answer.Id = nextAnswerId++;
            answer.LearningAttemptId = attempt.Id;
            answer.LearningAttempt = attempt;
            answer.QuestionBankItem = Questions.First(question =>
                question.Id == answer.QuestionBankItemId);
        }
        Attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<List<LearningAttempt>> GetAttemptsAsync(
        int userId,
        int? subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastAttemptUserId = userId;
        LastAttemptSubjectId = subjectId;
        return Task.FromResult(Attempts
            .Where(attempt =>
                attempt.UserId == userId
                && (!subjectId.HasValue || attempt.LearningSet.SubjectId == subjectId))
            .OrderByDescending(attempt => attempt.CompletedAt)
            .ToList());
    }

    public Task<List<LearningAttempt>> GetSubjectAttemptsAsync(
        int subjectId,
        int? learningSetId,
        DateTime? completedFromUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSubjectId = subjectId;
        LastAnalyticsLearningSetId = learningSetId;
        LastAnalyticsFromUtc = completedFromUtc;
        return Task.FromResult(Attempts
            .Where(attempt =>
                attempt.LearningSet.SubjectId == subjectId
                && (!learningSetId.HasValue || attempt.LearningSetId == learningSetId)
                && (!completedFromUtc.HasValue || attempt.CompletedAt >= completedFromUtc))
            .OrderBy(attempt => attempt.CompletedAt)
            .ToList());
    }

    public Task<List<LearningSetVersion>> GetLearningSetVersionsAsync(
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Versions
            .Where(version => version.LearningSetId == learningSetId)
            .OrderByDescending(version => version.VersionNumber)
            .ToList());
    }

    public Task<LearningSetVersion?> GetLearningSetVersionAsync(
        int learningSetId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Versions.FirstOrDefault(version =>
            version.Id == versionId
            && version.LearningSetId == learningSetId));
    }

    public Task<LearningSetVersion?> GetLatestLearningSetVersionAsync(
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Versions
            .Where(version => version.LearningSetId == learningSetId)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault());
    }

    public Task AddLearningSetVersionAsync(
        LearningSetVersion version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddVersionCallCount++;
        if (version.Id == 0)
            version.Id = Versions.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
        version.LearningSet = LearningSets.First(set => set.Id == version.LearningSetId);
        version.CreatedByUser ??= version.LearningSet.CreatedByUser;
        Versions.Add(version);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveChangesCallCount++;
        foreach (var set in LearningSets)
            RepairLearningSet(set);
        return Task.CompletedTask;
    }

    private void AttachQuestion(QuestionBankItem question)
    {
        if (question.Id == 0)
            question.Id = Questions.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
        question.Subject = Subjects.First(subject => subject.Id == question.SubjectId);
        if (question.ChapterId.HasValue)
        {
            question.Chapter = question.Subject.Chapters.FirstOrDefault(chapter =>
                chapter.Id == question.ChapterId.Value);
        }
        if (!Questions.Contains(question))
            Questions.Add(question);
    }

    private void AttachLearningSet(LearningSet set)
    {
        if (set.Id == 0)
            set.Id = LearningSets.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
        set.Subject = Subjects.First(subject => subject.Id == set.SubjectId);
        RepairLearningSet(set);
        if (!LearningSets.Contains(set))
            LearningSets.Add(set);
    }

    private void RepairLearningSet(LearningSet set)
    {
        set.Subject ??= Subjects.First(subject => subject.Id == set.SubjectId);
        var nextItemId = LearningSets
            .SelectMany(item => item.Items)
            .Select(item => item.Id)
            .DefaultIfEmpty()
            .Max() + 1;
        foreach (var item in set.Items)
        {
            item.LearningSet = set;
            item.LearningSetId = set.Id;
            if (item.Id == 0)
                item.Id = nextItemId++;

            if (item.QuestionBankItem is not null)
            {
                AttachQuestion(item.QuestionBankItem);
                item.QuestionBankItemId = item.QuestionBankItem.Id;
            }
            else
            {
                item.QuestionBankItem = Questions.First(question =>
                    question.Id == item.QuestionBankItemId);
            }

            if (!item.QuestionBankItem.LearningSetItems.Contains(item))
                item.QuestionBankItem.LearningSetItems.Add(item);
        }
    }

}
