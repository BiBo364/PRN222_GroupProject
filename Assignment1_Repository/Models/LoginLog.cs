using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class LoginLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
