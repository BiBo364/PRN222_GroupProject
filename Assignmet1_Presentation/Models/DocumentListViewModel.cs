namespace Assignmet1_Presentation.Models;

public class DocumentListViewModel
{
    public List<SubjectListItemViewModel> Subjects { get; set; } = [];
    public bool CanCreateSubject { get; set; }
}
