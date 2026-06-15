using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class ChatController : Controller
{
    private readonly IChatService _chatService;
    private readonly ISubscriptionService _subscriptionService;

    public ChatController(IChatService chatService, ISubscriptionService subscriptionService)
    {
        _chatService = chatService;
        _subscriptionService = subscriptionService;
    }

    public async Task<IActionResult> Index(int? subjectId)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToAction("Login", "Account");

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
                TempData["Error"] = "Selected subject is not available for chat.";
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

        return View(new ChatIndexViewModel
        {
            Subject = selectedSubject,
            AvailableSubjects = availableSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            SelectedSubjectId = selectedSubjectId,
            QuotaStatus = quotaStatus,
            Sessions = sessions.Select(ViewModelMapper.ToViewModel).ToList()
        });
    }

    public async Task<IActionResult> New(int? subjectId)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        if (!subjectId.HasValue || subjectId.Value <= 0)
        {
            TempData["Error"] = "Please select a subject before starting a new conversation.";
            return RedirectToAction(nameof(Index));
        }

        var session = await _chatService.CreateSessionAsync(userId, subjectId.Value);
        return RedirectToAction(nameof(Conversation), new { id = session.Id });
    }

    public async Task<IActionResult> Conversation(string id)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToAction("Login", "Account");

        var session = await _chatService.GetSessionAsync(id, userId);
        if (session is null)
            return NotFound();

        var viewModel = new ChatConversationViewModel
        {
            Session = ViewModelMapper.ToViewModel(session),
            AvailableSubjects = (await _chatService.GetAvailableSubjectsAsync())
                .Select(ViewModelMapper.ToViewModel)
                .ToList(),
            SelectedSubjectId = session.SubjectId
        };

        if (session.SubjectId.HasValue && !CanBypassSubscription())
        {
            viewModel.QuotaStatus = await BuildQuotaStatusAsync(
                numericUserId.Value,
                session.SubjectId.Value,
                viewModel.AvailableSubjects.FirstOrDefault(subject => subject.Id == session.SubjectId.Value));
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string id, ChatConversationViewModel model)
    {
        var userId = GetUserId();
        var numericUserId = GetNumericUserId();
        if (userId is null || numericUserId is null)
            return RedirectToAction("Login", "Account");

        id = id.Trim();
        var session = await _chatService.GetSessionAsync(id, userId);
        if (session is null)
        {
            TempData["Error"] = "Conversation not found. Please start a new chat.";
            return RedirectToAction(nameof(Index));
        }

        model.Session = ViewModelMapper.ToViewModel(session);
        model.SelectedSubjectId = session.SubjectId;
        model.AvailableSubjects = (await _chatService.GetAvailableSubjectsAsync())
            .Select(ViewModelMapper.ToViewModel)
            .ToList();

        if (session.SubjectId.HasValue && !CanBypassSubscription())
        {
            model.QuotaStatus = await BuildQuotaStatusAsync(
                numericUserId.Value,
                session.SubjectId.Value,
                model.AvailableSubjects.FirstOrDefault(subject => subject.Id == session.SubjectId.Value));

            if (model.QuotaStatus is not null && !model.QuotaStatus.IsAllowed)
            {
                var blockedMessage = model.QuotaStatus.Message
                    ?? "Free quota da het. Vui long doi reset hoac nang cap Plus.";

                if (IsAjaxRequest())
                    return StatusCode(403, new { error = blockedMessage });

                TempData["Error"] = blockedMessage;
                return RedirectToAction(nameof(Conversation), new { id });
            }
        }

        if (string.IsNullOrWhiteSpace(model.Question))
        {
            ModelState.AddModelError(nameof(model.Question), "Please enter a question.");
            if (IsAjaxRequest())
                return BadRequest(new { error = "Please enter a question." });

            return View("Conversation", model);
        }

        try
        {
            var reply = await _chatService.AskAsync(id, userId, model.Question);
            ChatQuotaStatusDto? updatedQuota = null;

            if (session.SubjectId.HasValue && !CanBypassSubscription())
            {
                updatedQuota = await _subscriptionService.RecordSuccessfulQuestionAsync(
                    numericUserId.Value,
                    session.SubjectId.Value);
            }

            if (IsAjaxRequest())
            {
                return Json(new
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

            return RedirectToAction(nameof(Conversation), new { id });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return BadRequest(new { error = ex.Message });

            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Conversation), new { id });
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
