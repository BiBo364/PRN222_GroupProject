using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Pages.Documents;

[RequireLogin]
public class RecycleModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public RecycleModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IHubContext<AppHub> appHub)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _appHub = appHub;
    }

    public List<DocumentListItemViewModel> Documents { get; private set; } = [];
    public PaginationSlice<DocumentListItemViewModel> DocumentsPagination { get; private set; } =
        PaginationHelper.Paginate<DocumentListItemViewModel>([], 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            TempData["Error"] = "Bạn không có quyền xem thùng rác tài liệu.";
            return RedirectToPage("/Home/Index");
        }

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var docs = await _documentService.GetDeletedDocumentsAsync();

        // Filter based on role
        if (roleId.Value == DocumentPermissions.LecturerRoleId)
        {
            docs = docs.Where(d => d.SubjectId == userSubjectId).ToList();
        }

        DocumentsPagination = PaginationHelper.Paginate(
            docs.Select(ViewModelMapper.ToViewModel),
            PageNumber,
            10);
        PageNumber = DocumentsPagination.CurrentPage;
        Documents = DocumentsPagination.Items.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            TempData["Error"] = "Bạn không có quyền khôi phục tài liệu.";
            return RedirectToPage("/Home/Index");
        }

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var deletedDocument = await _documentService.GetDeletedDocumentByIdAsync(id);
        if (deletedDocument is null)
        {
            TempData["Error"] = "Không tìm thấy tài liệu trong thùng rác.";
            return RedirectToPage("/Documents/Recycle", new { pageNumber = PageNumber });
        }

        if (roleId.Value == DocumentPermissions.LecturerRoleId)
        {
            var doc = deletedDocument;
            if (doc != null && doc.SubjectId != userSubjectId)
            {
                TempData["Error"] = "Bạn chỉ có thể khôi phục tài liệu thuộc môn học được phân công.";
                return RedirectToPage("/Documents/Recycle", new { pageNumber = PageNumber });
            }
        }

        var (success, restoreError) = await _documentService.RestoreDocumentAsync(id);
        if (success)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document is not null)
            {
                await _appHub.Clients.All.SendAsync("DocumentUpdated", ViewModelMapper.ToListItemViewModel(document));
                if (document.SubjectId is int subjectId)
                {
                    var subject = await _subjectService.GetSubjectAsync(subjectId);
                    if (subject is not null)
                        await _appHub.Clients.All.SendAsync("CourseUpdated", ViewModelMapper.ToListItemViewModel(subject));
                }
            }
            TempData["Success"] = $"Đã khôi phục tài liệu: {deletedDocument.OriginalName}.";
        }
        else
        {
            TempData["Error"] = restoreError ?? "Khôi phục tài liệu thất bại.";
        }

        return RedirectToPage("/Documents/Recycle", new { pageNumber = PageNumber });
    }
}
