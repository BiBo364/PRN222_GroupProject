using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
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
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

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

        return View(new ChatIndexViewModel
        {
            Subject = selectedSubject,
            AvailableSubjects = availableSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            SelectedSubjectId = selectedSubjectId,
            Sessions = sessions.Select(ViewModelMapper.ToViewModel).ToList()
        });
    }

    public async Task<IActionResult> New(int? subjectId)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

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
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

        var session = await _chatService.GetSessionAsync(id, userId);
        if (session is null)
            return NotFound();

        return View(new ChatConversationViewModel
        {
            Session = ViewModelMapper.ToViewModel(session),
            AvailableSubjects = (await _chatService.GetAvailableSubjectsAsync())
                .Select(ViewModelMapper.ToViewModel)
                .ToList(),
            SelectedSubjectId = session.SubjectId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string id, ChatConversationViewModel model)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

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
                    })
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

    private async Task<IActionResult?> EnsureChatAccessAsync()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is not null && SubscriptionPermissions.CanBypassSubscription(roleId.Value))
            return null;

        var numericUserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (await _subscriptionService.HasActiveSubscriptionAsync(numericUserId))
            return null;

        TempData["Error"] = "Bạn cần subscription đang hoạt động để dùng Chat. Vui lòng tạo ticket thanh toán.";
        return RedirectToAction("Index", "Subscription");
    }
    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
