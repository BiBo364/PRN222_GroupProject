using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Subjects;

// @page "/Subjects/Details/{id:int}"
[RequireLogin]
public class DetailsModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public DetailsModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public SubjectDetailViewModel ViewModel { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!CanViewSubjects())
            return RedirectToPage("/Account/Login");

        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        ViewModel = new SubjectDetailViewModel
        {
            Subject = ViewModelMapper.ToViewModel(subject.Subject),
            Documents = subject.Documents.Select(ViewModelMapper.ToViewModel).ToList(),
            CanCreateSubject = DocumentPermissions.CanManageSubjects(roleId),
            CanUploadDocument = DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, id),
            CanEditDocument = DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, id),
            CanDeleteDocument = DocumentPermissions.CanDelete(roleId)
                && DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, id)
        };

        return Page();
    }

    public IActionResult OnPost(int id)
    {
        TempData["Error"] = "Thao tác không hợp lệ. Vui lòng thử lại.";
        return RedirectToPage("/Subjects/Details", new { id });
    }

    private bool CanViewSubjects()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanView(roleId.Value);
    }
}
