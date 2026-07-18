using Microsoft.Extensions.Logging.Abstractions;
using RagEdu.Tests.Fakes;

namespace RagEdu.Tests.Services;

public sealed class GeminiServiceTests
{
    [Fact]
    public async Task GenerateAnswerAsync_EnglishQuestion_InstructsGeminiToReplyInQuestionLanguage()
    {
        var client = new FakeGeminiClient
        {
            TextResponse = "Serialization converts an object into a format that can be stored or transmitted."
        };
        var service = new GeminiService(client, NullLogger<GeminiService>.Instance);

        var answer = await service.GenerateAnswerAsync(
            "What is serialization?",
            [CreateChunk("Serialization converts an object into a serial format.")],
            []);

        Assert.StartsWith("Serialization converts", answer);
        Assert.Contains("same language as the user's current question", client.LastSystemInstruction);
        Assert.Contains(client.LastMessages!, message => message.Text == "Question: What is serialization?");
    }

    [Theory]
    [InlineData("What is serialization?", "I couldn't find relevant content")]
    [InlineData("Serialization là gì?", "Mình không tìm thấy nội dung phù hợp")]
    public async Task GenerateAnswerAsync_WithoutContext_UsesQuestionLanguage(string question, string expectedStart)
    {
        var service = new GeminiService(new FakeGeminiClient(), NullLogger<GeminiService>.Instance);

        var answer = await service.GenerateAnswerAsync(question, [], []);

        Assert.StartsWith(expectedStart, answer);
    }

    private static RetrievedChunk CreateChunk(string content)
    {
        return new RetrievedChunk
        {
            Chunk = new Chunk { Content = content },
            Document = new Document { OriginalName = "course.pdf" },
            Score = 0.9
        };
    }
}
