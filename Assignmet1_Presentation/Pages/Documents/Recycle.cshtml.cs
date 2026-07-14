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

    public async Task<IActionResult> OnGetAsync()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            TempData["Error"] = "Ban khong co quyen xem thung rac tai lieu.";
            return RedirectToPage("/Home/Index");
        }

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var docs = await _documentService.GetDeletedDocumentsAsync();

        // Filter based on role
        if (roleId.Value == DocumentPermissions.LecturerRoleId)
        {
            docs = docs.Where(d => d.SubjectId == userSubjectId).ToList();
        }

        Documents = docs.Select(ViewModelMapper.ToViewModel).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            TempData["Error"] = "Ban khong co quyen khoi phuc tai lieu.";
            return RedirectToPage("/Home/Index");
        }

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var deletedDocument = await _documentService.GetDeletedDocumentByIdAsync(id);
        if (deletedDocument is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu trong thung rac.";
            return RedirectToPage("/Documents/Recycle");
        }

        if (roleId.Value == DocumentPermissions.LecturerRoleId)
        {
            var doc = deletedDocument;
            if (doc != null && doc.SubjectId != userSubjectId)
            {
                TempData["Error"] = "Ban chi co the khoi phuc tai lieu trong mon hoc duoc gan.";
                return RedirectToPage("/Documents/Recycle");
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
            TempData["Success"] = $"Da khoi phuc tai lieu: {deletedDocument.OriginalName}.";
        }
        else
        {
            TempData["Error"] = restoreError ?? "Khoi phuc tai lieu that bai.";
        }

        return RedirectToPage("/Documents/Recycle");
    }
}
