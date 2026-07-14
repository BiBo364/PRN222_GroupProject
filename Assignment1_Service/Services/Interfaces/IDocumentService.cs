using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IDocumentService
{
    Task<List<DocumentListItemDto>> GetDocumentsAsync();
    Task<DocumentDetailDto?> GetDocumentByIdAsync(int id);
    Task<DocumentDetailDto?> GetDeletedDocumentByIdAsync(int id);
    Task<DocumentDetailDto?> CreateDocumentEntryAsync(
        string originalName,
        string fileType,
        int subjectId,
        int? chapterId,
        int userId);
    Task<(DocumentDetailDto? Document, string? Error)> UpdateDocumentMetadataAsync(
        int id,
        string originalName,
        int? chapterId,
        int userId);
    Task<bool> DeleteDocumentAsync(int id, string storageRoot, string contentRoot, string webRoot, int? deletedByUserId = null);
    Task<List<DocumentListItemDto>> GetDeletedDocumentsAsync();
    Task<(bool Success, string? Error)> RestoreDocumentAsync(int id);
    Task<(DocumentUploadResultDto? Result, string? Error)> ReindexDocumentAsync(
        int id,
        int userId,
        string storageRoot,
        string contentRoot,
        string webRoot);
    Task<(DocumentUploadResultDto? Result, string? Error)> UploadAndProcessAsync(
        Stream fileStream,
        string originalFileName,
        long fileSize,
        int subjectId,
        int? chapterId,
        int userId,
        string storageRoot,
        string webRoot);
}
