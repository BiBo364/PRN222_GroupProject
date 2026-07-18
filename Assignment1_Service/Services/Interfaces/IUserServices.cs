using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IUserServices
{
    Task<LoginUserDto?> LoginAsync(string username, string password);
    bool IsDefaultPassword(string password);
    Task<(bool Success, string? Error)> ChangeDefaultPasswordAsync(int userId, string newPassword);

    Task<List<UserListItemDto>> GetAllUsersAsync();
    Task<ImportUsersResultDto> ImportUsersFromFileAsync(Stream stream, string fileName, int? subjectId, int roleId);
    Task<(bool Success, string? Error)> AssignSubjectAsync(int userId, int? subjectId);
    Task<(bool Success, string? Error)> UpdateLecturerSubjectsAsync(int userId, IReadOnlyCollection<int> subjectIds);
    Task<(bool IsAvailable, string? Error)> ValidateTeacherSubjectAvailabilityAsync(int subjectId, int? excludeUserId = null);
    Task<(bool Success, string? Error)> ToggleUserStatusAsync(int userId);

    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
