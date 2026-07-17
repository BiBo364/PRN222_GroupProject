using System.Text.Json;
using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class QuizVersionHistoryTests
{
    [Fact]
    public async Task GetQuizVersionHistoryAsync_ReturnsNullForStudent()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Student.Id,
            set.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_ReturnsNullForOtherSubjectQuiz()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(
            environment.OtherSubject,
            environment.OtherLecturer);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_ReturnsNullForNonQuizActivity()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddLearningSet(LearningActivityTypes.Flashcard);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_CreatesInitialSnapshotWhenHistoryIsEmpty()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion();
        var second = environment.AddTrueFalseQuestion();
        var set = environment.AddQuiz(questions: [first, second]);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.NotNull(result);
        Assert.Equal(set.Id, result.LearningSetId);
        Assert.Equal(set.Title, result.LearningSetTitle);
        var version = Assert.Single(result.Versions);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(set.Title, version.Title);
        Assert.Equal(2, version.QuestionCount);
        Assert.Equal(set.IsPublished, version.IsPublished);
        Assert.Equal(set.DurationMinutes, version.DurationMinutes);
        Assert.Single(environment.LearningRepository.Versions);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_MapsCreatorFullName()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var version = AddVersion(environment, set, versionNumber: 1);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        var dto = Assert.Single(result!.Versions);
        Assert.Equal(version.Id, dto.Id);
        Assert.Equal(environment.Lecturer.FullName, dto.CreatedBy);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_FallsBackToUsernameWhenFullNameBlank()
    {
        var environment = new LearningTestEnvironment();
        environment.Lecturer.FullName = " ";
        var set = environment.AddQuiz();
        AddVersion(environment, set, versionNumber: 1);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Equal(environment.Lecturer.Username, result!.Versions.Single().CreatedBy);
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_UsesDefaultChangeSummary()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        AddVersion(environment, set, versionNumber: 1, changeSummary: null);

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.False(string.IsNullOrWhiteSpace(result!.Versions.Single().ChangeSummary));
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_OrdersLatestVersionFirst()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        AddVersion(environment, set, 1, title: "Phiên bản 1");
        AddVersion(environment, set, 3, title: "Phiên bản 3");
        AddVersion(environment, set, 2, title: "Phiên bản 2");

        var result = await environment.Service.GetQuizVersionHistoryAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Equal([3, 2, 1], result!.Versions.Select(version => version.VersionNumber));
        Assert.Equal(["Phiên bản 3", "Phiên bản 2", "Phiên bản 1"],
            result.Versions.Select(version => version.Title));
    }

    [Fact]
    public async Task GetQuizVersionHistoryAsync_RejectsMalformedSnapshot()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        environment.LearningRepository.Versions.Add(new LearningSetVersion
        {
            Id = 1,
            LearningSetId = set.Id,
            LearningSet = set,
            VersionNumber = 1,
            SnapshotJson = "{not-json",
            CreatedByUserId = environment.Lecturer.Id,
            CreatedByUser = environment.Lecturer,
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GetQuizVersionHistoryAsync(
                environment.Lecturer.Id,
                set.Id));
    }

    [Fact]
    public async Task RestoreQuizVersionAsync_RequiresAssignedLecturer()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var version = AddVersion(environment, set, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.RestoreQuizVersionAsync(
                environment.Student.Id,
                set.Id,
                version.Id));
    }

    [Fact]
    public async Task RestoreQuizVersionAsync_RejectsUnknownVersion()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.RestoreQuizVersionAsync(
                environment.Lecturer.Id,
                set.Id,
                versionId: 999));
    }

    [Fact]
    public async Task RestoreQuizVersionAsync_RestoresMetadataAndForcesDraft()
    {
        var environment = new LearningTestEnvironment();
        var question = environment.AddMultipleChoiceQuestion();
        var set = environment.AddQuiz(isPublished: true, questions: question);
        var version = AddVersion(
            environment,
            set,
            1,
            title: "Tiêu đề cũ",
            description: "Mô tả cũ",
            instructions: "Hướng dẫn cũ",
            durationMinutes: 45,
            isPublished: true,
            shuffleQuestions: false,
            shuffleOptions: false);
        set.Title = "Tiêu đề hiện tại";
        set.Description = "Mô tả hiện tại";
        set.Instructions = "Hướng dẫn hiện tại";
        set.DurationMinutes = 10;
        set.IsPublished = true;

        await environment.Service.RestoreQuizVersionAsync(
            environment.Lecturer.Id,
            set.Id,
            version.Id);

        Assert.Equal("Tiêu đề cũ", set.Title);
        Assert.Equal("Mô tả cũ", set.Description);
        Assert.Equal("Hướng dẫn cũ", set.Instructions);
        Assert.Equal(45, set.DurationMinutes);
        Assert.False(set.IsPublished);
        Assert.False(set.ShuffleQuestions);
        Assert.False(set.ShuffleOptions);
    }

    [Fact]
    public async Task RestoreQuizVersionAsync_RestoresQuestionContentAndOrder()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddMultipleChoiceQuestion(prompt: "Câu hiện tại một?");
        var second = environment.AddTrueFalseQuestion(prompt: "Câu hiện tại hai?");
        var set = environment.AddQuiz(questions: [first, second]);
        var version = AddVersion(
            environment,
            set,
            1,
            questionSnapshots:
            [
                SnapshotQuestion(second, 1, "Câu cũ thứ nhất?", 3m),
                SnapshotQuestion(first, 2, "Câu cũ thứ hai?", 2m)
            ]);

        await environment.Service.RestoreQuizVersionAsync(
            environment.Lecturer.Id,
            set.Id,
            version.Id);

        Assert.Equal(2, set.Items.Count);
        Assert.Collection(
            set.Items.OrderBy(item => item.OrderIndex),
            item =>
            {
                Assert.Equal(second.Id, item.QuestionBankItemId);
                Assert.Equal("Câu cũ thứ nhất?", item.QuestionBankItem.Prompt);
                Assert.Equal(3m, item.Points);
            },
            item =>
            {
                Assert.Equal(first.Id, item.QuestionBankItemId);
                Assert.Equal("Câu cũ thứ hai?", item.QuestionBankItem.Prompt);
                Assert.Equal(2m, item.Points);
            });
    }

    [Fact]
    public async Task RestoreQuizVersionAsync_CreatesBackupAndRestoredVersion()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();
        var target = AddVersion(environment, set, 1, title: "Bản cần khôi phục");
        set.Title = "Bản hiện tại khác";

        await environment.Service.RestoreQuizVersionAsync(
            environment.Lecturer.Id,
            set.Id,
            target.Id);

        Assert.Equal(3, environment.LearningRepository.Versions.Count);
        Assert.Equal([1, 2, 3],
            environment.LearningRepository.Versions
                .OrderBy(version => version.VersionNumber)
                .Select(version => version.VersionNumber));
        Assert.Contains(
            environment.LearningRepository.Versions,
            version => version.ChangeSummary!.Contains("Sao lưu", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            environment.LearningRepository.Versions,
            version => version.ChangeSummary!.Contains("Khôi phục", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RepeatedVersionRequest_DoesNotCreateDuplicateSnapshot()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz();

        await environment.Service.GetQuizVersionHistoryAsync(environment.Lecturer.Id, set.Id);
        await environment.Service.GetQuizVersionHistoryAsync(environment.Lecturer.Id, set.Id);

        Assert.Single(environment.LearningRepository.Versions);
    }

    private static LearningSetVersion AddVersion(
        LearningTestEnvironment environment,
        LearningSet set,
        int versionNumber,
        string? title = null,
        string? description = "Mô tả",
        string? instructions = "Hướng dẫn",
        int durationMinutes = 15,
        bool isPublished = true,
        bool shuffleQuestions = true,
        bool shuffleOptions = true,
        string? changeSummary = "Lưu phiên bản",
        IReadOnlyList<object>? questionSnapshots = null)
    {
        questionSnapshots ??= set.Items
            .OrderBy(item => item.OrderIndex)
            .Select(item => SnapshotQuestion(
                item.QuestionBankItem,
                item.OrderIndex,
                item.QuestionBankItem.Prompt,
                item.Points))
            .ToList();
        var snapshot = new
        {
            title = title ?? set.Title,
            description,
            instructions,
            durationMinutes,
            isPublished,
            shuffleQuestions,
            shuffleOptions,
            questions = questionSnapshots
        };
        var version = new LearningSetVersion
        {
            Id = environment.LearningRepository.Versions
                .Select(item => item.Id)
                .DefaultIfEmpty()
                .Max() + 1,
            LearningSetId = set.Id,
            LearningSet = set,
            VersionNumber = versionNumber,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            ChangeSummary = changeSummary,
            CreatedByUserId = environment.Lecturer.Id,
            CreatedByUser = environment.Lecturer,
            CreatedAt = DateTime.UtcNow.AddMinutes(versionNumber)
        };
        environment.LearningRepository.Versions.Add(version);
        return version;
    }

    private static object SnapshotQuestion(
        QuestionBankItem question,
        int orderIndex,
        string prompt,
        decimal points)
    {
        return new
        {
            questionId = question.Id,
            orderIndex,
            questionType = question.QuestionType,
            prompt,
            options = string.IsNullOrWhiteSpace(question.OptionsJson)
                ? (IReadOnlyList<string>)Array.Empty<string>()
                : JsonSerializer.Deserialize<List<string>>(question.OptionsJson)
                    ?? [],
            correctAnswer = question.CorrectAnswer,
            explanation = question.Explanation,
            difficulty = question.Difficulty,
            topic = question.Topic,
            points
        };
    }
}
