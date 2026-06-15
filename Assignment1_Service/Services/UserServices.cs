using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace Assignment1_Service.Services;

public class UserServices : IUserServices
{
    public const string DefaultPassword = "1234567";
    private const int TeacherRoleId = 2;

    private readonly IUserReposity _userRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly RagEduContext _context;
    private readonly IAccountNotificationService _accountNotificationService;

    public UserServices(
        IUserReposity userRepository,
        ISubjectRepository subjectRepository,
        RagEduContext context,
        IAccountNotificationService accountNotificationService)
    {
        _userRepository = userRepository;
        _subjectRepository = subjectRepository;
        _context = context;
        _accountNotificationService = accountNotificationService;
    }

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
            RoleId = user.RoleId,
            SubjectId = user.SubjectId,
            RequirePasswordChange = IsDefaultPassword(user.Password)
        };
    }

    public bool IsDefaultPassword(string password) => password == DefaultPassword;

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

    public async Task<ImportUsersResultDto> ImportUsersFromFileAsync(Stream stream, string fileName, int? subjectId, int roleId)
    {
        var result = new ImportUsersResultDto();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        List<ImportRowResultDto> rows;

        try
        {
            rows = ext switch
            {
                ".xlsx" or ".xls" => ParseExcel(stream),
                ".csv" => ParseCsv(stream),
                ".json" => ParseJson(stream),
                ".txt" => ParseTxt(stream),
                _ => throw new InvalidOperationException("Dinh dang file khong duoc ho tro.")
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Loi khi phan tich file: {ex.Message}");
        }

        var assignedTeacherSubjectsInBatch = new HashSet<int>();

        foreach (var rowResult in rows)
        {
            result.TotalRows++;

            if (string.IsNullOrWhiteSpace(rowResult.Email))
            {
                MarkError(rowResult, result, "Email khong duoc de trong.");
                continue;
            }

            if (!IsValidEmail(rowResult.Email))
            {
                MarkError(rowResult, result, $"Email '{rowResult.Email}' khong hop le.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rowResult.FullName))
            {
                MarkError(rowResult, result, "Ho ten khong duoc de trong.");
                continue;
            }

            var existing = await _userRepository.GetByEmailAsync(rowResult.Email);
            if (existing is not null)
            {
                rowResult.Status = ImportRowStatus.Duplicate;
                rowResult.Message = $"Email '{rowResult.Email}' da ton tai (username: {existing.Username}).";
                result.SkippedDuplicateCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            var username = string.IsNullOrWhiteSpace(rowResult.Username)
                ? GenerateUsername(rowResult.Email)
                : rowResult.Username;

            username = await EnsureUniqueUsernameAsync(username);
            rowResult.Username = username;

            if (roleId == TeacherRoleId && subjectId.HasValue)
            {
                if (!assignedTeacherSubjectsInBatch.Add(subjectId.Value))
                {
                    MarkError(rowResult, result, "Mon hoc nay da duoc chon cho giang vien khac trong cung file import.");
                    continue;
                }

                var (isAvailable, availabilityError) = await ValidateTeacherSubjectAssignmentAsync(
                    subjectId.Value,
                    excludeUserId: null);

                if (!isAvailable)
                {
                    MarkError(rowResult, result, availabilityError ?? "Mon hoc da co giang vien phu trach.");
                    continue;
                }
            }

            var newUser = new User
            {
                Username = username,
                Email = rowResult.Email.ToLowerInvariant(),
                FullName = rowResult.FullName,
                Password = DefaultPassword,
                RoleId = roleId,
                SubjectId = subjectId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _userRepository.AddAsync(newUser);

            rowResult.Status = ImportRowStatus.Created;
            rowResult.Message = $"Tao tai khoan thanh cong (username: {username}).";

            var notificationResult = await _accountNotificationService.SendAccountCreatedEmailAsync(
                newUser.Email,
                newUser.FullName ?? newUser.Username,
                newUser.Username,
                DefaultPassword);

            rowResult.NotificationSent = notificationResult.IsSuccess;
            rowResult.NotificationMessage = notificationResult.Message;

            if (notificationResult.IsSuccess)
            {
                result.NotificationSentCount++;
            }
            else
            {
                result.NotificationFailedCount++;
                rowResult.Message = $"{rowResult.Message} Tuy nhien, email thong bao chua gui duoc.";
            }

            result.CreatedCount++;
            result.Rows.Add(rowResult);
        }

        return result;
    }

    private List<ImportRowResultDto> ParseExcel(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName = row.Cell(1).GetString()?.Trim(),
                Email = row.Cell(2).GetString()?.Trim(),
                Username = row.Cell(3).GetString()?.Trim()
            });
        }

        return rows;
    }

    private List<ImportRowResultDto> ParseCsv(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        });

        var rowNum = 2;
        if (csv.Read())
        {
            csv.ReadHeader();
            while (csv.Read())
            {
                rows.Add(new ImportRowResultDto
                {
                    RowNumber = rowNum++,
                    FullName = csv.GetField(0),
                    Email = csv.GetField(1),
                    Username = csv.TryGetField(2, out string? username) ? username : null
                });
            }
        }

        return rows;
    }

    private List<ImportRowResultDto> ParseJson(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<List<JsonImportRow>>(json, options) ?? [];

        var rowNum = 2;
        return data.Select(item => new ImportRowResultDto
        {
            RowNumber = rowNum++,
            FullName = item.FullName,
            Email = item.Email,
            Username = item.Username
        }).ToList();
    }

    private class JsonImportRow
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
    }

    private List<ImportRowResultDto> ParseTxt(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        using var reader = new StreamReader(stream);

        var headerLine = reader.ReadLine();
        if (headerLine is null)
            return rows;

        var separator = headerLine.Contains('\t') ? '\t' : headerLine.Contains('|') ? '|' : ',';
        var rowNum = 2;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(separator);
            if (parts.Length < 2)
                continue;

            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum++,
                FullName = parts[0].Trim(),
                Email = parts[1].Trim(),
                Username = parts.Length > 2 ? parts[2].Trim() : null
            });
        }

        return rows;
    }

    public async Task<(bool Success, string? Error)> AssignSubjectAsync(int userId, int? subjectId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Khong tim thay nguoi dung.");

        if (subjectId.HasValue)
        {
            var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId.Value);
            if (subject is null)
                return (false, "Khong tim thay mon hoc.");

            if (user.RoleId == TeacherRoleId)
            {
                var (isAvailable, availabilityError) = await ValidateTeacherSubjectAssignmentAsync(
                    subjectId.Value,
                    excludeUserId: userId);

                if (!isAvailable)
                    return (false, availabilityError);
            }
        }

        user.SubjectId = subjectId;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    public Task<(bool IsAvailable, string? Error)> ValidateTeacherSubjectAvailabilityAsync(
        int subjectId,
        int? excludeUserId = null)
        => ValidateTeacherSubjectAssignmentAsync(subjectId, excludeUserId);

    public async Task<(bool Success, string? Error)> ToggleUserStatusAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Khong tim thay nguoi dung.");

        user.IsActive = !(user.IsActive ?? true);
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Khong tim thay tai khoan.");

        if (user.Password != currentPassword)
            return (false, "Mat khau hien tai khong dung.");

        var validationError = ValidateNewPassword(newPassword, user.Password);
        if (validationError is not null)
            return (false, validationError);

        user.Password = newPassword;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangeDefaultPasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Khong tim thay tai khoan.");

        if (!IsDefaultPassword(user.Password))
            return (false, "Tai khoan nay khong con dung mat khau mac dinh.");

        var validationError = ValidateNewPassword(newPassword, user.Password);
        if (validationError is not null)
            return (false, validationError);

        user.Password = newPassword;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    private async Task<(bool IsAvailable, string? Error)> ValidateTeacherSubjectAssignmentAsync(
        int subjectId,
        int? excludeUserId)
    {
        var existingTeacher = await _userRepository.GetTeacherAssignedToSubjectAsync(subjectId, excludeUserId);
        if (existingTeacher is null)
            return (true, null);

        var teacherName = existingTeacher.FullName ?? existingTeacher.Username;
        var subjectCode = existingTeacher.Subject?.Code ?? subjectId.ToString();
        return (false, $"Mon hoc {subjectCode} da duoc gan cho giang vien {teacherName}.");
    }

    private static void MarkError(ImportRowResultDto rowResult, ImportUsersResultDto result, string message)
    {
        rowResult.Status = ImportRowStatus.Error;
        rowResult.Message = message;
        result.ErrorCount++;
        result.Rows.Add(rowResult);
    }

    private static string GenerateUsername(string email)
    {
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

    private static string? ValidateNewPassword(string newPassword, string currentPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return "Mat khau moi phai co it nhat 6 ky tu.";

        if (newPassword == currentPassword)
            return "Mat khau moi khong duoc trung voi mat khau hien tai.";

        return null;
    }
}
