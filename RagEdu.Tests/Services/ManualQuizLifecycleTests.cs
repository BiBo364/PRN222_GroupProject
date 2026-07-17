using System.Text.Json;
using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class ManualQuizLifecycleTests
{
    [Fact]
    public async Task GetManualQuizEditorAsync_ReturnsBlankEditorForNewQuiz()
    {
        var environment = new LearningTestEnvironment();

        var editor = await environment.Service.GetManualQuizEditorAsync(
            environment.Lecturer.Id,
            learningSetId: null);

        Assert.NotNull(editor);
        Assert.Null(editor.Id);
        Assert.Equal(environment.Subject.Code, editor.Subject.Code);
        Assert.Equal(15, editor.DurationMinutes);
        Assert.True(editor.ShuffleQuestions);
        Assert.True(editor.ShuffleOptions);
        Assert.Empty(editor.Questions);
    }

    [Fact]
    public async Task GetManualQuizEditorAsync_ReturnsNullForStudent()
    {
        var environment = new LearningTestEnvironment();

        var editor = await environment.Service.GetManualQuizEditorAsync(
            environment.Student.Id,
            learningSetId: null);

        Assert.Null(editor);
    }

    [Fact]
    public async Task GetManualQuizEditorAsync_MapsExistingQuizAndQuestionOrder()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion(prompt: "Câu một có nội dung gì?");
        var second = environment.AddTrueFalseQuestion(prompt: "Câu hai có đúng hay sai?");
        var set = environment.AddQuiz(questions: [first, second]);
        set.Items.First(item => item.QuestionBankItemId == first.Id).OrderIndex = 2;
        set.Items.First(item => item.QuestionBankItemId == second.Id).OrderIndex = 1;

        var editor = await environment.Service.GetManualQuizEditorAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.NotNull(editor);
        Assert.Equal(set.Id, editor.Id);
        Assert.Equal([second.Id, first.Id], editor.Questions.Select(question => question.Id));
        Assert.Equal(["question-" + second.Id, "question-" + first.Id],
            editor.Questions.Select(question => question.ClientKey));
    }

    [Fact]
    public async Task GetManualQuizEditorAsync_RejectsNonQuizActivity()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddLearningSet(LearningActivityTypes.Flashcard);

        var editor = await environment.Service.GetManualQuizEditorAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Null(editor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public async Task SaveManualQuizAsync_RejectsInvalidDuration(int duration)
    {
        var environment = new LearningTestEnvironment();
        var request = ValidQuizRequest(duration: duration);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));

        Assert.Empty(environment.LearningRepository.LearningSets);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RejectsMoreThanOneHundredQuestions()
    {
        var environment = new LearningTestEnvironment();
        var questions = Enumerable.Range(1, 101)
            .Select(index => ValidMultipleChoiceRequest($"q-{index}", $"Câu hỏi số {index}?"))
            .ToList();
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz quá dài",
            DurationMinutes = 30,
            Questions = questions
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));
    }

    [Fact]
    public async Task SaveManualQuizAsync_CreatesDraftWithManualQuestion()
    {
        var environment = new LearningTestEnvironment();
        var request = ValidQuizRequest();

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        var set = Assert.Single(environment.LearningRepository.LearningSets);
        var item = Assert.Single(set.Items);
        Assert.Equal(result.Id, set.Id);
        Assert.False(result.IsPublished);
        Assert.Equal("Quiz thủ công", set.Title);
        Assert.Equal(LearningActivityTypes.Quiz, set.ActivityType);
        Assert.False(item.QuestionBankItem.IsAiGenerated);
        Assert.True(item.QuestionBankItem.IsActive);
        Assert.Equal(item.QuestionBankItemId, result.QuestionIds["question-1"]);
        Assert.Single(environment.LearningRepository.Versions);
    }

    [Fact]
    public async Task SaveManualQuizAsync_TrimsAndLimitsMetadata()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "  " + new string('T', 350) + "  ",
            Description = "  " + new string('D', 2100) + "  ",
            Instructions = "  " + new string('I', 2100) + "  ",
            DurationMinutes = 20,
            Questions = [ValidMultipleChoiceRequest("question-1", "Câu hỏi hợp lệ?")]
        };

        await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        var set = environment.LearningRepository.LearningSets[0];
        Assert.Equal(300, set.Title.Length);
        Assert.Equal(2000, set.Description!.Length);
        Assert.Equal(2000, set.Instructions!.Length);
    }

    [Fact]
    public async Task SaveManualQuizAsync_AutosavesIncompleteQuestionAsInactive()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "",
            DurationMinutes = 15,
            IsPublished = true,
            Questions =
            [
                new SaveManualQuizQuestionRequest
                {
                    ClientKey = "draft-question",
                    QuestionType = LearningQuestionTypes.MultipleChoice,
                    Prompt = "Câu hỏi đang viết dở",
                    Options = ["A"],
                    CorrectAnswer = "",
                    Points = 1m
                }
            ]
        };

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: true);

        var set = environment.LearningRepository.LearningSets[0];
        Assert.Equal("Quiz chưa đặt tên", set.Title);
        Assert.False(result.IsPublished);
        Assert.False(set.Items.Single().QuestionBankItem.IsActive);
        Assert.Empty(environment.LearningRepository.Versions);
    }

    [Fact]
    public async Task SaveManualQuizAsync_AutosavePreservesPublicationOnlyWhenComplete()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(isPublished: true, questions: question);
        var request = new SaveManualQuizRequest
        {
            Id = set.Id,
            Title = set.Title,
            DurationMinutes = 15,
            IsPublished = true,
            Questions =
            [
                ValidMultipleChoiceRequest(
                    "existing",
                    question.Prompt,
                    question.Id,
                    ["ASP.NET Core", "Django", "Laravel", "Spring MVC"],
                    "ASP.NET Core")
            ]
        };

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: true);

        Assert.True(result.IsPublished);
        Assert.True(set.IsPublished);
        Assert.Empty(environment.LearningRepository.Versions);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RequiresTitleBeforePublishing()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = " ",
            DurationMinutes = 15,
            IsPublished = true,
            Questions = [ValidMultipleChoiceRequest("question-1", "Câu hỏi hợp lệ?")]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));

        Assert.Empty(environment.LearningRepository.LearningSets);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RequiresAtLeastOneQuestionBeforePublishing()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz không có câu hỏi",
            DurationMinutes = 15,
            IsPublished = true,
            Questions = []
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));
    }

    [Fact]
    public async Task SaveManualQuizAsync_RejectsIncompleteMultipleChoiceWhenPublishing()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz chưa hoàn chỉnh",
            DurationMinutes = 15,
            IsPublished = true,
            Questions =
            [
                new SaveManualQuizQuestionRequest
                {
                    ClientKey = "invalid",
                    QuestionType = LearningQuestionTypes.MultipleChoice,
                    Prompt = "Câu hỏi có đáp án ngoài phương án?",
                    Options = ["A", "B", "C", "D"],
                    CorrectAnswer = "E"
                }
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));
    }

    [Fact]
    public async Task SaveManualQuizAsync_NormalizesTrueFalseOptions()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz đúng sai",
            DurationMinutes = 15,
            IsPublished = true,
            Questions =
            [
                new SaveManualQuizQuestionRequest
                {
                    ClientKey = "true-false",
                    QuestionType = LearningQuestionTypes.TrueFalse,
                    Prompt = "ASP.NET Core hỗ trợ đa nền tảng.",
                    Options = ["Có", "Không", "Khác"],
                    CorrectAnswer = "Dung",
                    Difficulty = LearningDifficultyLevels.Easy
                }
            ]
        };

        await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        var question = environment.LearningRepository.LearningSets[0]
            .Items.Single().QuestionBankItem;
        Assert.Equal(["Đúng", "Sai"], JsonSerializer.Deserialize<List<string>>(question.OptionsJson!));
        Assert.True(question.IsActive);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RemovesOptionsForShortAnswer()
    {
        var environment = new LearningTestEnvironment();
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz tự luận ngắn",
            DurationMinutes = 15,
            Questions =
            [
                new SaveManualQuizQuestionRequest
                {
                    ClientKey = "short",
                    QuestionType = LearningQuestionTypes.ShortAnswer,
                    Prompt = "Thành phần xử lý request gọi là gì?",
                    Options = ["Middleware", "Controller"],
                    CorrectAnswer = "Middleware",
                    Difficulty = LearningDifficultyLevels.Hard
                }
            ]
        };

        await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        var question = environment.LearningRepository.LearningSets[0]
            .Items.Single().QuestionBankItem;
        Assert.Empty(JsonSerializer.Deserialize<List<string>>(question.OptionsJson!)!);
    }

    [Theory]
    [InlineData(0.01, 0.25)]
    [InlineData(1, 1)]
    [InlineData(150, 100)]
    public async Task SaveManualQuizAsync_ClampsQuestionPoints(
        double requestedPoints,
        double expectedPoints)
    {
        var environment = new LearningTestEnvironment();
        var question = ValidMultipleChoiceRequest("points", "Điểm của câu hỏi?", points: (decimal)requestedPoints);
        var request = new SaveManualQuizRequest
        {
            Title = "Quiz điểm",
            DurationMinutes = 15,
            Questions = [question]
        };

        await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        Assert.Equal(
            (decimal)expectedPoints,
            environment.LearningRepository.LearningSets[0].Items.Single().Points);
    }

    [Fact]
    public async Task SaveManualQuizAsync_UpdatesOwnedUnusedManualQuestionInPlace()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(isAiGenerated: false);
        var set = environment.AddQuiz(questions: question);
        var request = ExistingQuizRequest(
            set,
            ValidMultipleChoiceRequest(
                "existing",
                "Câu hỏi đã được sửa?",
                question.Id,
                ["Một", "Hai", "Ba", "Bốn"],
                "Hai"));

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        Assert.Equal(question.Id, result.QuestionIds["existing"]);
        Assert.Equal("Câu hỏi đã được sửa?", question.Prompt);
        Assert.Equal("Hai", question.CorrectAnswer);
        Assert.Single(environment.LearningRepository.Questions);
    }

    [Fact]
    public async Task SaveManualQuizAsync_CopiesAiQuestionBeforeEditing()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(isAiGenerated: true);
        var set = environment.AddQuiz(questions: question);
        var request = ExistingQuizRequest(
            set,
            ValidMultipleChoiceRequest(
                "copied",
                "Bản chỉnh sửa của câu AI?",
                question.Id,
                ["Một", "Hai", "Ba", "Bốn"],
                "Một"));

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        var newQuestionId = result.QuestionIds["copied"];
        Assert.NotEqual(question.Id, newQuestionId);
        Assert.Equal(2, environment.LearningRepository.Questions.Count);
        Assert.Equal("Bản chỉnh sửa của câu AI?", set.Items.Single().QuestionBankItem.Prompt);
        Assert.Equal("ASP.NET Core hỗ trợ tính năng nào? 1", question.Prompt);
    }

    [Fact]
    public async Task SaveManualQuizAsync_CopiesQuestionThatAlreadyHasAttemptAnswers()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(isAiGenerated: false);
        var set = environment.AddQuiz(questions: question);
        environment.AddAttempt(set);
        var request = ExistingQuizRequest(
            set,
            ValidMultipleChoiceRequest(
                "copied-after-attempt",
                "Câu hỏi mới sau khi đã có bài làm?",
                question.Id,
                ["Một", "Hai", "Ba", "Bốn"],
                "Một"));

        var result = await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        Assert.NotEqual(question.Id, result.QuestionIds["copied-after-attempt"]);
        Assert.Equal(2, environment.LearningRepository.Questions.Count);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RemovesQuestionsMissingFromRequest()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion();
        var second = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(questions: [first, second]);
        var request = ExistingQuizRequest(
            set,
            ValidMultipleChoiceRequest(
                "kept",
                first.Prompt,
                first.Id,
                JsonSerializer.Deserialize<List<string>>(first.OptionsJson!)!,
                first.CorrectAnswer));

        await environment.Service.SaveManualQuizAsync(
            environment.Lecturer.Id,
            request,
            isAutosave: false);

        Assert.Single(set.Items);
        Assert.Equal(first.Id, set.Items.Single().QuestionBankItemId);
        Assert.Single(environment.LearningRepository.RemovedItems);
        Assert.Equal(second.Id, environment.LearningRepository.RemovedItems[0].QuestionBankItemId);
    }

    [Fact]
    public async Task SaveManualQuizAsync_RejectsQuizFromOtherSubject()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(
            subject: environment.OtherSubject,
            creator: environment.OtherLecturer);
        var request = new SaveManualQuizRequest
        {
            Id = set.Id,
            Title = set.Title,
            DurationMinutes = 15
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SaveManualQuizAsync(
                environment.Lecturer.Id,
                request,
                isAutosave: false));
    }

    [Fact]
    public async Task SetPublishedAsync_PublishesCompleteQuizAndCreatesVersion()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: false);

        await environment.Service.SetPublishedAsync(
            environment.Lecturer.Id,
            set.Id,
            isPublished: true);

        Assert.True(set.IsPublished);
        Assert.Single(environment.LearningRepository.Versions);
    }

    [Theory]
    [InlineData("empty-title")]
    [InlineData("empty-items")]
    [InlineData("inactive-question")]
    public async Task SetPublishedAsync_RejectsIncompleteQuiz(string state)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(isPublished: false, questions: question);
        if (state == "empty-title")
            set.Title = "";
        if (state == "empty-items")
            set.Items.Clear();
        if (state == "inactive-question")
            question.IsActive = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SetPublishedAsync(
                environment.Lecturer.Id,
                set.Id,
                isPublished: true));

        Assert.False(set.IsPublished);
    }

    [Fact]
    public async Task DeleteRestoreAndPermanentDelete_CompleteLifecycle()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: true);

        await environment.Service.DeleteLearningSetAsync(environment.Lecturer.Id, set.Id);
        Assert.True(set.IsDeleted);
        Assert.NotNull(set.DeletedAt);

        var recycleBin = await environment.Service.GetRecycleBinAsync(environment.Lecturer.Id);
        var deletedItem = Assert.Single(recycleBin!.Items);
        Assert.Equal(set.Id, deletedItem.Id);
        Assert.True(deletedItem.WasPublished);

        await environment.Service.RestoreLearningSetAsync(environment.Lecturer.Id, set.Id);
        Assert.False(set.IsDeleted);
        Assert.Null(set.DeletedAt);
        Assert.False(set.IsPublished);

        await environment.Service.DeleteLearningSetAsync(environment.Lecturer.Id, set.Id);
        await environment.Service.PermanentlyDeleteLearningSetAsync(
            environment.Lecturer.Id,
            set.Id);
        Assert.Empty(environment.LearningRepository.LearningSets);
        Assert.Equal(1, environment.LearningRepository.PermanentDeleteCallCount);
    }

    private static SaveManualQuizRequest ValidQuizRequest(int duration = 15)
    {
        return new SaveManualQuizRequest
        {
            Title = "Quiz thủ công",
            Description = "Mô tả Quiz",
            Instructions = "Chọn đáp án đúng",
            DurationMinutes = duration,
            IsPublished = false,
            ShuffleQuestions = true,
            ShuffleOptions = true,
            Questions = [ValidMultipleChoiceRequest("question-1", "Framework Web .NET là gì?")]
        };
    }

    private static SaveManualQuizQuestionRequest ValidMultipleChoiceRequest(
        string clientKey,
        string prompt,
        int? id = null,
        IReadOnlyList<string>? options = null,
        string correctAnswer = "ASP.NET Core",
        decimal points = 1m)
    {
        return new SaveManualQuizQuestionRequest
        {
            Id = id,
            ClientKey = clientKey,
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = prompt,
            Options = options ?? ["ASP.NET Core", "Django", "Laravel", "Spring MVC"],
            CorrectAnswer = correctAnswer,
            Explanation = "Đáp án được giải thích rõ ràng.",
            Difficulty = LearningDifficultyLevels.Medium,
            Topic = "ASP.NET Core",
            Points = points
        };
    }

    private static SaveManualQuizRequest ExistingQuizRequest(
        LearningSet set,
        params SaveManualQuizQuestionRequest[] questions)
    {
        return new SaveManualQuizRequest
        {
            Id = set.Id,
            Title = set.Title,
            Description = set.Description,
            Instructions = set.Instructions,
            DurationMinutes = set.DurationMinutes ?? 15,
            IsPublished = set.IsPublished,
            ShuffleQuestions = set.ShuffleQuestions,
            ShuffleOptions = set.ShuffleOptions,
            Questions = questions
        };
    }
}
