using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IStudyContentAiService
{
    Task<IReadOnlyList<GeneratedQuestionDraft>> GenerateQuestionsAsync(
        GenerateQuestionsAiRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedLearningSetPlan> ComposeLearningSetAsync(
        ComposeLearningSetAiRequest request,
        CancellationToken cancellationToken = default);
}
