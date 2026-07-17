using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class LearningCompositionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("video")]
    public async Task ComposeLearningSetAsync_RejectsUnknownActivityType(string activityType)
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();
        var request = ValidRequest(activityType: activityType);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.ComposeLearningSetAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.CompositionCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task ComposeLearningSetAsync_RejectsQuestionCountOutsideRange(int count)
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();
        var request = ValidRequest(count: count);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.ComposeLearningSetAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.CompositionCallCount);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_RejectsUnknownDifficulty()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();
        var request = new ComposeLearningSetRequest
        {
            ActivityType = LearningActivityTypes.Quiz,
            QuestionCount = 1,
            Difficulty = "expert"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.ComposeLearningSetAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.CompositionCallCount);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_RequiresLecturerRole()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.ComposeLearningSetAsync(
                environment.Student.Id,
                ValidRequest()));
    }

    [Fact]
    public async Task ComposeLearningSetAsync_RequiresEnoughCompatibleQuestions()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.ComposeLearningSetAsync(
                environment.Lecturer.Id,
                ValidRequest(count: 2)));

        Assert.Contains("1", exception.Message);
        Assert.Equal(0, environment.AiService.CompositionCallCount);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_FiltersShortAnswerFromSpeedChallenge()
    {
        var environment = new LearningTestEnvironment();
        environment.AddShortAnswerQuestion();
        environment.AddMultipleChoiceQuestion();
        environment.AddTrueFalseQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Thử thách tốc độ",
            SelectedQuestionIds = environment.LearningRepository.Questions
                .Select(question => question.Id)
                .ToList()
        };
        var request = ValidRequest(
            count: 2,
            activityType: LearningActivityTypes.SpeedChallenge);

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            request);

        Assert.Equal(2, result.Questions.Count);
        Assert.DoesNotContain(
            result.Questions,
            question => question.QuestionType == LearningQuestionTypes.ShortAnswer);
        Assert.DoesNotContain(
            environment.AiService.LastCompositionRequest!.Candidates,
            question => question.QuestionType == LearningQuestionTypes.ShortAnswer);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_PassesCandidatesAndFocusToAi()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion();
        var second = environment.AddTrueFalseQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Ôn tập kiến trúc",
            SelectedQuestionIds = [first.Id, second.Id]
        };
        var request = new ComposeLearningSetRequest
        {
            ActivityType = LearningActivityTypes.Quiz,
            QuestionCount = 2,
            Difficulty = LearningDifficultyLevels.Mixed,
            Focus = "Middleware",
            PublishImmediately = true
        };

        await environment.Service.ComposeLearningSetAsync(environment.Lecturer.Id, request);

        var aiRequest = environment.AiService.LastCompositionRequest;
        Assert.NotNull(aiRequest);
        Assert.Equal(environment.Subject.Code, aiRequest.SubjectCode);
        Assert.Equal("Middleware", aiRequest.Focus);
        Assert.Equal(2, aiRequest.Candidates.Count);
        Assert.Equal([first.Id, second.Id], aiRequest.Candidates.Select(item => item.Id));
    }

    [Fact]
    public async Task ComposeLearningSetAsync_UsesAiSelectionOrder()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion(prompt: "Câu thứ nhất có nội dung gì?");
        var second = environment.AddMultipleChoiceQuestion(prompt: "Câu thứ hai có nội dung gì?");
        var third = environment.AddMultipleChoiceQuestion(prompt: "Câu thứ ba có nội dung gì?");
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Quiz có thứ tự",
            SelectedQuestionIds = [third.Id, first.Id]
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest(count: 2));

        Assert.Equal([third.Id, first.Id], result.Questions.Select(question => question.Id));
        Assert.Equal([1, 2], result.Questions.Select(question => question.OrderIndex));
        Assert.DoesNotContain(result.Questions, question => question.Id == second.Id);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_FillsMissingAiSelectionFromCandidates()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion();
        var second = environment.AddMultipleChoiceQuestion();
        var third = environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Quiz được bù câu hỏi",
            SelectedQuestionIds = [9999, first.Id, first.Id]
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest(count: 3));

        Assert.Equal(3, result.Questions.Count);
        Assert.Equal(3, result.Questions.Select(question => question.Id).Distinct().Count());
        Assert.Contains(result.Questions, question => question.Id == first.Id);
        Assert.Contains(result.Questions, question => question.Id == second.Id);
        Assert.Contains(result.Questions, question => question.Id == third.Id);
    }

    [Theory]
    [InlineData(LearningActivityTypes.Quiz, 2, 4)]
    [InlineData(LearningActivityTypes.Flashcard, 2, 3)]
    [InlineData(LearningActivityTypes.Matching, 5, 5)]
    [InlineData(LearningActivityTypes.SpeedChallenge, 4, 4)]
    public async Task ComposeLearningSetAsync_EstimatesAndClampsDuration(
        string activityType,
        int questionCount,
        int expectedDuration)
    {
        var environment = new LearningTestEnvironment();
        for (var index = 0; index < questionCount; index++)
            environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Bộ ôn tập",
            SelectedQuestionIds = environment.LearningRepository.Questions
                .Select(question => question.Id)
                .ToList(),
            DurationMinutes = null
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest(questionCount, activityType));

        Assert.Equal(expectedDuration, result.DurationMinutes);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(999, 120)]
    public async Task ComposeLearningSetAsync_ClampsAiDuration(int aiDuration, int expected)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Quiz",
            SelectedQuestionIds = [question.Id],
            DurationMinutes = aiDuration
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest());

        Assert.Equal(expected, result.DurationMinutes);
    }

    [Theory]
    [InlineData(LearningActivityTypes.Quiz, true)]
    [InlineData(LearningActivityTypes.SpeedChallenge, true)]
    [InlineData(LearningActivityTypes.Flashcard, false)]
    [InlineData(LearningActivityTypes.Matching, false)]
    public async Task ComposeLearningSetAsync_ConfiguresOptionShuffleByActivity(
        string activityType,
        bool expectedShuffle)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Hoạt động ôn tập",
            SelectedQuestionIds = [question.Id]
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest(activityType: activityType));

        Assert.Equal(expectedShuffle, result.ShuffleOptions);
        Assert.True(result.ShuffleQuestions);
    }

    [Fact]
    public async Task ComposeLearningSetAsync_UsesFallbackTitle_WhenAiTitleIsBlank()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "   ",
            SelectedQuestionIds = [question.Id]
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            ValidRequest());

        Assert.Contains(environment.Subject.Code, result.Title);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
    }

    [Fact]
    public async Task ComposeLearningSetAsync_PersistsModelPublicationAndInitialVersion()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        environment.AiService.LearningSetPlan = new GeneratedLearningSetPlan
        {
            Title = "Quiz kiến trúc",
            Description = "Mô tả",
            Instructions = "Hướng dẫn",
            SelectedQuestionIds = [question.Id]
        };
        var request = new ComposeLearningSetRequest
        {
            ActivityType = LearningActivityTypes.Quiz,
            QuestionCount = 1,
            Difficulty = LearningDifficultyLevels.Mixed,
            PublishImmediately = true
        };

        var result = await environment.Service.ComposeLearningSetAsync(
            environment.Lecturer.Id,
            request);

        var set = Assert.Single(environment.LearningRepository.LearningSets);
        Assert.Equal(result.Id, set.Id);
        Assert.True(set.IsPublished);
        Assert.Equal(environment.GeminiClient.ModelName, set.AiModel);
        Assert.Equal("Mô tả", set.Description);
        Assert.Equal("Hướng dẫn", set.Instructions);
        Assert.Equal(1, environment.LearningRepository.AddVersionCallCount);
        Assert.Single(environment.LearningRepository.Versions);
    }

    [Fact]
    public async Task UpdateQuestionAsync_UpdatesNormalizedQuestion()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var request = new UpdateQuestionBankItemRequest
        {
            Id = question.Id,
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = "  Middleware nào xử lý xác thực trong ASP.NET Core?  ",
            Options = ["Authentication", "Routing", "Static Files", "CORS"],
            CorrectAnswer = "authentication",
            Explanation = "  Authentication middleware xác thực danh tính.  ",
            Difficulty = LearningDifficultyLevels.Hard,
            Topic = "  Security  ",
            LearningObjective = "  Hiểu pipeline bảo mật  "
        };

        await environment.Service.UpdateQuestionAsync(environment.Lecturer.Id, request);

        Assert.Equal("Middleware nào xử lý xác thực trong ASP.NET Core?", question.Prompt);
        Assert.Equal("Authentication", question.CorrectAnswer);
        Assert.Equal("Authentication middleware xác thực danh tính.", question.Explanation);
        Assert.Equal("Security", question.Topic);
        Assert.Equal("Hiểu pipeline bảo mật", question.LearningObjective);
        Assert.Equal(1, environment.LearningRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateQuestionAsync_RejectsQuestionFromAnotherSubject()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(subject: environment.OtherSubject);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.UpdateQuestionAsync(
                environment.Lecturer.Id,
                new UpdateQuestionBankItemRequest
                {
                    Id = question.Id,
                    QuestionType = LearningQuestionTypes.MultipleChoice,
                    Prompt = question.Prompt,
                    Options = ["A", "B", "C", "D"],
                    CorrectAnswer = "A",
                    Difficulty = LearningDifficultyLevels.Medium
                }));
    }

    [Fact]
    public async Task SetQuestionActiveAsync_ChangesStateAndTimestamp()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(isActive: true);
        var before = question.UpdatedAt;

        await environment.Service.SetQuestionActiveAsync(
            environment.Lecturer.Id,
            question.Id,
            isActive: false);

        Assert.False(question.IsActive);
        Assert.True(question.UpdatedAt >= before);
        Assert.Equal(1, environment.LearningRepository.SaveChangesCallCount);
    }

    private static ComposeLearningSetRequest ValidRequest(
        int count = 1,
        string activityType = LearningActivityTypes.Quiz)
    {
        return new ComposeLearningSetRequest
        {
            ActivityType = activityType,
            QuestionCount = count,
            Difficulty = LearningDifficultyLevels.Mixed,
            PublishImmediately = false
        };
    }
}
