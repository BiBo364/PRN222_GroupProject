using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface ISubscriptionRepository
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync();

    Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);

    Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);

    Task<List<PaymentTicket>> GetTicketsByUserAsync(int userId);

    Task<List<PaymentTicket>> GetAllTicketsAsync(string? status = null);

    Task<PaymentTicket?> GetTicketByIdAsync(int ticketId);

    Task<bool> HasPendingTicketAsync(int userId, int planId);

    Task<PaymentTicket> AddTicketAsync(PaymentTicket ticket);

    Task UpdateTicketAsync(PaymentTicket ticket);

    Task<UserSubscription> AddSubscriptionAsync(UserSubscription subscription);

    Task UpdateSubscriptionAsync(UserSubscription subscription);

    Task DeactivateExpiredSubscriptionsAsync(int userId);
}
