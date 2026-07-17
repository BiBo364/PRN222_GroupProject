namespace RagEdu.Tests.Generated;

public sealed class AnswerNormalizationScenario
{
    public required string Id { get; init; }
    public required string CorrectAnswer { get; init; }
    public required string SelectedAnswer { get; init; }
    public bool ExpectedCorrect { get; init; }

    public override string ToString()
    {
        return $"{Id}: '{SelectedAnswer}' => '{CorrectAnswer}'";
    }
}
