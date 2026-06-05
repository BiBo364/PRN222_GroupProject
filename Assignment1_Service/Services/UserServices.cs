using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Đọc file (Excel/CSV) và tạo tài khoản hàng loạt.
    /// Cấu trúc: Cột A = FullName, Cột B = Email, Cột C = Username (tuỳ chọn).
    /// </summary>
    public async Task<ImportUsersResultDto> ImportUsersFromFileAsync(Stream stream, string fileName, int? subjectId, int roleId)
    {
        var result = new ImportUsersResultDto();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        List<ImportRowResultDto> rows = [];

        try
        {
            if (ext == ".xlsx" || ext == ".xls")
            {
                rows = ParseExcel(stream);
            }
            else if (ext == ".csv")
            {
                rows = ParseCsv(stream);
            }
            else if (ext == ".json")
            {
                rows = ParseJson(stream);
            }
            else if (ext == ".txt")
            {
                rows = ParseTxt(stream);
            }
            else
            {
                throw new InvalidOperationException("Định dạng file không được hỗ trợ.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi phân tích file: {ex.Message}");
        }

        foreach (var rowResult in rows)
        {
            result.TotalRows++;

            // Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrWhiteSpace(rowResult.Email))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Email không được để trống.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            if (!IsValidEmail(rowResult.Email))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = $"Email '{rowResult.Email}' không hợp lệ.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            // Kiểm tra quy tắc tên miền email theo vai trò
            if (roleId == 4 && !rowResult.Email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Sinh viên phải sử dụng email có đuôi @fpt.edu.vn.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }
            else if ((roleId == 1 || roleId == 2 || roleId == 3) && !rowResult.Email.EndsWith("@edu.vn", StringComparison.OrdinalIgnoreCase))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Giảng viên/Admin phải sử dụng email có đuôi @edu.vn.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rowResult.FullName))
            {
                rowResult.Status  = ImportRowStatus.Error;
                rowResult.Message = "Họ tên không được để trống.";
                result.ErrorCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            // Kiểm tra email đã tồn tại
            var existing = await _userRepository.GetByEmailAsync(rowResult.Email);
            if (existing is not null)
            {
                rowResult.Status  = ImportRowStatus.Duplicate;
                rowResult.Message = $"Email '{rowResult.Email}' đã tồn tại (username: {existing.Username}).";
                result.SkippedDuplicateCount++;
                result.Rows.Add(rowResult);
                continue;
            }

            // Sinh username từ email nếu không cung cấp
            var username = rowResult.Username;
            if (string.IsNullOrWhiteSpace(username))
                username = GenerateUsername(rowResult.Email);

            // Đảm bảo username duy nhất
            username = await EnsureUniqueUsernameAsync(username);
            rowResult.Username = username;

            // Tạo user mới
            var newUser = new User
            {
                Username  = username,
                Email     = rowResult.Email.ToLowerInvariant(),
                FullName  = rowResult.FullName,
                Password  = DefaultPassword,
                RoleId    = roleId,
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

    private List<ImportRowResultDto> ParseExcel(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Bỏ qua dòng 1 (header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            rows.Add(new ImportRowResultDto
            {
                RowNumber = rowNum,
                FullName  = row.Cell(1).GetString()?.Trim(),
                Email     = row.Cell(2).GetString()?.Trim(),
                Username  = row.Cell(3).GetString()?.Trim()
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

        int rowNum = 2;
        if (csv.Read())
        {
            csv.ReadHeader();
            while (csv.Read())
            {
                rows.Add(new ImportRowResultDto
                {
                    RowNumber = rowNum++,
                    FullName  = csv.GetField(0),
                    Email     = csv.GetField(1),
                    Username  = csv.TryGetField(2, out string? username) ? username : null
                });
            }
        }
        return rows;
    }

    private List<ImportRowResultDto> ParseJson(Stream stream)
    {
        var rows = new List<ImportRowResultDto>();
        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<List<JsonImportRow>>(json, options);

            if (data != null)
            {
                int rowNum = 2; // Assuming row 1 is structural representation
                foreach (var item in data)
                {
                    rows.Add(new ImportRowResultDto
                    {
                        RowNumber = rowNum++,
                        FullName = item.FullName,
                        Email = item.Email,
                        Username = item.Username
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Lỗi phân tích JSON: {ex.Message}", ex);
        }
        return rows;
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
        
        string? headerLine = reader.ReadLine();
        if (headerLine == null) return rows;

        // Try to detect separator
        char separator = headerLine.Contains('\t') ? '\t' : (headerLine.Contains('|') ? '|' : ',');

        int rowNum = 2;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(separator);
            if (parts.Length >= 2)
            {
                rows.Add(new ImportRowResultDto
                {
                    RowNumber = rowNum++,
                    FullName = parts[0].Trim(),
                    Email = parts[1].Trim(),
                    Username = parts.Length > 2 ? parts[2].Trim() : null
                });
            }
        }

        return rows;
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

