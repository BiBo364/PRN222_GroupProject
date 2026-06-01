namespace Assignmet1_Presentation.Models;

public class SubjectDetailViewModel
{
    public SubjectViewModel Subject { get; set; } = null!;
    public List<DocumentListItemViewModel> Documents { get; set; } = [];
    public bool CanCreateSubject { get; set; }
    public bool CanUploadDocument { get; set; }
}