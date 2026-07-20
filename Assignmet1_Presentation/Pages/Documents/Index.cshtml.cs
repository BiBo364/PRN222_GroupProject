using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Documents;

[RequireLogin]
public class IndexModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public IndexModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public DocumentListViewModel ViewModel { get; set; } = new();
    public PaginationSlice<SubjectListItemViewModel> SubjectsPagination { get; private set; } =
        PaginationHelper.Paginate<SubjectListItemViewModel>([], 1, 12);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CanViewDocuments())
            return RedirectToPage("/Account/Login");

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var subjects = await _subjectService.GetSubjectsAsync();
        SubjectsPagination = PaginationHelper.Paginate(
            subjects.Select(ViewModelMapper.ToViewModel),
            PageNumber,
            12);
        PageNumber = SubjectsPagination.CurrentPage;

        ViewModel = new DocumentListViewModel
        {
            Subjects = SubjectsPagination.Items.ToList(),
            CanCreateSubject = DocumentPermissions.CanManageSubjects(roleId)
        };

        return Page();
    }

    private bool CanViewDocuments()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanView(roleId.Value);
    }
}
