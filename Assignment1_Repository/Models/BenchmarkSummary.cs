using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class BenchmarkSummary
{
    public int Id { get; set; }

    public int RunId { get; set; }

    public int? TotalQuestions { get; set; }

    public double? AvgFaithfulness { get; set; }

    public double? AvgAnswerRelevancy { get; set; }

    public double? AvgContextPrecision { get; set; }

    public double? AvgContextRecall { get; set; }

    public double? AvgAnswerCorrectness { get; set; }

    public double? AvgLatencyMs { get; set; }

    public double? P95LatencyMs { get; set; }

    public DateTime? ComputedAt { get; set; }

    public virtual BenchmarkRun Run { get; set; } = null!;
}
