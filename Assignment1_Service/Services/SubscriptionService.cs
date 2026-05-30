using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;

    public SubscriptionService(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SubscriptionPlanDto>> GetActivePlansAsync()
    {
        var plans = await _repository.GetActivePlansAsync();
        return plans.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(int userId)
    {
        await _repository.DeactivateExpiredSubscriptionsAsync(userId);
        var subscription = await _repository.GetActiveSubscriptionAsync(userId);
        return subscription is null ? null : DtoMapper.ToDto(subscription);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await GetActiveSubscriptionAsync(userId) is not null;
    }

    public async Task<List<PaymentTicketDto>> GetUserTicketsAsync(int userId)
    {
        var tickets = await _repository.GetTicketsByUserAsync(userId);
        return tickets.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<List<PaymentTicketDto>> GetAllTicketsAsync(string? status = null)
    {
        var tickets = await _repository.GetAllTicketsAsync(status);
        return tickets.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<int> GetPendingTicketCountAsync()
    {
        var tickets = await _repository.GetAllTicketsAsync(PaymentTicketStatuses.Pending);
        return tickets.Count;
    }

    public async Task<string?> CreateTicketAsync(int userId, int planId)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan is null)
            return "Gói đăng ký không hợp lệ.";

        if (await _repository.HasPendingTicketAsync(userId, planId))
            return "Bạn đã có ticket chờ duyệt cho gói này.";

        await _repository.AddTicketAsync(new PaymentTicket
        {
            UserId = userId,
            PlanId = plan.Id,
            Amount = plan.Price,
            TransferReference = null,
            Status = PaymentTicketStatuses.Pending
        });

        return null;
    }

    public async Task<(bool Success, string? Error)> ApproveTicketAsync(
        int ticketId, int adminUserId, string? adminNote)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);
        if (ticket is null)
            return (false, "Không tìm thấy ticket.");

        if (ticket.Status != PaymentTicketStatuses.Pending)
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

        ticket.Status = PaymentTicketStatuses.Approved;
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

        if (ticket.Status != PaymentTicketStatuses.Pending)
            return (false, "Ticket này đã được xử lý.");

        ticket.Status = PaymentTicketStatuses.Rejected;
        ticket.ReviewedBy = adminUserId;
        ticket.ReviewedAt = DateTime.UtcNow;
        ticket.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        await _repository.UpdateTicketAsync(ticket);

        return (true, null);
    }
}
