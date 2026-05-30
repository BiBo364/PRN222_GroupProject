namespace Assignment1_Service.Models;

public class SubjectDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ChapterDto> Chapters { get; set; } = [];
}
