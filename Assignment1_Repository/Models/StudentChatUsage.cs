using System;

namespace Assignment1_Repository.Models;

public partial class StudentChatUsage
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubjectId { get; set; }

    public DateTime WindowStart { get; set; }

    public int QuestionCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
