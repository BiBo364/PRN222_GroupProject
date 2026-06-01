using Assignment1_Repository.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IEmbeddingService
{
    Task<Dictionary<int, List<float[]>>> GenerateDocumentEmbeddingsAsync(
        IReadOnlyList<string> texts,
        IReadOnlyCollection<EmbeddingModel> models,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, float[]>> GenerateQueryEmbeddingsAsync(
        string text,
        IReadOnlyCollection<EmbeddingModel> models,
        CancellationToken cancellationToken = default);
}
