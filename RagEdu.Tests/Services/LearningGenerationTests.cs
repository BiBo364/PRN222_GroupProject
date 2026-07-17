using System.Text.Json;
using RagEdu.Tests.Fixtures;

namespace RagEdu.Tests.Services;

public sealed class LearningGenerationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task GenerateQuestionsAsync_RejectsQuestionCountOutsideRange(int count)
    {
        var environment = ReadyEnvironment();
        var request = ValidRequest(count);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Contains("1", exception.Message);
        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RejectsEmptyQuestionTypes()
    {
        var environment = ReadyEnvironment();
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 5,
            QuestionTypes = [],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RejectsUnknownQuestionType()
    {
        var environment = ReadyEnvironment();
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 5,
            QuestionTypes = ["essay"],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RejectsUnknownDifficulty()
    {
        var environment = ReadyEnvironment();
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 5,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = "expert"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RequiresLecturerAssignment()
    {
        var environment = ReadyEnvironment();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(
                environment.Student.Id,
                ValidRequest()));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RequiresIndexedDocument()
    {
        var environment = new LearningTestEnvironment();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(
                environment.Lecturer.Id,
                ValidRequest()));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RejectsDocumentFromOtherSubject()
    {
        var environment = ReadyEnvironment();
        var otherDocument = environment.AddIndexedDocument(subject: environment.OtherSubject);
        var request = new GenerateQuestionBankRequest
        {
            DocumentIds = [otherDocument.Id],
            QuestionCount = 1,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RejectsChapterFromOtherSubject()
    {
        var environment = ReadyEnvironment();
        var chapter = environment.AddChapter(environment.OtherSubject);
        var request = new GenerateQuestionBankRequest
        {
            ChapterId = chapter.Id,
            QuestionCount = 1,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RequiresSourceChunksInSelectedScope()
    {
        var environment = new LearningTestEnvironment();
        environment.AddIndexedDocument();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(
                environment.Lecturer.Id,
                ValidRequest()));

        Assert.Equal(0, environment.AiService.GenerationCallCount);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_PassesSubjectAndSourceContextToAi()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
            [LearningTestEnvironment.ValidMultipleChoiceDraft()];
        var request = ValidRequest();

        await environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request);

        var aiRequest = Assert.IsType<GenerateQuestionsAiRequest>(
            environment.AiService.LastGenerationRequest);
        Assert.Equal(environment.Subject.Code, aiRequest.SubjectCode);
        Assert.Equal(environment.Subject.Name, aiRequest.SubjectName);
        Assert.Equal(request.QuestionCount, aiRequest.QuestionCount);
        Assert.Contains("Giáo trình PRN222.pdf", aiRequest.Context);
        Assert.Contains("Nội dung kiểm thử", aiRequest.Context);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_SavesValidMultipleChoiceQuestion()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
            [LearningTestEnvironment.ValidMultipleChoiceDraft()];

        var result = await environment.Service.GenerateQuestionsAsync(
            environment.Lecturer.Id,
            ValidRequest());

        Assert.Equal(1, result.RequestedCount);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.SkippedCount);
        var question = Assert.Single(environment.LearningRepository.Questions);
        Assert.Equal(environment.Subject.Id, question.SubjectId);
        Assert.Equal(environment.Lecturer.Id, question.CreatedByUserId);
        Assert.True(question.IsAiGenerated);
        Assert.True(question.IsActive);
        Assert.Equal(environment.GeminiClient.ModelName, question.AiModel);
        Assert.Equal(4, JsonSerializer.Deserialize<List<string>>(question.OptionsJson!)!.Count);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_NormalizesCorrectAnswerCasing()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
        [
            LearningTestEnvironment.ValidMultipleChoiceDraft(correctAnswer: "asp.net core")
        ];

        await environment.Service.GenerateQuestionsAsync(
            environment.Lecturer.Id,
            ValidRequest());

        Assert.Equal("ASP.NET Core", environment.LearningRepository.Questions[0].CorrectAnswer);
    }

    [Theory]
    [InlineData("Đúng", "Đúng")]
    [InlineData("Dung", "Đúng")]
    [InlineData("DUNG", "Đúng")]
    [InlineData("Sai", "Sai")]
    [InlineData("SAI", "Sai")]
    public async Task GenerateQuestionsAsync_NormalizesVietnameseTrueFalseAnswer(
        string generatedAnswer,
        string expectedAnswer)
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
            [LearningTestEnvironment.ValidTrueFalseDraft(answer: generatedAnswer)];
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 1,
            QuestionTypes = [LearningQuestionTypes.TrueFalse],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        await environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request);

        var question = Assert.Single(environment.LearningRepository.Questions);
        Assert.Equal(expectedAnswer, question.CorrectAnswer);
        Assert.Equal(["Đúng", "Sai"], JsonSerializer.Deserialize<List<string>>(question.OptionsJson!));
    }

    [Fact]
    public async Task GenerateQuestionsAsync_RemovesOptionsFromShortAnswer()
    {
        var environment = ReadyEnvironment();
        var draft = LearningTestEnvironment.ValidShortAnswerDraft();
        draft = new GeneratedQuestionDraft
        {
            QuestionType = draft.QuestionType,
            Prompt = draft.Prompt,
            Options = ["Không nên được lưu", "Phương án thừa"],
            CorrectAnswer = draft.CorrectAnswer,
            Explanation = draft.Explanation,
            Difficulty = draft.Difficulty,
            Topic = draft.Topic,
            LearningObjective = draft.LearningObjective,
            SourceReference = draft.SourceReference
        };
        environment.AiService.GeneratedQuestions = [draft];
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 1,
            QuestionTypes = [LearningQuestionTypes.ShortAnswer],
            Difficulty = LearningDifficultyLevels.Hard
        };

        await environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request);

        Assert.Empty(JsonSerializer.Deserialize<List<string>>(
            environment.LearningRepository.Questions[0].OptionsJson!)!);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_SkipsDuplicatePromptIgnoringWhitespace()
    {
        var environment = ReadyEnvironment();
        environment.AddMultipleChoiceQuestion(
            prompt: "Framework nào được dùng cho Web .NET hiện đại?");
        environment.AiService.GeneratedQuestions =
        [
            LearningTestEnvironment.ValidMultipleChoiceDraft(
                prompt: "  Framework   nào được dùng cho Web .NET hiện đại?  "),
            LearningTestEnvironment.ValidMultipleChoiceDraft(
                prompt: "ASP.NET Core có chạy đa nền tảng hay không?")
        ];
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 2,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = LearningDifficultyLevels.Mixed
        };

        var result = await environment.Service.GenerateQuestionsAsync(
            environment.Lecturer.Id,
            request);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(2, environment.LearningRepository.Questions.Count);
    }

    [Theory]
    [MemberData(nameof(InvalidDrafts))]
    public async Task GenerateQuestionsAsync_SkipsInvalidDraft(GeneratedQuestionDraft draft)
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions = [draft];

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => environment.Service.GenerateQuestionsAsync(
                environment.Lecturer.Id,
                ValidRequest()));

        Assert.Empty(environment.LearningRepository.Questions);
    }

    public static TheoryData<GeneratedQuestionDraft> InvalidDrafts => new()
    {
        new GeneratedQuestionDraft
        {
            QuestionType = "essay",
            Prompt = "Đây là một câu hỏi đủ dài nhưng sai loại.",
            CorrectAnswer = "Đáp án",
            Difficulty = LearningDifficultyLevels.Medium
        },
        new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = "Ngắn",
            Options = ["A", "B", "C", "D"],
            CorrectAnswer = "A",
            Difficulty = LearningDifficultyLevels.Medium
        },
        new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = "Câu hỏi này có ít hơn bốn phương án phải không?",
            Options = ["A", "B", "C"],
            CorrectAnswer = "A",
            Difficulty = LearningDifficultyLevels.Medium
        },
        new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = "Câu hỏi này có đáp án không nằm trong phương án?",
            Options = ["A", "B", "C", "D"],
            CorrectAnswer = "E",
            Difficulty = LearningDifficultyLevels.Medium
        },
        new GeneratedQuestionDraft
        {
            QuestionType = LearningQuestionTypes.MultipleChoice,
            Prompt = "Câu hỏi này có phương án trùng lặp sau chuẩn hóa?",
            Options = ["A", "A", "C", "D"],
            CorrectAnswer = "A",
            Difficulty = LearningDifficultyLevels.Medium
        }
    };

    [Fact]
    public async Task GenerateQuestionsAsync_StopsAtRequestedCount()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions = Enumerable.Range(1, 5)
            .Select(index => LearningTestEnvironment.ValidMultipleChoiceDraft(
                prompt: $"Câu hỏi hợp lệ số {index} về ASP.NET Core là gì?"))
            .ToList();

        var result = await environment.Service.GenerateQuestionsAsync(
            environment.Lecturer.Id,
            ValidRequest(count: 2));

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(3, result.SkippedCount);
        Assert.Equal(2, environment.LearningRepository.Questions.Count);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_FallsBackToRequestedDifficulty()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
        [
            LearningTestEnvironment.ValidMultipleChoiceDraft(difficulty: "unknown")
        ];
        var request = new GenerateQuestionBankRequest
        {
            QuestionCount = 1,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = LearningDifficultyLevels.Hard
        };

        await environment.Service.GenerateQuestionsAsync(environment.Lecturer.Id, request);

        Assert.Equal(
            LearningDifficultyLevels.Hard,
            environment.LearningRepository.Questions[0].Difficulty);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_FallsBackToMedium_ForMixedDifficulty()
    {
        var environment = ReadyEnvironment();
        environment.AiService.GeneratedQuestions =
        [
            LearningTestEnvironment.ValidMultipleChoiceDraft(difficulty: "unknown")
        ];

        await environment.Service.GenerateQuestionsAsync(
            environment.Lecturer.Id,
            ValidRequest());

        Assert.Equal(
            LearningDifficultyLevels.Medium,
            environment.LearningRepository.Questions[0].Difficulty);
    }

    private static LearningTestEnvironment ReadyEnvironment()
    {
        var environment = new LearningTestEnvironment();
        var document = environment.AddIndexedDocument();
        environment.AddChunk(document, "Nội dung kiểm thử về ASP.NET Core và Razor Pages.");
        return environment;
    }

    private static GenerateQuestionBankRequest ValidRequest(int count = 1)
    {
        return new GenerateQuestionBankRequest
        {
            QuestionCount = count,
            QuestionTypes = [LearningQuestionTypes.MultipleChoice],
            Difficulty = LearningDifficultyLevels.Mixed,
            Focus = "Kiến trúc Web"
        };
    }
}
