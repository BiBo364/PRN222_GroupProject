using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly RagEduContext _context;

    public ChatRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<Subject?> GetDemoSubjectAsync()
    {
        return _context.Subjects.OrderBy(s => s.Id).FirstOrDefaultAsync();
    }

    public Task<EmbeddingModel?> GetDefaultEmbeddingModelAsync()
    {
        return _context.EmbeddingModels.OrderBy(m => m.Id).FirstOrDefaultAsync();
    }

    public Task<List<Chunk>> GetIndexedChunksBySubjectAsync(int subjectId)
    {
        return _context.Chunks
            .Include(c => c.Embeddings)
            .Include(c => c.Document)
            .Where(c => c.Document.SubjectId == subjectId && c.Document.Status == "indexed")
            .ToListAsync();
    }

    public Task<List<Session>> GetUserSessionsAsync(string userId, int subjectId)
    {
        return _context.Sessions
            .Where(s => s.UserId == userId && s.SubjectId == subjectId && s.IsArchived != true)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Session?> GetSessionForUserAsync(string sessionId, string userId)
    {
        sessionId = sessionId.Trim();
        userId = userId.Trim();

        var sessions = await _context.Sessions
            .Include(s => s.Subject)
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.MessageCitations)
                    .ThenInclude(mc => mc.Chunk)
                        .ThenInclude(c => c.Document)
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return sessions.FirstOrDefault(s => s.Id.Trim() == sessionId);
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task UpdateSessionAsync(Session session)
    {
        var existing = await _context.Sessions.FindAsync(session.Id);
        if (existing is null)
            return;

        existing.Title = session.Title;
        existing.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task<Message> AddMessageAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task AddCitationsAsync(IEnumerable<MessageCitation> citations)
    {
        _context.MessageCitations.AddRange(citations);
        await _context.SaveChangesAsync();
    }
}
