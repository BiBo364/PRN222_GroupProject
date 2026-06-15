using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Controllers;

[RequireAdmin]
public class AdminController : Controller
{
    private readonly IUserServices _userServices;
    private readonly ISubjectService _subjectService;

    public AdminController(IUserServices userServices, ISubjectService subjectService)
    {
        _userServices = userServices;
        _subjectService = subjectService;
    }

    public async Task<IActionResult> Index(string? search, int? roleId, int? subjectId, int page = 1)
    {
        var allUsers = await _userServices.GetAllUsersAsync();
        var allSubjects = await _subjectService.GetSubjectsAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            allUsers = allUsers.Where(u =>
                u.Username.ToLowerInvariant().Contains(term) ||
                u.Email.ToLowerInvariant().Contains(term) ||
                (u.FullName ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
        }

        if (roleId.HasValue)
            allUsers = allUsers.Where(u => u.RoleId == roleId.Value).ToList();

        if (subjectId.HasValue)
            allUsers = allUsers.Where(u => u.SubjectId == subjectId.Value).ToList();

        const int pageSize = 10;
        var totalItems = allUsers.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedUsers = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var teacherBySubjectId = await BuildTeacherBySubjectIdAsync();

        var model = new UserListViewModel
        {
            Users = pagedUsers.Select(ToViewModel).ToList(),
            Subjects = allSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            TeacherBySubjectId = teacherBySubjectId,
            SearchTerm = search,
            FilterRoleId = roleId,
            FilterSubjectId = subjectId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ImportUsers()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        return View(new ImportUsersViewModel
        {
            SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList(),
            TeacherBySubjectId = await BuildTeacherBySubjectIdAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportUsers(ImportUsersViewModel model)
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        model.SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList();
        model.TeacherBySubjectId = await BuildTeacherBySubjectIdAsync();

        if (!ModelState.IsValid)
            return View(model);

        if (model.RoleId == 2 && model.SubjectId.HasValue)
        {
            var (isAvailable, availabilityError) = await _userServices.ValidateTeacherSubjectAvailabilityAsync(
                model.SubjectId.Value);

            if (!isAvailable)
            {
                ModelState.AddModelError(nameof(model.SubjectId), availabilityError ?? "Mon hoc nay da co giang vien phu trach.");
                return View(model);
            }
        }

        var file = model.File!;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv" && ext != ".json" && ext != ".txt")
        {
            ModelState.AddModelError(nameof(model.File), "Chi ho tro file .xlsx, .csv, .json hoac .txt");
            return View(model);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _userServices.ImportUsersFromFileAsync(stream, file.FileName, model.SubjectId, model.RoleId);

            model.Result = new ImportResultViewModel
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
                TempData["Success"] = $"Import thanh cong {result.CreatedCount} tai khoan moi. Gui mail thanh cong: {result.NotificationSentCount}.";

            if (result.ErrorCount > 0 || result.SkippedDuplicateCount > 0 || result.NotificationFailedCount > 0)
                TempData["Warning"] = $"{result.SkippedDuplicateCount} trung lap, {result.ErrorCount} loi, {result.NotificationFailedCount} email chua gui duoc.";
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong the doc file: {ex.Message}");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(int userId, int? subjectId)
    {
        var (success, error) = await _userServices.AssignSubjectAsync(userId, subjectId);

        if (!success)
            TempData["Error"] = error ?? "Phan cong mon hoc that bai.";
        else
            TempData["Success"] = subjectId.HasValue
                ? "Phan cong mon hoc thanh cong!"
                : "Da go bo mon hoc khoi nguoi dung nay.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int userId)
    {
        var (success, error) = await _userServices.ToggleUserStatusAsync(userId);

        if (!success)
            TempData["Error"] = error ?? "Thao tac that bai.";
        else
            TempData["Success"] = "Cap nhat trang thai tai khoan thanh cong!";

        return RedirectToAction(nameof(Index));
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

    private static UserListItemViewModel ToViewModel(UserListItemDto dto) => new()
    {
        Id = dto.Id,
        Username = dto.Username,
        Email = dto.Email,
        FullName = dto.FullName,
        RoleId = dto.RoleId,
        RoleName = dto.RoleName,
        RoleLabel = dto.RoleLabel,
        SubjectId = dto.SubjectId,
        SubjectCode = dto.SubjectCode,
        SubjectName = dto.SubjectName,
        IsActive = dto.IsActive,
        LastLoginAt = dto.LastLoginAt,
        CreatedAt = dto.CreatedAt
    };
}
