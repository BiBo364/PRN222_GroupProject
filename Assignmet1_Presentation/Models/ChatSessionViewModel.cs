namespace Assignmet1_Presentation.Models;

public class ChatSessionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public List<ChatMessageViewModel> Messages { get; set; } = [];
}
