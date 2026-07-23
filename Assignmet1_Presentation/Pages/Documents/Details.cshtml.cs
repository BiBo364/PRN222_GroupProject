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
        var viewModel = ViewModelMapper.ToDocumentDetailPage(document);
        viewModel.CanUpload = document.SubjectId.HasValue
            && CanUploadToSubject(document.SubjectId.Value);
        viewModel.CanEdit = viewModel.CanUpload;
        viewModel.CanDelete = document.SubjectId.HasValue
            && DocumentPermissions.CanDeleteDocumentFromSubject(
                roleId,
                HttpContext.Session,
                document.SubjectId.Value);
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
            TempData["Error"] = "Không tìm thấy tài liệu cần cập nhật.";
            return RedirectToPage("/Documents/Index");
        }

        var (updated, error) = await _documentService.UpdateDocumentMetadataAsync(
            id,
            originalName,
            chapterId,
            userId.Value);

        if (updated is null)
        {
            TempData["Error"] = error ?? "Cập nhật tài liệu thất bại.";
            return RedirectToPage("/Documents/Details", new { id });
        }

        await BroadcastDocumentUpdatedAsync(updated.Id);
        await BroadcastCourseUpdatedAsync(updated.SubjectId);
        TempData["Success"] = $"Đã cập nhật tài liệu: {updated.OriginalName}.";
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
            TempData["Error"] = error ?? "Lập lại chỉ mục thất bại.";
            return RedirectToPage("/Documents/Details", new { id });
        }

        await BroadcastDocumentUpdatedAsync(result.Id);
        await BroadcastCourseUpdatedAsync(document?.SubjectId);
        TempData["Success"] = $"Re-indexed: {result.OriginalName} ({result.ChunkCount} chunks).";
        return RedirectToPage("/Documents/Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        int? returnSubjectId = null,
        int? returnPageNumber = null)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (roleId is null || userId is null || !DocumentPermissions.CanDelete(roleId.Value))
        {
            TempData["Error"] = "Bạn không có quyền xóa tài liệu.";
            return RedirectAfterDelete(returnSubjectId, returnPageNumber);
        }

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document is null)
        {
            TempData["Error"] = "Không tìm thấy tài liệu cần xóa.";
            return RedirectAfterDelete(returnSubjectId, returnPageNumber);
        }

        if (!document.SubjectId.HasValue
            || !DocumentPermissions.CanDeleteDocumentFromSubject(
                roleId.Value,
                HttpContext.Session,
                document.SubjectId.Value))
        {
            TempData["Error"] = "Bạn chỉ có thể xóa tài liệu thuộc môn học được phân công.";
            return RedirectAfterDelete(
                returnSubjectId ?? document.SubjectId,
                returnPageNumber);
        }

        var deletedDocumentName = document.OriginalName;
        var deletedSubjectId = document.SubjectId;
        var paths = GetStoragePaths();
        var deleted = await _documentService.DeleteDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot, userId.Value);

        if (!deleted)
        {
            TempData["Error"] = "Không thể xóa tài liệu. Vui lòng thử lại.";
            return RedirectAfterDelete(
                returnSubjectId ?? document.SubjectId,
                returnPageNumber);
        }

        await BroadcastDocumentDeletedAsync(id);
        await BroadcastCourseUpdatedAsync(deletedSubjectId);
        TempData["Success"] = $"Đã chuyển tài liệu vào thùng rác: {deletedDocumentName}. Bạn có thể khôi phục trong thùng rác tài liệu.";
        return RedirectAfterDelete(
            returnSubjectId ?? deletedSubjectId,
            returnPageNumber);
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

    private IActionResult RedirectAfterDelete(
        int? subjectId,
        int? pageNumber = null)
    {
        return subjectId.HasValue
            ? RedirectToPage(
                "/Subjects/Details",
                new { id = subjectId.Value, pageNumber })
            : RedirectToPage(
                "/Documents/Index",
                new { pageNumber });
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

        return DocumentPermissions.CanUploadToAssignedSubject(
            roleId.Value,
            DocumentPermissions.GetAssignedSubjectIds(HttpContext.Session),
            subjectId);
    }
}
