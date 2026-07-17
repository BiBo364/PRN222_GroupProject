namespace Assignment1_Repository.Models;

public static class LearningActivityTypes
{
    public const string Quiz = "quiz";
    public const string Flashcard = "flashcard";
    public const string Matching = "matching";
    public const string SpeedChallenge = "speed_challenge";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Quiz, Flashcard, Matching, SpeedChallenge],
        StringComparer.OrdinalIgnoreCase);
}

public static class LearningQuestionTypes
{
    public const string MultipleChoice = "multiple_choice";
    public const string TrueFalse = "true_false";
    public const string ShortAnswer = "short_answer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [MultipleChoice, TrueFalse, ShortAnswer],
        StringComparer.OrdinalIgnoreCase);
}

public static class LearningDifficultyLevels
{
    public const string Easy = "easy";
    public const string Medium = "medium";
    public const string Hard = "hard";
    public const string Mixed = "mixed";

    public static readonly IReadOnlySet<string> QuestionLevels = new HashSet<string>(
        [Easy, Medium, Hard],
        StringComparer.OrdinalIgnoreCase);
}
