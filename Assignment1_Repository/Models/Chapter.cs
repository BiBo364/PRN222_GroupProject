using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Chapter
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
}
