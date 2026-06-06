namespace Assignment1_Service.Models;

public class LoginUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int RoleId { get; set; }
    public int? SubjectId { get; set; }
}
