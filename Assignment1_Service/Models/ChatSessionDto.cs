namespace Assignment1_Service.Models;

public class ChatSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = [];
}
