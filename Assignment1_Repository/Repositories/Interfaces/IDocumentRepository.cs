using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface IDocumentRepository
{
    Task<List<Document>> GetDocumentsWithDetailsAsync();
    Task<Document?> GetByIdWithDetailsAsync(int id);
    Task<Document?> GetDeletedByIdWithDetailsAsync(int id);
    Task<bool> ExistsActiveDocumentNameAsync(int subjectId, string originalName, int? excludedDocumentId = null);
    Task<bool> ExistsActiveDocumentHashAsync(int subjectId, string fileHash);
    Task<Document> AddDocumentAsync(Document document);
    Task UpdateDocumentAsync(Document document);
    Task DeleteDocumentAsync(int id);
    Task<List<Document>> GetDeletedDocumentsAsync();
    Task<bool> RestoreDocumentAsync(int id);
    Task<Subject?> GetFirstSubjectWithChaptersAsync();
    Task<EmbeddingModel?> GetFirstEmbeddingModelAsync();
    Task<List<EmbeddingModel>> GetEmbeddingModelsAsync();
    Task<ChunkingConfig?> GetFirstChunkingConfigAsync();
    Task<ChunkingConfig> UpsertChunkingConfigAsync(string name, string strategy, int chunkSize, int chunkOverlap, string? description);
    Task AddChunksAsync(IEnumerable<Chunk> chunks);
    Task ClearChunksAsync(int documentId);
}
