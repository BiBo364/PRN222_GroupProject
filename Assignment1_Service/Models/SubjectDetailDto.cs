namespace Assignment1_Service.Models;

public class SubjectDetailDto
{
    public SubjectDto Subject { get; set; } = null!;
    public List<DocumentListItemDto> Documents { get; set; } = [];
}