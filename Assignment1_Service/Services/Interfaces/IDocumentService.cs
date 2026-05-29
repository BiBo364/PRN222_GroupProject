using Assignment1_Repository.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IDocumentService
{
    Task<Subject?> GetDemoSubjectAsync();
    Task<List<Document>> GetDocumentsAsync();
    Task<Document?> GetDocumentByIdAsync(int id);
    Task<bool> DeleteDocumentAsync(int id, string storageRoot, string contentRoot, string webRoot);
    Task<(Document? Document, string? Error)> ReindexDocumentAsync(
        int id,
        string storageRoot,
        string contentRoot,
        string webRoot);
    Task<(Document? Document, string? Error)> UploadAndProcessAsync(
        Stream fileStream,
        string originalFileName,
        long fileSize,
        int subjectId,
        int? chapterId,
        int userId,
        string storageRoot,
        string webRoot);
}
