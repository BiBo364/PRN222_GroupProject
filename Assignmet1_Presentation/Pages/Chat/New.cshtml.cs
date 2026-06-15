using Assignmet1_Presentation.Filters;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Chat;

[RequireLogin]
public class NewModel : PageModel
{
    private readonly IChatService _chatService;

    public NewModel(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<IActionResult> OnGetAsync(int? subjectId)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToPage("/Account/Login");

        if (!subjectId.HasValue || subjectId.Value <= 0)
        {
            TempData["Error"] = "Please select a subject before starting a new conversation.";
            return RedirectToPage("/Chat/Index");
        }

        var session = await _chatService.CreateSessionAsync(userId, subjectId.Value);
        return RedirectToPage("/Chat/Conversation", new { id = session.Id });
    }

    private string? GetUserId()
    {
        return HttpContext.Session.GetInt32("UserId")?.ToString();
    }
}
