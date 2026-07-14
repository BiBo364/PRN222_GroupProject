using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Pages.Subscription;

// @page "/Subscription/Index"
[RequireLogin]
public class IndexModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IChatService _chatService;
    private readonly SubscriptionQuotaSettings _quotaSettings;

    public IndexModel(
        ISubscriptionService subscriptionService,
        IChatService chatService,
        IOptions<SubscriptionQuotaSettings> quotaSettings)
    {
        _subscriptionService = subscriptionService;
        _chatService = chatService;
        _quotaSettings = quotaSettings.Value;
    }

    public SubscriptionIndexViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(DateTime? fromDate, DateTime? toDate)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId == 1)
        {
            await BuildAdminViewModelAsync(fromDate, toDate);
            return Page();
        }

        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var plans = (await _subscriptionService.GetActivePlansAsync())
            .Where(plan => plan.Price > 0)
            .OrderBy(plan => plan.Price)
            .ToList();
        var active = await _subscriptionService.GetActiveSubscriptionAsync(userId);
        var tickets = await _subscriptionService.GetUserTicketsAsync(userId);
        var subjects = await _chatService.GetAvailableSubjectsAsync();
        var quotaStatuses = await _subscriptionService.GetChatQuotaStatusesAsync(
            userId,
            subjects.Select(subject => subject.Id));

        var quotaLookup = quotaStatuses.ToDictionary(status => status.SubjectId);
        var subjectQuotas = subjects
            .Select(subject =>
            {
                var quotaViewModel = ViewModelMapper.ToViewModel(quotaLookup[subject.Id]);
                quotaViewModel.SubjectCode = subject.Code;
                quotaViewModel.SubjectName = subject.Name;
                return quotaViewModel;
            })
            .ToList();

        var freeQuotas = subjectQuotas.Where(quota => !quota.IsPlus).ToList();

        ViewModel = new SubscriptionIndexViewModel
        {
            Plans = plans.Select(ViewModelMapper.ToViewModel).ToList(),
            IsPlus = active is not null,
            CurrentPlanName = active is null ? "Free" : "Plus",
            CurrentPackageName = active?.PlanName,
            FreeQuestionLimit = _quotaSettings.FreeQuestionLimit > 0 ? _quotaSettings.FreeQuestionLimit : 5,
            FreeQuotaWindowHours = _quotaSettings.FreeQuotaWindowHours > 0 ? _quotaSettings.FreeQuotaWindowHours : 24,
            ActiveSubscription = active is null ? null : ViewModelMapper.ToViewModel(active),
            SubjectQuotas = subjectQuotas,
            Tickets = tickets.Select(ViewModelMapper.ToViewModel).ToList(),
            SubjectCount = subjectQuotas.Count,
            SubjectsOutOfQuotaCount = freeQuotas.Count(quota => !quota.IsAllowed),
            TotalQuestionsRemaining = active is null ? freeQuotas.Sum(quota => quota.QuestionsRemaining) : int.MaxValue,
            LowestQuestionsRemaining = active is null && freeQuotas.Count > 0 ? freeQuotas.Min(quota => quota.QuestionsRemaining) : int.MaxValue,
            NextResetAt = active is null
                ? freeQuotas
                    .Select(quota => (DateTime?)quota.WindowEndAt)
                    .OrderBy(windowEnd => windowEnd)
                    .FirstOrDefault()
                : active?.EndAt
        };

        return Page();
    }

    private async Task BuildAdminViewModelAsync(DateTime? fromDate, DateTime? toDate)
    {
        var plans = (await _subscriptionService.GetActivePlansAsync())
            .Where(plan => plan.Price > 0)
            .OrderBy(plan => plan.Price)
            .ToList();

        var approvedTickets = (await _subscriptionService.GetAllTicketsAsync(PaymentTicketStatuses.Approved))
            .Where(ticket => ticket.CreatedAt.HasValue)
            .ToList();

        if (fromDate.HasValue)
        {
            var fromBoundary = fromDate.Value.Date;
            approvedTickets = approvedTickets
                .Where(ticket => ticket.CreatedAt!.Value >= fromBoundary)
                .ToList();
        }

        if (toDate.HasValue)
        {
            var toBoundaryExclusive = toDate.Value.Date.AddDays(1);
            approvedTickets = approvedTickets
                .Where(ticket => ticket.CreatedAt!.Value < toBoundaryExclusive)
                .ToList();
        }

        var totalPurchaseCount = approvedTickets.Count;
        var totalRevenue = approvedTickets.Sum(ticket => ticket.Amount);
        var groupedByPlan = approvedTickets
            .GroupBy(ticket => ticket.PlanName)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    PurchaseCount = group.Count(),
                    Revenue = group.Sum(ticket => ticket.Amount)
                },
                StringComparer.OrdinalIgnoreCase);

        var planReports = plans
            .Select(plan =>
            {
                groupedByPlan.TryGetValue(plan.Name, out var summary);
                var purchaseCount = summary?.PurchaseCount ?? 0;
                var revenue = summary?.Revenue ?? 0m;

                return new AdminSubscriptionPlanReportViewModel
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    Price = plan.Price,
                    DurationDays = plan.DurationDays,
                    PurchaseCount = purchaseCount,
                    Revenue = revenue,
                    PurchaseSharePercent = totalPurchaseCount == 0
                        ? 0
                        : Math.Round((decimal)purchaseCount * 100m / totalPurchaseCount, 1)
                };
            })
            .OrderByDescending(report => report.PurchaseCount)
            .ThenByDescending(report => report.Revenue)
            .ThenBy(report => report.Price)
            .ToList();

        var topPlan = planReports.FirstOrDefault(report => report.PurchaseCount > 0);

        ViewModel = new SubscriptionIndexViewModel
        {
            IsAdminView = true,
            Plans = plans.Select(ViewModelMapper.ToViewModel).ToList(),
            FilterFromDate = fromDate?.Date,
            FilterToDate = toDate?.Date,
            AdminTotalRevenue = totalRevenue,
            AdminSuccessfulOrders = totalPurchaseCount,
            AdminTopPlanName = topPlan?.Name ?? "Chua co giao dich",
            AdminTopPlanCount = topPlan?.PurchaseCount ?? 0,
            AdminAverageOrderValue = totalPurchaseCount == 0
                ? 0
                : Math.Round(totalRevenue / totalPurchaseCount, 0, MidpointRounding.AwayFromZero),
            AdminPlanReports = planReports
        };
    }
}
