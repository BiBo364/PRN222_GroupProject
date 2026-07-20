using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.Admin;

// @page "/Admin/Index"
[RequireAdmin]
public class IndexModel : PageModel
{
    private readonly IUserServices _userServices;
    private readonly ISubjectService _subjectService;

    public IndexModel(IUserServices userServices, ISubjectService subjectService)
    {
        _userServices = userServices;
        _subjectService = subjectService;
    }

    public UserListViewModel ViewModel { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? CreatedUserId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var allUsers = await _userServices.GetAllUsersAsync();
        var allSubjects = await _subjectService.GetSubjectsAsync();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim().ToLowerInvariant();
            allUsers = allUsers.Where(u =>
                u.Username.ToLowerInvariant().Contains(term) ||
                u.Email.ToLowerInvariant().Contains(term) ||
                (u.FullName ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
        }

        if (RoleId.HasValue)
            allUsers = allUsers.Where(u => u.RoleId == RoleId.Value).ToList();

        if (SubjectId.HasValue)
            allUsers = allUsers
                .Where(u => u.AssignedSubjects.Any(subject => subject.Id == SubjectId.Value))
                .ToList();

        const int pageSize = 10;
        var totalItems = allUsers.Count;
        var activeItems = allUsers.Count(user => user.IsActive);
        var lecturerItems = allUsers.Count(user => user.RoleId == 2);
        var studentItems = allUsers.Count(user => user.RoleId == 3);
        var unassignedLecturerItems = allUsers.Count(
            user => user.RoleId == 2 && user.AssignedSubjects.Count == 0);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, totalPages));

        var pagedUsers = allUsers
            .Skip((PageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var teacherBySubjectId = await BuildTeacherBySubjectIdAsync();

        ViewModel = new UserListViewModel
        {
            Users = pagedUsers.Select(ToViewModel).ToList(),
            Subjects = allSubjects.Select(ViewModelMapper.ToViewModel).ToList(),
            TeacherBySubjectId = teacherBySubjectId,
            SearchTerm = Search,
            FilterRoleId = RoleId,
            FilterSubjectId = SubjectId,
            CurrentPage = PageNumber,
            TotalPages = totalPages,
            TotalItems = totalItems,
            ActiveItems = activeItems,
            LecturerItems = lecturerItems,
            StudentItems = studentItems,
            UnassignedLecturerItems = unassignedLecturerItems,
            PageSize = pageSize
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateLecturerSubjectsAsync(int userId, List<int>? subjectIds)
    {
        var (success, error) = await _userServices.UpdateLecturerSubjectsAsync(userId, subjectIds ?? []);

        if (!success)
            TempData["Error"] = error ?? "Phân công môn học thất bại.";
        else
            TempData["Success"] = "Đã cập nhật phân công môn học.";

        return RedirectToPage("/Admin/Index", CurrentListRouteValues());
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int userId)
    {
        var (success, error) = await _userServices.ToggleUserStatusAsync(userId);

        if (!success)
            TempData["Error"] = error ?? "Thao tác thất bại.";
        else
            TempData["Success"] = "Cập nhật trạng thái tài khoản thành công.";

        return RedirectToPage("/Admin/Index", CurrentListRouteValues());
    }

    private object CurrentListRouteValues()
    {
        return new
        {
            search = Search,
            roleId = RoleId,
            subjectId = SubjectId,
            pageNumber = PageNumber
        };
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
        AssignedSubjects = dto.AssignedSubjects.Select(subject => new UserSubjectAssignmentViewModel
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name
        }).ToList(),
        IsActive = dto.IsActive,
        LastLoginAt = dto.LastLoginAt,
        CreatedAt = dto.CreatedAt
    };
}
