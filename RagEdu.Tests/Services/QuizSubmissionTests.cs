using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class QuizSubmissionTests
{
    [Fact]
    public async Task SubmitQuizAsync_RejectsUnknownUser()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SubmitQuizAsync(
                999,
                ValidRequest(set)));

        Assert.Empty(environment.LearningRepository.Attempts);
    }

    [Fact]
    public async Task SubmitQuizAsync_RejectsUnknownLearningSet()
    {
        var environment = new LearningTestEnvironment();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SubmitQuizAsync(
                environment.Student.Id,
                new SubmitLearningAttemptRequest
                {
                    LearningSetId = 999,
                    StartedAt = DateTime.UtcNow.AddMinutes(-5)
                }));
    }

    [Fact]
    public async Task SubmitQuizAsync_AllowsStudentWithoutSubjectToTakePublishedQuiz()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(subject: environment.OtherSubject);

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            ValidRequest(set));

        Assert.Equal(set.Items.Count, result.TotalQuestions);
        Assert.Equal(1, environment.LearningRepository.AddAttemptCallCount);
    }

    [Fact]
    public async Task SubmitQuizAsync_RejectsUnpublishedQuizForStudent()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SubmitQuizAsync(
                environment.Student.Id,
                ValidRequest(set)));

        Assert.Empty(environment.LearningRepository.Attempts);
    }

    [Fact]
    public async Task SubmitQuizAsync_AllowsAssignedLecturerToPreviewDraftQuiz()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: false);

        var result = await environment.Service.SubmitQuizAsync(
            environment.Lecturer.Id,
            ValidRequest(set));

        Assert.Equal(100m, result.Percentage);
    }

    [Fact]
    public async Task SubmitQuizAsync_RejectsOtherLecturerDraft()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(
            subject: environment.OtherSubject,
            creator: environment.OtherLecturer,
            isPublished: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SubmitQuizAsync(
                environment.Lecturer.Id,
                ValidRequest(set)));
    }

    [Fact]
    public async Task SubmitQuizAsync_RejectsNonQuizActivity()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddLearningSet(LearningActivityTypes.Flashcard);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.SubmitQuizAsync(
                environment.Student.Id,
                ValidRequest(set)));
    }

    [Fact]
    public async Task SubmitQuizAsync_AwardsConfiguredPointsForCorrectAnswers()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion(correctAnswer: "ASP.NET Core");
        var second = environment.AddTrueFalseQuestion(correctAnswer: "Đúng");
        var set = environment.AddQuiz(questions: [first, second]);
        set.Items.ElementAt(0).Points = 2.5m;
        set.Items.ElementAt(1).Points = 4m;
        var request = new SubmitLearningAttemptRequest
        {
            LearningSetId = set.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            Answers = new Dictionary<int, string?>
            {
                [first.Id] = "ASP.NET Core",
                [second.Id] = "Đúng"
            }
        };

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            request);

        Assert.Equal(6.5m, result.Score);
        Assert.Equal(6.5m, result.TotalPoints);
        Assert.Equal(2, result.CorrectCount);
        Assert.Equal(100m, result.Percentage);
        Assert.All(result.Answers, answer => Assert.True(answer.IsCorrect));
    }

    [Fact]
    public async Task SubmitQuizAsync_UsesWeightedPercentage()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion();
        var second = environment.AddTrueFalseQuestion();
        var set = environment.AddQuiz(questions: [first, second]);
        set.Items.ElementAt(0).Points = 1m;
        set.Items.ElementAt(1).Points = 3m;
        var request = new SubmitLearningAttemptRequest
        {
            LearningSetId = set.Id,
            Answers = new Dictionary<int, string?>
            {
                [first.Id] = first.CorrectAnswer,
                [second.Id] = "Sai"
            }
        };

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            request);

        Assert.Equal(1m, result.Score);
        Assert.Equal(4m, result.TotalPoints);
        Assert.Equal(25m, result.Percentage);
        Assert.Equal(1, result.CorrectCount);
    }

    [Theory]
    [InlineData("Đúng")]
    [InlineData("đúng")]
    [InlineData("DUNG")]
    [InlineData("  Dung  ")]
    public async Task SubmitQuizAsync_MatchesVietnameseAnswerIgnoringCaseAndDiacritics(
        string answer)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddTrueFalseQuestion(correctAnswer: "Đúng");
        var set = environment.AddQuiz(questions: question);

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                Answers = new Dictionary<int, string?> { [question.Id] = answer }
            });

        Assert.Equal(100m, result.Percentage);
        Assert.True(result.Answers[0].IsCorrect);
    }

    [Theory]
    [InlineData("Dependency Injection")]
    [InlineData("dependency injection")]
    [InlineData("DEPENDENCY-INJECTION")]
    [InlineData("dependency   injection")]
    public async Task SubmitQuizAsync_NormalizesPunctuationAndWhitespace(string answer)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddShortAnswerQuestion(
            correctAnswer: "dependency injection");
        var set = environment.AddQuiz(questions: question);

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                Answers = new Dictionary<int, string?> { [question.Id] = answer }
            });

        Assert.True(result.Answers[0].IsCorrect);
    }

    [Fact]
    public async Task SubmitQuizAsync_RecordsMissingAnswerAsIncorrect()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(questions: question);

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                Answers = new Dictionary<int, string?>()
            });

        Assert.Equal(0m, result.Score);
        Assert.Equal(0, result.CorrectCount);
        Assert.Null(result.Answers[0].SelectedAnswer);
        var persisted = Assert.Single(environment.LearningRepository.Attempts);
        Assert.Null(persisted.Answers.Single().SelectedAnswer);
    }

    [Fact]
    public async Task SubmitQuizAsync_IgnoresAnswersForQuestionsOutsideQuiz()
    {
        var environment = new LearningTestEnvironment();
        var included = environment.AddMultipleChoiceQuestion();
        var outside = environment.AddTrueFalseQuestion();
        var set = environment.AddQuiz(questions: included);
        var request = new SubmitLearningAttemptRequest
        {
            LearningSetId = set.Id,
            Answers = new Dictionary<int, string?>
            {
                [included.Id] = included.CorrectAnswer,
                [outside.Id] = outside.CorrectAnswer
            }
        };

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            request);

        Assert.Single(result.Answers);
        Assert.Equal(included.Id, result.Answers[0].QuestionId);
        Assert.Single(environment.LearningRepository.Attempts[0].Answers);
    }

    [Fact]
    public async Task SubmitQuizAsync_UsesCurrentTimeWhenStartedAtIsDefault()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var before = DateTime.UtcNow;

        await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                StartedAt = default
            });

        var attempt = environment.LearningRepository.Attempts[0];
        Assert.InRange(attempt.StartedAt, before, DateTime.UtcNow);
        Assert.Equal(attempt.StartedAt, attempt.CompletedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SubmitQuizAsync_ConvertsProvidedStartTimeToUtc()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var localStart = new DateTimeOffset(2026, 7, 17, 8, 0, 0, TimeSpan.FromHours(7));

        await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                StartedAt = localStart.DateTime
            });

        Assert.Equal(
            localStart.DateTime.ToUniversalTime(),
            environment.LearningRepository.Attempts[0].StartedAt);
    }

    [Fact]
    public async Task SubmitQuizAsync_ReturnsZeroPercentageWhenQuizHasZeroPoints()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(questions: question);
        set.Items.ElementAt(0).Points = 0m;

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            new SubmitLearningAttemptRequest
            {
                LearningSetId = set.Id,
                Answers = new Dictionary<int, string?>
                {
                    [question.Id] = question.CorrectAnswer
                }
            });

        Assert.Equal(0m, result.TotalPoints);
        Assert.Equal(0m, result.Percentage);
        Assert.Equal(1, result.CorrectCount);
    }

    [Fact]
    public async Task SubmitQuizAsync_PersistsAnswerAuditFields()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(questions: question);
        var before = DateTime.UtcNow;

        await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            ValidRequest(set));

        var attempt = Assert.Single(environment.LearningRepository.Attempts);
        var answer = Assert.Single(attempt.Answers);
        Assert.Equal(attempt.Id, answer.LearningAttemptId);
        Assert.Equal(question.Id, answer.QuestionBankItemId);
        Assert.True(answer.IsCorrect);
        Assert.InRange(answer.AnsweredAt, before, DateTime.UtcNow);
    }

    private static SubmitLearningAttemptRequest ValidRequest(LearningSet set)
    {
        return new SubmitLearningAttemptRequest
        {
            LearningSetId = set.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            Answers = set.Items.ToDictionary(
                item => item.QuestionBankItemId,
                item => (string?)item.QuestionBankItem.CorrectAnswer)
        };
    }
}
