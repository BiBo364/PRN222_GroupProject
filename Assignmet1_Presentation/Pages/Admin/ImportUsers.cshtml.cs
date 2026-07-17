using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin;

// @page "/Admin/ImportUsers"
[RequireAdmin]
[RequestSizeLimit(10 * 1024 * 1024)]
public class ImportUsersModel : PageModel
{
    private readonly IUserServices _userServices;
    private readonly ISubjectService _subjectService;

    public ImportUsersModel(IUserServices userServices, ISubjectService subjectService)
    {
        _userServices = userServices;
        _subjectService = subjectService;
    }

    [BindProperty]
    public ImportUsersViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        Input = new ImportUsersViewModel
        {
            SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList(),
            TeacherBySubjectId = await BuildTeacherBySubjectIdAsync()
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        Input.SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList();
        Input.TeacherBySubjectId = await BuildTeacherBySubjectIdAsync();

        if (!ModelState.IsValid)
            return Page();

        if (Input.RoleId is not 2 and not 3)
        {
            ModelState.AddModelError(nameof(Input.RoleId), "Chỉ hỗ trợ nhập giảng viên hoặc học sinh, sinh viên.");
            return Page();
        }

        if (Input.RoleId != 2)
            Input.SubjectId = null;

        if (Input.RoleId == 2 && Input.SubjectId.HasValue)
        {
            var (isAvailable, availabilityError) = await _userServices.ValidateTeacherSubjectAvailabilityAsync(
                Input.SubjectId.Value);

            if (!isAvailable)
            {
                ModelState.AddModelError(nameof(Input.SubjectId), availabilityError ?? "Môn học này đã có giảng viên phụ trách.");
                return Page();
            }
        }

        var file = Input.File!;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv" && ext != ".json" && ext != ".txt")
        {
            ModelState.AddModelError(nameof(Input.File), "Chỉ hỗ trợ tệp .xlsx, .csv, .json hoặc .txt.");
            return Page();
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _userServices.ImportUsersFromFileAsync(stream, file.FileName, Input.SubjectId, Input.RoleId);

            Input.Result = new ImportResultViewModel
            {
                TotalRows = result.TotalRows,
                CreatedCount = result.CreatedCount,
                SkippedDuplicateCount = result.SkippedDuplicateCount,
                ErrorCount = result.ErrorCount,
                NotificationSentCount = result.NotificationSentCount,
                NotificationFailedCount = result.NotificationFailedCount,
                Rows = result.Rows.Select(r => new ImportRowResultViewModel
                {
                    RowNumber = r.RowNumber,
                    FullName = r.FullName,
                    Email = r.Email,
                    Username = r.Username,
                    Status = r.Status,
                    Message = r.Message,
                    NotificationSent = r.NotificationSent,
                    NotificationMessage = r.NotificationMessage
                }).ToList()
            };

            if (result.CreatedCount > 0)
                TempData["Success"] = $"Đã nhập thành công {result.CreatedCount} tài khoản mới. Đã gửi email cho {result.NotificationSentCount} tài khoản.";

            if (result.ErrorCount > 0 || result.SkippedDuplicateCount > 0 || result.NotificationFailedCount > 0)
                TempData["Warning"] = $"{result.SkippedDuplicateCount} bản ghi trùng lặp, {result.ErrorCount} lỗi, {result.NotificationFailedCount} email chưa gửi được.";
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Không thể đọc tệp: {ex.Message}");
        }

        return Page();
    }

    private async Task<Dictionary<int, string>> BuildTeacherBySubjectIdAsync()
    {
        var users = await _userServices.GetAllUsersAsync();
        return users
            .Where(u => u.RoleId == 2 && u.SubjectId.HasValue)
            .GroupBy(u => u.SubjectId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.First().FullName ?? group.First().Username);
    }
}
