using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IGeminiClient
{
    string ModelName { get; }

    Task<string> GenerateTextAsync(
        string systemInstruction,
        IReadOnlyCollection<GeminiMessage> messages,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default);

    Task<T> GenerateJsonAsync<T>(
        string systemInstruction,
        string prompt,
        object responseSchema,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default);
}
