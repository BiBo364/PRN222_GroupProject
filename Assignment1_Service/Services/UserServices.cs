using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using ClosedXML.Excel;

namespace Assignment1_Service.Services;

public class UserServices : IUserServices
{
    private const string DefaultPassword = "1234567";
    private const int StudentRoleId = 4;

    private readonly IUserReposity _userRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly RagEduContext _context;

    public UserServices(
        IUserReposity userRepository,
        ISubjectRepository subjectRepository,
        RagEduContext context)
    {
        _userRepository = userRepository;
        _subjectRepository = subjectRepository;
        _context = context;
    }

    // ─────────────────────────────────────────────────────────────────
    // Auth
    // ─────────────────────────────────────────────────────────────────

    public async Task<LoginUserDto?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null || user.IsActive == false)
            return null;

        if (user.Password != password)
            return null;

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return new LoginUserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleId = user.RoleId
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Admin — Quản lý users
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<UserListItemDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            RoleId = u.RoleId,
            RoleName = u.Role?.Name ?? string.Empty,
            RoleLabel = u.Role?.Label ?? string.Empty,
            SubjectId = u.SubjectId,
            SubjectCode = u.Subject?.Code,
            SubjectName = u.Subject?.Name,
            IsActive = u.IsActive ?? true,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Đọc file Excel và tạo tài khoản sinh viên hàng loạt.
    /// Cấu trúc Excel: Cột A = FullName, Cột B = Email, Cột C = Username (tuỳ chọn).
    /// Nếu email đã tồn tại → skip và báo cáo Duplicate.
    /// </summary>
    public async Task<ImportStudentsResultDto> ImportStudentsFromExcelAsync(Stream excelStream, int? subjectId)
    {
        var result = new ImportStudentsResultDto();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        // Tìm dòng cuối có dữ liệu
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Bỏ qua dòng 1 (header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            result.TotalRows++;
            var row = worksheet.Row(rowNum);

            var fullName = row.Cell(1).GetString()?.Trim();
            var email    = row.Cell(2).GetString()?.Trim();
            var username = row.Cell(3).GetString()?.Trim();

            var rowResult = new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName  = fullName,
                Email     = email,
                Username  = username
            };

            // Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrWhiteSpace(email))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Email không được để trống.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            if (!IsValidEmail(email))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = $"Email '{email}' không hợp lệ.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Họ tên không được để trống.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            // Kiểm tra email đã tồn tại
            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing is not null)
            {
                rowResult.Status  = ImportRowStatus.Duplicate;
                rowResult.Message = $"Email '{email}' đã tồn tại trong hệ thống (username: {existing.Username}).";
                result.SkippedDuplicateCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            // Sinh username từ email nếu không cung cấp
            if (string.IsNullOrWhiteSpace(username))
                username = GenerateUsername(email);

            // Đảm bảo username duy nhất
            username = await EnsureUniqueUsernameAsync(username);
            rowResult.Username = username;

            // Tạo user mới
            var newUser = new User
            {
                Username  = username,
                Email     = email.ToLowerInvariant(),
                FullName  = fullName,
                Password  = DefaultPassword,
                RoleId    = StudentRoleId,
                SubjectId = subjectId,
                IsActive  = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _userRepository.AddAsync(newUser);

            rowResult.Status  = ImportRowStatus.Created;
            rowResult.Message = $"Tạo tài khoản thành công (username: {username}).";
            result.CreatedCount++;
            result.Rows.Add(rowResult);
        }

        return result;
    }

    public async Task<(bool Success, string? Error)> AssignSubjectAsync(int userId, int? subjectId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Không tìm thấy người dùng.");

        if (subjectId.HasValue)
        {
            var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId.Value);
            if (subject is null)
                return (false, "Không tìm thấy môn học.");
        }

        user.SubjectId = subjectId;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleUserStatusAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Không tìm thấy người dùng.");

        user.IsActive = !(user.IsActive ?? true);
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────
    // Student — Tự quản lý
    // ─────────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Không tìm thấy tài khoản.");

        if (user.Password != currentPassword)
            return (false, "Mật khẩu hiện tại không đúng.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        user.Password = newPassword;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static string GenerateUsername(string email)
    {
        // Lấy phần trước @ và loại bỏ ký tự không hợp lệ
        var local = email.Split('@')[0]
            .ToLowerInvariant()
            .Replace(".", "")
            .Replace("-", "")
            .Replace("_", "");

        return local.Length > 50 ? local[..50] : local;
    }

    private async Task<string> EnsureUniqueUsernameAsync(string baseUsername)
    {
        var candidate = baseUsername;
        var counter = 1;
        while (await _userRepository.GetByUsernameAsync(candidate) is not null)
        {
            candidate = $"{baseUsername}{counter}";
            counter++;
        }
        return candidate;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

