using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;

    public ChatService(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public Task<Subject?> GetDemoSubjectAsync()
    {
        return _chatRepository.GetDemoSubjectAsync();
    }

    public async Task<List<Session>> GetSessionsAsync(string userId)
    {
        var subject = await _chatRepository.GetDemoSubjectAsync()
            ?? throw new InvalidOperationException("No subject configured.");

        return await _chatRepository.GetUserSessionsAsync(userId, subject.Id);
    }

    public async Task<Session> CreateSessionAsync(string userId, string? title = null)
    {
        var subject = await _chatRepository.GetDemoSubjectAsync()
            ?? throw new InvalidOperationException("No subject configured.");

        var session = new Session
        {
            Id = Guid.NewGuid().ToString("D"),
            SubjectId = subject.Id,
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? $"Chat {DateTime.Now:dd/MM HH:mm}" : title.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsArchived = false
        };

        return await _chatRepository.CreateSessionAsync(session);
    }

    public Task<Session?> GetSessionAsync(string sessionId, string userId)
    {
        return _chatRepository.GetSessionForUserAsync(sessionId, userId);
    }

    public async Task<ChatReplyDto> AskAsync(string sessionId, string userId, string question)
    {
        var session = await _chatRepository.GetSessionForUserAsync(sessionId, userId)
            ?? throw new InvalidOperationException("Session not found.");

        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.");

        var subjectId = session.SubjectId
            ?? throw new InvalidOperationException("Session has no subject.");

        var isFirstQuestion = !session.Messages.Any(m => m.Role == "user");

        await _chatRepository.AddMessageAsync(new Message
        {
            SessionId = sessionId,
            Role = "user",
            Content = question.Trim(),
            CreatedAt = DateTime.Now
        });

        var embeddingModel = await _chatRepository.GetDefaultEmbeddingModelAsync()
            ?? throw new InvalidOperationException("No embedding model configured.");

        var chunks = await _chatRepository.GetIndexedChunksBySubjectAsync(subjectId);
        var retrieved = ChunkRetriever.Retrieve(question, chunks, embeddingModel.Dimension);
        var reply = RagAnswerBuilder.Build(question, retrieved);

        var assistantMessage = await _chatRepository.AddMessageAsync(new Message
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = reply.Answer,
            CreatedAt = DateTime.Now
        });

        if (reply.Citations.Count > 0)
        {
            var citations = reply.Citations.Select((c, index) => new MessageCitation
            {
                MessageId = assistantMessage.Id,
                ChunkId = c.ChunkId,
                SimilarityScore = c.Score,
                RankOrder = index + 1,
                WasUsed = true
            });
            await _chatRepository.AddCitationsAsync(citations);
        }

        session.UpdatedAt = DateTime.Now;
        if (isFirstQuestion && session.Title?.StartsWith("Chat ") == true)
            session.Title = TruncateTitle(question);

        await _chatRepository.UpdateSessionAsync(session);

        return reply;
    }

    private static string TruncateTitle(string question)
    {
        var title = question.Trim();
        return title.Length <= 80 ? title : title[..77] + "...";
    }
}
