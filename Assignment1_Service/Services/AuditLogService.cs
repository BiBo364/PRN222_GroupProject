using System.Text.Json;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;

namespace Assignment1_Service.Services;

public sealed class AuditLogService : IAuditLogService
{
    private const int AdministratorRoleId = 1;
    private const int LecturerRoleId = 2;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task RecordAsync(
        RecordAuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoleId is not AdministratorRoleId and not LecturerRoleId)
            return Task.CompletedTask;

        var log = new AuditLog
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            Action = Limit(request.Action, 100, "update"),
            Category = Limit(request.Category, 100, "system"),
            EntityType = Limit(request.EntityType, 100, "request"),
            EntityId = LimitOptional(request.EntityId, 100),
            Description = Limit(request.Description, 1000, "Thực hiện thao tác trên hệ thống."),
            DetailsJson = request.Details is null
                ? null
                : JsonSerializer.Serialize(request.Details, JsonOptions),
            IpAddress = LimitOptional(request.IpAddress, 64),
            UserAgent = LimitOptional(request.UserAgent, 500),
            RequestPath = LimitOptional(request.RequestPath, 1000),
            HttpMethod = LimitOptional(request.HttpMethod, 10),
            StatusCode = request.StatusCode,
            TraceIdentifier = LimitOptional(request.TraceIdentifier, 100),
            CreatedAt = DateTime.UtcNow
        };

        return _repository.AddAsync(log, cancellationToken);
    }

    public async Task<AuditLogPageDto> GetPageAsync(
        int requesterUserId,
        int requesterRoleId,
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        if (requesterRoleId is not AdministratorRoleId and not LecturerRoleId)
            throw new InvalidOperationException("Bạn không có quyền xem nhật ký thao tác.");

        var scopedUserId = requesterRoleId == AdministratorRoleId
            ? (int?)null
            : requesterUserId;
        var pageSize = Math.Clamp(query.PageSize, 10, 100);
        var page = Math.Max(1, query.Page);
        var fromUtc = query.FromDate?.Date.ToUniversalTime();
        var toUtc = query.ToDate?.Date.AddDays(1).ToUniversalTime();

        var total = await _repository.CountAsync(
            scopedUserId,
            Normalize(query.Category),
            Normalize(query.Action),
            Normalize(query.Search),
            fromUtc,
            toUtc,
            cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);

        var logs = await _repository.GetPageAsync(
            scopedUserId,
            Normalize(query.Category),
            Normalize(query.Action),
            Normalize(query.Search),
            fromUtc,
            toUtc,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        var categories = await _repository.GetCategoriesAsync(scopedUserId, cancellationToken);

        return new AuditLogPageDto
        {
            Items = logs.Select(Map).ToList(),
            Categories = categories,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages,
            CanViewAllUsers = requesterRoleId == AdministratorRoleId
        };
    }

    private static AuditLogEntryDto Map(AuditLog log)
    {
        var displayName = log.User?.FullName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = log.User?.Username;

        return new AuditLogEntryDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserDisplayName = displayName ?? "Tài khoản đã xóa",
            RoleLabel = log.RoleId switch
            {
                AdministratorRoleId => "Quản trị viên",
                LecturerRoleId => "Giảng viên",
                _ => "Hệ thống"
            },
            Action = log.Action,
            ActionLabel = ActionLabel(log.Action),
            Category = log.Category,
            CategoryLabel = CategoryLabel(log.Category),
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Description = log.Description,
            IpAddress = log.IpAddress,
            RequestPath = log.RequestPath,
            HttpMethod = log.HttpMethod,
            StatusCode = log.StatusCode,
            TraceIdentifier = log.TraceIdentifier,
            CreatedAt = log.CreatedAt
        };
    }

    private static string ActionLabel(string action) => action switch
    {
        "create" => "Tạo mới",
        "update" => "Cập nhật",
        "delete" => "Xóa",
        "restore" => "Khôi phục",
        "publish" => "Phát hành",
        "unpublish" => "Thu hồi",
        "import" => "Nhập dữ liệu",
        "review" => "Duyệt",
        _ => "Thao tác"
    };

    private static string CategoryLabel(string category) => category switch
    {
        "learning" => "Quiz và ôn tập",
        "documents" => "Tài liệu",
        "subjects" => "Môn học",
        "users" => "Người dùng",
        "payments" => "Thanh toán",
        "account" => "Tài khoản",
        _ => "Hệ thống"
    };

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Limit(string? value, int maximumLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }

    private static string? LimitOptional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
