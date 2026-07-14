using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetActivePlansAsync();

    Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(int userId);

    Task<bool> HasActiveSubscriptionAsync(int userId);

    Task<ChatQuotaStatusDto> GetChatQuotaStatusAsync(int userId, int subjectId);

    Task<List<ChatQuotaStatusDto>> GetChatQuotaStatusesAsync(int userId, IEnumerable<int> subjectIds);

    Task<ChatQuotaStatusDto> RecordSuccessfulQuestionAsync(int userId, int subjectId);

    Task<List<PaymentTicketDto>> GetUserTicketsAsync(int userId);

    Task<List<PaymentTicketDto>> GetAllTicketsAsync(string? status = null);

    Task<int> GetPendingTicketCountAsync();

    Task<(bool Success, string? Error)> CompleteTicketAsync(int ticketId, string? note = null);
}
