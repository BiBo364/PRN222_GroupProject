using Assignment1_Repository.Models;
using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IChatService
{
    Task<Subject?> GetDemoSubjectAsync();
    Task<List<Session>> GetSessionsAsync(string userId);
    Task<Session> CreateSessionAsync(string userId, string? title = null);
    Task<Session?> GetSessionAsync(string sessionId, string userId);
    Task<ChatReplyDto> AskAsync(string sessionId, string userId, string question);
}
