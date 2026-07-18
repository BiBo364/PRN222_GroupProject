using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Subject
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Soft-delete fields
    public bool? IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public int? LecturerId { get; set; }

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual User? DeletedByNavigation { get; set; }

    public virtual User? Lecturer { get; set; }
}
