using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        IReadOnlyCollection<ChatMessageDto> recentHistory,
        CancellationToken cancellationToken = default);
}