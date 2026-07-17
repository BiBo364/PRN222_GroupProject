using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
[EnableRateLimiting("quiz-submission")]
public class PlayModel : PageModel
{
    private readonly ILearningService _learningService;

    public PlayModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public LearningSetDetailDto? LearningSet { get; private set; }
    public LearningAttemptResultDto? Result { get; private set; }

    [BindProperty]
    public Dictionary<int, string?> Answers { get; set; } = [];

    [BindProperty]
    public DateTime StartedAt { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (!await LoadSetAsync(id, cancellationToken))
            return RedirectToPage("/Learning/Index");

        StartedAt = DateTime.UtcNow;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        if (!await LoadSetAsync(id, cancellationToken))
            return RedirectToPage("/Learning/Index");

        try
        {
            Result = await _learningService.SubmitQuizAsync(
                CurrentUserId(),
                new SubmitLearningAttemptRequest
                {
                    LearningSetId = id,
                    Answers = Answers,
                    StartedAt = StartedAt
                },
                cancellationToken);
            return Page();
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task<bool> LoadSetAsync(int id, CancellationToken cancellationToken)
    {
        LearningSet = await _learningService.GetLearningSetAsync(
            CurrentUserId(),
            id,
            cancellationToken);
        if (LearningSet is not null)
            return true;

        TempData["Error"] = "Không tìm thấy hoạt động ôn tập hoặc hoạt động chưa được phát hành.";
        return false;
    }

    private int CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId")
            ?? throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
    }
}
