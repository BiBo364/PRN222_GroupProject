using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly SubscriptionQuotaSettings _quotaSettings;

    public SubscriptionService(
        ISubscriptionRepository repository,
        IOptions<SubscriptionQuotaSettings> quotaSettings)
    {
        _repository = repository;
        _quotaSettings = quotaSettings.Value;
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

    public async Task<ChatQuotaStatusDto> GetChatQuotaStatusAsync(int userId, int subjectId)
    {
        await _repository.DeactivateExpiredSubscriptionsAsync(userId);

        var activeSubscription = await _repository.GetActiveSubscriptionAsync(userId);
        if (activeSubscription is not null)
        {
            return CreatePlusQuotaStatus(subjectId, activeSubscription.Plan.Name);
        }

        var windowStart = GetCurrentWindowStart(DateTime.UtcNow);
        var windowEnd = windowStart.AddHours(GetWindowHours());
        var usage = await _repository.GetChatUsageAsync(userId, subjectId, windowStart);
        return CreateFreeQuotaStatus(subjectId, usage?.QuestionCount ?? 0, windowStart, windowEnd);
    }

    public async Task<List<ChatQuotaStatusDto>> GetChatQuotaStatusesAsync(int userId, IEnumerable<int> subjectIds)
    {
        await _repository.DeactivateExpiredSubscriptionsAsync(userId);

        var ids = subjectIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return [];

        var activeSubscription = await _repository.GetActiveSubscriptionAsync(userId);
        if (activeSubscription is not null)
        {
            return ids
                .Select(subjectId => CreatePlusQuotaStatus(subjectId, activeSubscription.Plan.Name))
                .ToList();
        }

        var windowStart = GetCurrentWindowStart(DateTime.UtcNow);
        var windowEnd = windowStart.AddHours(GetWindowHours());
        var usages = await _repository.GetChatUsagesAsync(userId, ids, windowStart);
        var usageLookup = usages.ToDictionary(usage => usage.SubjectId, usage => usage.QuestionCount);

        return ids
            .Select(subjectId => CreateFreeQuotaStatus(
                subjectId,
                usageLookup.GetValueOrDefault(subjectId),
                windowStart,
                windowEnd))
            .ToList();
    }

    public async Task<ChatQuotaStatusDto> RecordSuccessfulQuestionAsync(int userId, int subjectId)
    {
        await _repository.DeactivateExpiredSubscriptionsAsync(userId);

        var activeSubscription = await _repository.GetActiveSubscriptionAsync(userId);
        if (activeSubscription is not null)
        {
            return CreatePlusQuotaStatus(subjectId, activeSubscription.Plan.Name);
        }

        var windowStart = GetCurrentWindowStart(DateTime.UtcNow);
        var windowEnd = windowStart.AddHours(GetWindowHours());
        var usage = await _repository.GetChatUsageAsync(userId, subjectId, windowStart);
        var limit = GetFreeQuestionLimit();

        if (usage is null)
        {
            usage = await _repository.AddChatUsageAsync(new StudentChatUsage
            {
                UserId = userId,
                SubjectId = subjectId,
                WindowStart = windowStart,
                QuestionCount = 1
            });
        }
        else if (usage.QuestionCount < limit)
        {
            usage.QuestionCount += 1;
            await _repository.UpdateChatUsageAsync(usage);
        }

        return CreateFreeQuotaStatus(subjectId, usage.QuestionCount, windowStart, windowEnd);
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

    public async Task<(bool Success, string? Error)> CompleteTicketAsync(int ticketId, string? note = null)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);
        if (ticket is null)
            return (false, "Khong tim thay ticket.");

        if (ticket.Status == PaymentTicketStatuses.Approved)
            return (true, null);

        if (ticket.Status != PaymentTicketStatuses.Pending && ticket.Status != PaymentTicketStatuses.MomoPending)
            return (false, "Ticket nay da duoc xu ly.");

        return await CompleteSubscriptionAsync(ticket, null, note);
    }

    private async Task<(bool Success, string? Error)> CompleteSubscriptionAsync(
        PaymentTicket ticket,
        int? reviewedBy,
        string? adminNote)
    {
        var plan = ticket.Plan ?? await _repository.GetPlanByIdAsync(ticket.PlanId);
        if (plan is null)
            return (false, "Goi dang ky khong ton tai.");

        var existingSubscription = await _repository.GetSubscriptionByPaymentTicketIdAsync(ticket.Id);
        if (existingSubscription is not null)
        {
            if (ticket.Status != PaymentTicketStatuses.Approved)
            {
                ticket.Status = PaymentTicketStatuses.Approved;
                ticket.ReviewedBy = reviewedBy;
                ticket.ReviewedAt = DateTime.UtcNow;
                ticket.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
                await _repository.UpdateTicketAsync(ticket);
            }

            return (true, null);
        }

        await _repository.DeactivateExpiredSubscriptionsAsync(ticket.UserId);

        var now = DateTime.UtcNow;
        var current = await _repository.GetActiveSubscriptionAsync(ticket.UserId);
        var startAt = current is not null && current.EndAt > now ? current.EndAt : now;
        var endAt = startAt.AddDays(plan.DurationDays);

        await _repository.AddSubscriptionAsync(new UserSubscription
            {
                UserId = ticket.UserId,
                PlanId = plan.Id,
                StartAt = startAt,
                EndAt = endAt,
                IsActive = true,
                PaymentTicketId = ticket.Id
            });

        ticket.Status = PaymentTicketStatuses.Approved;
        ticket.ReviewedBy = reviewedBy;
        ticket.ReviewedAt = now;
        ticket.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        await _repository.UpdateTicketAsync(ticket);

        return (true, null);
    }

    private ChatQuotaStatusDto CreatePlusQuotaStatus(int subjectId, string packageName)
    {
        return new ChatQuotaStatusDto
        {
            SubjectId = subjectId,
            IsPlus = true,
            IsAllowed = true,
            QuestionLimit = 0,
            QuestionsUsed = 0,
            QuestionsRemaining = int.MaxValue,
            WindowStartAt = DateTime.UtcNow,
            WindowEndAt = DateTime.UtcNow.AddHours(GetWindowHours()),
            CurrentPlanName = "Plus",
            CurrentPackageName = packageName,
            Message = "Goi Plus dang hoat dong. Ban co the dat cau hoi khong gioi han."
        };
    }

    private ChatQuotaStatusDto CreateFreeQuotaStatus(
        int subjectId,
        int questionsUsed,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var limit = GetFreeQuestionLimit();
        var remaining = Math.Max(0, limit - questionsUsed);
        var isAllowed = questionsUsed < limit;

        return new ChatQuotaStatusDto
        {
            SubjectId = subjectId,
            IsPlus = false,
            IsAllowed = isAllowed,
            QuestionLimit = limit,
            QuestionsUsed = Math.Min(questionsUsed, limit),
            QuestionsRemaining = remaining,
            WindowStartAt = windowStart,
            WindowEndAt = windowEnd,
            CurrentPlanName = "Free",
            CurrentPackageName = null,
            Message = isAllowed
                ? $"Free plan: con {remaining}/{limit} cau hoi cho mon nay den {windowEnd.ToLocalTime():dd/MM/yyyy HH:mm}."
                : $"Ban da dung het {limit} cau hoi mien phi cho mon nay. Vui long cho den {windowEnd.ToLocalTime():dd/MM/yyyy HH:mm} hoac nang cap Plus de tiep tuc."
        };
    }

    private DateTime GetCurrentWindowStart(DateTime utcNow)
    {
        var windowHours = GetWindowHours();
        var windowTicks = TimeSpan.FromHours(windowHours).Ticks;
        var utcTicks = utcNow.Ticks - (utcNow.Ticks % windowTicks);
        return new DateTime(utcTicks, DateTimeKind.Utc);
    }

    private int GetFreeQuestionLimit()
    {
        return _quotaSettings.FreeQuestionLimit > 0
            ? _quotaSettings.FreeQuestionLimit
            : 5;
    }

    private int GetWindowHours()
    {
        return _quotaSettings.FreeQuotaWindowHours > 0
            ? _quotaSettings.FreeQuotaWindowHours
            : 24;
    }
}
