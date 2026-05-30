namespace Assignmet1_Presentation.Models;

public class ChatIndexViewModel
{
    public SubjectViewModel? Subject { get; set; }
    public List<ChatSessionListItemViewModel> Sessions { get; set; } = [];
}
