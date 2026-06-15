namespace Assignment1_Service.Models;

public class DocumentUploadResultDto
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
}
