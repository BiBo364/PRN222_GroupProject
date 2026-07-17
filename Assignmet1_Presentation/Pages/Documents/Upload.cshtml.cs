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
[RequireDocumentUpload]
public class UploadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IWebHostEnvironment _environment;
    private readonly IHubContext<AppHub> _appHub;

    public UploadModel(
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

    [BindProperty]
    public DocumentUploadViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? subjectId = null)
    {
        if (subjectId.HasValue && !CanUploadToSubject(subjectId.Value))
        {
            TempData["Error"] = "Bạn chỉ được phép tải tài liệu lên môn học được phân công.";
            return RedirectToPage("/Documents/Index");
        }

        ViewModel = await BuildUploadViewModelAsync(subjectId, allowFallback: true);
        if (ViewModel.Subject is null)
            TempData["Error"] = GetSubjectAccessError() ?? "Chưa có môn học trong hệ thống. Vui lòng tạo môn học trước.";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var baseModel = await BuildUploadViewModelAsync(ViewModel.SubjectId, allowFallback: false);
        ViewModel.Subject = baseModel.Subject;
        ViewModel.SubjectId = baseModel.SubjectId;
        ViewModel.ChapterOptions = baseModel.ChapterOptions;

        if (ViewModel.Subject is null || !ViewModel.SubjectId.HasValue)
        {
            var subjectError = GetSubjectAccessError() ?? "Chưa có môn học được cấu hình. Vui lòng tạo môn học trước.";
            ModelState.AddModelError(string.Empty, subjectError);

            if (IsAjaxRequest())
                return BadRequest(new { error = subjectError });

            return Page();
        }

        if (!CanUploadToSubject(ViewModel.SubjectId.Value))
        {
            const string accessError = "Bạn chỉ được phép tải tài liệu lên môn học được phân công.";
            ModelState.AddModelError(string.Empty, accessError);

            if (IsAjaxRequest())
                return BadRequest(new { error = accessError });

            return Page();
        }

        ViewModel.ChapterId ??= ViewModel.Subject.Chapters.FirstOrDefault()?.Id;

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return BadRequest(new { error = GetFirstModelError() ?? "Vui lòng chọn tài liệu hợp lệ trước khi tải lên." });

            return Page();
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
        {
            if (IsAjaxRequest())
                return StatusCode(401, new { error = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });

            return RedirectToPage("/Account/Login");
        }

        var paths = GetStoragePaths();
        await using var stream = ViewModel.File!.OpenReadStream();

        var (result, error) = await _documentService.UploadAndProcessAsync(
            stream,
            ViewModel.File.FileName,
            ViewModel.File.Length,
            ViewModel.SubjectId.Value,
            ViewModel.ChapterId,
            userId.Value,
            paths.StorageRoot,
            paths.WebRoot);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Tải tài liệu lên thất bại.");

            if (IsAjaxRequest())
                return BadRequest(new { error = error ?? "Tải tài liệu lên thất bại." });

            return Page();
        }

        await BroadcastDocumentCreatedAsync(result.Id);
        await BroadcastCourseUpdatedAsync(ViewModel.SubjectId);
        if (IsAjaxRequest())
        {
            TempData["Success"] = $"Đã tải lên và lập chỉ mục: {result.OriginalName}.";
            return new JsonResult(new
            {
                message = $"Đã tải lên và lập chỉ mục: {result.OriginalName}.",
                redirectUrl = Url.Page("/Subjects/Details", new { id = ViewModel.SubjectId.Value })
            });
        }

        TempData["Success"] = $"Đã tải lên và lập chỉ mục: {result.OriginalName}.";
        return RedirectToPage("/Subjects/Details", new { id = ViewModel.SubjectId.Value });
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

    private async Task BroadcastDocumentCreatedAsync(Assignment1_Service.Models.DocumentDetailDto document)
    {
        await _appHub.Clients.All.SendAsync("DocumentCreated", ViewModelMapper.ToListItemViewModel(document));
    }

    private async Task BroadcastDocumentCreatedAsync(int documentId)
    {
        var document = await _documentService.GetDocumentByIdAsync(documentId);
        if (document is null)
            return;

        await BroadcastDocumentCreatedAsync(document);
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
                    Text = $"Chương {c.Number}: {c.Title}"
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
                    Text = $"Chương {c.Number}: {c.Title}"
                })
                .ToList() ?? []
        };
    }

    private async Task<SubjectViewModel?> ResolveSubjectAsync(int? subjectId, bool allowFallback)
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");

        if (roleId == DocumentPermissions.LecturerRoleId)
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
        if (roleId != DocumentPermissions.LecturerRoleId)
            return null;

        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        return userSubjectId.HasValue
            ? null
            : "Bạn chưa được phân công môn học. Vui lòng liên hệ quản trị viên.";
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
