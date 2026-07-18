using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Service.Services;

public class UserServices : IUserServices
{
    public const string DefaultPassword = "1234567";
    private const int LecturerRoleId = 2;
    private const int StudentRoleId = 3;
    private const int MaxGeneratedUsernameLength = 50;
    private const string LecturerEmailDomain = "edu.vn";
    private const string StudentEmailDomain = "fpt.edu.vn";

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
            AssignedSubjects = u.AssignedSubjects
                .Where(subject => subject.IsDeleted != true)
                .OrderBy(subject => subject.Code)
                .Select(subject => new UserSubjectAssignmentDto
                {
                    Id = subject.Id,
                    Code = subject.Code,
                    Name = subject.Name
                })
                .ToList(),
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
                _ => throw new InvalidOperationException("Định dạng tệp không được hỗ trợ.")
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi phân tích tệp: {ex.Message}");
        }

        if (roleId is not LecturerRoleId and not StudentRoleId)
            throw new InvalidOperationException("Chỉ hỗ trợ nhập giảng viên hoặc học sinh, sinh viên.");

        var importSubjectId = roleId == LecturerRoleId ? subjectId : null;
        var assignedTeacherSubjectsInBatch = new HashSet<int>();

        foreach (var rowResult in rows)
        {
            result.TotalRows++;

            var fullName = NormalizeFullName(rowResult.FullName);
            if (fullName is null)
            {
                MarkError(rowResult, result, "Họ tên không được để trống.");
                continue;
            }

            rowResult.FullName = fullName;

            if (roleId == LecturerRoleId && importSubjectId.HasValue)
            {
                if (!assignedTeacherSubjectsInBatch.Add(importSubjectId.Value))
                {
                    MarkError(rowResult, result, "Môn học này đã được chọn cho giảng viên khác trong cùng tệp nhập.");
                    continue;
                }

                var (isAvailable, availabilityError) = await ValidateTeacherSubjectAssignmentAsync(
                    importSubjectId.Value,
                    excludeUserId: null);

                if (!isAvailable)
                {
                    MarkError(rowResult, result, availabilityError ?? "Môn học đã có giảng viên phụ trách.");
                    continue;
                }
            }

            var (username, email) = await GenerateUniqueAccountIdentityAsync(fullName, roleId);
            rowResult.Username = username;
            rowResult.Email = email;

            var newUser = new User
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Password = DefaultPassword,
                RoleId = roleId,
                SubjectId = importSubjectId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _userRepository.AddAsync(newUser);

            if (roleId == LecturerRoleId && importSubjectId.HasValue)
                await UpdateLecturerSubjectsAsync(newUser.Id, [importSubjectId.Value]);

            rowResult.Status = ImportRowStatus.Created;
            rowResult.Message = $"Tạo tài khoản thành công (tên đăng nhập: {username}).";

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
                rowResult.Message = $"{rowResult.Message} Tuy nhiên, email thông báo chưa gửi được.";
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

        for (int rowNum = 1; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            var fullName = row.Cell(1).GetString()?.Trim();
            if (ShouldSkipImportedName(fullName))
                continue;

            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName = fullName
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
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        });

        var rowNum = 0;
        while (csv.Read())
        {
            rowNum++;
            var fullName = csv.TryGetField(0, out string? value) ? value?.Trim() : null;
            if (ShouldSkipImportedName(fullName))
                continue;

            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName = fullName
            });
        }

        return rows;
    }

    private List<ImportRowResultDto> ParseJson(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Tệp JSON phải là một mảng danh sách người dùng.");

        var rows = new List<ImportRowResultDto>();
        var rowNum = 1;
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var fullName = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Object => GetJsonString(element, "FullName", "Name", "HoTen", "HoVaTen"),
                _ => null
            };

            if (ShouldSkipImportedName(fullName))
            {
                rowNum++;
                continue;
            }

            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum++,
                FullName = fullName?.Trim()
            });
        }

        return rows;
    }

    private List<ImportRowResultDto> ParseTxt(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        using var reader = new StreamReader(stream);

        var rowNum = 0;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            rowNum++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fullName = ExtractFirstTextColumn(line);
            if (ShouldSkipImportedName(fullName))
                continue;

            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName = fullName
            });
        }

        return rows;
    }

    public async Task<(bool Success, string? Error)> AssignSubjectAsync(int userId, int? subjectId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return (false, "Không tìm thấy người dùng.");

        if (user.RoleId != LecturerRoleId)
            return (false, "Chỉ có thể phân công môn học cho giảng viên.");

        if (subjectId.HasValue)
        {
            var subject = await _subjectRepository.GetByIdWithDetailsAsync(subjectId.Value);
            if (subject is null)
                return (false, "Không tìm thấy môn học.");

            var (isAvailable, availabilityError) = await ValidateTeacherSubjectAssignmentAsync(
                subjectId.Value,
                excludeUserId: userId);

            if (!isAvailable)
                return (false, availabilityError);
        }

        user.SubjectId = subjectId;
        await _userRepository.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateLecturerSubjectsAsync(
        int userId,
        IReadOnlyCollection<int> subjectIds)
    {
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user is null)
            return (false, "Không tìm thấy người dùng.");

        if (user.RoleId != LecturerRoleId)
            return (false, "Chỉ có thể phân công môn học cho giảng viên.");

        var requestedIds = subjectIds.Distinct().ToArray();
        var requestedSubjects = await _context.Subjects
            .Where(subject => requestedIds.Contains(subject.Id) && subject.IsDeleted != true)
            .ToListAsync();

        if (requestedSubjects.Count != requestedIds.Length)
            return (false, "Một hoặc nhiều môn học không tồn tại.");

        var conflict = requestedSubjects.FirstOrDefault(subject =>
            subject.LecturerId.HasValue && subject.LecturerId.Value != userId);
        if (conflict is not null)
        {
            var teacher = await _context.Users.FindAsync(conflict.LecturerId);
            var teacherName = teacher?.FullName ?? teacher?.Username ?? "giảng viên khác";
            return (false, $"Môn học {conflict.Code} đã được phân công cho giảng viên {teacherName}. Vui lòng gỡ môn này khỏi giảng viên hiện tại trước.");
        }

        var currentlyAssigned = await _context.Subjects
            .Where(subject => subject.LecturerId == userId)
            .ToListAsync();

        foreach (var subject in currentlyAssigned)
            subject.LecturerId = null;

        foreach (var subject in requestedSubjects)
            subject.LecturerId = userId;

        await _context.SaveChangesAsync();
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
            return (false, "Không tìm thấy người dùng.");

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
            return (false, "Không tìm thấy tài khoản.");

        if (user.Password != currentPassword)
            return (false, "Mật khẩu hiện tại không đúng.");

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
            return (false, "Không tìm thấy tài khoản.");

        if (!IsDefaultPassword(user.Password))
            return (false, "Tài khoản này không còn sử dụng mật khẩu mặc định.");

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
        return (false, $"Môn học {subjectCode} đã được phân công cho giảng viên {teacherName}.");
    }

    private static void MarkError(ImportRowResultDto rowResult, ImportUsersResultDto result, string message)
    {
        rowResult.Status = ImportRowStatus.Error;
        rowResult.Message = message;
        result.ErrorCount++;
        result.Rows.Add(rowResult);
    }

    private async Task<(string Username, string Email)> GenerateUniqueAccountIdentityAsync(string fullName, int roleId)
    {
        var baseUsername = GenerateUsername(fullName);
        var counter = 0;

        while (true)
        {
            var suffix = counter == 0 ? string.Empty : counter.ToString(CultureInfo.InvariantCulture);
            var maxBaseLength = Math.Max(1, MaxGeneratedUsernameLength - suffix.Length);
            var usernameBase = baseUsername.Length > maxBaseLength ? baseUsername[..maxBaseLength] : baseUsername;
            var username = $"{usernameBase}{suffix}";
            var email = GenerateEmail(username, roleId);

            if (await _userRepository.GetByUsernameAsync(username) is null
                && await _userRepository.GetByEmailAsync(email) is null)
            {
                return (username, email);
            }

            counter++;
        }
    }

    private static string GenerateUsername(string fullName)
    {
        var normalized = RemoveDiacritics(fullName).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        var username = builder.Length == 0 ? "user" : builder.ToString();
        return username.Length > MaxGeneratedUsernameLength
            ? username[..MaxGeneratedUsernameLength]
            : username;
    }

    private static string GenerateEmail(string username, int roleId)
    {
        var domain = roleId switch
        {
            LecturerRoleId => LecturerEmailDomain,
            StudentRoleId => StudentEmailDomain,
            _ => throw new InvalidOperationException("Vai trò nhập dữ liệu không hợp lệ.")
        };

        return $"{username}@{domain}";
    }

    private static string? NormalizeFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var normalized = string.Join(
            ' ',
            fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string RemoveDiacritics(string value)
    {
        value = value.Replace('đ', 'd').Replace('Đ', 'D');
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string? ExtractFirstTextColumn(string line)
    {
        var separator = line.Contains('\t') ? '\t' : line.Contains('|') ? '|' : line.Contains(',') ? ',' : (char?)null;
        return separator.HasValue
            ? line.Split(separator.Value)[0].Trim()
            : line.Trim();
    }

    private static bool ShouldSkipImportedName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = RemoveDiacritics(value)
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

        return normalized is "fullname" or "name" or "hoten" or "hovaten";
    }

    private static string? GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!propertyNames.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            return property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
        }

        return null;
    }

    private static string? ValidateNewPassword(string newPassword, string currentPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return "Mật khẩu mới phải có ít nhất 6 ký tự.";

        if (newPassword == currentPassword)
            return "Mật khẩu mới không được trùng với mật khẩu hiện tại.";

        return null;
    }
}
