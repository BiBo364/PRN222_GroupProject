using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IDocumentService
{
    Task<SubjectDto?> GetDemoSubjectAsync();
    Task<List<DocumentListItemDto>> GetDocumentsAsync();
    Task<DocumentDetailDto?> GetDocumentByIdAsync(int id);
    Task<bool> DeleteDocumentAsync(int id, string storageRoot, string contentRoot, string webRoot);
    Task<(DocumentUploadResultDto? Result, string? Error)> ReindexDocumentAsync(
        int id,
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
