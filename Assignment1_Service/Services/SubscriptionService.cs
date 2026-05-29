using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;

    public SubscriptionService(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<SubscriptionPlan>> GetActivePlansAsync() =>
        _repository.GetActivePlansAsync();

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
    {
        await _repository.DeactivateExpiredSubscriptionsAsync(userId);
        return await _repository.GetActiveSubscriptionAsync(userId);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        var sub = await GetActiveSubscriptionAsync(userId);
        return sub is not null;
    }

    public Task<List<PaymentTicket>> GetUserTicketsAsync(int userId) =>
        _repository.GetTicketsByUserAsync(userId);

    public Task<List<PaymentTicket>> GetAllTicketsAsync(string? status = null) =>
        _repository.GetAllTicketsAsync(status);

    public async Task<(PaymentTicket? Ticket, string? Error)> CreateTicketAsync(int userId, int planId)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan is null)
            return (null, "Gói đăng ký không hợp lệ.");

        if (await _repository.HasPendingTicketAsync(userId, planId))
            return (null, "Bạn đã có ticket chờ duyệt cho gói này.");

        var ticket = new PaymentTicket
        {
            UserId = userId,
            PlanId = plan.Id,
            Amount = plan.Price,
            TransferReference = null,
            Status = PaymentTicketStatus.Pending
        };

        ticket = await _repository.AddTicketAsync(ticket);
        return (ticket, null);
    }

    public async Task<(bool Success, string? Error)> ApproveTicketAsync(
        int ticketId, int adminUserId, string? adminNote)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);
        if (ticket is null)
            return (false, "Không tìm thấy ticket.");

        if (ticket.Status != PaymentTicketStatus.Pending)
            return (false, "Ticket này đã được xử lý.");

        var plan = ticket.Plan ?? await _repository.GetPlanByIdAsync(ticket.PlanId);
        if (plan is null)
            return (false, "Gói đăng ký không tồn tại.");

        await _repository.DeactivateExpiredSubscriptionsAsync(ticket.UserId);

        var now = DateTime.UtcNow;
        var current = await _repository.GetActiveSubscriptionAsync(ticket.UserId);
        var startAt = current is not null && current.EndAt > now ? current.EndAt : now;
        var endAt = startAt.AddDays(plan.DurationDays);

        if (current is not null && current.EndAt > now)
        {
            current.EndAt = endAt;
            current.PlanId = plan.Id;
            await _repository.UpdateSubscriptionAsync(current);
        }
        else
        {
            await _repository.AddSubscriptionAsync(new UserSubscription
            {
                UserId = ticket.UserId,
                PlanId = plan.Id,
                StartAt = startAt,
                EndAt = endAt,
                IsActive = true,
                PaymentTicketId = ticket.Id
            });
        }

        ticket.Status = PaymentTicketStatus.Approved;
        ticket.ReviewedBy = adminUserId;
        ticket.ReviewedAt = now;
        ticket.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        await _repository.UpdateTicketAsync(ticket);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectTicketAsync(
        int ticketId, int adminUserId, string? adminNote)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);
        if (ticket is null)
            return (false, "Không tìm thấy ticket.");

        if (ticket.Status != PaymentTicketStatus.Pending)
            return (false, "Ticket này đã được xử lý.");

        ticket.Status = PaymentTicketStatus.Rejected;
        ticket.ReviewedBy = adminUserId;
        ticket.ReviewedAt = DateTime.UtcNow;
        ticket.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        await _repository.UpdateTicketAsync(ticket);

        return (true, null);
    }
}
