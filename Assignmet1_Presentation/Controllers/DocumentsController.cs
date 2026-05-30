using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(IDocumentService documentService, IWebHostEnvironment environment)
    {
        _documentService = documentService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        if (!CanViewDocuments())
            return RedirectToAction("Login", "Account");

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var subject = await _documentService.GetDemoSubjectAsync();
        var documents = await _documentService.GetDocumentsAsync();

        if (subject is not null)
            documents = documents.Where(d => d.SubjectId == subject.Id).ToList();

        return View(new DocumentListViewModel
        {
            DemoSubject = subject is null ? null : ViewModelMapper.ToViewModel(subject),
            Documents = documents.Select(ViewModelMapper.ToViewModel).ToList(),
            CanUpload = DocumentPermissions.CanUpload(roleId),
            CanDelete = DocumentPermissions.CanDelete(roleId)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!CanViewDocuments())
            return RedirectToAction("Login", "Account");

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var viewModel = ViewModelMapper.ToDocumentDetailPage(document);
        viewModel.CanUpload = DocumentPermissions.CanUpload(roleId);
        viewModel.CanDelete = DocumentPermissions.CanDelete(roleId);
        viewModel.CanReindex = DocumentPermissions.CanUpload(roleId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentUpload]
    public async Task<IActionResult> Reindex(int id)
    {
        var paths = GetStoragePaths();
        var (result, error) = await _documentService.ReindexDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

        if (result is null)
        {
            TempData["Error"] = error ?? "Re-index failed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = $"Re-indexed: {result.OriginalName} ({result.ChunkCount} chunks).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [RequireDocumentUpload]
    public async Task<IActionResult> Upload()
    {
        var model = await BuildUploadViewModelAsync();
        if (model.DemoSubject is null)
            TempData["Error"] = "No subject found in database. Please seed at least one subject.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentUpload]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model)
    {
        var baseModel = await BuildUploadViewModelAsync();
        model.DemoSubject = baseModel.DemoSubject;
        model.ChapterOptions = baseModel.ChapterOptions;

        if (model.DemoSubject is null)
        {
            ModelState.AddModelError(string.Empty, "No subject configured for demo.");
            return View(model);
        }

        model.ChapterId ??= model.DemoSubject.Chapters.FirstOrDefault()?.Id;

        if (!ModelState.IsValid)
            return View(model);

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var paths = GetStoragePaths();
        await using var stream = model.File!.OpenReadStream();

        var (result, error) = await _documentService.UploadAndProcessAsync(
            stream,
            model.File.FileName,
            model.File.Length,
            model.DemoSubject.Id,
            model.ChapterId,
            userId.Value,
            paths.StorageRoot,
            paths.WebRoot);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Upload failed.");
            return View(model);
        }

        TempData["Success"] = $"Uploaded and indexed: {result.OriginalName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var paths = GetStoragePaths();
        var deleted = await _documentService.DeleteDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

        if (!deleted)
        {
            TempData["Error"] = "Document not found.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    private bool CanViewDocuments()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanView(roleId.Value);
    }

    private (string StorageRoot, string ContentRoot, string WebRoot) GetStoragePaths()
    {
        return (
            Path.Combine(_environment.ContentRootPath, "uploads"),
            _environment.ContentRootPath,
            _environment.WebRootPath
        );
    }

    private async Task<DocumentUploadViewModel> BuildUploadViewModelAsync()
    {
        var subjectDto = await _documentService.GetDemoSubjectAsync();
        var subject = subjectDto is null ? null : ViewModelMapper.ToViewModel(subjectDto);
        return new DocumentUploadViewModel
        {
            DemoSubject = subject,
            ChapterOptions = subject?.Chapters
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"Chapter {c.Number}: {c.Title}"
                })
                .ToList() ?? []
        };
    }
}
