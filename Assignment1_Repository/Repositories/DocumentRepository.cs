using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly RagEduContext _context;

    public DocumentRepository(RagEduContext context)
    {
        _context = context;
    }

    public Task<List<Document>> GetDocumentsWithDetailsAsync()
    {
        return _context.Documents
            .Include(d => d.Subject)
            .Include(d => d.Chapter)
            .Include(d => d.UploadedByNavigation)
            .Include(d => d.Chunks)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public Task<Document?> GetByIdWithDetailsAsync(int id)
    {
        return _context.Documents
            .Include(d => d.Subject)
            .Include(d => d.Chapter)
            .Include(d => d.UploadedByNavigation)
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document> AddDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task UpdateDocumentAsync(Document document)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    public Task<Subject?> GetFirstSubjectWithChaptersAsync()
    {
        return _context.Subjects
            .Include(s => s.Chapters.OrderBy(c => c.Number))
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public Task<EmbeddingModel?> GetFirstEmbeddingModelAsync()
    {
        return _context.EmbeddingModels.OrderBy(m => m.Id).FirstOrDefaultAsync();
    }

    public Task<ChunkingConfig?> GetFirstChunkingConfigAsync()
    {
        return _context.ChunkingConfigs.OrderBy(c => c.Id).FirstOrDefaultAsync();
    }

    public async Task AddChunksAsync(IEnumerable<Chunk> chunks)
    {
        _context.Chunks.AddRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task ClearChunksAsync(int documentId)
    {
        var chunks = await _context.Chunks
            .Include(c => c.Embeddings)
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();

        foreach (var chunk in chunks)
            _context.Embeddings.RemoveRange(chunk.Embeddings);

        _context.Chunks.RemoveRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents
            .Include(d => d.Chunks)
            .ThenInclude(c => c.Embeddings)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
            return;

        foreach (var chunk in document.Chunks)
            _context.Embeddings.RemoveRange(chunk.Embeddings);

        _context.Chunks.RemoveRange(document.Chunks);
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
    }
}
