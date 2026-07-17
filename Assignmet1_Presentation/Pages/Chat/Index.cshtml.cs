using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Chat;

[RequireLogin]
public class IndexModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly ISubscriptionService _subscriptionService;

    public IndexModel(IChatService chatService, ISubscriptionService subscriptionService)
    {
        _chatService = chatService;
        _subscriptionService = subscriptionService;
    }

    public ChatIndexViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? subjectId)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToPage("/Account/Login");

        var availableSubjects = await _chatService.GetAvailableSubjectsAsync();
        SubjectViewModel? selectedSubject = null;
        int? selectedSubjectId = null;

        if (subjectId.HasValue)
        {
            selectedSubject = availableSubjects
                .Select(ViewModelMapper.ToViewModel)
                .FirstOrDefault(subject => subject.Id == subjectId.Value);

            if (selectedSubject is null)
            {
                TempData["Error"] = "Môn học đã chọn hiện không khả dụng cho Chat AI.";
            }
            else
            {
                selectedSubjectId = selectedSubject.Id;
            }
        }

        var sessions = selectedSubjectId.HasValue
            ? await _chatService.GetSessionsAsync(userId, selectedSubjectId)
            : [];

        var quotaStatus = selectedSubjectId.HasValue && !CanBypassSubscription()
            ? await BuildQuotaStatusAsync(numericUserId.Value, selectedSubjectId.Value, selectedSubject)
            : null;

        ViewModel = new ChatIndexViewModel
        {
            Subject = selectedSubject,
            AvailableSubjects = availableSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            SelectedSubjectId = selectedSubjectId,
            QuotaStatus = quotaStatus,
            Sessions = sessions.Select(ViewModelMapper.ToViewModel).ToList()
        };

        return Page();
    }

    private string? GetUserId()
    {
        return HttpContext.Session.GetInt32("UserId")?.ToString();
    }

    private int? GetNumericUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
    }

    private bool CanBypassSubscription()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && SubscriptionPermissions.CanBypassSubscription(roleId.Value);
    }

    private async Task<QuotaStatusViewModel> BuildQuotaStatusAsync(
        int userId,
        int subjectId,
        SubjectViewModel? subject)
    {
        var quota = await _subscriptionService.GetChatQuotaStatusAsync(userId, subjectId);
        var viewModel = ViewModelMapper.ToViewModel(quota);
        viewModel.SubjectCode = subject?.Code;
        viewModel.SubjectName = subject?.Name;
        return viewModel;
    }
}
