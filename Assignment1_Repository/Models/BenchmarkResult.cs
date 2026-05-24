using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class BenchmarkResult
{
    public int Id { get; set; }

    public int RunId { get; set; }

    public int QuestionId { get; set; }

    public string? GeneratedAnswer { get; set; }

    public string? RetrievedChunkIds { get; set; }

    public int? LatencyMs { get; set; }

    public double? Faithfulness { get; set; }

    public double? AnswerRelevancy { get; set; }

    public double? ContextPrecision { get; set; }

    public double? ContextRecall { get; set; }

    public double? AnswerCorrectness { get; set; }

    public string? ErrorMsg { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TestQuestion Question { get; set; } = null!;

    public virtual BenchmarkRun Run { get; set; } = null!;
}
