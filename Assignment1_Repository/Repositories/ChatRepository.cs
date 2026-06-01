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

    public Task<EmbeddingModel?> GetDefaultEmbeddingModelAsync()
    {
        return _context.EmbeddingModels.OrderBy(m => m.Id).FirstOrDefaultAsync();
    }

    public Task<List<EmbeddingModel>> GetEmbeddingModelsAsync()
    {
        return _context.EmbeddingModels
            .OrderBy(model => model.Id)
            .ToListAsync();
    }

    public Task<List<Subject>> GetSubjectsWithIndexedDocumentsAsync()
    {
        return _context.Subjects
            .Include(subject => subject.Chapters)
            .Where(subject => subject.Documents.Any(document => document.Status == "indexed"))
            .OrderBy(subject => subject.Code)
            .ToListAsync();
    }

    public Task<List<Chunk>> GetIndexedChunkBatchBySubjectAsync(
        int subjectId,
        IReadOnlyCollection<int> embeddingModelIds,
        int? lastChunkId,
        int take)
    {
        var modelIds = embeddingModelIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var query = _context.Chunks
            .AsNoTracking()
            .Include(c => c.Document)
            .Include(c => c.Embeddings.Where(e => modelIds.Contains(e.EmbeddingModelId)))
            .Where(c => c.Document.SubjectId == subjectId && c.Document.Status == "indexed")
            .OrderBy(c => c.Id)
            .AsSplitQuery();

        if (lastChunkId.HasValue)
            query = query.Where(c => c.Id > lastChunkId.Value);

        return query
            .Take(take)
            .ToListAsync();
    }

    public Task<List<Session>> GetUserSessionsAsync(string userId, int? subjectId = null)
    {
        var query = _context.Sessions
            .Where(session => session.UserId == userId && session.IsArchived != true);

        if (subjectId.HasValue)
            query = query.Where(session => session.SubjectId == subjectId.Value);

        return query
            .OrderByDescending(session => session.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Session?> GetSessionForUserAsync(string sessionId, string userId)
    {
        sessionId = sessionId.Trim();
        userId = userId.Trim();

        return await _context.Sessions
            .Include(s => s.Subject)
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.MessageCitations)
                    .ThenInclude(mc => mc.Chunk)
                        .ThenInclude(c => c.Document)
            .Where(s => s.UserId == userId && s.Id.Trim() == sessionId)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
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
