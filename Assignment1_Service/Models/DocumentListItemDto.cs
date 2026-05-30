namespace Assignment1_Service.Models;

public class DocumentListItemDto
{
    public int Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMsg { get; set; }
    public int ChunkCount { get; set; }
    public int? SubjectId { get; set; }
    public int? ChapterNumber { get; set; }
    public string? ChapterTitle { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? IndexedAt { get; set; }
}
