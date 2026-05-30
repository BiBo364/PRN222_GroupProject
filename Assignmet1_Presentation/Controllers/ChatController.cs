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

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

        var subject = await _chatService.GetDemoSubjectAsync();
        var sessions = await _chatService.GetSessionsAsync(userId);

        return View(new ChatIndexViewModel
        {
            Subject = subject is null ? null : ViewModelMapper.ToViewModel(subject),
            Sessions = sessions.Select(ViewModelMapper.ToViewModel).ToList()
        });
    }

    public async Task<IActionResult> New()
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var access = await EnsureChatAccessAsync();
        if (access is not null)
            return access;

        var session = await _chatService.CreateSessionAsync(userId);
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
            Session = ViewModelMapper.ToViewModel(session)
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

        if (string.IsNullOrWhiteSpace(model.Question))
        {
            ModelState.AddModelError(nameof(model.Question), "Please enter a question.");
            return View("Conversation", model);
        }

        try
        {
            await _chatService.AskAsync(id, userId, model.Question);
            return RedirectToAction(nameof(Conversation), new { id });
        }
        catch (Exception ex)
        {
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
}
