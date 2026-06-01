namespace Assignmet1_Presentation.Models;

public class SubjectListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ChapterCount { get; set; }
    public int DocumentCount { get; set; }
    public int IndexedDocumentCount { get; set; }
}