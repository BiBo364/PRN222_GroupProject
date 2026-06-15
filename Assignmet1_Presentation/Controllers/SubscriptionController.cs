using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IChatService _chatService;
    private readonly SubscriptionQuotaSettings _quotaSettings;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IChatService chatService,
        IOptions<SubscriptionQuotaSettings> quotaSettings)
    {
        _subscriptionService = subscriptionService;
        _chatService = chatService;
        _quotaSettings = quotaSettings.Value;
    }

    public async Task<IActionResult> Index()
    {
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

        return View(new SubscriptionIndexViewModel
        {
            Plans = plans.Select(ViewModelMapper.ToViewModel).ToList(),
            IsPlus = active is not null,
            CurrentPlanName = active is null ? "Free" : "Plus",
            CurrentPackageName = active?.PlanName,
            FreeQuestionLimit = _quotaSettings.FreeQuestionLimit > 0 ? _quotaSettings.FreeQuestionLimit : 5,
            FreeQuotaWindowHours = _quotaSettings.FreeQuotaWindowHours > 0 ? _quotaSettings.FreeQuotaWindowHours : 24,
            ActiveSubscription = active is null ? null : ViewModelMapper.ToViewModel(active),
            SubjectQuotas = subjectQuotas,
            Tickets = tickets.Select(ViewModelMapper.ToViewModel).ToList()
        });
    }
}
