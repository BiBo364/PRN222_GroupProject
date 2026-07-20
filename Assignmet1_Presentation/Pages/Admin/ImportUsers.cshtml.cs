using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
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

    [BindProperty]
    public ManualCreateUserViewModel ManualInput { get; set; } = new();

    public string? ActiveForm { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadFormOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ActiveForm = "import";
        RemoveModelStatePrefix(nameof(ManualInput));
        await LoadFormOptionsAsync();

        if (Input.File is null || Input.File.Length == 0)
        {
            ModelState.AddModelError(
                $"{nameof(Input)}.{nameof(Input.File)}",
                "Vui lòng chọn tệp (.xlsx, .csv, .json hoặc .txt).");
        }

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

    public async Task<IActionResult> OnPostCreateManualAsync()
    {
        ActiveForm = "manual";
        RemoveModelStatePrefix(nameof(Input));
        await LoadFormOptionsAsync();

        if (ManualInput.RoleId != 2)
            ManualInput.SubjectId = null;

        if (!ModelState.IsValid)
            return Page();

        var result = await _userServices.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = ManualInput.FullName,
            Username = ManualInput.Username,
            Email = ManualInput.Email,
            RoleId = ManualInput.RoleId,
            SubjectId = ManualInput.SubjectId
        });

        if (!result.Success)
        {
            ModelState.AddModelError(
                nameof(ManualInput),
                result.Error ?? "Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin.");
            return Page();
        }

        TempData["Success"] =
            $"Đã tạo tài khoản {result.Username} cho {result.FullName}. Email đăng nhập: {result.Email}. Mật khẩu tạm thời: {result.TemporaryPassword}.";
        if (!result.NotificationSent)
        {
            TempData["Warning"] =
                $"Tài khoản đã được tạo nhưng email thông báo chưa gửi được. {result.NotificationMessage}";
        }

        return RedirectToPage(
            "/Admin/Index",
            new { createdUserId = result.UserId });
    }

    private async Task LoadFormOptionsAsync()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        var subjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList();
        var teacherBySubjectId = await BuildTeacherBySubjectIdAsync();

        Input.SubjectOptions = subjectOptions;
        Input.TeacherBySubjectId = teacherBySubjectId;
        ManualInput.SubjectOptions = subjectOptions;
        ManualInput.TeacherBySubjectId = teacherBySubjectId;
    }

    private void RemoveModelStatePrefix(string prefix)
    {
        var keys = ModelState.Keys
            .Where(key =>
                string.Equals(key, prefix, StringComparison.Ordinal)
                || key.StartsWith($"{prefix}.", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keys)
            ModelState.Remove(key);
    }

    private async Task<Dictionary<int, string>> BuildTeacherBySubjectIdAsync()
    {
        var users = await _userServices.GetAllUsersAsync();
        return users
            .Where(u => u.RoleId == 2)
            .SelectMany(user => user.AssignedSubjects.Select(subject => new { subject.Id, TeacherName = user.FullName ?? user.Username }))
            .GroupBy(item => item.Id)
            .ToDictionary(
                group => group.Key,
                group => group.First().TeacherName);
    }
}
