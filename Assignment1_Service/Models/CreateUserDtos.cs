namespace Assignment1_Service.Models;

public sealed class CreateUserRequestDto
{
    public string FullName { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Email { get; init; }
    public int RoleId { get; init; }
    public int? SubjectId { get; init; }
}

public sealed class CreateUserResultDto
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? UserId { get; init; }
    public string? FullName { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? TemporaryPassword { get; init; }
    public bool NotificationSent { get; init; }
    public string? NotificationMessage { get; init; }
}
