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
        viewModel.CanDelete = DocumentPermissions.CanDelete(roleId);
        viewModel.CanReindex = viewModel.CanUpload;

        ViewModel = viewModel;
        return Page();
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

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanDelete(roleId.Value))
        {
            TempData["Error"] = "You do not have permission to delete documents.";
            return RedirectToPage("/Documents/Index");
        }

        var document = await _documentService.GetDocumentByIdAsync(id);
        var paths = GetStoragePaths();
        var deleted = await _documentService.DeleteDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

        if (!deleted)
        {
            TempData["Error"] = "Document not found.";
            return RedirectToPage("/Documents/Index");
        }

        await BroadcastDocumentDeletedAsync(id);
        await BroadcastCourseUpdatedAsync(document?.SubjectId);
        TempData["Success"] = "Document deleted.";
        return RedirectToPage("/Documents/Index");
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
