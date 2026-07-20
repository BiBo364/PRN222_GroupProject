using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Pages.Subjects;

// @page "/Subjects/Manage"
[RequireLogin]
[RequireAdmin]
public class ManageModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public ManageModel(ISubjectService subjectService, IHubContext<AppHub> appHub)
    {
        _subjectService = subjectService;
        _appHub = appHub;
    }

    public List<SubjectListItemViewModel> Subjects { get; private set; } = [];
    public PaginationSlice<SubjectListItemViewModel> SubjectsPagination { get; private set; } =
        PaginationHelper.Paginate<SubjectListItemViewModel>([], 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        SubjectsPagination = PaginationHelper.Paginate(
            subjects.Select(ViewModelMapper.ToViewModel),
            PageNumber,
            10);
        PageNumber = SubjectsPagination.CurrentPage;
        Subjects = SubjectsPagination.Items.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var (success, error) = await _subjectService.DeleteSubjectWithDocumentsAsync(id, userId);
        if (success)
        {
            await _appHub.Clients.All.SendAsync("CourseDeleted", id);
            TempData["Success"] = "Đã chuyển môn học vào thùng rác.";
        }
        else
        {
            TempData["Error"] = error ?? "Không thể xóa môn học.";
        }

        return RedirectToPage("/Subjects/Manage", new { pageNumber = PageNumber });
    }
}
