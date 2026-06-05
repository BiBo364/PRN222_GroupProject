using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IUserServices
{
    Task<LoginUserDto?> LoginAsync(string username, string password);

    // ----- Admin: quản lý users -----
    Task<List<UserListItemDto>> GetAllUsersAsync();
    Task<ImportStudentsResultDto> ImportStudentsFromExcelAsync(Stream excelStream, int? subjectId);
    Task<(bool Success, string? Error)> AssignSubjectAsync(int userId, int? subjectId);
    Task<(bool Success, string? Error)> ToggleUserStatusAsync(int userId);

    // ----- Student: tự quản lý -----
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

