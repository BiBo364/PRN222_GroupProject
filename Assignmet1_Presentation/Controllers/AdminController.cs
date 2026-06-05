using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignmet1_Presentation.Controllers;

/// <summary>
/// Controller quản lý người dùng dành cho Admin (RoleId 1 hoặc 2).
/// </summary>
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

    // ─────────────────────────────────────────────────────────────────
    // Danh sách Users
    // ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(string? search, int? roleId, int? subjectId, int page = 1)
    {
        var allUsers   = await _userServices.GetAllUsersAsync();
        var allSubjects = await _subjectService.GetSubjectsAsync();

        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            allUsers = allUsers.Where(u =>
                u.Username.ToLowerInvariant().Contains(term) ||
                u.Email.ToLowerInvariant().Contains(term) ||
                (u.FullName ?? "").ToLowerInvariant().Contains(term)).ToList();
        }

        // Lọc theo vai trò
        if (roleId.HasValue)
            allUsers = allUsers.Where(u => u.RoleId == roleId.Value).ToList();

        // Lọc theo môn học
        if (subjectId.HasValue)
            allUsers = allUsers.Where(u => u.SubjectId == subjectId.Value).ToList();

        // Phân trang
        int pageSize = 10;
        int totalItems = allUsers.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedUsers = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var model = new UserListViewModel
        {
            Users      = pagedUsers.Select(ToViewModel).ToList(),
            Subjects   = allSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            SearchTerm    = search,
            FilterRoleId  = roleId,
            FilterSubjectId = subjectId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────
    // Import người dùng từ Excel/CSV
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ImportUsers()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        return View(new ImportUsersViewModel
        {
            SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportUsers(ImportUsersViewModel model)
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        model.SubjectOptions = subjects.Select(ViewModelMapper.ToViewModel).ToList();

        if (!ModelState.IsValid)
            return View(model);

        // Kiểm tra định dạng file
        var file = model.File!;
        var ext  = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv" && ext != ".json" && ext != ".txt")
        {
            ModelState.AddModelError(nameof(model.File), "Chỉ hỗ trợ file .xlsx, .csv, .json hoặc .txt");
            return View(model);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _userServices.ImportUsersFromFileAsync(stream, file.FileName, model.SubjectId, model.RoleId);

            model.Result = new ImportResultViewModel
            {
                TotalRows            = result.TotalRows,
                CreatedCount         = result.CreatedCount,
                SkippedDuplicateCount = result.SkippedDuplicateCount,
                ErrorCount           = result.ErrorCount,
                Rows                 = result.Rows.Select(r => new ImportRowResultViewModel
                {
                    RowNumber = r.RowNumber,
                    FullName  = r.FullName,
                    Email     = r.Email,
                    Username  = r.Username,
                    Status    = r.Status,
                    Message   = r.Message
                }).ToList()
            };

            if (result.CreatedCount > 0)
                TempData["Success"] = $"Import thành công {result.CreatedCount} tài khoản mới!";

            if (result.ErrorCount > 0 || result.SkippedDuplicateCount > 0)
                TempData["Warning"] = $"{result.SkippedDuplicateCount} trùng lặp, {result.ErrorCount} lỗi.";
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Không thể đọc file: {ex.Message}");
        }

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────
    // Phân công môn học
    // ─────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(int userId, int? subjectId)
    {
        var (success, error) = await _userServices.AssignSubjectAsync(userId, subjectId);

        if (!success)
            TempData["Error"] = error ?? "Phân công môn học thất bại.";
        else
            TempData["Success"] = subjectId.HasValue
                ? "Phân công môn học thành công!"
                : "Đã gỡ bỏ môn học khỏi người dùng này.";

        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────────
    // Bật / Tắt tài khoản
    // ─────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int userId)
    {
        var (success, error) = await _userServices.ToggleUserStatusAsync(userId);

        if (!success)
            TempData["Error"] = error ?? "Thao tác thất bại.";
        else
            TempData["Success"] = "Cập nhật trạng thái tài khoản thành công!";

        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static UserListItemViewModel ToViewModel(UserListItemDto dto) => new()
    {
        Id          = dto.Id,
        Username    = dto.Username,
        Email       = dto.Email,
        FullName    = dto.FullName,
        RoleId      = dto.RoleId,
        RoleName    = dto.RoleName,
        RoleLabel   = dto.RoleLabel,
        SubjectId   = dto.SubjectId,
        SubjectCode = dto.SubjectCode,
        SubjectName = dto.SubjectName,
        IsActive    = dto.IsActive,
        LastLoginAt = dto.LastLoginAt,
        CreatedAt   = dto.CreatedAt
    };
}
