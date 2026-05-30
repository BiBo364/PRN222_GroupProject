using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IChatService
{
    Task<SubjectDto?> GetDemoSubjectAsync();
    Task<List<ChatSessionListItemDto>> GetSessionsAsync(string userId);
    Task<ChatSessionDto> CreateSessionAsync(string userId, string? title = null);
    Task<ChatSessionDto?> GetSessionAsync(string sessionId, string userId);
    Task<ChatReplyDto> AskAsync(string sessionId, string userId, string question);
}
