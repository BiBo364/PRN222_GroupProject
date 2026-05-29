namespace Assignment1_Service.Models;

public class ChatCitationDto
{
    public int ChunkId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int? SlideNumber { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public double Score { get; set; }
}
