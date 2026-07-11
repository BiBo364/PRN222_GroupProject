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
            .Where(d => d.IsDeleted != true)
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
            .FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted != true);
    }

    public Task<Document?> GetDeletedByIdWithDetailsAsync(int id)
    {
        return _context.Documents
            .Include(d => d.Subject)
            .Include(d => d.Chapter)
            .Include(d => d.UploadedByNavigation)
            .Include(d => d.DeletedByNavigation)
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted == true);
    }

    public Task<bool> ExistsActiveDocumentNameAsync(int subjectId, string originalName, int? excludedDocumentId = null)
    {
        var normalizedName = originalName.Trim().ToLower();

        return _context.Documents.AnyAsync(d =>
            d.SubjectId == subjectId
            && d.IsDeleted != true
            && (!excludedDocumentId.HasValue || d.Id != excludedDocumentId.Value)
            && d.OriginalName.ToLower() == normalizedName);
    }

    public Task<bool> ExistsActiveDocumentHashAsync(int subjectId, string fileHash)
    {
        return _context.Documents.AnyAsync(d =>
            d.SubjectId == subjectId
            && d.IsDeleted != true
            && d.FileHash == fileHash);
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

    public Task<List<EmbeddingModel>> GetEmbeddingModelsAsync()
    {
        return _context.EmbeddingModels
            .OrderBy(model => model.Id)
            .ToListAsync();
    }

    public Task<ChunkingConfig?> GetFirstChunkingConfigAsync()
    {
        return _context.ChunkingConfigs.OrderBy(c => c.Id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Tạo mới hoặc cập nhật bản ghi ChunkingConfig đầu tiên trong DB.
    /// Được gọi lúc khởi động để đồng bộ cấu hình từ appsettings.json vào database.
    /// </summary>
    public async Task<ChunkingConfig> UpsertChunkingConfigAsync(
        string name,
        string strategy,
        int chunkSize,
        int chunkOverlap,
        string? description)
    {
        var existing = await _context.ChunkingConfigs
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            // Cập nhật bản ghi hiện có để DB phản ánh đúng cấu hình mới nhất
            existing.Name        = name;
            existing.Strategy    = strategy;
            existing.ChunkSize   = chunkSize;
            existing.ChunkOverlap = chunkOverlap;
            existing.Description = description;
            await _context.SaveChangesAsync();
            return existing;
        }

        // Chưa có → tạo mới
        var config = new ChunkingConfig
        {
            Name        = name,
            Strategy    = strategy,
            ChunkSize   = chunkSize,
            ChunkOverlap = chunkOverlap,
            Description = description,
            CreatedAt   = DateTime.Now
        };
        _context.ChunkingConfigs.Add(config);
        await _context.SaveChangesAsync();
        return config;
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

        var chunkIds = chunks.Select(c => c.Id).ToList();
        var citations = await _context.MessageCitations
            .Where(c => chunkIds.Contains(c.ChunkId))
            .ToListAsync();

        _context.MessageCitations.RemoveRange(citations);

        foreach (var chunk in chunks)
            _context.Embeddings.RemoveRange(chunk.Embeddings);

        _context.Chunks.RemoveRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted != true);

        if (document is null)
            return;

        document.IsDeleted = true;
        document.DeletedAt = DateTime.Now;
        // DeletedBy can be set in Service layer or left null if not provided
        
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    public Task<List<Document>> GetDeletedDocumentsAsync()
    {
        return _context.Documents
            .Include(d => d.Subject)
            .Include(d => d.Chapter)
            .Include(d => d.UploadedByNavigation)
            .Include(d => d.DeletedByNavigation)
            .Where(d => d.IsDeleted == true)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();
    }

    public async Task<bool> RestoreDocumentAsync(int id)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted == true);

        if (document is null)
            return false;

        document.IsDeleted = false;
        document.DeletedAt = null;
        document.DeletedBy = null;

        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
        return true;
    }
}
