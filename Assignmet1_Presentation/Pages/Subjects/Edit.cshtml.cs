using Assignment1_Service.Services.Interfaces;
using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
namespace Assignmet1_Presentation.Pages.Subjects;
[RequireLogin]
[RequireTeacher]
public class EditModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public EditModel(ISubjectService subjectService, IHubContext<AppHub> appHub)
    {
        _subjectService = subjectService;
        _appHub = appHub;
    }

    [BindProperty]  // ← thêm attribute này
    public SubjectEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        Input = new SubjectEditViewModel
        {
            Id = subject.Subject.Id,
            Code = subject.Subject.Code,
            Name = subject.Subject.Name,
            Description = subject.Subject.Description
        };
        return Page();
    }

    // ← bỏ parameter "model", chỉ giữ "id" từ route
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (id != Input.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _subjectService.UpdateSubjectAsync(
                id, Input.Code!, Input.Name!, Input.Description);

            await _appHub.Clients.All.SendAsync(
                "CourseUpdated", ViewModelMapper.ToListItemViewModel(updated));

            TempData["Success"] = $"Updated subject {Input.Code} successfully.";
            return RedirectToPage("/Subjects/Manage");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}