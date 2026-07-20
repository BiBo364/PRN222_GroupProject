namespace Assignment1_Service.Models;

public sealed class RecordAuditLogRequest
{
    public int? UserId { get; init; }
    public int? RoleId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public object? Details { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public int StatusCode { get; init; }
    public string? TraceIdentifier { get; init; }
}

public sealed class AuditLogQuery
{
    public string? Category { get; init; }
    public string? Action { get; init; }
    public string? Search { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AuditLogPageDto
{
    public IReadOnlyList<AuditLogEntryDto> Items { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool CanViewAllUsers { get; init; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public sealed class AuditLogEntryDto
{
    public long Id { get; init; }
    public int? UserId { get; init; }
    public string UserDisplayName { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ActionLabel { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public int StatusCode { get; init; }
    public string? TraceIdentifier { get; init; }
    public DateTime CreatedAt { get; init; }
}
