namespace Assignmet1_Presentation.Models;

public class ChatMessageViewModel
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<ChatCitationViewModel> Citations { get; set; } = [];
}
