using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IDocumentRepository
{
    Task<List<Document>> GetDocumentsWithDetailsAsync();
    Task<Document?> GetByIdWithDetailsAsync(int id);
    Task<Document> AddDocumentAsync(Document document);
    Task UpdateDocumentAsync(Document document);
    Task DeleteDocumentAsync(int id);
    Task<Subject?> GetFirstSubjectWithChaptersAsync();
    Task<EmbeddingModel?> GetFirstEmbeddingModelAsync();
    Task<ChunkingConfig?> GetFirstChunkingConfigAsync();
    Task AddChunksAsync(IEnumerable<Chunk> chunks);
    Task ClearChunksAsync(int documentId);
}
