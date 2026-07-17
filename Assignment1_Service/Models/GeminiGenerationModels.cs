namespace Assignment1_Service.Models;

public sealed record GeminiMessage(string Role, string Text);

public sealed class GenerateQuestionsAiRequest
{
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
    public IReadOnlyCollection<string> QuestionTypes { get; init; } = [];
    public string Difficulty { get; init; } = string.Empty;
    public string? Focus { get; init; }
}

public sealed class GeneratedQuestionDraft
{
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string LearningObjective { get; init; } = string.Empty;
    public string SourceReference { get; init; } = string.Empty;
}

public sealed class ComposeLearningSetAiRequest
{
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string ActivityType { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
    public string Difficulty { get; init; } = string.Empty;
    public string? Focus { get; init; }
    public IReadOnlyCollection<LearningQuestionCandidate> Candidates { get; init; } = [];
}

public sealed class LearningQuestionCandidate
{
    public int Id { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string? Topic { get; init; }
}

public sealed class GeneratedLearningSetPlan
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
    public IReadOnlyList<int> SelectedQuestionIds { get; init; } = [];
    public int? DurationMinutes { get; init; }
}
