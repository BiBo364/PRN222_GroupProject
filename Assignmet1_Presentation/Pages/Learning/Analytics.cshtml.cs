using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLecturer]
public class AnalyticsModel : PageModel
{
    private readonly ILearningService _learningService;

    public AnalyticsModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public QuizAnalyticsDashboardDto? Dashboard { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? QuizId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 30;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Dashboard = await _learningService.GetQuizAnalyticsAsync(
                CurrentUserId(),
                QuizId,
                Days,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToPage("/Learning/Analytics");
        }

        if (Dashboard is not null)
            return Page();

        TempData["Error"] = "Bạn cần được phân công môn học để xem thống kê Quiz.";
        return RedirectToPage("/Learning/Index");
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }
}
