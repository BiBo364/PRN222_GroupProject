using RagEdu.Tests.Fixtures;
using RagEdu.Tests.Generated;

namespace RagEdu.Tests.Services;

public sealed class QuizAnswerRegressionMatrixTests
{
    [Theory]
    [MemberData(
        nameof(QuizAnswerRegressionScenarios.All),
        MemberType = typeof(QuizAnswerRegressionScenarios))]
    public async Task SubmitQuizAsync_NormalizesVietnameseAndTechnicalAnswers(
        AnswerNormalizationScenario scenario)
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddShortAnswerQuestion(
            prompt: $"Nhập thuật ngữ chính xác cho kịch bản {scenario.Id}.",
            correctAnswer: scenario.CorrectAnswer);
        var quiz = environment.AddQuiz(
            isPublished: true,
            title: $"Ma trận hồi quy {scenario.Id}",
            questions: question);
        var request = new SubmitLearningAttemptRequest
        {
            LearningSetId = quiz.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-1),
            Answers = new Dictionary<int, string?>
            {
                [question.Id] = scenario.SelectedAnswer
            }
        };

        var result = await environment.Service.SubmitQuizAsync(
            environment.Student.Id,
            request);

        var answer = Assert.Single(result.Answers);
        Assert.Equal(scenario.ExpectedCorrect, answer.IsCorrect);
        Assert.Equal(scenario.ExpectedCorrect ? 1 : 0, result.CorrectCount);
        Assert.Equal(scenario.ExpectedCorrect ? 100m : 0m, result.Percentage);
        Assert.Equal(scenario.SelectedAnswer, answer.SelectedAnswer);

        var persistedAttempt = Assert.Single(environment.LearningRepository.Attempts);
        var persistedAnswer = Assert.Single(persistedAttempt.Answers);
        Assert.Equal(scenario.ExpectedCorrect, persistedAnswer.IsCorrect);
        Assert.Equal(scenario.SelectedAnswer, persistedAnswer.SelectedAnswer);
    }

    [Fact]
    public void Catalog_HasStableUniqueIdentifiers()
    {
        var scenarios = QuizAnswerRegressionScenarios.All
            .Select(row => Assert.IsType<AnswerNormalizationScenario>(Assert.Single(row)))
            .ToList();

        Assert.Equal(900, scenarios.Count);
        Assert.Equal(
            scenarios.Count,
            scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(scenarios, scenario =>
        {
            Assert.StartsWith("answer-", scenario.Id, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(scenario.CorrectAnswer));
            Assert.False(string.IsNullOrWhiteSpace(scenario.SelectedAnswer));
        });
    }

    [Fact]
    public void Catalog_CoversPositiveAndNegativeNormalizationBoundaries()
    {
        var scenarios = QuizAnswerRegressionScenarios.All
            .Select(row => Assert.IsType<AnswerNormalizationScenario>(Assert.Single(row)))
            .ToList();

        Assert.Equal(720, scenarios.Count(scenario => scenario.ExpectedCorrect));
        Assert.Equal(180, scenarios.Count(scenario => !scenario.ExpectedCorrect));
        Assert.Contains(scenarios, scenario =>
            scenario.SelectedAnswer.Any(character => character > 127));
        Assert.Contains(scenarios, scenario =>
            scenario.SelectedAnswer.Contains('-', StringComparison.Ordinal));
        Assert.Contains(scenarios, scenario =>
            scenario.SelectedAnswer.Contains('\t')
            || scenario.SelectedAnswer.Contains('\n'));
        Assert.Contains(scenarios, scenario =>
            !string.Equals(
                scenario.CorrectAnswer,
                scenario.SelectedAnswer,
                StringComparison.Ordinal)
            && scenario.ExpectedCorrect);
    }
}
