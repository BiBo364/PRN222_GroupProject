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
public class CreateModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public CreateModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IHubContext<AppHub> appHub)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _appHub = appHub;
    }

    [BindProperty]
    public DocumentCreateViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? subjectId = null)
    {
        if (subjectId.HasValue && !CanUploadToSubject(subjectId.Value))
        {
            TempData["Error"] = "Bạn chỉ được phép upload tài liệu cho môn học được gán.";
            return RedirectToPage("/Documents/Index");
        }

        ViewModel = await BuildCreateViewModelAsync(subjectId, allowFallback: true);
        if (ViewModel.Subject is null)
            TempData["Error"] = GetSubjectAccessError() ?? "No subject found in database. Please create a subject first.";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var baseModel = await BuildCreateViewModelAsync(ViewModel.SubjectId, allowFallback: false);
        ViewModel.Subject = baseModel.Subject;
        ViewModel.SubjectId = baseModel.SubjectId;
        ViewModel.ChapterOptions = baseModel.ChapterOptions;

        if (ViewModel.Subject is null || !ViewModel.SubjectId.HasValue)
        {
            ModelState.AddModelError(string.Empty, GetSubjectAccessError() ?? "No subject configured. Please create a subject first.");
            return Page();
        }

        if (!CanUploadToSubject(ViewModel.SubjectId.Value))
        {
            ModelState.AddModelError(string.Empty, "Bạn chỉ được phép upload tài liệu cho môn học được gán.");
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToPage("/Account/Login");

        var created = await _documentService.CreateDocumentEntryAsync(
            ViewModel.OriginalName!,
            ViewModel.FileType,
            ViewModel.SubjectId.Value,
            ViewModel.ChapterId,
            userId.Value);

        if (created is null)
        {
            TempData["Error"] = "Create document entry failed.";
            return Page();
        }

        await BroadcastDocumentCreatedAsync(created);
        await BroadcastCourseUpdatedAsync(created.SubjectId);
        TempData["Success"] = $"Created file entry: {created.OriginalName}";
        return RedirectToPage("/Documents/Details", new { id = created.Id });
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
}
