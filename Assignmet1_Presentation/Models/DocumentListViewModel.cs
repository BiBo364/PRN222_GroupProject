using Assignment1_Repository.Models;

namespace Assignmet1_Presentation.Models;

public class DocumentListViewModel
{
    public Subject? DemoSubject { get; set; }
    public List<Document> Documents { get; set; } = new();
    public bool CanUpload { get; set; }
    public bool CanDelete { get; set; }
}
