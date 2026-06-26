namespace Assignmet1_Presentation.Models;

public class ChunkViewModel
{
    public int Id { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public int? PageNumber { get; set; }
    public int? CharStart { get; set; }
    public int? CharEnd { get; set; }
    public int? TokenCount { get; set; }
}
