using Assignment1_Repository.Models;

namespace Assignment1_Repository.Repositories.Interfaces;

public interface ISubscriptionRepository
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync();

    Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);

    Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);

    Task<UserSubscription?> GetSubscriptionByPaymentTicketIdAsync(int ticketId);

    Task<List<PaymentTicket>> GetTicketsByUserAsync(int userId);

    Task<List<PaymentTicket>> GetAllTicketsAsync(string? status = null);

    Task<PaymentTicket?> GetTicketByIdAsync(int ticketId);

    Task<PaymentTicket?> GetTicketByOrderIdAsync(string orderId);

    Task<PaymentTicket?> GetTicketByRequestIdAsync(string requestId);

    Task<bool> HasPendingTicketAsync(int userId, int planId, DateTime? createdAfterUtc = null);

    Task<PaymentTicket> AddTicketAsync(PaymentTicket ticket);

    Task UpdateTicketAsync(PaymentTicket ticket);

    Task<UserSubscription> AddSubscriptionAsync(UserSubscription subscription);

    Task UpdateSubscriptionAsync(UserSubscription subscription);

    Task<StudentChatUsage?> GetChatUsageAsync(int userId, int subjectId, DateTime windowStart);

    Task<List<StudentChatUsage>> GetChatUsagesAsync(int userId, IReadOnlyCollection<int> subjectIds, DateTime windowStart);

    Task<StudentChatUsage> AddChatUsageAsync(StudentChatUsage usage);

    Task UpdateChatUsageAsync(StudentChatUsage usage);

    Task DeactivateExpiredSubscriptionsAsync(int userId);
}
