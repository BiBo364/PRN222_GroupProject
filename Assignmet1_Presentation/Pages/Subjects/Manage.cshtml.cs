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
[RequireTeacher]
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

    public async Task<IActionResult> OnGetAsync()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        Subjects = subjects.Select(ViewModelMapper.ToViewModel).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var (success, error) = await _subjectService.DeleteSubjectAsync(id);
        if (success)
        {
            await _appHub.Clients.All.SendAsync("CourseDeleted", id);
            TempData["Success"] = "Deleted subject.";
        }
        else
        {
            TempData["Error"] = error ?? "Error deleting subject.";
        }

        return RedirectToPage("/Subjects/Manage");
    }
}