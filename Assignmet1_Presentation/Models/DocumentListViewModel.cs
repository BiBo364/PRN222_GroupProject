namespace Assignmet1_Presentation.Models;

public class DocumentListViewModel
{
    public SubjectViewModel? DemoSubject { get; set; }
    public List<DocumentListItemViewModel> Documents { get; set; } = [];
    public bool CanUpload { get; set; }
    public bool CanDelete { get; set; }
}
