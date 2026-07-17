using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class QuizAnalyticsTests
{
    [Fact]
    public async Task GetQuizAnalyticsAsync_ReturnsNullForStudent()
    {
        var environment = new LearningTestEnvironment();

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Student.Id,
            learningSetId: null,
            days: 30);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    [InlineData(90, 90)]
    [InlineData(0, 30)]
    [InlineData(14, 30)]
    [InlineData(365, 30)]
    public async Task GetQuizAnalyticsAsync_NormalizesSupportedDateRanges(
        int requestedDays,
        int expectedDays)
    {
        var environment = new LearningTestEnvironment();

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: requestedDays);

        Assert.Equal(expectedDays, result!.SelectedDays);
        Assert.Equal(expectedDays, result.Trend.Count);
        Assert.Equal(
            DateTime.UtcNow.Date.AddDays(-(expectedDays - 1)),
            environment.LearningRepository.LastAnalyticsFromUtc);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_RejectsQuizOutsideAssignedSubject()
    {
        var environment = new LearningTestEnvironment();
        var otherSet = environment.AddQuiz(
            environment.OtherSubject,
            environment.OtherLecturer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GetQuizAnalyticsAsync(
                environment.Lecturer.Id,
                otherSet.Id,
                days: 30));
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_OnlyOffersQuizActivitiesAsFilters()
    {
        var environment = new LearningTestEnvironment();
        var quiz = environment.AddQuiz(title: "Quiz");
        environment.AddLearningSet(LearningActivityTypes.Flashcard);
        environment.AddLearningSet(LearningActivityTypes.Matching);
        environment.AddLearningSet(LearningActivityTypes.SpeedChallenge);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 30);

        var option = Assert.Single(result!.LearningSets);
        Assert.Equal(quiz.Id, option.Id);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_ReturnsEmptyMetricsWithoutAttempts()
    {
        var environment = new LearningTestEnvironment();
        environment.AddQuiz();

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 7);

        Assert.Equal(0, result!.TotalAttempts);
        Assert.Equal(0, result.UniqueStudents);
        Assert.Equal(0m, result.AveragePercentage);
        Assert.Equal(0m, result.PassRate);
        Assert.Equal(0m, result.AverageDurationMinutes);
        Assert.Empty(result.QuizPerformance);
        Assert.Empty(result.Questions);
        Assert.All(result.Trend, point =>
        {
            Assert.Equal(0, point.AttemptCount);
            Assert.Equal(0m, point.AveragePercentage);
        });
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_AggregatesHeadlineMetrics()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        environment.AddAttempt(
            set,
            environment.Student,
            score: 8m,
            totalPoints: 10m,
            duration: TimeSpan.FromMinutes(10));
        environment.AddAttempt(
            set,
            environment.OtherStudent,
            score: 4m,
            totalPoints: 10m,
            duration: TimeSpan.FromMinutes(20));
        environment.AddAttempt(
            set,
            environment.Student,
            score: 6m,
            totalPoints: 10m,
            duration: TimeSpan.FromMinutes(30));

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 30);

        Assert.Equal(3, result!.TotalAttempts);
        Assert.Equal(2, result.UniqueStudents);
        Assert.Equal(60m, result.AveragePercentage);
        Assert.Equal(66.7m, result.PassRate);
        Assert.Equal(20m, result.AverageDurationMinutes);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_IgnoresUnrealisticDurationOverOneDay()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        environment.AddAttempt(set, duration: TimeSpan.FromMinutes(15));
        environment.AddAttempt(
            set,
            environment.OtherStudent,
            duration: TimeSpan.FromHours(25));

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 30);

        Assert.Equal(15m, result!.AverageDurationMinutes);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_BuildsContinuousDailyTrend()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var today = DateTime.UtcNow.Date.AddHours(10);
        var twoDaysAgo = today.AddDays(-2);
        environment.AddAttempt(set, completedAt: today, score: 10m, totalPoints: 10m);
        environment.AddAttempt(set, completedAt: today, score: 5m, totalPoints: 10m);
        environment.AddAttempt(set, completedAt: twoDaysAgo, score: 2m, totalPoints: 10m);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 7);

        Assert.Equal(7, result!.Trend.Count);
        var todayPoint = result.Trend.Single(point => point.Date == today.Date);
        Assert.Equal(2, todayPoint.AttemptCount);
        Assert.Equal(75m, todayPoint.AveragePercentage);
        var earlierPoint = result.Trend.Single(point => point.Date == twoDaysAgo.Date);
        Assert.Equal(1, earlierPoint.AttemptCount);
        Assert.Equal(20m, earlierPoint.AveragePercentage);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_GroupsPerformanceByQuiz()
    {
        var environment = new LearningTestEnvironment();
        var popular = environment.AddQuiz(title: "Quiz phổ biến");
        var quiet = environment.AddQuiz(title: "Quiz ít lượt");
        environment.AddAttempt(popular, environment.Student, score: 10m, totalPoints: 10m);
        environment.AddAttempt(popular, environment.OtherStudent, score: 5m, totalPoints: 10m);
        environment.AddAttempt(quiet, environment.Student, score: 4m, totalPoints: 10m);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            learningSetId: null,
            days: 30);

        Assert.Equal(2, result!.QuizPerformance.Count);
        var first = result.QuizPerformance[0];
        Assert.Equal(popular.Id, first.LearningSetId);
        Assert.Equal(2, first.AttemptCount);
        Assert.Equal(2, first.UniqueStudents);
        Assert.Equal(75m, first.AveragePercentage);
        Assert.Equal(100m, first.PassRate);
        var second = result.QuizPerformance[1];
        Assert.Equal(quiet.Id, second.LearningSetId);
        Assert.Equal(0m, second.PassRate);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_FiltersAttemptsBySelectedQuiz()
    {
        var environment = new LearningTestEnvironment();
        var selected = environment.AddQuiz(title: "Được chọn");
        var other = environment.AddQuiz(title: "Không chọn");
        environment.AddAttempt(selected);
        environment.AddAttempt(other);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            selected.Id,
            days: 30);

        Assert.Equal(selected.Id, result!.SelectedLearningSetId);
        Assert.Equal(1, result.TotalAttempts);
        Assert.Single(result.QuizPerformance);
        Assert.Equal(selected.Id, result.QuizPerformance[0].LearningSetId);
        Assert.Equal(selected.Id, environment.LearningRepository.LastAnalyticsLearningSetId);
    }

    [Theory]
    [InlineData(10, 10, "easy", "Dễ")]
    [InlineData(8, 10, "easy", "Dễ")]
    [InlineData(7, 10, "medium", "Trung bình")]
    [InlineData(5, 10, "medium", "Trung bình")]
    [InlineData(4, 10, "hard", "Khó")]
    [InlineData(0, 10, "hard", "Khó")]
    public async Task GetQuizAnalyticsAsync_ClassifiesQuestionDifficulty(
        int correctAnswers,
        int totalAnswers,
        string expectedBand,
        string expectedLabel)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(
            options: ["A", "B", "C", "D"],
            correctAnswer: "A");
        var set = environment.AddQuiz(questions: question);
        for (var index = 0; index < totalAnswers; index++)
        {
            var selected = index < correctAnswers ? "A" : "B";
            environment.AddAttempt(
                set,
                index % 2 == 0 ? environment.Student : environment.OtherStudent,
                selectedAnswers: new Dictionary<int, string?> { [question.Id] = selected });
        }

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            set.Id,
            days: 30);

        var analytics = Assert.Single(result!.Questions);
        Assert.Equal(totalAnswers, analytics.AnswerCount);
        Assert.Equal(correctAnswers, analytics.CorrectCount);
        Assert.Equal(
            Math.Round(correctAnswers / (decimal)totalAnswers * 100m, 1),
            analytics.CorrectRate);
        Assert.Equal(expectedBand, analytics.DifficultyBand);
        Assert.Equal(expectedLabel, analytics.DifficultyLabel);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_CalculatesSelectionRateForEveryConfiguredOption()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(
            options: ["A", "B", "C", "D"],
            correctAnswer: "A");
        var set = environment.AddQuiz(questions: question);
        AddSelections(environment, set, question, "A", "A", "B", "C", null);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            set.Id,
            days: 30);

        var options = result!.Questions.Single().Options;
        Assert.Equal(5, options.Count);
        AssertOption(options, "A", 2, 40m, isCorrect: true);
        AssertOption(options, "B", 1, 20m, isCorrect: false);
        AssertOption(options, "C", 1, 20m, isCorrect: false);
        AssertOption(options, "D", 0, 0m, isCorrect: false);
        AssertOption(options, "Không trả lời", 1, 20m, isCorrect: false);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_IncludesUnexpectedSelectedOption()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion(
            options: ["A", "B", "C", "D"],
            correctAnswer: "A");
        var set = environment.AddQuiz(questions: question);
        AddSelections(environment, set, question, "E");

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            set.Id,
            days: 30);

        Assert.Contains(
            result!.Questions.Single().Options,
            option => option.Option == "E"
                && option.SelectionCount == 1
                && option.SelectionRate == 100m);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_LimitsShortAnswerGroupsAndCombinesRemainder()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddShortAnswerQuestion(correctAnswer: "middleware");
        var set = environment.AddQuiz(questions: question);
        var selections = Enumerable.Range(1, 10)
            .Select(index => $"Câu trả lời {index}")
            .Cast<string?>()
            .ToArray();
        AddSelections(environment, set, question, selections);

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            set.Id,
            days: 30);

        var options = result!.Questions.Single().Options;
        Assert.Equal(9, options.Count);
        var remainder = options.Single(option => option.Option == "Các câu trả lời khác");
        Assert.Equal(2, remainder.SelectionCount);
        Assert.Equal(20m, remainder.SelectionRate);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_OrdersHardestQuestionsFirst()
    {
        var environment = new LearningTestEnvironment();
        var easy = environment.AddMultipleChoiceQuestion(
            prompt: "Câu dễ có nội dung gì?",
            options: ["A", "B", "C", "D"],
            correctAnswer: "A");
        var hard = environment.AddMultipleChoiceQuestion(
            prompt: "Câu khó có nội dung gì?",
            options: ["A", "B", "C", "D"],
            correctAnswer: "A");
        var set = environment.AddQuiz(questions: [easy, hard]);
        for (var index = 0; index < 5; index++)
        {
            environment.AddAttempt(
                set,
                selectedAnswers: new Dictionary<int, string?>
                {
                    [easy.Id] = "A",
                    [hard.Id] = index == 0 ? "A" : "B"
                });
        }

        var result = await environment.Service.GetQuizAnalyticsAsync(
            environment.Lecturer.Id,
            set.Id,
            days: 30);

        Assert.Equal([hard.Id, easy.Id], result!.Questions.Select(question => question.QuestionId));
    }

    private static void AddSelections(
        LearningTestEnvironment environment,
        LearningSet set,
        QuestionBankItem question,
        params string?[] selections)
    {
        for (var index = 0; index < selections.Length; index++)
        {
            environment.AddAttempt(
                set,
                index % 2 == 0 ? environment.Student : environment.OtherStudent,
                selectedAnswers: new Dictionary<int, string?>
                {
                    [question.Id] = selections[index]
                });
        }
    }

    private static void AssertOption(
        IReadOnlyList<OptionSelectionAnalyticsDto> options,
        string label,
        int count,
        decimal rate,
        bool isCorrect)
    {
        var option = options.Single(item => item.Option == label);
        Assert.Equal(count, option.SelectionCount);
        Assert.Equal(rate, option.SelectionRate);
        Assert.Equal(isCorrect, option.IsCorrect);
    }
}
