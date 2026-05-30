using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetActivePlansAsync();

    Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(int userId);

    Task<bool> HasActiveSubscriptionAsync(int userId);

    Task<List<PaymentTicketDto>> GetUserTicketsAsync(int userId);

    Task<List<PaymentTicketDto>> GetAllTicketsAsync(string? status = null);

    Task<int> GetPendingTicketCountAsync();

    Task<string?> CreateTicketAsync(int userId, int planId);

    Task<(bool Success, string? Error)> ApproveTicketAsync(int ticketId, int adminUserId, string? adminNote);

    Task<(bool Success, string? Error)> RejectTicketAsync(int ticketId, int adminUserId, string? adminNote);
}
