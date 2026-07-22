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
    public PaginationSlice<DocumentListItemViewModel> DocumentsPagination { get; private set; } =
        PaginationHelper.Paginate<DocumentListItemViewModel>([], 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!CanViewSubjects())
            return RedirectToPage("/Account/Login");

        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var allDocuments = subject.Documents
            .Select(ViewModelMapper.ToViewModel)
            .ToList();
        DocumentsPagination = PaginationHelper.Paginate(
            allDocuments,
            PageNumber,
            10);
        PageNumber = DocumentsPagination.CurrentPage;
        ViewModel = new SubjectDetailViewModel
        {
            Subject = ViewModelMapper.ToViewModel(subject.Subject),
            Documents = DocumentsPagination.Items.ToList(),
            TotalDocumentCount = allDocuments.Count,
            IndexedDocumentCount = allDocuments.Count(document => document.Status == "indexed"),
            CanCreateSubject = DocumentPermissions.CanManageSubjects(roleId),
            CanUploadDocument = CanUploadToSubject(roleId, id),
            CanEditDocument = CanUploadToSubject(roleId, id),
            CanDeleteDocument = DocumentPermissions.CanDelete(roleId)
                && CanUploadToSubject(roleId, id)
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

    private bool CanUploadToSubject(int roleId, int subjectId)
        => DocumentPermissions.CanUploadToAssignedSubject(
            roleId,
            DocumentPermissions.GetAssignedSubjectIds(HttpContext.Session),
            subjectId);
}
