namespace Assignmet1_Presentation.Models;

public class SubjectDetailViewModel
{
    public SubjectViewModel Subject { get; set; } = null!;
    public List<DocumentListItemViewModel> Documents { get; set; } = [];
    public int TotalDocumentCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public bool CanCreateSubject { get; set; }
    public bool CanUploadDocument { get; set; }
    public bool CanEditDocument { get; set; }
    public bool CanDeleteDocument { get; set; }
}
