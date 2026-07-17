using System.Text.Json;
using RagEdu.Tests.Fakes;

namespace RagEdu.Tests.Fixtures;

internal sealed class LearningTestEnvironment
{
    private int _nextQuestionId = 1;
    private int _nextSetId = 1;
    private int _nextAttemptId = 1;

    public LearningTestEnvironment()
    {
        Subject = CreateSubject(1, "PRN222", "Lập trình Web .NET");
        OtherSubject = CreateSubject(2, "SWT301", "Kiểm thử phần mềm");

        Lecturer = CreateUser(10, 2, Subject, "lecturer", "Giảng viên Nguyễn An");
        OtherLecturer = CreateUser(11, 2, OtherSubject, "other-lecturer", "Giảng viên Trần Bình");
        Student = CreateUser(20, 3, null, "student", "Sinh viên Lê Chi");
        OtherStudent = CreateUser(21, 3, null, "student-two", "Sinh viên Phạm Dũng");
        Administrator = CreateUser(30, 1, null, "admin", "Quản trị viên");

        LearningRepository.Subjects.AddRange([Subject, OtherSubject]);
        UserRepository.Users.AddRange(
            [Lecturer, OtherLecturer, Student, OtherStudent, Administrator]);

        Service = new LearningService(
            LearningRepository,
            UserRepository,
            AiService,
            GeminiClient);
    }

    public FakeLearningRepository LearningRepository { get; } = new();
    public FakeUserRepository UserRepository { get; } = new();
    public FakeStudyContentAiService AiService { get; } = new();
    public FakeGeminiClient GeminiClient { get; } = new();
    public LearningService Service { get; }
    public Subject Subject { get; }
    public Subject OtherSubject { get; }
    public User Lecturer { get; }
    public User OtherLecturer { get; }
    public User Student { get; }
    public User OtherStudent { get; }
    public User Administrator { get; }

    public Chapter AddChapter(
        Subject? subject = null,
        int number = 1,
        string? title = null)
    {
        subject ??= Subject;
        var chapter = new Chapter
        {
            Id = subject.Chapters.Select(item => item.Id).DefaultIfEmpty().Max() + 1,
            SubjectId = subject.Id,
            Subject = subject,
            Number = number,
            Title = title ?? $"Chương {number}",
            CreatedAt = DateTime.UtcNow
        };
        subject.Chapters.Add(chapter);
        return chapter;
    }

