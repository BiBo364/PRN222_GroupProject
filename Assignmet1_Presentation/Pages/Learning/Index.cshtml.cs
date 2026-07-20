using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Learning;

[RequireLogin]
public class IndexModel : PageModel
{
    private readonly ILearningService _learningService;

    public IndexModel(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public LearningDashboardDto? Dashboard { get; private set; }
    public PaginationSlice<LearningSetSummaryDto> LearningSetsPagination { get; private set; } =
        PaginationHelper.Paginate<LearningSetSummaryDto>([], 1, 9);
    public PaginationSlice<LearningAttemptSummaryDto> AttemptsPagination { get; private set; } =
        PaginationHelper.Paginate<LearningAttemptSummaryDto>([], 1, 6);

    [BindProperty(SupportsGet = true)]
    public int QuizPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int AttemptPage { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToPage("/Account/Login");

        Dashboard = await _learningService.GetDashboardAsync(userId.Value, cancellationToken);
        if (Dashboard is not null)
        {
            LearningSetsPagination = PaginationHelper.Paginate(
                Dashboard.LearningSets,
                QuizPage,
                9);
            AttemptsPagination = PaginationHelper.Paginate(
                Dashboard.RecentAttempts,
                AttemptPage,
                6);
            QuizPage = LearningSetsPagination.CurrentPage;
            AttemptPage = AttemptsPagination.CurrentPage;
        }
        return Page();
    }
}
