using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Pages.Subjects;

// @page "/Subjects/Create"

[RequireLogin]
[RequireAdmin]
public class CreateModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public CreateModel(
        ISubjectService subjectService,
        IHubContext<AppHub> appHub)
    {
        _subjectService = subjectService;
        _appHub = appHub;
    }

    [BindProperty]
    public SubjectCreateViewModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        Input = new SubjectCreateViewModel();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
       

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var created = await _subjectService.CreateSubjectAsync(Input.Code!, Input.Name!, Input.Description);
            await _appHub.Clients.All.SendAsync("CourseCreated", ViewModelMapper.ToListItemViewModel(created));
            TempData["Success"] = $"Đã tạo môn học {created.Subject.Code} - {created.Subject.Name}.";
            return RedirectToPage("/Subjects/Manage");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
