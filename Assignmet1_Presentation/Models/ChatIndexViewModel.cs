namespace Assignmet1_Presentation.Models;

public class ChatIndexViewModel
{
    public SubjectViewModel? Subject { get; set; }
    public List<SubjectViewModel> AvailableSubjects { get; set; } = [];
    public int? SelectedSubjectId { get; set; }
    public QuotaStatusViewModel? QuotaStatus { get; set; }
    public List<ChatSessionListItemViewModel> Sessions { get; set; } = [];
}
