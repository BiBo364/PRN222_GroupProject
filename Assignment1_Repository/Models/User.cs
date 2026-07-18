using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public int RoleId { get; set; }

    public int? SubjectId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<PaymentTicket> PaymentTickets { get; set; } = new List<PaymentTicket>();

    public virtual ICollection<PaymentTicket> ReviewedPaymentTickets { get; set; } = new List<PaymentTicket>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public virtual ICollection<Subject> AssignedSubjects { get; set; } = new List<Subject>();

    public virtual Role Role { get; set; } = null!;

    public virtual Subject? Subject { get; set; }
}
