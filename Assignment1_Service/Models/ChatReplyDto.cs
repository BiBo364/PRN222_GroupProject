namespace Assignment1_Service.Models;

public class ChatReplyDto
{
    public string Answer { get; set; } = string.Empty;
    public List<ChatCitationDto> Citations { get; set; } = new();
    public bool FoundInDocuments { get; set; }
}
