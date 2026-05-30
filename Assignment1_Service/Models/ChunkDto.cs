namespace Assignment1_Service.Models;

public class ChunkDto
{
    public int Id { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public int? PageNumber { get; set; }
    public int? TokenCount { get; set; }
}
