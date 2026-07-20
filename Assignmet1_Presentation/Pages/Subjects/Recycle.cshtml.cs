using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Pages.Subjects;

[RequireLogin]
[RequireAdmin]
public class RecycleModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public RecycleModel(ISubjectService subjectService, IHubContext<AppHub> appHub)
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
        var subjects = await _subjectService.GetDeletedSubjectsAsync();
        SubjectsPagination = PaginationHelper.Paginate(
            subjects.Select(ViewModelMapper.ToViewModel),
            PageNumber,
            10);
        PageNumber = SubjectsPagination.CurrentPage;
        Subjects = SubjectsPagination.Items.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var success = await _subjectService.RestoreSubjectAsync(id);
        if (success)
        {
            var subject = await _subjectService.GetSubjectAsync(id);
            if (subject is not null)
            {
                await _appHub.Clients.All.SendAsync("CourseCreated", ViewModelMapper.ToListItemViewModel(subject));
            }
            TempData["Success"] = "Khôi phục môn học thành công.";
        }
        else
        {
            TempData["Error"] = "Lỗi khi khôi phục môn học.";
        }

        return RedirectToPage("/Subjects/Recycle", new { pageNumber = PageNumber });
    }
}
