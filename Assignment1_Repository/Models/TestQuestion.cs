using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class TestQuestion
{
    public int Id { get; set; }

    public int? SubjectId { get; set; }

    public int? ChapterId { get; set; }

    public string Question { get; set; } = null!;

    public string GroundTruth { get; set; } = null!;

    public string? GroundTruthChunks { get; set; }

    public string? Difficulty { get; set; }

    public string? Category { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BenchmarkResult> BenchmarkResults { get; set; } = new List<BenchmarkResult>();

    public virtual Chapter? Chapter { get; set; }

    public virtual Subject? Subject { get; set; }
}
