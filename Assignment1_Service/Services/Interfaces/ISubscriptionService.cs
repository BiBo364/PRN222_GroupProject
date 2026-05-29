using Assignment1_Repository.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync();

    Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);

    Task<bool> HasActiveSubscriptionAsync(int userId);

    Task<List<PaymentTicket>> GetUserTicketsAsync(int userId);

    Task<List<PaymentTicket>> GetAllTicketsAsync(string? status = null);

    Task<(PaymentTicket? Ticket, string? Error)> CreateTicketAsync(int userId, int planId);

    Task<(bool Success, string? Error)> ApproveTicketAsync(int ticketId, int adminUserId, string? adminNote);

    Task<(bool Success, string? Error)> RejectTicketAsync(int ticketId, int adminUserId, string? adminNote);
}
