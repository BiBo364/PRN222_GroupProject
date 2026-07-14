using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IChatService
{
    Task<List<SubjectDto>> GetAvailableSubjectsAsync();
    Task<List<ChatSessionListItemDto>> GetSessionsAsync(string userId, int? subjectId = null);
    Task<ChatSessionDto> CreateSessionAsync(string userId, int subjectId, string? title = null);
    Task<ChatSessionDto?> GetSessionAsync(string sessionId, string userId);
    Task<ChatReplyDto> AskAsync(string sessionId, string userId, string question);
}
