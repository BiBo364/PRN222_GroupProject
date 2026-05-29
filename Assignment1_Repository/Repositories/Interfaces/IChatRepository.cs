using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IChatRepository
{
    Task<Subject?> GetDemoSubjectAsync();
    Task<EmbeddingModel?> GetDefaultEmbeddingModelAsync();
    Task<List<Chunk>> GetIndexedChunksBySubjectAsync(int subjectId);
    Task<List<Session>> GetUserSessionsAsync(string userId, int subjectId);
    Task<Session?> GetSessionForUserAsync(string sessionId, string userId);
    Task<Session> CreateSessionAsync(Session session);
    Task UpdateSessionAsync(Session session);
    Task<Message> AddMessageAsync(Message message);
    Task AddCitationsAsync(IEnumerable<MessageCitation> citations);
}
