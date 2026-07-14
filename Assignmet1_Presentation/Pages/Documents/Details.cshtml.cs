using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Pages.Documents;

[RequireLogin]
public class DetailsModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IWebHostEnvironment _environment;
    private readonly IHubContext<AppHub> _appHub;

    public DetailsModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IWebHostEnvironment environment,
        IHubContext<AppHub> appHub)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _environment = environment;
        _appHub = appHub;
    }

    public DocumentDetailViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!CanViewDocuments())
            return RedirectToPage("/Account/Login");

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var viewModel = ViewModelMapper.ToDocumentDetailPage(document);
        viewModel.CanUpload = document.SubjectId.HasValue
            && DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, document.SubjectId.Value);
        viewModel.CanEdit = viewModel.CanUpload;
        viewModel.CanDelete = document.SubjectId.HasValue
            && DocumentPermissions.CanDelete(roleId)
            && DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, document.SubjectId.Value);
        viewModel.CanReindex = viewModel.CanUpload;
        viewModel.ChapterOptions = await BuildChapterOptionsAsync(document.SubjectId, document.ChapterId);

        ViewModel = viewModel;
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string originalName, int? chapterId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToPage("/Account/Login");

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu can cap nhat.";
            return RedirectToPage("/Documents/Index");
        }

        var (updated, error) = await _documentService.UpdateDocumentMetadataAsync(
            id,
            originalName,
            chapterId,
            userId.Value);

        if (updated is null)
        {
            TempData["Error"] = error ?? "Cap nhat tai lieu that bai.";
            return RedirectToPage("/Documents/Details", new { id });
        }

        await BroadcastDocumentUpdatedAsync(updated.Id);
        await BroadcastCourseUpdatedAsync(updated.SubjectId);
        TempData["Success"] = $"Da cap nhat tai lieu: {updated.OriginalName}.";
        return RedirectToPage("/Documents/Details", new { id });
    }

    public async Task<IActionResult> OnPostReindexAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToPage("/Account/Login");

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document?.SubjectId is int subjectId && !CanUploadToSubject(subjectId))
        {
            TempData["Error"] = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            return RedirectToPage("/Documents/Details", new { id });
        }

        var paths = GetStoragePaths();
        var (result, error) = await _documentService.ReindexDocumentAsync(
            id, userId.Value, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

        if (result is null)
        {
            TempData["Error"] = error ?? "Re-index failed.";
            return RedirectToPage("/Documents/Details", new { id });
        }

        await BroadcastDocumentUpdatedAsync(result.Id);
        await BroadcastCourseUpdatedAsync(document?.SubjectId);
        TempData["Success"] = $"Re-indexed: {result.OriginalName} ({result.ChunkCount} chunks).";
        return RedirectToPage("/Documents/Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int? returnSubjectId = null)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (roleId is null || userId is null || !DocumentPermissions.CanDelete(roleId.Value))
        {
            TempData["Error"] = "Ban khong co quyen xoa tai lieu.";
            return RedirectAfterDelete(returnSubjectId);
        }

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu can xoa.";
            return RedirectAfterDelete(returnSubjectId);
        }

        if (!document.SubjectId.HasValue
            || !DocumentPermissions.CanUploadToSubject(
                roleId.Value,
                HttpContext.Session.GetInt32("SubjectId"),
                document.SubjectId.Value))
        {
            TempData["Error"] = "Ban chi co the xoa tai lieu trong mon hoc duoc gan.";
            return RedirectAfterDelete(returnSubjectId ?? document.SubjectId);
        }

        var deletedDocumentName = document.OriginalName;
        var deletedSubjectId = document.SubjectId;
        var paths = GetStoragePaths();
        var deleted = await _documentService.DeleteDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot, userId.Value);

        if (!deleted)
        {
            TempData["Error"] = "Khong the xoa tai lieu. Vui long thu lai.";
            return RedirectAfterDelete(returnSubjectId ?? document.SubjectId);
        }

        await BroadcastDocumentDeletedAsync(id);
        await BroadcastCourseUpdatedAsync(deletedSubjectId);
        TempData["Success"] = $"Da xoa mem tai lieu: {deletedDocumentName}. Ban co the khoi phuc trong Thung rac tai lieu.";
        return RedirectAfterDelete(returnSubjectId ?? deletedSubjectId);
    }

    private bool CanViewDocuments()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanView(roleId.Value);
    }

    private async Task BroadcastCourseUpdatedAsync(int? subjectId)
    {
        if (!subjectId.HasValue)
            return;

        var subject = await _subjectService.GetSubjectAsync(subjectId.Value);
        if (subject is null)
            return;

        await _appHub.Clients.All.SendAsync("CourseUpdated", ViewModelMapper.ToListItemViewModel(subject));
    }

    private async Task BroadcastDocumentUpdatedAsync(int documentId)
    {
        var document = await _documentService.GetDocumentByIdAsync(documentId);
        if (document is null)
            return;

        await _appHub.Clients.All.SendAsync("DocumentUpdated", ViewModelMapper.ToListItemViewModel(document));
    }

    private Task BroadcastDocumentDeletedAsync(int documentId)
    {
        return _appHub.Clients.All.SendAsync("DocumentDeleted", documentId);
    }

    private async Task<List<SelectListItem>> BuildChapterOptionsAsync(int? subjectId, int? selectedChapterId)
    {
        if (!subjectId.HasValue)
            return [];

        var subject = await _subjectService.GetSubjectAsync(subjectId.Value);
        if (subject is null)
            return [];

        return subject.Subject.Chapters
            .OrderBy(chapter => chapter.Number)
            .Select(chapter => new SelectListItem
            {
                Value = chapter.Id.ToString(),
                Text = $"Ch.{chapter.Number}: {chapter.Title}",
                Selected = selectedChapterId == chapter.Id
            })
            .ToList();
    }

    private IActionResult RedirectAfterDelete(int? subjectId)
    {
        return subjectId.HasValue
            ? RedirectToPage("/Subjects/Details", new { id = subjectId.Value })
            : RedirectToPage("/Documents/Index");
    }

    private (string StorageRoot, string ContentRoot, string WebRoot) GetStoragePaths()
    {
        return (
            Path.Combine(_environment.ContentRootPath, "uploads"),
            _environment.ContentRootPath,
            _environment.WebRootPath
        );
    }

    private bool CanUploadToSubject(int subjectId)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null)
            return false;

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        return DocumentPermissions.CanUploadToSubject(roleId.Value, userSubjectId, subjectId);
    }
}
