namespace RagEdu.Tests.Fakes;

internal sealed class FakeGeminiClient : IGeminiClient
{
    public string ModelName { get; set; } = "gemini-test";
    public string TextResponse { get; set; } = string.Empty;
    public object? JsonResponse { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public Queue<Exception> ExceptionsToThrow { get; } = new();
    public int TextCallCount { get; private set; }
    public int JsonCallCount { get; private set; }
    public string? LastSystemInstruction { get; private set; }
    public IReadOnlyCollection<GeminiMessage>? LastMessages { get; private set; }
    public string? LastPrompt { get; private set; }
    public object? LastSchema { get; private set; }
    public double LastTemperature { get; private set; }
    public int LastMaximumOutputTokens { get; private set; }

    public Task<string> GenerateTextAsync(
        string systemInstruction,
        IReadOnlyCollection<GeminiMessage> messages,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TextCallCount++;
        LastSystemInstruction = systemInstruction;
        LastMessages = messages;
        LastTemperature = temperature;
        LastMaximumOutputTokens = maximumOutputTokens;

        if (ExceptionsToThrow.TryDequeue(out var queuedException))
            throw queuedException;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult(TextResponse);
    }

    public Task<T> GenerateJsonAsync<T>(
        string systemInstruction,
        string prompt,
        object responseSchema,
        double temperature,
        int maximumOutputTokens,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        JsonCallCount++;
        LastSystemInstruction = systemInstruction;
        LastPrompt = prompt;
        LastSchema = responseSchema;
        LastTemperature = temperature;
        LastMaximumOutputTokens = maximumOutputTokens;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (JsonResponse is T response)
            return Task.FromResult(response);

        throw new InvalidOperationException(
            $"Fake Gemini response is not compatible with {typeof(T).FullName}.");
    }
}
