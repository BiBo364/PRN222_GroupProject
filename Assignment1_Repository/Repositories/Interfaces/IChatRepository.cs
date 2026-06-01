using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IChatRepository
{
    Task<EmbeddingModel?> GetDefaultEmbeddingModelAsync();
    Task<List<EmbeddingModel>> GetEmbeddingModelsAsync();
    Task<List<Subject>> GetSubjectsWithIndexedDocumentsAsync();
    Task<List<Chunk>> GetIndexedChunkBatchBySubjectAsync(
        int subjectId,
        IReadOnlyCollection<int> embeddingModelIds,
        int? lastChunkId,
        int take);
    Task<List<Session>> GetUserSessionsAsync(string userId, int? subjectId = null);
    Task<Session?> GetSessionForUserAsync(string sessionId, string userId);
    Task<Session> CreateSessionAsync(Session session);
    Task UpdateSessionAsync(Session session);
    Task<Message> AddMessageAsync(Message message);
    Task AddCitationsAsync(IEnumerable<MessageCitation> citations);
}
