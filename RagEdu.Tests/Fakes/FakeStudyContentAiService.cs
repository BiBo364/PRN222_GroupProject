namespace RagEdu.Tests.Fakes;

internal sealed class FakeStudyContentAiService : IStudyContentAiService
{
    public IReadOnlyList<GeneratedQuestionDraft> GeneratedQuestions { get; set; } = [];
    public GeneratedLearningSetPlan LearningSetPlan { get; set; } = new();
    public Exception? GenerationException { get; set; }
    public Exception? CompositionException { get; set; }
    public int GenerationCallCount { get; private set; }
    public int CompositionCallCount { get; private set; }
    public GenerateQuestionsAiRequest? LastGenerationRequest { get; private set; }
    public ComposeLearningSetAiRequest? LastCompositionRequest { get; private set; }

    public Task<IReadOnlyList<GeneratedQuestionDraft>> GenerateQuestionsAsync(
        GenerateQuestionsAiRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GenerationCallCount++;
        LastGenerationRequest = request;

        if (GenerationException is not null)
            throw GenerationException;

        return Task.FromResult(GeneratedQuestions);
    }

    public Task<GeneratedLearningSetPlan> ComposeLearningSetAsync(
        ComposeLearningSetAiRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CompositionCallCount++;
        LastCompositionRequest = request;

        if (CompositionException is not null)
            throw CompositionException;

        return Task.FromResult(LearningSetPlan);
    }
}
