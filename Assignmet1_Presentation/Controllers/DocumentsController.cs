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
    private readonly ISubjectService _subjectService;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        IDocumentService documentService,
        ISubjectService subjectService,
        IWebHostEnvironment environment)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        if (!CanViewDocuments())
            return RedirectToAction("Login", "Account");

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var subjects = await _subjectService.GetSubjectsAsync();

        return View(new DocumentListViewModel
        {
            Subjects = subjects.Select(ViewModelMapper.ToViewModel).ToList(),
            CanCreateSubject = DocumentPermissions.CanManageSubjects(roleId)
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
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        var viewModel = ViewModelMapper.ToDocumentDetailPage(document);
        viewModel.CanUpload = document.SubjectId.HasValue
            && DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, document.SubjectId.Value);
        viewModel.CanDelete = DocumentPermissions.CanDelete(roleId);
        viewModel.CanReindex = viewModel.CanUpload;

        return View(viewModel);
    }

    [HttpGet]
    [RequireDocumentUpload]
    public async Task<IActionResult> Create(int? subjectId = null)
    {
        if (subjectId.HasValue && !CanUploadToSubject(subjectId.Value))
        {
            TempData["Error"] = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildCreateViewModelAsync(subjectId, allowFallback: true);
        if (model.Subject is null)
            TempData["Error"] = GetSubjectAccessError() ?? "No subject found in database. Please create a subject first.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentUpload]
    public async Task<IActionResult> Create(DocumentCreateViewModel model)
    {
        var baseModel = await BuildCreateViewModelAsync(model.SubjectId, allowFallback: false);
        model.Subject = baseModel.Subject;
        model.SubjectId = baseModel.SubjectId;
        model.ChapterOptions = baseModel.ChapterOptions;

        if (model.Subject is null || !model.SubjectId.HasValue)
        {
            ModelState.AddModelError(string.Empty, GetSubjectAccessError() ?? "No subject configured. Please create a subject first.");
            return View(model);
        }

        if (!CanUploadToSubject(model.SubjectId.Value))
        {
            ModelState.AddModelError(string.Empty, "Bạn chỉ được phép upload tài liệu cho môn học được gán.");
            return View(model);
        }

        if (!ModelState.IsValid)
            return View(model);

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var created = await _documentService.CreateDocumentEntryAsync(
            model.OriginalName!,
            model.FileType,
            model.SubjectId.Value,
            model.ChapterId,
            userId.Value);

        if (created is null)
        {
            TempData["Error"] = "Create document entry failed.";
            return View(model);
        }

        TempData["Success"] = $"Created file entry: {created.OriginalName}";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentUpload]
    public async Task<IActionResult> Reindex(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document?.SubjectId is int subjectId && !CanUploadToSubject(subjectId))
        {
            TempData["Error"] = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var paths = GetStoragePaths();
        var (result, error) = await _documentService.ReindexDocumentAsync(
            id, userId.Value, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

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
    public async Task<IActionResult> Upload(int? subjectId = null)
    {
        if (subjectId.HasValue && !CanUploadToSubject(subjectId.Value))
        {
            TempData["Error"] = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildUploadViewModelAsync(subjectId, allowFallback: true);
        if (model.Subject is null)
            TempData["Error"] = GetSubjectAccessError() ?? "No subject found in database. Please create a subject first.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireDocumentUpload]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model)
    {
        var baseModel = await BuildUploadViewModelAsync(model.SubjectId, allowFallback: false);
        model.Subject = baseModel.Subject;
        model.SubjectId = baseModel.SubjectId;
        model.ChapterOptions = baseModel.ChapterOptions;

        if (model.Subject is null || !model.SubjectId.HasValue)
        {
            var subjectError = GetSubjectAccessError() ?? "No subject configured. Please create a subject first.";
            ModelState.AddModelError(string.Empty, subjectError);

            if (IsAjaxRequest())
                return BadRequest(new { error = subjectError });

            return View(model);
        }

        if (!CanUploadToSubject(model.SubjectId.Value))
        {
            const string accessError = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            ModelState.AddModelError(string.Empty, accessError);

            if (IsAjaxRequest())
                return BadRequest(new { error = accessError });

            return View(model);
        }

        model.ChapterId ??= model.Subject.Chapters.FirstOrDefault()?.Id;

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return BadRequest(new { error = GetFirstModelError() ?? "Please select a valid file before uploading." });

            return View(model);
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
        {
            if (IsAjaxRequest())
                return Unauthorized(new { error = "Your login session has expired. Please sign in again." });

            return RedirectToAction("Login", "Account");
        }

        var paths = GetStoragePaths();
        await using var stream = model.File!.OpenReadStream();

        var (result, error) = await _documentService.UploadAndProcessAsync(
            stream,
            model.File.FileName,
            model.File.Length,
            model.SubjectId.Value,
            model.ChapterId,
            userId.Value,
            paths.StorageRoot,
            paths.WebRoot);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Upload failed.");

            if (IsAjaxRequest())
                return BadRequest(new { error = error ?? "Upload failed." });

            return View(model);
        }

        if (IsAjaxRequest())
        {
            TempData["Success"] = $"Uploaded and indexed: {result.OriginalName}";
            return Json(new
            {
                message = $"Uploaded and indexed: {result.OriginalName}",
                redirectUrl = Url.Action(nameof(Index), "Documents")
            });
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

    [HttpDelete("/api/documents/{id}")]
    [RequireDocumentDelete]
    public async Task<IActionResult> DeleteApi(int id)
    {
        var paths = GetStoragePaths();
        var deleted = await _documentService.DeleteDocumentAsync(
            id, paths.StorageRoot, paths.ContentRoot, paths.WebRoot);

        if (!deleted)
            return NotFound(new { message = "Document not found." });

        return NoContent();
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
        return await BuildUploadViewModelAsync(null, allowFallback: true);
    }

    private async Task<DocumentUploadViewModel> BuildUploadViewModelAsync(int? subjectId, bool allowFallback)
    {
        var subject = await ResolveSubjectAsync(subjectId, allowFallback);
        return new DocumentUploadViewModel
        {
            Subject = subject,
            SubjectId = subject?.Id,
            ChapterOptions = subject?.Chapters
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"Chapter {c.Number}: {c.Title}"
                })
                .ToList() ?? []
        };
    }

    private async Task<DocumentCreateViewModel> BuildCreateViewModelAsync()
    {
        return await BuildCreateViewModelAsync(null, allowFallback: true);
    }

    private async Task<DocumentCreateViewModel> BuildCreateViewModelAsync(int? subjectId, bool allowFallback)
    {
        var subject = await ResolveSubjectAsync(subjectId, allowFallback);
        return new DocumentCreateViewModel
        {
            Subject = subject,
            SubjectId = subject?.Id,
            ChapterOptions = subject?.Chapters
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"Chapter {c.Number}: {c.Title}"
                })
                .ToList() ?? []
        };
    }

    private async Task<SubjectViewModel?> ResolveSubjectAsync(int? subjectId, bool allowFallback)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");

        if (roleId == DocumentPermissions.TeacherRoleId)
        {
            if (!userSubjectId.HasValue)
                return null;

            if (subjectId.HasValue && subjectId.Value != userSubjectId.Value)
                return null;

            var assignedSubject = await _subjectService.GetSubjectAsync(userSubjectId.Value);
            return assignedSubject is null ? null : ViewModelMapper.ToViewModel(assignedSubject.Subject);
        }

        if (subjectId.HasValue)
        {
            var subject = await _subjectService.GetSubjectAsync(subjectId.Value);
            if (subject is not null)
                return ViewModelMapper.ToViewModel(subject.Subject);

            return null;
        }

        if (!allowFallback)
            return null;

        var firstSubject = (await _subjectService.GetSubjectsAsync()).FirstOrDefault();
        if (firstSubject is null)
            return null;

        var firstDetail = await _subjectService.GetSubjectAsync(firstSubject.Id);
        return firstDetail is null ? null : ViewModelMapper.ToViewModel(firstDetail.Subject);
    }

    private bool CanUploadToSubject(int subjectId)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId is null)
            return false;

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        return DocumentPermissions.CanUploadToSubject(roleId.Value, userSubjectId, subjectId);
    }

    private string? GetSubjectAccessError()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId != DocumentPermissions.TeacherRoleId)
            return null;

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        return userSubjectId.HasValue
            ? null
            : "Bạn chưa được gán môn học. Vui lòng liên hệ quản trị viên.";
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }

    private string? GetFirstModelError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
    }
}
