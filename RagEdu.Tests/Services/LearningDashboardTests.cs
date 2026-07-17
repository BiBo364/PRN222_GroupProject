using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class LearningDashboardTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        var environment = new LearningTestEnvironment();

        var result = await environment.Service.GetDashboardAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenUserIsInactive()
    {
        var environment = new LearningTestEnvironment();
        environment.Student.IsActive = false;

        var result = await environment.Service.GetDashboardAsync(environment.Student.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsGlobalCatalog_ForStudentWithoutSubject()
    {
        var environment = new LearningTestEnvironment();
        environment.AddQuiz(subject: environment.Subject, title: "Quiz PRN222");
        environment.AddQuiz(subject: environment.OtherSubject, title: "Quiz SWT301");

        var result = await environment.Service.GetDashboardAsync(environment.Student.Id);

        Assert.NotNull(result);
        Assert.True(result.IsGlobalCatalog);
        Assert.False(result.CanManage);
        Assert.Null(result.Subject);
        Assert.Equal(2, result.SubjectCount);
        Assert.Equal(2, result.LearningSets.Count);
    }

    [Fact]
    public async Task GetDashboardAsync_OnlyIncludesPublishedAndActiveSets_ForStudent()
    {
        var environment = new LearningTestEnvironment();
        var published = environment.AddQuiz(isPublished: true, title: "Đã phát hành");
        environment.AddQuiz(isPublished: false, title: "Bản nháp");
        environment.AddQuiz(isPublished: true, isDeleted: true, title: "Đã xóa");

        var result = await environment.Service.GetDashboardAsync(environment.Student.Id);

        var item = Assert.Single(result!.LearningSets);
        Assert.Equal(published.Id, item.Id);
        Assert.Equal("Đã phát hành", item.Title);
    }

    [Fact]
    public async Task GetDashboardAsync_MapsRecentAttemptsAcrossSubjects_ForStudent()
    {
        var environment = new LearningTestEnvironment();
        var first = environment.AddQuiz(subject: environment.Subject);
        var second = environment.AddQuiz(subject: environment.OtherSubject);
        environment.AddAttempt(first, score: 1m, totalPoints: 2m);
        environment.AddAttempt(second, score: 3m, totalPoints: 4m);

        var result = await environment.Service.GetDashboardAsync(environment.Student.Id);

        Assert.Equal(2, result!.RecentAttempts.Count);
        Assert.Contains(result.RecentAttempts, attempt =>
            attempt.LearningSetId == first.Id
            && attempt.Percentage == 50m
            && attempt.SubjectCode == environment.Subject.Code);
        Assert.Contains(result.RecentAttempts, attempt =>
            attempt.LearningSetId == second.Id
            && attempt.Percentage == 75m
            && attempt.SubjectCode == environment.OtherSubject.Code);
        Assert.Null(environment.LearningRepository.LastAttemptSubjectId);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsManagementData_ForLecturer()
    {
        var environment = new LearningTestEnvironment();
        var quizQuestion = environment.AddMultipleChoiceQuestion(isActive: true);
        environment.AddTrueFalseQuestion(isActive: true);
        environment.AddShortAnswerQuestion(isActive: false);
        environment.AddQuiz(
            isPublished: false,
            title: "Quiz bản nháp",
            questions: quizQuestion);

        var result = await environment.Service.GetDashboardAsync(environment.Lecturer.Id);

        Assert.NotNull(result);
        Assert.True(result.CanManage);
        Assert.False(result.IsGlobalCatalog);
        Assert.Equal(environment.Subject.Id, result.Subject!.Id);
        Assert.Equal(2, result.ActiveQuestionCount);
        Assert.Single(result.LearningSets);
        Assert.False(result.LearningSets[0].IsPublished);
        Assert.True(environment.LearningRepository.LastIncludeUnpublished);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenLecturerHasNoSubject()
    {
        var environment = new LearningTestEnvironment();
        environment.Lecturer.SubjectId = null;
        environment.Lecturer.Subject = null;

        var result = await environment.Service.GetDashboardAsync(environment.Lecturer.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenAssignedSubjectWasDeleted()
    {
        var environment = new LearningTestEnvironment();
        environment.Subject.IsDeleted = true;

        var result = await environment.Service.GetDashboardAsync(environment.Lecturer.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGenerationOptionsAsync_MapsDocumentsAndChapters()
    {
        var environment = new LearningTestEnvironment();
        var chapterTwo = environment.AddChapter(number: 2, title: "Razor Pages");
        var chapterOne = environment.AddChapter(number: 1, title: "Middleware");
        environment.AddIndexedDocument(chapter: chapterTwo, name: "Razor.pdf");
        environment.AddIndexedDocument(chapter: chapterOne, name: "Middleware.pdf");

        var result = await environment.Service.GetGenerationOptionsAsync(environment.Lecturer.Id);

        Assert.NotNull(result);
        Assert.Equal(environment.Subject.Code, result.Subject.Code);
        Assert.Equal(2, result.Documents.Count);
        Assert.Collection(
            result.Chapters,
            first =>
            {
                Assert.Equal(1, first.Number);
                Assert.Equal("Middleware", first.Title);
            },
            second =>
            {
                Assert.Equal(2, second.Number);
                Assert.Equal("Razor Pages", second.Title);
            });
    }

    [Fact]
    public async Task GetGenerationOptionsAsync_ReturnsNull_ForStudent()
    {
        var environment = new LearningTestEnvironment();

        var result = await environment.Service.GetGenerationOptionsAsync(environment.Student.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestionBankAsync_NormalizesInvalidFilters()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion();

        var result = await environment.Service.GetQuestionBankAsync(
            environment.Lecturer.Id,
            "ASP.NET",
            "unknown",
            "unknown",
            includeInactive: false);

        Assert.NotNull(result);
        Assert.Null(environment.LearningRepository.LastDifficulty);
        Assert.Null(environment.LearningRepository.LastQuestionType);
        Assert.Equal("ASP.NET", environment.LearningRepository.LastSearch);
        Assert.True(environment.LearningRepository.LastActiveOnly);
    }

    [Fact]
    public async Task GetQuestionBankAsync_IncludesInactive_WhenRequested()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion(isActive: true);
        environment.AddMultipleChoiceQuestion(isActive: false);

        var result = await environment.Service.GetQuestionBankAsync(
            environment.Lecturer.Id,
            null,
            null,
            null,
            includeInactive: true);

        Assert.Equal(2, result!.Questions.Count);
        Assert.False(environment.LearningRepository.LastActiveOnly);
    }

    [Fact]
    public async Task GetComposeOptionsAsync_GroupsAvailableQuestions()
    {
        var environment = new LearningTestEnvironment();
        environment.AddMultipleChoiceQuestion(difficulty: LearningDifficultyLevels.Easy);
        environment.AddMultipleChoiceQuestion(difficulty: LearningDifficultyLevels.Hard);
        environment.AddTrueFalseQuestion(difficulty: LearningDifficultyLevels.Easy);
        environment.AddShortAnswerQuestion(difficulty: LearningDifficultyLevels.Hard);
        environment.AddShortAnswerQuestion(isActive: false);

        var result = await environment.Service.GetComposeOptionsAsync(environment.Lecturer.Id);

        Assert.NotNull(result);
        Assert.Equal(4, result.AvailableQuestionCount);
        Assert.Equal(2, result.CountByDifficulty[LearningDifficultyLevels.Easy]);
        Assert.Equal(2, result.CountByDifficulty[LearningDifficultyLevels.Hard]);
        Assert.Equal(2, result.CountByQuestionType[LearningQuestionTypes.MultipleChoice]);
        Assert.Equal(1, result.CountByQuestionType[LearningQuestionTypes.TrueFalse]);
        Assert.Equal(1, result.CountByQuestionType[LearningQuestionTypes.ShortAnswer]);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public async Task GetLearningSetAsync_AppliesRoleAccessRules(int roleId, bool expectedVisible)
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: true);
        var user = roleId switch
        {
            1 => environment.Administrator,
            2 => environment.Lecturer,
            3 => environment.Student,
            _ => new User
            {
                Id = 99,
                Username = "guest-role",
                Email = "guest@example.edu.vn",
                Password = "test",
                FullName = "Vai trò khác",
                RoleId = roleId,
                IsActive = true
            }
        };
        if (roleId == 4)
            environment.UserRepository.Users.Add(user);

        var result = await environment.Service.GetLearningSetAsync(user.Id, set.Id);

        Assert.Equal(expectedVisible, result is not null);
    }

    [Fact]
    public async Task GetLearningSetAsync_AllowsAnyStudentToStudyPublishedQuiz()
    {
        var environment = new LearningTestEnvironment();
        environment.Student.SubjectId = environment.OtherSubject.Id;
        environment.Student.Subject = environment.OtherSubject;
        var set = environment.AddQuiz(subject: environment.Subject, isPublished: true);

        var result = await environment.Service.GetLearningSetAsync(
            environment.Student.Id,
            set.Id);

        Assert.NotNull(result);
        Assert.False(result.CanManage);
        Assert.Equal(environment.Subject.Id, result.SubjectId);
    }

    [Fact]
    public async Task GetLearningSetAsync_HidesUnpublishedQuizFromStudent()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(isPublished: false);

        var result = await environment.Service.GetLearningSetAsync(
            environment.Student.Id,
            set.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLearningSetAsync_HidesOtherLecturerDraft()
    {
        var environment = new LearningTestEnvironment();
        var set = environment.AddQuiz(
            subject: environment.OtherSubject,
            creator: environment.OtherLecturer,
            isPublished: false);

        var result = await environment.Service.GetLearningSetAsync(
            environment.Lecturer.Id,
            set.Id);

        Assert.Null(result);
    }
}
