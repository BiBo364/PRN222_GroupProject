namespace Assignment1_Service.Models;

public class DocumentDetailDto
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ChapterNumber { get; set; }
    public string? ChapterTitle { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime? IndexedAt { get; set; }
    public List<ChunkDto> Chunks { get; set; } = [];
}
