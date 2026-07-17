namespace Assignment1_Repository.Models;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public int? RoleId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int StatusCode { get; set; }
    public string? TraceIdentifier { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
