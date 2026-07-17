using Assignmet1_Presentation.Filters;
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

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToPage("/Account/Login");

        Dashboard = await _learningService.GetDashboardAsync(userId.Value, cancellationToken);
        return Page();
    }
}
