namespace Assignmet1_Presentation.Models;

public class SubjectViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ChapterViewModel> Chapters { get; set; } = [];
}
