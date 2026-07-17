using System.Text;
using System.Text.Json;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public sealed class LearningService : ILearningService
{
    private const int LecturerRoleId = 2;
    private const int StudentRoleId = 3;
    private const int MaximumContextCharacters = 60_000;
    private const int MaximumSourceChunks = 180;
    private const int MaximumGenerationCount = 30;
    private const int MaximumSetQuestionCount = 50;
    private const int MaximumManualQuestionCount = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILearningRepository _learningRepository;
    private readonly IUserReposity _userRepository;
    private readonly IStudyContentAiService _studyContentAiService;
    private readonly IGeminiClient _geminiClient;

    public LearningService(
        ILearningRepository learningRepository,
        IUserReposity userRepository,
        IStudyContentAiService studyContentAiService,
        IGeminiClient geminiClient)
    {
        _learningRepository = learningRepository;
        _userRepository = userRepository;
        _studyContentAiService = studyContentAiService;
        _geminiClient = geminiClient;
    }

    public async Task<LearningDashboardDto?> GetDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.IsActive == false)
            return null;

        if (user.RoleId == StudentRoleId)
        {
            var publishedSets = await _learningRepository.GetPublishedLearningSetsAsync(
                cancellationToken);
            var allAttempts = await _learningRepository.GetAttemptsAsync(
                userId,
                subjectId: null,
                cancellationToken);

            return new LearningDashboardDto
            {
                IsGlobalCatalog = true,
                SubjectCount = publishedSets
                    .Select(set => set.SubjectId)
                    .Distinct()
                    .Count(),
                LearningSets = publishedSets.Select(MapSetSummary).ToList(),
                RecentAttempts = allAttempts.Select(MapAttemptSummary).ToList()
            };
        }

        if (user.SubjectId is not int subjectId
            || user.Subject is null
            || user.Subject.IsDeleted == true)
        {
            return null;
        }

        var canManage = user.RoleId == LecturerRoleId;
        var sets = await _learningRepository.GetLearningSetsAsync(
            subjectId,
            includeUnpublished: canManage,
            cancellationToken);
        var questions = canManage
            ? await _learningRepository.GetQuestionBankAsync(
                subjectId,
                null,
                null,
                null,
                activeOnly: true,
                cancellationToken)
            : [];
        var attempts = await _learningRepository.GetAttemptsAsync(userId, subjectId, cancellationToken);

        return new LearningDashboardDto
        {
            Subject = MapSubject(user.Subject),
            CanManage = canManage,
            SubjectCount = 1,
            ActiveQuestionCount = questions.Count,
            LearningSets = sets.Select(MapSetSummary).ToList(),
            RecentAttempts = attempts.Select(MapAttemptSummary).ToList()
        };
    }

    public async Task<LearningGenerationOptionsDto?> GetGenerationOptionsAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var (user, subject) = access.Value;
        var documents = await _learningRepository.GetIndexedDocumentsAsync(subject.Id, cancellationToken);

        return new LearningGenerationOptionsDto
        {
            Subject = MapSubject(subject),
            Documents = documents.Select(document => new LearningDocumentOptionDto
            {
                Id = document.Id,
                Name = document.OriginalName,
                FileType = document.FileType,
                ChapterId = document.ChapterId,
                ChapterName = document.Chapter is null
                    ? null
                    : $"Chương {document.Chapter.Number}: {document.Chapter.Title}"
            }).ToList(),
            Chapters = subject.Chapters
                .OrderBy(chapter => chapter.Number)
                .Select(chapter => new ChapterDto
                {
                    Id = chapter.Id,
                    Number = chapter.Number,
                    Title = chapter.Title
                })
                .ToList()
        };
    }

    public async Task<GenerateQuestionBankResult> GenerateQuestionsAsync(
        int lecturerUserId,
        GenerateQuestionBankRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var subject = access.Subject;
        ValidateGenerationRequest(request);

        var indexedDocuments = await _learningRepository.GetIndexedDocumentsAsync(
            subject.Id,
            cancellationToken);
        if (indexedDocuments.Count == 0)
        {
            throw new InvalidOperationException(
                "Môn học chưa có tài liệu được lập chỉ mục. Vui lòng tải lên và lập chỉ mục tài liệu trước.");
        }

        var selectedDocumentIds = request.DocumentIds
            .Distinct()
            .ToHashSet();
        if (selectedDocumentIds.Count > 0)
        {
            var availableDocumentIds = indexedDocuments.Select(document => document.Id).ToHashSet();
            if (!selectedDocumentIds.IsSubsetOf(availableDocumentIds))
                throw new InvalidOperationException("Một hoặc nhiều tài liệu đã chọn không thuộc môn học được phân công.");
        }

        if (request.ChapterId.HasValue
            && subject.Chapters.All(chapter => chapter.Id != request.ChapterId.Value))
        {
            throw new InvalidOperationException("Chương đã chọn không thuộc môn học được phân công.");
        }

        var sourceChunks = await _learningRepository.GetSourceChunksAsync(
            subject.Id,
            selectedDocumentIds,
            request.ChapterId,
            MaximumSourceChunks,
            cancellationToken);
        if (sourceChunks.Count == 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy nội dung đã lập chỉ mục trong phạm vi tài liệu hoặc chương đã chọn.");
        }

        var drafts = await _studyContentAiService.GenerateQuestionsAsync(
            new GenerateQuestionsAiRequest
            {
                SubjectCode = subject.Code,
                SubjectName = subject.Name,
                Context = BuildSourceContext(sourceChunks),
                QuestionCount = request.QuestionCount,
                QuestionTypes = request.QuestionTypes,
                Difficulty = request.Difficulty,
                Focus = request.Focus
            },
            cancellationToken);

        var existingPrompts = await _learningRepository.GetExistingPromptsAsync(
            subject.Id,
            cancellationToken);
        var acceptedQuestions = new List<QuestionBankItem>();
        var now = DateTime.UtcNow;

        foreach (var draft in drafts)
        {
            if (acceptedQuestions.Count >= request.QuestionCount)
                break;

            var normalizedPrompt = LearningTextNormalizer.NormalizeForComparison(draft.Prompt);
            if (normalizedPrompt.Length == 0 || !existingPrompts.Add(normalizedPrompt))
                continue;

            if (!TryNormalizeDraft(draft, request, out var normalizedDraft))
                continue;

            acceptedQuestions.Add(new QuestionBankItem
            {
                SubjectId = subject.Id,
                ChapterId = request.ChapterId,
                QuestionType = normalizedDraft.QuestionType,
                Prompt = normalizedDraft.Prompt,
                OptionsJson = JsonSerializer.Serialize(normalizedDraft.Options, JsonOptions),
                CorrectAnswer = normalizedDraft.CorrectAnswer,
                Explanation = normalizedDraft.Explanation,
                Difficulty = normalizedDraft.Difficulty,
                Topic = LimitLength(normalizedDraft.Topic, 200),
                LearningObjective = LimitLength(normalizedDraft.LearningObjective, 500),
                SourceReferencesJson = JsonSerializer.Serialize(
                    new[] { normalizedDraft.SourceReference },
                    JsonOptions),
                CreatedByUserId = lecturerUserId,
                IsAiGenerated = true,
                AiModel = _geminiClient.ModelName,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (acceptedQuestions.Count == 0)
        {
            throw new InvalidOperationException(
                "AI chưa tạo được câu hỏi đạt yêu cầu kiểm tra chất lượng. Vui lòng giảm số lượng hoặc điều chỉnh trọng tâm.");
        }

        await _learningRepository.AddQuestionsAsync(acceptedQuestions, cancellationToken);
        return new GenerateQuestionBankResult
        {
            RequestedCount = request.QuestionCount,
            CreatedCount = acceptedQuestions.Count,
            SkippedCount = Math.Max(0, drafts.Count - acceptedQuestions.Count)
        };
    }

    public async Task<QuestionBankPageDto?> GetQuestionBankAsync(
        int lecturerUserId,
        string? search,
        string? difficulty,
        string? questionType,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var (_, subject) = access.Value;
        var questions = await _learningRepository.GetQuestionBankAsync(
            subject.Id,
            search,
            NormalizeOptionalDifficulty(difficulty),
            NormalizeOptionalQuestionType(questionType),
            activeOnly: !includeInactive,
            cancellationToken);

        return new QuestionBankPageDto
        {
            Subject = MapSubject(subject),
            Questions = questions.Select(MapQuestion).ToList()
        };
    }

    public async Task UpdateQuestionAsync(
        int lecturerUserId,
        UpdateQuestionBankItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var question = await _learningRepository.GetQuestionAsync(
            request.Id,
            tracking: true,
            cancellationToken);

        if (question is null || question.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy câu hỏi trong môn học được phân công.");

        var draft = new GeneratedQuestionDraft
        {
            QuestionType = request.QuestionType,
            Prompt = request.Prompt,
            Options = request.Options.Where(option => !string.IsNullOrWhiteSpace(option)).ToList(),
            CorrectAnswer = request.CorrectAnswer,
            Explanation = request.Explanation ?? string.Empty,
            Difficulty = request.Difficulty,
            Topic = request.Topic ?? string.Empty,
            LearningObjective = request.LearningObjective ?? string.Empty,
            SourceReference = ReadJsonList(question.SourceReferencesJson).FirstOrDefault() ?? string.Empty
        };

        var validationRequest = new GenerateQuestionBankRequest
        {
            QuestionCount = 1,
            QuestionTypes = [request.QuestionType],
            Difficulty = request.Difficulty
        };
        if (!TryNormalizeDraft(draft, validationRequest, out var normalized))
            throw new InvalidOperationException("Nội dung câu hỏi chưa hợp lệ. Vui lòng kiểm tra loại câu hỏi, phương án và đáp án.");

        question.QuestionType = normalized.QuestionType;
        question.Prompt = normalized.Prompt;
        question.OptionsJson = JsonSerializer.Serialize(normalized.Options, JsonOptions);
        question.CorrectAnswer = normalized.CorrectAnswer;
        question.Explanation = normalized.Explanation;
        question.Difficulty = normalized.Difficulty;
        question.Topic = LimitLength(normalized.Topic, 200);
        question.LearningObjective = LimitLength(normalized.LearningObjective, 500);
        question.UpdatedAt = DateTime.UtcNow;
        await _learningRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetQuestionActiveAsync(
        int lecturerUserId,
        int questionId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var question = await _learningRepository.GetQuestionAsync(
            questionId,
            tracking: true,
            cancellationToken);

        if (question is null || question.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy câu hỏi trong môn học được phân công.");

        question.IsActive = isActive;
        question.UpdatedAt = DateTime.UtcNow;
        await _learningRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComposeLearningSetOptionsDto?> GetComposeOptionsAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var (_, subject) = access.Value;
        var questions = await _learningRepository.GetQuestionBankAsync(
            subject.Id,
            null,
            null,
            null,
            activeOnly: true,
            cancellationToken);

        return new ComposeLearningSetOptionsDto
        {
            Subject = MapSubject(subject),
            AvailableQuestionCount = questions.Count,
            CountByDifficulty = questions
                .GroupBy(question => question.Difficulty)
                .ToDictionary(group => group.Key, group => group.Count()),
            CountByQuestionType = questions
                .GroupBy(question => question.QuestionType)
                .ToDictionary(group => group.Key, group => group.Count())
        };
    }

    public async Task<LearningSetDetailDto> ComposeLearningSetAsync(
        int lecturerUserId,
        ComposeLearningSetRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        ValidateComposeRequest(request);

        var allQuestions = await _learningRepository.GetQuestionBankAsync(
            access.Subject.Id,
            null,
            request.Difficulty == LearningDifficultyLevels.Mixed ? null : request.Difficulty,
            null,
            activeOnly: true,
            cancellationToken);

        var compatibleQuestions = FilterCompatibleQuestions(allQuestions, request.ActivityType)
            .Take(120)
            .ToList();
        if (compatibleQuestions.Count < request.QuestionCount)
        {
            throw new InvalidOperationException(
                $"Ngân hàng chỉ có {compatibleQuestions.Count} câu phù hợp. Vui lòng tạo thêm câu hỏi hoặc giảm số lượng.");
        }

        var plan = await _studyContentAiService.ComposeLearningSetAsync(
            new ComposeLearningSetAiRequest
            {
                SubjectCode = access.Subject.Code,
                SubjectName = access.Subject.Name,
                ActivityType = request.ActivityType,
                QuestionCount = request.QuestionCount,
                Difficulty = request.Difficulty,
                Focus = request.Focus,
                Candidates = compatibleQuestions.Select(question => new LearningQuestionCandidate
                {
                    Id = question.Id,
                    QuestionType = question.QuestionType,
                    Prompt = question.Prompt,
                    Difficulty = question.Difficulty,
                    Topic = question.Topic
                }).ToList()
            },
            cancellationToken);

        var candidateById = compatibleQuestions.ToDictionary(question => question.Id);
        var selectedIds = plan.SelectedQuestionIds
            .Where(candidateById.ContainsKey)
            .Distinct()
            .Take(request.QuestionCount)
            .ToList();

        if (selectedIds.Count < request.QuestionCount)
        {
            selectedIds.AddRange(
                compatibleQuestions
                    .Where(question => !selectedIds.Contains(question.Id))
                    .OrderBy(_ => Random.Shared.Next())
                    .Select(question => question.Id)
                    .Take(request.QuestionCount - selectedIds.Count));
        }

        var now = DateTime.UtcNow;
        var learningSet = new LearningSet
        {
            SubjectId = access.Subject.Id,
            Title = LimitLength(
                string.IsNullOrWhiteSpace(plan.Title)
                    ? BuildFallbackTitle(request.ActivityType, access.Subject.Code)
                    : plan.Title,
                300) ?? BuildFallbackTitle(request.ActivityType, access.Subject.Code),
            Description = plan.Description,
            Instructions = plan.Instructions,
            ActivityType = request.ActivityType,
            DurationMinutes = Math.Clamp(plan.DurationMinutes ?? EstimateDuration(request), 3, 120),
            IsPublished = request.PublishImmediately,
            ShuffleQuestions = true,
            ShuffleOptions = request.ActivityType is LearningActivityTypes.Quiz
                or LearningActivityTypes.SpeedChallenge,
            CreatedByUserId = lecturerUserId,
            AiModel = _geminiClient.ModelName,
            CreatedAt = now,
            UpdatedAt = now,
            Items = selectedIds.Select((questionId, index) => new LearningSetItem
            {
                QuestionBankItemId = questionId,
                OrderIndex = index + 1,
                Points = 1m
            }).ToList()
        };

        await _learningRepository.AddLearningSetAsync(learningSet, cancellationToken);
        var created = await _learningRepository.GetLearningSetAsync(
            learningSet.Id,
            tracking: false,
            cancellationToken);
        await CreateLearningSetVersionAsync(
            lecturerUserId,
            learningSet.Id,
            "Tạo bộ ôn tập bằng AI.",
            cancellationToken);

        return MapLearningSet(
            created ?? throw new InvalidOperationException("Không thể tải bộ ôn tập vừa tạo."),
            canManage: true);
    }

    public async Task<LearningSetDetailDto?> GetLearningSetAsync(
        int userId,
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.IsActive == false)
            return null;

        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: false,
            cancellationToken);
        if (set is null)
            return null;

        var isAdmin = user.RoleId == 1;
        var canManage = CanManageLearningSet(user, set);
        var canStudy = CanStudyLearningSet(user, set);
        if (!isAdmin && !canManage && !canStudy)
            return null;

        return MapLearningSet(set, canManage || isAdmin);
    }

    public async Task<ManualQuizEditorDto?> GetManualQuizEditorAsync(
        int lecturerUserId,
        int? learningSetId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var (_, subject) = access.Value;
        if (!learningSetId.HasValue)
        {
            return new ManualQuizEditorDto
            {
                Subject = MapSubject(subject)
            };
        }

        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId.Value,
            tracking: false,
            cancellationToken);
        if (set is null
            || set.SubjectId != subject.Id
            || set.ActivityType != LearningActivityTypes.Quiz)
            return null;

        return new ManualQuizEditorDto
        {
            Id = set.Id,
            Subject = MapSubject(subject),
            Title = set.Title,
            Description = set.Description,
            Instructions = set.Instructions,
            DurationMinutes = set.DurationMinutes ?? 15,
            IsPublished = set.IsPublished,
            ShuffleQuestions = set.ShuffleQuestions,
            ShuffleOptions = set.ShuffleOptions,
            UpdatedAt = set.UpdatedAt,
            Questions = set.Items
                .OrderBy(item => item.OrderIndex)
                .Select(item => new ManualQuizQuestionDto
                {
                    Id = item.QuestionBankItemId,
                    ClientKey = $"question-{item.QuestionBankItemId}",
                    QuestionType = item.QuestionBankItem.QuestionType,
                    Prompt = item.QuestionBankItem.Prompt,
                    Options = ReadJsonList(item.QuestionBankItem.OptionsJson),
                    CorrectAnswer = item.QuestionBankItem.CorrectAnswer,
                    Explanation = item.QuestionBankItem.Explanation,
                    Difficulty = item.QuestionBankItem.Difficulty,
                    Topic = item.QuestionBankItem.Topic,
                    Points = item.Points
                })
                .ToList()
        };
    }

    public async Task<SaveManualQuizResult> SaveManualQuizAsync(
        int lecturerUserId,
        SaveManualQuizRequest request,
        bool isAutosave,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        ValidateManualQuizRequest(request);

        var now = DateTime.UtcNow;
        var isNew = !request.Id.HasValue;
        LearningSet set;

        if (isNew)
        {
            set = new LearningSet
            {
                SubjectId = access.Subject.Id,
                ActivityType = LearningActivityTypes.Quiz,
                CreatedByUserId = lecturerUserId,
                IsDeleted = false,
                CreatedAt = now
            };
        }
        else
        {
            set = await _learningRepository.GetLearningSetAsync(
                request.Id!.Value,
                tracking: true,
                cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy Quiz cần chỉnh sửa.");

            if (set.SubjectId != access.Subject.Id)
                throw new InvalidOperationException("Bạn chỉ có thể chỉnh sửa Quiz thuộc môn học được phân công.");

            if (set.ActivityType != LearningActivityTypes.Quiz)
                throw new InvalidOperationException("Chỉ hoạt động dạng Quiz mới có thể chỉnh sửa bằng trình soạn thủ công.");
        }

        var normalizedTitle = request.Title?.Trim() ?? string.Empty;
        var publishRequested = !isAutosave && request.IsPublished;
        if (publishRequested && string.IsNullOrWhiteSpace(normalizedTitle))
            throw new InvalidOperationException("Vui lòng nhập tiêu đề trước khi phát hành Quiz.");

        set.Title = LimitLength(normalizedTitle, 300) ?? "Quiz chưa đặt tên";
        set.Description = LimitLength(request.Description, 2_000);
        set.Instructions = LimitLength(request.Instructions, 2_000);
        set.ActivityType = LearningActivityTypes.Quiz;
        set.DurationMinutes = Math.Clamp(request.DurationMinutes, 1, 300);
        set.IsPublished = isAutosave
            ? !isNew
                && set.IsPublished
                && normalizedTitle.Length > 0
                && request.Questions.Count > 0
                && request.Questions.All(question => NormalizeManualQuestion(question).IsComplete)
            : request.IsPublished;
        set.ShuffleQuestions = request.ShuffleQuestions;
        set.ShuffleOptions = request.ShuffleOptions;
        set.UpdatedAt = now;

        var synchronizedQuestions = SynchronizeManualQuestions(
            set,
            lecturerUserId,
            request.Questions,
            requireCompleteQuestions: publishRequested,
            now);

        if (publishRequested && set.Items.Count == 0)
            throw new InvalidOperationException("Quiz phải có ít nhất một câu hỏi trước khi phát hành.");

        if (isNew)
            await _learningRepository.AddLearningSetAsync(set, cancellationToken);
        else
            await _learningRepository.SaveChangesAsync(cancellationToken);

        if (!isAutosave)
        {
            await CreateLearningSetVersionAsync(
                lecturerUserId,
                set.Id,
                isNew ? "Tạo Quiz thủ công." : "Lưu thay đổi Quiz.",
                cancellationToken);
        }

        return new SaveManualQuizResult
        {
            Id = set.Id,
            IsPublished = set.IsPublished,
            SavedAt = set.UpdatedAt,
            QuestionIds = synchronizedQuestions
                .Where(binding =>
                    !string.IsNullOrWhiteSpace(binding.ClientKey)
                    && binding.Item.QuestionBankItemId > 0)
                .GroupBy(binding => binding.ClientKey, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Item.QuestionBankItemId,
                    StringComparer.Ordinal)
        };
    }

    public async Task SetPublishedAsync(
        int lecturerUserId,
        int learningSetId,
        bool isPublished,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: true,
            cancellationToken);

        if (set is null || set.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy bộ ôn tập trong môn học được phân công.");

        if (isPublished
            && (string.IsNullOrWhiteSpace(set.Title)
                || set.Items.Count == 0
                || set.Items.Any(item => !item.QuestionBankItem.IsActive)))
        {
            throw new InvalidOperationException(
                "Quiz chưa hoàn chỉnh. Vui lòng bổ sung đầy đủ câu hỏi và đáp án trước khi phát hành.");
        }

        set.IsPublished = isPublished;
        set.UpdatedAt = DateTime.UtcNow;
        await _learningRepository.SaveChangesAsync(cancellationToken);
        await CreateLearningSetVersionAsync(
            lecturerUserId,
            set.Id,
            isPublished ? "Phát hành Quiz." : "Chuyển Quiz về bản nháp.",
            cancellationToken);
    }

    public async Task DeleteLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: true,
            cancellationToken);

        if (set is null || set.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy bộ ôn tập trong môn học được phân công.");

        set.IsDeleted = true;
        set.DeletedAt = DateTime.UtcNow;
        set.UpdatedAt = DateTime.UtcNow;
        await _learningRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<LearningRecycleBinDto?> GetRecycleBinAsync(
        int lecturerUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var (_, subject) = access.Value;
        var deletedSets = await _learningRepository.GetDeletedLearningSetsAsync(
            subject.Id,
            cancellationToken);

        return new LearningRecycleBinDto
        {
            Subject = MapSubject(subject),
            Items = deletedSets.Select(set => new DeletedLearningSetDto
            {
                Id = set.Id,
                Title = set.Title,
                Description = set.Description,
                QuestionCount = set.Items.Count,
                WasPublished = set.IsPublished,
                DeletedAt = set.DeletedAt
            }).ToList()
        };
    }

    public async Task RestoreLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var set = await _learningRepository.GetDeletedLearningSetAsync(
            learningSetId,
            tracking: true,
            cancellationToken);

        if (set is null || set.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy Quiz trong thùng rác của môn học.");

        set.IsDeleted = false;
        set.DeletedAt = null;
        set.IsPublished = false;
        set.UpdatedAt = DateTime.UtcNow;
        await _learningRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task PermanentlyDeleteLearningSetAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var set = await _learningRepository.GetDeletedLearningSetAsync(
            learningSetId,
            tracking: true,
            cancellationToken);

        if (set is null || set.SubjectId != access.Subject.Id)
            throw new InvalidOperationException("Không tìm thấy Quiz trong thùng rác của môn học.");

        await _learningRepository.DeleteLearningSetAsync(set, cancellationToken);
    }

    public async Task<LearningAttemptResultDto> SubmitQuizAsync(
        int userId,
        SubmitLearningAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
        var set = await _learningRepository.GetLearningSetAsync(
            request.LearningSetId,
            tracking: false,
            cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy bài ôn tập.");

        var canManage = CanManageLearningSet(user, set);
        var canStudy = CanStudyLearningSet(user, set);
        if (!canManage && !canStudy)
            throw new InvalidOperationException("Bạn không có quyền làm bài ôn tập này.");

        if (set.ActivityType != LearningActivityTypes.Quiz)
            throw new InvalidOperationException("Hoạt động này không sử dụng hình thức nộp bài Quiz.");

        var now = DateTime.UtcNow;
        var answerResults = new List<LearningAnswerResultDto>();
        var answerEntities = new List<LearningAttemptAnswer>();
        decimal earnedPoints = 0;

        foreach (var item in set.Items.OrderBy(item => item.OrderIndex))
        {
            request.Answers.TryGetValue(item.QuestionBankItemId, out var selectedAnswer);
            var isCorrect = AnswersMatch(selectedAnswer, item.QuestionBankItem.CorrectAnswer);
            var awardedPoints = isCorrect ? item.Points : 0m;
            earnedPoints += awardedPoints;

            answerResults.Add(new LearningAnswerResultDto
            {
                QuestionId = item.QuestionBankItemId,
                Prompt = item.QuestionBankItem.Prompt,
                SelectedAnswer = selectedAnswer,
                CorrectAnswer = item.QuestionBankItem.CorrectAnswer,
                Explanation = item.QuestionBankItem.Explanation ?? string.Empty,
                IsCorrect = isCorrect
            });
            answerEntities.Add(new LearningAttemptAnswer
            {
                QuestionBankItemId = item.QuestionBankItemId,
                SelectedAnswer = selectedAnswer,
                IsCorrect = isCorrect,
                AwardedPoints = awardedPoints,
                AnsweredAt = now
            });
        }

        var totalPoints = set.Items.Sum(item => item.Points);
        var attempt = new LearningAttempt
        {
            LearningSetId = set.Id,
            UserId = userId,
            StartedAt = request.StartedAt == default
                ? now
                : request.StartedAt.ToUniversalTime(),
            CompletedAt = now,
            Score = earnedPoints,
            TotalPoints = totalPoints,
            CorrectCount = answerResults.Count(answer => answer.IsCorrect),
            TotalQuestions = answerResults.Count,
            Answers = answerEntities
        };

        await _learningRepository.AddAttemptAsync(attempt, cancellationToken);
        return new LearningAttemptResultDto
        {
            AttemptId = attempt.Id,
            Score = earnedPoints,
            TotalPoints = totalPoints,
            CorrectCount = attempt.CorrectCount,
            TotalQuestions = attempt.TotalQuestions,
            Percentage = totalPoints <= 0 ? 0 : Math.Round(earnedPoints / totalPoints * 100m, 1),
            Answers = answerResults
        };
    }

    public async Task<QuizAnalyticsDashboardDto?> GetQuizAnalyticsAsync(
        int lecturerUserId,
        int? learningSetId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var normalizedDays = days is 7 or 30 or 90 ? days : 30;
        var sets = (await _learningRepository.GetLearningSetsAsync(
                access.Value.Subject.Id,
                includeUnpublished: true,
                cancellationToken))
            .Where(set => set.ActivityType == LearningActivityTypes.Quiz)
            .ToList();

        if (learningSetId.HasValue && sets.All(set => set.Id != learningSetId.Value))
            throw new InvalidOperationException("Không tìm thấy Quiz cần phân tích trong môn học được phân công.");

        var fromUtc = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));
        var attempts = await _learningRepository.GetSubjectAttemptsAsync(
            access.Value.Subject.Id,
            learningSetId,
            fromUtc,
            cancellationToken);
        var percentages = attempts.Select(AttemptPercentage).ToList();
        var durations = attempts
            .Select(attempt => Math.Max(0, (attempt.CompletedAt - attempt.StartedAt).TotalMinutes))
            .Where(minutes => minutes <= 24 * 60)
            .ToList();

        var trendByDate = attempts
            .GroupBy(attempt => attempt.CompletedAt.Date)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Count(),
                    Average = group.Average(AttemptPercentage)
                });
        var trend = Enumerable.Range(0, normalizedDays)
            .Select(offset => fromUtc.Date.AddDays(offset))
            .Select(date =>
            {
                trendByDate.TryGetValue(date, out var point);
                return new QuizTrendPointDto
                {
                    Date = date,
                    AttemptCount = point?.Count ?? 0,
                    AveragePercentage = point is null ? 0 : Math.Round(point.Average, 1)
                };
            })
            .ToList();

        var quizPerformance = attempts
            .GroupBy(attempt => new
            {
                attempt.LearningSetId,
                attempt.LearningSet.Title
            })
            .Select(group =>
            {
                var groupPercentages = group.Select(AttemptPercentage).ToList();
                return new QuizPerformanceDto
                {
                    LearningSetId = group.Key.LearningSetId,
                    Title = group.Key.Title,
                    AttemptCount = group.Count(),
                    UniqueStudents = group.Select(attempt => attempt.UserId).Distinct().Count(),
                    AveragePercentage = Math.Round(groupPercentages.Average(), 1),
                    PassRate = Percentage(
                        groupPercentages.Count(value => value >= 50m),
                        groupPercentages.Count),
                    LastAttemptAt = group.Max(attempt => attempt.CompletedAt)
                };
            })
            .OrderByDescending(item => item.AttemptCount)
            .ThenBy(item => item.Title)
            .ToList();

        var questionAnalytics = attempts
            .SelectMany(attempt => attempt.Answers)
            .GroupBy(answer => answer.QuestionBankItemId)
            .Select(BuildQuestionAnalytics)
            .OrderBy(question => question.CorrectRate)
            .ThenByDescending(question => question.AnswerCount)
            .ToList();

        return new QuizAnalyticsDashboardDto
        {
            Subject = MapSubject(access.Value.Subject),
            SelectedLearningSetId = learningSetId,
            SelectedDays = normalizedDays,
            LearningSets = sets.Select(set => new QuizAnalyticsFilterOptionDto
            {
                Id = set.Id,
                Title = set.Title,
                IsPublished = set.IsPublished
            }).ToList(),
            TotalAttempts = attempts.Count,
            UniqueStudents = attempts.Select(attempt => attempt.UserId).Distinct().Count(),
            AveragePercentage = percentages.Count == 0
                ? 0
                : Math.Round(percentages.Average(), 1),
            PassRate = Percentage(
                percentages.Count(value => value >= 50m),
                percentages.Count),
            AverageDurationMinutes = durations.Count == 0
                ? 0
                : Math.Round((decimal)durations.Average(), 1),
            Trend = trend,
            QuizPerformance = quizPerformance,
            Questions = questionAnalytics
        };
    }

    public async Task<QuizVersionHistoryDto?> GetQuizVersionHistoryAsync(
        int lecturerUserId,
        int learningSetId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetLecturerAccessAsync(lecturerUserId, cancellationToken);
        if (access is null)
            return null;

        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: false,
            cancellationToken);
        if (set is null
            || set.SubjectId != access.Value.Subject.Id
            || set.ActivityType != LearningActivityTypes.Quiz)
        {
            return null;
        }

        var versions = await _learningRepository.GetLearningSetVersionsAsync(
            learningSetId,
            cancellationToken);
        if (versions.Count == 0)
        {
            await CreateLearningSetVersionAsync(
                lecturerUserId,
                learningSetId,
                "Khởi tạo lịch sử phiên bản.",
                cancellationToken);
            versions = await _learningRepository.GetLearningSetVersionsAsync(
                learningSetId,
                cancellationToken);
        }

        return new QuizVersionHistoryDto
        {
            LearningSetId = set.Id,
            LearningSetTitle = set.Title,
            CurrentUpdatedAt = set.UpdatedAt,
            Versions = versions.Select(version =>
            {
                var snapshot = DeserializeSnapshot(version.SnapshotJson);
                return new QuizVersionDto
                {
                    Id = version.Id,
                    VersionNumber = version.VersionNumber,
                    Title = snapshot.Title,
                    Description = snapshot.Description,
                    QuestionCount = snapshot.Questions.Count,
                    IsPublished = snapshot.IsPublished,
                    DurationMinutes = snapshot.DurationMinutes,
                    ChangeSummary = version.ChangeSummary ?? "Lưu phiên bản Quiz.",
                    CreatedBy = string.IsNullOrWhiteSpace(version.CreatedByUser.FullName)
                        ? version.CreatedByUser.Username
                        : version.CreatedByUser.FullName,
                    CreatedAt = version.CreatedAt
                };
            }).ToList()
        };
    }

    public async Task RestoreQuizVersionAsync(
        int lecturerUserId,
        int learningSetId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var access = await RequireLecturerAccessAsync(lecturerUserId, cancellationToken);
        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: true,
            cancellationToken);
        if (set is null
            || set.SubjectId != access.Subject.Id
            || set.ActivityType != LearningActivityTypes.Quiz)
        {
            throw new InvalidOperationException("Không tìm thấy Quiz cần khôi phục phiên bản.");
        }

        var version = await _learningRepository.GetLearningSetVersionAsync(
            learningSetId,
            versionId,
            cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản Quiz đã chọn.");
        await CreateLearningSetVersionAsync(
            lecturerUserId,
            learningSetId,
            "Sao lưu tự động trước khi khôi phục.",
            cancellationToken);
        var snapshot = DeserializeSnapshot(version.SnapshotJson);
        var now = DateTime.UtcNow;

        set.Title = LimitLength(snapshot.Title, 300) ?? "Quiz chưa đặt tên";
        set.Description = LimitLength(snapshot.Description, 2_000);
        set.Instructions = LimitLength(snapshot.Instructions, 2_000);
        set.DurationMinutes = Math.Clamp(snapshot.DurationMinutes, 1, 300);
        set.ShuffleQuestions = snapshot.ShuffleQuestions;
        set.ShuffleOptions = snapshot.ShuffleOptions;
        set.IsPublished = false;
        set.UpdatedAt = now;

        SynchronizeManualQuestions(
            set,
            lecturerUserId,
            snapshot.Questions.Select(question => new SaveManualQuizQuestionRequest
            {
                Id = question.QuestionId,
                ClientKey = $"version-{version.Id}-{question.OrderIndex}",
                QuestionType = question.QuestionType,
                Prompt = question.Prompt,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Explanation = question.Explanation,
                Difficulty = question.Difficulty,
                Topic = question.Topic,
                Points = question.Points
            }).ToList(),
            requireCompleteQuestions: false,
            now);

        await _learningRepository.SaveChangesAsync(cancellationToken);
        await CreateLearningSetVersionAsync(
            lecturerUserId,
            learningSetId,
            $"Khôi phục từ phiên bản {version.VersionNumber}.",
            cancellationToken);
    }

    private async Task<(User User, Subject Subject)?> GetLecturerAccessAsync(
        int lecturerUserId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(lecturerUserId);
        if (user is null
            || user.RoleId != LecturerRoleId
            || user.IsActive == false
            || user.SubjectId is null
            || user.Subject is null
            || user.Subject.IsDeleted == true)
        {
            return null;
        }

        var subject = await _learningRepository.GetSubjectAsync(user.SubjectId.Value, cancellationToken);
        return subject is null ? null : (user, subject);
    }

    private async Task<(User User, Subject Subject)> RequireLecturerAccessAsync(
        int lecturerUserId,
        CancellationToken cancellationToken)
    {
        return await GetLecturerAccessAsync(lecturerUserId, cancellationToken)
            ?? throw new InvalidOperationException(
                "Chỉ giảng viên đã được phân công môn học mới có thể quản lý nội dung ôn tập.");
    }

    private IReadOnlyList<ManualQuestionBinding> SynchronizeManualQuestions(
        LearningSet set,
        int lecturerUserId,
        IReadOnlyList<SaveManualQuizQuestionRequest> requestedQuestions,
        bool requireCompleteQuestions,
        DateTime now)
    {
        var existingItems = set.Items.ToList();
        var existingByQuestionId = existingItems
            .GroupBy(item => item.QuestionBankItemId)
            .ToDictionary(group => group.Key, group => group.First());
        var retainedItems = new HashSet<LearningSetItem>();
        var usedQuestionIds = new HashSet<int>();
        var bindings = new List<ManualQuestionBinding>(requestedQuestions.Count);

        for (var index = 0; index < requestedQuestions.Count; index++)
        {
            var request = requestedQuestions[index];
            var normalized = NormalizeManualQuestion(request);
            if (requireCompleteQuestions && !normalized.IsComplete)
            {
                throw new InvalidOperationException(
                    $"Câu hỏi {index + 1} chưa hoàn chỉnh. Vui lòng kiểm tra nội dung, phương án và đáp án đúng.");
            }

            LearningSetItem item;
            if (request.Id.HasValue
                && usedQuestionIds.Add(request.Id.Value)
                && existingByQuestionId.TryGetValue(request.Id.Value, out var existingItem))
            {
                item = existingItem;
                if (!ManualQuestionMatches(existingItem.QuestionBankItem, normalized))
                {
                    var question = existingItem.QuestionBankItem;
                    var canUpdateInPlace = !question.IsAiGenerated
                        && question.LearningSetItems.Count <= 1
                        && question.AttemptAnswers.Count == 0;
                    if (canUpdateInPlace)
                    {
                        UpdateManualQuestion(question, normalized, now);
                    }
                    else
                    {
                        item.QuestionBankItem = CreateManualQuestion(
                            set.SubjectId,
                            lecturerUserId,
                            normalized,
                            now);
                        item.QuestionBankItemId = 0;
                    }
                }
            }
            else
            {
                item = new LearningSetItem
                {
                    QuestionBankItem = CreateManualQuestion(
                        set.SubjectId,
                        lecturerUserId,
                        normalized,
                        now)
                };
                set.Items.Add(item);
            }

            item.OrderIndex = index + 1;
            item.Points = normalized.Points;
            retainedItems.Add(item);
            bindings.Add(new ManualQuestionBinding(
                LimitLength(request.ClientKey, 100) ?? $"question-{index + 1}",
                item));
        }

        var removedItems = existingItems
            .Where(item => !retainedItems.Contains(item))
            .ToList();
        if (removedItems.Count > 0)
        {
            foreach (var removedItem in removedItems)
                set.Items.Remove(removedItem);

            _learningRepository.RemoveLearningSetItems(removedItems);
        }

        return bindings;
    }

    private static NormalizedManualQuestion NormalizeManualQuestion(
        SaveManualQuizQuestionRequest request)
    {
        var questionType = LearningQuestionTypes.All.Contains(request.QuestionType)
            ? request.QuestionType
            : LearningQuestionTypes.MultipleChoice;
        var prompt = request.Prompt?.Trim() ?? string.Empty;
        var options = request.Options
            .Select(option => option?.Trim() ?? string.Empty)
            .Where(option => option.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
        var correctAnswer = request.CorrectAnswer?.Trim() ?? string.Empty;

        if (questionType == LearningQuestionTypes.TrueFalse)
            options = ["Đúng", "Sai"];
        else if (questionType == LearningQuestionTypes.ShortAnswer)
            options = [];

        var difficulty = LearningDifficultyLevels.QuestionLevels.Contains(request.Difficulty)
            ? request.Difficulty
            : LearningDifficultyLevels.Medium;
        var isComplete = prompt.Length > 0 && correctAnswer.Length > 0;

        if (questionType == LearningQuestionTypes.MultipleChoice)
        {
            isComplete = isComplete
                && options.Count >= 2
                && options.Any(option =>
                    string.Equals(option, correctAnswer, StringComparison.OrdinalIgnoreCase));
        }
        else if (questionType == LearningQuestionTypes.TrueFalse)
        {
            isComplete = isComplete
                && options.Any(option =>
                    AnswersMatch(option, correctAnswer));
        }

        return new NormalizedManualQuestion(
            questionType,
            prompt,
            options,
            correctAnswer,
            LimitLength(request.Explanation, 4_000),
            difficulty,
            LimitLength(request.Topic, 200),
            Math.Clamp(request.Points, 0.25m, 100m),
            isComplete);
    }

    private static QuestionBankItem CreateManualQuestion(
        int subjectId,
        int lecturerUserId,
        NormalizedManualQuestion question,
        DateTime now)
    {
        return new QuestionBankItem
        {
            SubjectId = subjectId,
            QuestionType = question.QuestionType,
            Prompt = question.Prompt,
            OptionsJson = JsonSerializer.Serialize(question.Options, JsonOptions),
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            Topic = question.Topic,
            CreatedByUserId = lecturerUserId,
            IsAiGenerated = false,
            IsActive = question.IsComplete,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void UpdateManualQuestion(
        QuestionBankItem question,
        NormalizedManualQuestion source,
        DateTime now)
    {
        question.QuestionType = source.QuestionType;
        question.Prompt = source.Prompt;
        question.OptionsJson = JsonSerializer.Serialize(source.Options, JsonOptions);
        question.CorrectAnswer = source.CorrectAnswer;
        question.Explanation = source.Explanation;
        question.Difficulty = source.Difficulty;
        question.Topic = source.Topic;
        question.IsActive = source.IsComplete;
        question.UpdatedAt = now;
    }

    private static bool ManualQuestionMatches(
        QuestionBankItem existing,
        NormalizedManualQuestion question)
    {
        return string.Equals(existing.QuestionType, question.QuestionType, StringComparison.Ordinal)
            && string.Equals(existing.Prompt, question.Prompt, StringComparison.Ordinal)
            && ReadJsonList(existing.OptionsJson).SequenceEqual(question.Options)
            && string.Equals(existing.CorrectAnswer, question.CorrectAnswer, StringComparison.Ordinal)
            && string.Equals(existing.Explanation, question.Explanation, StringComparison.Ordinal)
            && string.Equals(existing.Difficulty, question.Difficulty, StringComparison.Ordinal)
            && string.Equals(existing.Topic, question.Topic, StringComparison.Ordinal)
            && existing.IsActive == question.IsComplete;
    }

    private static void ValidateManualQuizRequest(SaveManualQuizRequest request)
    {
        if (request.Questions.Count > MaximumManualQuestionCount)
        {
            throw new InvalidOperationException(
                $"Mỗi Quiz thủ công được có tối đa {MaximumManualQuestionCount} câu hỏi.");
        }

        if (request.DurationMinutes is < 1 or > 300)
            throw new InvalidOperationException("Thời lượng Quiz phải từ 1 đến 300 phút.");
    }

    private static void ValidateGenerationRequest(GenerateQuestionBankRequest request)
    {
        if (request.QuestionCount is < 1 or > MaximumGenerationCount)
            throw new InvalidOperationException($"Mỗi lần có thể tạo từ 1 đến {MaximumGenerationCount} câu hỏi.");

        var types = request.QuestionTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (types.Count == 0 || types.Any(type => !LearningQuestionTypes.All.Contains(type)))
            throw new InvalidOperationException("Vui lòng chọn ít nhất một loại câu hỏi hợp lệ.");

        if (request.Difficulty != LearningDifficultyLevels.Mixed
            && !LearningDifficultyLevels.QuestionLevels.Contains(request.Difficulty))
        {
            throw new InvalidOperationException("Mức độ câu hỏi không hợp lệ.");
        }
    }

    private static void ValidateComposeRequest(ComposeLearningSetRequest request)
    {
        if (!LearningActivityTypes.All.Contains(request.ActivityType))
            throw new InvalidOperationException("Loại hoạt động ôn tập không hợp lệ.");

        if (request.QuestionCount is < 1 or > MaximumSetQuestionCount)
            throw new InvalidOperationException($"Mỗi bộ ôn tập có từ 1 đến {MaximumSetQuestionCount} câu hỏi.");

        if (request.Difficulty != LearningDifficultyLevels.Mixed
            && !LearningDifficultyLevels.QuestionLevels.Contains(request.Difficulty))
        {
            throw new InvalidOperationException("Mức độ bộ ôn tập không hợp lệ.");
        }
    }

    private static bool TryNormalizeDraft(
        GeneratedQuestionDraft draft,
        GenerateQuestionBankRequest request,
        out GeneratedQuestionDraft normalized)
    {
        normalized = new GeneratedQuestionDraft();
        var type = draft.QuestionType.Trim().ToLowerInvariant();
        if (!LearningQuestionTypes.All.Contains(type)
            || !request.QuestionTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var prompt = draft.Prompt.Trim();
        var correctAnswer = draft.CorrectAnswer.Trim();
        if (prompt.Length < 10 || correctAnswer.Length == 0)
            return false;

        var options = draft.Options
            .Select(option => option.Trim())
            .Where(option => option.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (type == LearningQuestionTypes.MultipleChoice)
        {
            if (options.Count != 4)
                return false;

            var matchingOption = options.FirstOrDefault(
                option => string.Equals(option, correctAnswer, StringComparison.OrdinalIgnoreCase));
            if (matchingOption is null)
                return false;
            correctAnswer = matchingOption;
        }
        else if (type == LearningQuestionTypes.TrueFalse)
        {
            options = ["Đúng", "Sai"];
            if (AnswersMatch(correctAnswer, "Đúng"))
                correctAnswer = "Đúng";
            else if (AnswersMatch(correctAnswer, "Sai"))
                correctAnswer = "Sai";
            else
                return false;
        }
        else
        {
            options = [];
        }

        var difficulty = draft.Difficulty.Trim().ToLowerInvariant();
        if (!LearningDifficultyLevels.QuestionLevels.Contains(difficulty))
        {
            difficulty = request.Difficulty == LearningDifficultyLevels.Mixed
                ? LearningDifficultyLevels.Medium
                : request.Difficulty;
        }

        normalized = new GeneratedQuestionDraft
        {
            QuestionType = type,
            Prompt = prompt,
            Options = options,
            CorrectAnswer = correctAnswer,
            Explanation = draft.Explanation.Trim(),
            Difficulty = difficulty,
            Topic = draft.Topic.Trim(),
            LearningObjective = draft.LearningObjective.Trim(),
            SourceReference = draft.SourceReference.Trim()
        };
        return true;
    }

    private static string BuildSourceContext(IReadOnlyCollection<Chunk> chunks)
    {
        var builder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            var pageLabel = chunk.PageNumber.HasValue
                ? $"trang {chunk.PageNumber.Value}"
                : "không rõ trang";
            var section =
                $"\n--- Tài liệu: {chunk.Document.OriginalName} | {pageLabel} | đoạn {chunk.ChunkIndex + 1} ---\n{chunk.Content.Trim()}\n";

            if (builder.Length + section.Length > MaximumContextCharacters)
                break;

            builder.Append(section);
        }

        return builder.ToString();
    }

    private static IEnumerable<QuestionBankItem> FilterCompatibleQuestions(
        IEnumerable<QuestionBankItem> questions,
        string activityType)
    {
        return activityType == LearningActivityTypes.SpeedChallenge
            ? questions.Where(question =>
                question.QuestionType is LearningQuestionTypes.MultipleChoice
                    or LearningQuestionTypes.TrueFalse)
            : questions;
    }

    private static int EstimateDuration(ComposeLearningSetRequest request)
    {
        var minutesPerQuestion = request.ActivityType switch
        {
            LearningActivityTypes.Flashcard => 1,
            LearningActivityTypes.Matching => 1,
            LearningActivityTypes.SpeedChallenge => 1,
            _ => 2
        };
        return Math.Max(3, request.QuestionCount * minutesPerQuestion);
    }

    private static string BuildFallbackTitle(string activityType, string subjectCode)
    {
        var typeLabel = activityType switch
        {
            LearningActivityTypes.Flashcard => "Thẻ ghi nhớ",
            LearningActivityTypes.Matching => "Ghép cặp kiến thức",
            LearningActivityTypes.SpeedChallenge => "Thử thách tốc độ",
            _ => "Bài Quiz ôn tập"
        };
        return $"{typeLabel} - {subjectCode}";
    }

    private static SubjectDto MapSubject(Subject subject)
    {
        return new SubjectDto
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description
        };
    }

    private static LearningSetSummaryDto MapSetSummary(LearningSet set)
    {
        return new LearningSetSummaryDto
        {
            Id = set.Id,
            SubjectId = set.SubjectId,
            SubjectCode = set.Subject.Code,
            SubjectName = set.Subject.Name,
            Title = set.Title,
            Description = set.Description,
            ActivityType = set.ActivityType,
            QuestionCount = set.Items.Count,
            DurationMinutes = set.DurationMinutes,
            IsPublished = set.IsPublished,
            UpdatedAt = set.UpdatedAt
        };
    }

    private static LearningAttemptSummaryDto MapAttemptSummary(LearningAttempt attempt)
    {
        return new LearningAttemptSummaryDto
        {
            Id = attempt.Id,
            LearningSetId = attempt.LearningSetId,
            SubjectCode = attempt.LearningSet.Subject.Code,
            LearningSetTitle = attempt.LearningSet.Title,
            Percentage = attempt.TotalPoints <= 0
                ? 0
                : Math.Round(attempt.Score / attempt.TotalPoints * 100m, 1),
            CorrectCount = attempt.CorrectCount,
            TotalQuestions = attempt.TotalQuestions,
            CompletedAt = attempt.CompletedAt
        };
    }

    private static QuestionBankItemDto MapQuestion(QuestionBankItem question)
    {
        return new QuestionBankItemDto
        {
            Id = question.Id,
            SubjectId = question.SubjectId,
            ChapterId = question.ChapterId,
            ChapterName = question.Chapter is null
                ? null
                : $"Chương {question.Chapter.Number}: {question.Chapter.Title}",
            QuestionType = question.QuestionType,
            Prompt = question.Prompt,
            Options = ReadJsonList(question.OptionsJson),
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation ?? string.Empty,
            Difficulty = question.Difficulty,
            Topic = question.Topic,
            LearningObjective = question.LearningObjective,
            SourceReferences = ReadJsonList(question.SourceReferencesJson),
            IsAiGenerated = question.IsAiGenerated,
            IsActive = question.IsActive,
            UpdatedAt = question.UpdatedAt
        };
    }

    private static LearningSetDetailDto MapLearningSet(LearningSet set, bool canManage)
    {
        return new LearningSetDetailDto
        {
            Id = set.Id,
            SubjectId = set.SubjectId,
            SubjectCode = set.Subject.Code,
            SubjectName = set.Subject.Name,
            Title = set.Title,
            Description = set.Description,
            Instructions = set.Instructions,
            ActivityType = set.ActivityType,
            DurationMinutes = set.DurationMinutes,
            IsPublished = set.IsPublished,
            ShuffleQuestions = set.ShuffleQuestions,
            ShuffleOptions = set.ShuffleOptions,
            CanManage = canManage,
            UpdatedAt = set.UpdatedAt,
            Questions = set.Items
                .OrderBy(item => item.OrderIndex)
                .Select(item =>
                {
                    var question = item.QuestionBankItem;
                    return new LearningSetQuestionDto
                    {
                        Id = question.Id,
                        OrderIndex = item.OrderIndex,
                        Points = item.Points,
                        QuestionType = question.QuestionType,
                        Prompt = question.Prompt,
                        Options = ReadJsonList(question.OptionsJson),
                        CorrectAnswer = question.CorrectAnswer,
                        Explanation = question.Explanation ?? string.Empty,
                        Difficulty = question.Difficulty,
                        Topic = question.Topic,
                        SourceReference = ReadJsonList(question.SourceReferencesJson).FirstOrDefault()
                    };
                })
                .ToList()
        };
    }

    private static IReadOnlyList<string> ReadJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                ?.Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? NormalizeOptionalDifficulty(string? difficulty)
    {
        return LearningDifficultyLevels.QuestionLevels.Contains(difficulty ?? string.Empty)
            ? difficulty
            : null;
    }

    private static string? NormalizeOptionalQuestionType(string? questionType)
    {
        return LearningQuestionTypes.All.Contains(questionType ?? string.Empty)
            ? questionType
            : null;
    }

    private static bool AnswersMatch(string? left, string? right)
    {
        return LearningTextNormalizer.NormalizeForComparison(left)
            == LearningTextNormalizer.NormalizeForComparison(right);
    }

    private async Task CreateLearningSetVersionAsync(
        int lecturerUserId,
        int learningSetId,
        string changeSummary,
        CancellationToken cancellationToken)
    {
        var set = await _learningRepository.GetLearningSetAsync(
            learningSetId,
            tracking: false,
            cancellationToken);
        if (set is null)
            return;

        var snapshot = new LearningSetSnapshot
        {
            Title = set.Title,
            Description = set.Description,
            Instructions = set.Instructions,
            DurationMinutes = set.DurationMinutes ?? 15,
            IsPublished = set.IsPublished,
            ShuffleQuestions = set.ShuffleQuestions,
            ShuffleOptions = set.ShuffleOptions,
            Questions = set.Items
                .OrderBy(item => item.OrderIndex)
                .Select(item => new LearningSetQuestionSnapshot
                {
                    QuestionId = item.QuestionBankItemId,
                    OrderIndex = item.OrderIndex,
                    QuestionType = item.QuestionBankItem.QuestionType,
                    Prompt = item.QuestionBankItem.Prompt,
                    Options = ReadJsonList(item.QuestionBankItem.OptionsJson),
                    CorrectAnswer = item.QuestionBankItem.CorrectAnswer,
                    Explanation = item.QuestionBankItem.Explanation,
                    Difficulty = item.QuestionBankItem.Difficulty,
                    Topic = item.QuestionBankItem.Topic,
                    Points = item.Points
                })
                .ToList()
        };
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var latest = await _learningRepository.GetLatestLearningSetVersionAsync(
            learningSetId,
            cancellationToken);
        if (string.Equals(latest?.SnapshotJson, snapshotJson, StringComparison.Ordinal))
            return;

        await _learningRepository.AddLearningSetVersionAsync(
            new LearningSetVersion
            {
                LearningSetId = learningSetId,
                VersionNumber = (latest?.VersionNumber ?? 0) + 1,
                SnapshotJson = snapshotJson,
                ChangeSummary = LimitLength(changeSummary, 500),
                CreatedByUserId = lecturerUserId,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private static LearningSetSnapshot DeserializeSnapshot(string snapshotJson)
    {
        try
        {
            return JsonSerializer.Deserialize<LearningSetSnapshot>(snapshotJson, JsonOptions)
                ?? throw new InvalidOperationException("Phiên bản Quiz không chứa dữ liệu hợp lệ.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Không thể đọc dữ liệu của phiên bản Quiz đã chọn.",
                exception);
        }
    }

    private static QuestionAnalyticsDto BuildQuestionAnalytics(
        IGrouping<int, LearningAttemptAnswer> answerGroup)
    {
        var answers = answerGroup.ToList();
        var question = answers[0].QuestionBankItem;
        var answerCount = answers.Count;
        var correctCount = answers.Count(answer => answer.IsCorrect);
        var correctRate = Percentage(correctCount, answerCount);
        var difficultyBand = correctRate >= 80m
            ? "easy"
            : correctRate >= 50m
                ? "medium"
                : "hard";

        return new QuestionAnalyticsDto
        {
            QuestionId = question.Id,
            Prompt = question.Prompt,
            QuestionType = question.QuestionType,
            CorrectAnswer = question.CorrectAnswer,
            AnswerCount = answerCount,
            CorrectCount = correctCount,
            CorrectRate = correctRate,
            DifficultyBand = difficultyBand,
            DifficultyLabel = difficultyBand switch
            {
                "easy" => "Dễ",
                "medium" => "Trung bình",
                _ => "Khó"
            },
            Options = BuildOptionAnalytics(question, answers)
        };
    }

    private static IReadOnlyList<OptionSelectionAnalyticsDto> BuildOptionAnalytics(
        QuestionBankItem question,
        IReadOnlyList<LearningAttemptAnswer> answers)
    {
        var configuredOptions = ReadJsonList(question.OptionsJson).ToList();
        var selectedGroups = answers
            .GroupBy(
                answer => string.IsNullOrWhiteSpace(answer.SelectedAnswer)
                    ? "Không trả lời"
                    : answer.SelectedAnswer.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Option = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .ToList();

        if (question.QuestionType == LearningQuestionTypes.ShortAnswer)
        {
            var visibleGroups = selectedGroups.Take(8).ToList();
            var remainingCount = selectedGroups.Skip(8).Sum(group => group.Count);
            var results = visibleGroups.Select(group => new OptionSelectionAnalyticsDto
            {
                Option = group.Option,
                SelectionCount = group.Count,
                SelectionRate = Percentage(group.Count, answers.Count),
                IsCorrect = AnswersMatch(group.Option, question.CorrectAnswer)
            }).ToList();
            if (remainingCount > 0)
            {
                results.Add(new OptionSelectionAnalyticsDto
                {
                    Option = "Các câu trả lời khác",
                    SelectionCount = remainingCount,
                    SelectionRate = Percentage(remainingCount, answers.Count)
                });
            }

            return results;
        }

        var optionLabels = configuredOptions.ToList();
        foreach (var selected in selectedGroups)
        {
            if (optionLabels.All(option => !AnswersMatch(option, selected.Option)))
                optionLabels.Add(selected.Option);
        }

        return optionLabels.Select(option =>
        {
            var count = answers.Count(answer =>
                AnswersMatch(
                    string.IsNullOrWhiteSpace(answer.SelectedAnswer)
                        ? "Không trả lời"
                        : answer.SelectedAnswer,
                    option));
            return new OptionSelectionAnalyticsDto
            {
                Option = option,
                SelectionCount = count,
                SelectionRate = Percentage(count, answers.Count),
                IsCorrect = AnswersMatch(option, question.CorrectAnswer)
            };
        }).ToList();
    }

    private static decimal AttemptPercentage(LearningAttempt attempt)
    {
        return attempt.TotalPoints <= 0
            ? 0
            : Math.Round(attempt.Score / attempt.TotalPoints * 100m, 1);
    }

    private static decimal Percentage(int numerator, int denominator)
    {
        return denominator <= 0
            ? 0
            : Math.Round(numerator / (decimal)denominator * 100m, 1);
    }

    private static bool CanManageLearningSet(User user, LearningSet set)
    {
        return user.RoleId == LecturerRoleId
            && user.SubjectId == set.SubjectId;
    }

    private static bool CanStudyLearningSet(User user, LearningSet set)
    {
        return user.RoleId == StudentRoleId
            && set.IsPublished;
    }

    private sealed record NormalizedManualQuestion(
        string QuestionType,
        string Prompt,
        IReadOnlyList<string> Options,
        string CorrectAnswer,
        string? Explanation,
        string Difficulty,
        string? Topic,
        decimal Points,
        bool IsComplete);

    private sealed record ManualQuestionBinding(
        string ClientKey,
        LearningSetItem Item);

    private sealed class LearningSetSnapshot
    {
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Instructions { get; init; }
        public int DurationMinutes { get; init; }
        public bool IsPublished { get; init; }
        public bool ShuffleQuestions { get; init; }
        public bool ShuffleOptions { get; init; }
        public IReadOnlyList<LearningSetQuestionSnapshot> Questions { get; init; } = [];
    }

    private sealed class LearningSetQuestionSnapshot
    {
        public int QuestionId { get; init; }
        public int OrderIndex { get; init; }
        public string QuestionType { get; init; } = string.Empty;
        public string Prompt { get; init; } = string.Empty;
        public IReadOnlyList<string> Options { get; init; } = [];
        public string CorrectAnswer { get; init; } = string.Empty;
        public string? Explanation { get; init; }
        public string Difficulty { get; init; } = string.Empty;
        public string? Topic { get; init; }
        public decimal Points { get; init; }
    }

    private static string? LimitLength(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
