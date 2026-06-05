namespace Assignment1_Service.Models;

public class UserListItemDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public int? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
