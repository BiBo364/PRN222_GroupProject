namespace Assignment1_Service.Models;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<ChatCitationDto> Citations { get; set; } = [];
}