    public Document AddIndexedDocument(
        Subject? subject = null,
        Chapter? chapter = null,
        string? name = null,
        string status = "indexed")
    {
        subject ??= Subject;
        var document = new Document
        {
            Id = LearningRepository.Documents.Select(item => item.Id).DefaultIfEmpty().Max() + 1,
            SubjectId = subject.Id,
            Subject = subject,
            ChapterId = chapter?.Id,
            Chapter = chapter,
            Filename = $"document-{Guid.NewGuid():N}.pdf",
            OriginalName = name ?? "Giáo trình PRN222.pdf",
            FileType = "pdf",
            StoragePath = "tests/document.pdf",
            Status = status,
            IndexedAt = string.Equals(status, "indexed", StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow
                : null,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        LearningRepository.Documents.Add(document);
        subject.Documents.Add(document);
        chapter?.Documents.Add(document);
        return document;
    }

    public Chunk AddChunk(
        Document document,
        string content,
        int chunkIndex = 0,
        int? pageNumber = 1)
    {
        var chunk = new Chunk
        {
            Id = LearningRepository.Chunks.Select(item => item.Id).DefaultIfEmpty().Max() + 1,
            DocumentId = document.Id,
            Document = document,
            ChunkIndex = chunkIndex,
            Content = content,
            PageNumber = pageNumber,
            CreatedAt = DateTime.UtcNow
        };
        document.Chunks.Add(chunk);
        LearningRepository.Chunks.Add(chunk);
        return chunk;
    }

    public QuestionBankItem AddMultipleChoiceQuestion(
        Subject? subject = null,
        string? prompt = null,
        string difficulty = LearningDifficultyLevels.Medium,
        bool isActive = true,
        bool isAiGenerated = false,
        string? correctAnswer = null,
        IReadOnlyList<string>? options = null,
        User? creator = null)
    {
        options ??= ["ASP.NET Core", "Django", "Laravel", "Spring MVC"];
        correctAnswer ??= options[0];
        return AddQuestion(
            subject,
            LearningQuestionTypes.MultipleChoice,
            prompt ?? $"ASP.NET Core hỗ trợ tính năng nào? {_nextQuestionId}",
            options,
            correctAnswer,
            difficulty,
            isActive,
            isAiGenerated,
            creator);
    }

    public QuestionBankItem AddTrueFalseQuestion(
        Subject? subject = null,
        string? prompt = null,
        string correctAnswer = "Đúng",
        string difficulty = LearningDifficultyLevels.Easy,
        bool isActive = true,
        bool isAiGenerated = false,
        User? creator = null)
    {
        return AddQuestion(
            subject,
            LearningQuestionTypes.TrueFalse,
            prompt ?? $"Entity Framework Core là một ORM. {_nextQuestionId}",
            ["Đúng", "Sai"],
            correctAnswer,
            difficulty,
            isActive,
            isAiGenerated,
            creator);
    }

    public QuestionBankItem AddShortAnswerQuestion(
        Subject? subject = null,
        string? prompt = null,
        string correctAnswer = "dependency injection",
        string difficulty = LearningDifficultyLevels.Hard,
        bool isActive = true,
        bool isAiGenerated = false,
        User? creator = null)
    {
        return AddQuestion(
            subject,
            LearningQuestionTypes.ShortAnswer,
            prompt ?? $"Kỹ thuật cung cấp dependency từ bên ngoài gọi là gì? {_nextQuestionId}",
            [],
            correctAnswer,
            difficulty,
            isActive,
            isAiGenerated,
            creator);
    }

    public QuestionBankItem AddQuestion(
        Subject? subject,
        string questionType,
        string prompt,
        IReadOnlyList<string> options,
        string correctAnswer,
        string difficulty,
        bool isActive,
        bool isAiGenerated,
        User? creator = null)
    {
        subject ??= Subject;
        creator ??= subject.Id == Subject.Id ? Lecturer : OtherLecturer;
        var now = DateTime.UtcNow;
        var question = new QuestionBankItem
        {
            Id = _nextQuestionId++,
            SubjectId = subject.Id,
            Subject = subject,
            QuestionType = questionType,
            Prompt = prompt,
            OptionsJson = JsonSerializer.Serialize(options),
            CorrectAnswer = correctAnswer,
            Explanation = $"Giải thích cho: {prompt}",
            Difficulty = difficulty,
            Topic = "ASP.NET Core",
            LearningObjective = "Hiểu kiến trúc ứng dụng Web .NET",
            SourceReferencesJson = JsonSerializer.Serialize(new[] { "Giáo trình, trang 10" }),
            CreatedByUserId = creator.Id,
            CreatedByUser = creator,
            IsAiGenerated = isAiGenerated,
            AiModel = isAiGenerated ? GeminiClient.ModelName : null,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        LearningRepository.Questions.Add(question);
        return question;
    }

    public LearningSet AddQuiz(
        Subject? subject = null,
        User? creator = null,
        bool isPublished = true,
        bool isDeleted = false,
        string? title = null,
        params QuestionBankItem[] questions)
    {
        subject ??= Subject;
        creator ??= subject.Id == Subject.Id ? Lecturer : OtherLecturer;
        if (questions.Length == 0)
            questions = [AddMultipleChoiceQuestion(subject: subject, creator: creator)];

        var now = DateTime.UtcNow;
        var set = new LearningSet
        {
            Id = _nextSetId++,
            SubjectId = subject.Id,
            Subject = subject,
            Title = title ?? $"Quiz {subject.Code} {_nextSetId - 1}",
            Description = "Bài kiểm tra ôn tập tự động.",
            Instructions = "Chọn đáp án đúng nhất.",
            ActivityType = LearningActivityTypes.Quiz,
            DurationMinutes = 15,
            IsPublished = isPublished,
            ShuffleQuestions = true,
            ShuffleOptions = true,
            CreatedByUserId = creator.Id,
            CreatedByUser = creator,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
            Items = questions.Select((question, index) => new LearningSetItem
            {
                Id = ((_nextSetId - 1) * 1000) + index + 1,
                QuestionBankItemId = question.Id,
                QuestionBankItem = question,
                OrderIndex = index + 1,
                Points = index + 1
            }).ToList()
        };
        foreach (var item in set.Items)
        {
            item.LearningSetId = set.Id;
            item.LearningSet = set;
            item.QuestionBankItem.LearningSetItems.Add(item);
        }
        LearningRepository.LearningSets.Add(set);
        return set;
    }

    public LearningSet AddLearningSet(
        string activityType,
        Subject? subject = null,
        User? creator = null,
        bool isPublished = true,
        params QuestionBankItem[] questions)
    {
        var set = AddQuiz(subject, creator, isPublished, false, null, questions);
        set.ActivityType = activityType;
        set.ShuffleOptions = activityType is LearningActivityTypes.Quiz
            or LearningActivityTypes.SpeedChallenge;
        return set;
    }

    public LearningAttempt AddAttempt(
        LearningSet set,
        User? student = null,
        decimal? score = null,
        decimal? totalPoints = null,
        DateTime? completedAt = null,
        TimeSpan? duration = null,
        IReadOnlyDictionary<int, string?>? selectedAnswers = null)
    {
        student ??= Student;
        completedAt ??= DateTime.UtcNow;
        duration ??= TimeSpan.FromMinutes(10);
        totalPoints ??= set.Items.Sum(item => item.Points);

        var answers = set.Items.Select(item =>
        {
            var selected = selectedAnswers is not null
                && selectedAnswers.TryGetValue(item.QuestionBankItemId, out var answer)
                    ? answer
                    : item.QuestionBankItem.CorrectAnswer;
            var correct = string.Equals(
                selected,
                item.QuestionBankItem.CorrectAnswer,
                StringComparison.OrdinalIgnoreCase);
            return new LearningAttemptAnswer
            {
                Id = (_nextAttemptId * 1000) + item.OrderIndex,
                QuestionBankItemId = item.QuestionBankItemId,
                QuestionBankItem = item.QuestionBankItem,
                SelectedAnswer = selected,
                IsCorrect = correct,
                AwardedPoints = correct ? item.Points : 0,
                AnsweredAt = completedAt.Value
            };
        }).ToList();

        score ??= answers.Sum(answer => answer.AwardedPoints);
        var attempt = new LearningAttempt
        {
            Id = _nextAttemptId++,
            LearningSetId = set.Id,
            LearningSet = set,
            UserId = student.Id,
            User = student,
            StartedAt = completedAt.Value.Subtract(duration.Value),
            CompletedAt = completedAt.Value,
            Score = score.Value,
            TotalPoints = totalPoints.Value,
            CorrectCount = answers.Count(answer => answer.IsCorrect),
            TotalQuestions = answers.Count,
            Answers = answers
        };
        foreach (var answer in answers)
        {
            answer.LearningAttemptId = attempt.Id;
            answer.LearningAttempt = attempt;
            answer.QuestionBankItem.AttemptAnswers.Add(answer);
        }
        set.Attempts.Add(attempt);
        LearningRepository.Attempts.Add(attempt);
        return attempt;
    }

    public static GeneratedQuestionDraft ValidMultipleChoiceDraft(
        string? prompt = null,
        string difficulty = LearningDifficultyLevels.Medium,
        string correctAnswer = "ASP.NET Core",
        IReadOnlyList<string>? options = null)
    {
        return new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = prompt ?? "Framework nào của Microsoft được dùng để xây dựng ứng dụng Web hiện đại?",
            Options = options ?? ["ASP.NET Core", "Django", "Laravel", "Spring MVC"],
            CorrectAnswer = correctAnswer,
            Explanation = "ASP.NET Core là framework Web đa nền tảng của Microsoft.",
            Difficulty = difficulty,
            Topic = "ASP.NET Core",
            LearningObjective = "Nhận biết framework Web .NET",
            SourceReference = "Giáo trình PRN222, trang 10"
        };
    }

    public static GeneratedQuestionDraft ValidTrueFalseDraft(
        string? prompt = null,
        string answer = "Đúng")
    {
        return new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.TrueFalse,
            Prompt = prompt ?? "ASP.NET Core hỗ trợ dependency injection tích hợp sẵn.",
            Options = ["True", "False"],
            CorrectAnswer = answer,
            Explanation = "Framework cung cấp DI container mặc định.",
            Difficulty = LearningDifficultyLevels.Easy,
            Topic = "Dependency Injection",
            LearningObjective = "Nhận biết dịch vụ tích hợp",
            SourceReference = "Giáo trình PRN222, trang 15"
        };
    }

    public static GeneratedQuestionDraft ValidShortAnswerDraft(
        string? prompt = null,
        string answer = "middleware")
    {
        return new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.ShortAnswer,
            Prompt = prompt ?? "Thành phần nào xử lý tuần tự một HTTP request trong ASP.NET Core?",
            Options = [],
            CorrectAnswer = answer,
            Explanation = "Middleware tạo thành pipeline xử lý request.",
            Difficulty = LearningDifficultyLevels.Hard,
            Topic = "HTTP pipeline",
            LearningObjective = "Giải thích pipeline",
            SourceReference = "Giáo trình PRN222, trang 20"
        };
    }

    private static Subject CreateSubject(int id, string code, string name)
    {
        return new Subject
        {
            Id = id,
            Code = code,
            Name = name,
            Description = $"Mô tả môn {code}",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static User CreateUser(
        int id,
        int roleId,
        Subject? subject,
        string username,
        string fullName)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.edu.vn",
            Password = "not-used-in-tests",
            FullName = fullName,
            RoleId = roleId,
            SubjectId = subject?.Id,
            Subject = subject,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
