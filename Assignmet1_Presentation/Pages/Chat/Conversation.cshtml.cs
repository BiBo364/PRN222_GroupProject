using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Chat;

[RequireLogin]
public class ConversationModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly ISubscriptionService _subscriptionService;

    public ConversationModel(IChatService chatService, ISubscriptionService subscriptionService)
    {
        _chatService = chatService;
        _subscriptionService = subscriptionService;
    }

    [BindProperty]
    public ChatConversationViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToPage("/Account/Login");

        var session = await _chatService.GetSessionAsync(id, userId);
        if (session is null)
            return NotFound();

        ViewModel = new ChatConversationViewModel
        {
            Session = ViewModelMapper.ToViewModel(session),
            AvailableSubjects = (await _chatService.GetAvailableSubjectsAsync())
                .Select(ViewModelMapper.ToViewModel)
                .ToList(),
            SelectedSubjectId = session.SubjectId
        };

        if (session.SubjectId.HasValue && !CanBypassSubscription())
        {
            ViewModel.QuotaStatus = await BuildQuotaStatusAsync(
                numericUserId.Value,
                session.SubjectId.Value,
                ViewModel.AvailableSubjects.FirstOrDefault(subject => subject.Id == session.SubjectId.Value));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAskAsync(string id)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToPage("/Account/Login");

        id = id.Trim();
        var session = await _chatService.GetSessionAsync(id, userId);
        if (session is null)
        {
            TempData["Error"] = "Conversation not found. Please start a new chat.";
            return RedirectToPage("/Chat/Index");
        }

        ViewModel.Session = ViewModelMapper.ToViewModel(session);
        ViewModel.SelectedSubjectId = session.SubjectId;
        ViewModel.AvailableSubjects = (await _chatService.GetAvailableSubjectsAsync())
            .Select(ViewModelMapper.ToViewModel)
            .ToList();

        if (session.SubjectId.HasValue && !CanBypassSubscription())
        {
            ViewModel.QuotaStatus = await BuildQuotaStatusAsync(
            numericUserId.Value,
            session.SubjectId.Value,
            ViewModel.AvailableSubjects.FirstOrDefault(s => s.Id == session.SubjectId.Value));

            if (ViewModel.QuotaStatus is not null && !ViewModel.QuotaStatus.IsAllowed)
            {
                var blockedMessage = ViewModel.QuotaStatus.Message
                    ?? "Free quota da het. Vui long doi reset hoac nang cap Plus.";

                if (IsAjaxRequest())
                    return StatusCode(403, new { error = blockedMessage });

                TempData["Error"] = blockedMessage;
                return RedirectToPage("/Chat/Conversation", new { id });
            }
        }

        if (string.IsNullOrWhiteSpace(ViewModel.Question))
        {
            ModelState.AddModelError(nameof(ViewModel.Question), "Please enter a question.");
            if (IsAjaxRequest())
                return BadRequest(new { error = "Please enter a question." });
            return Page();
        }

        try
        {
            var reply = await _chatService.AskAsync(id, userId, ViewModel.Question);
            ChatQuotaStatusDto? updatedQuota = null;

            if (session.SubjectId.HasValue && !CanBypassSubscription())
            {
                updatedQuota = await _subscriptionService.RecordSuccessfulQuestionAsync(
                    numericUserId.Value,
                    session.SubjectId.Value);
            }

            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    answer = reply.Answer,
                    foundInDocuments = reply.FoundInDocuments,
                    citations = reply.Citations.Select(citation => new
                    {
                        documentName = citation.DocumentName,
                        slideNumber = citation.SlideNumber,
                        score = citation.Score
                    }),
                    quota = updatedQuota is null
                        ? null
                        : new
                        {
                            isPlus = updatedQuota.IsPlus,
                            isAllowed = updatedQuota.IsAllowed,
                            questionLimit = updatedQuota.QuestionLimit,
                            questionsUsed = updatedQuota.QuestionsUsed,
                            questionsRemaining = updatedQuota.QuestionsRemaining,
                            currentPlanName = updatedQuota.CurrentPlanName,
                            currentPackageName = updatedQuota.CurrentPackageName,
                            message = updatedQuota.Message,
                            windowEndText = updatedQuota.WindowEndAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                        }
                });
            }

            return RedirectToPage("/Chat/Conversation", new { id });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return BadRequest(new { error = ex.Message });

            TempData["Error"] = ex.Message;
            return RedirectToPage("/Chat/Conversation", new { id });
        }
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

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
