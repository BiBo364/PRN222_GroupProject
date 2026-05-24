using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class BenchmarkRun
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? EmbeddingModelId { get; set; }

    public int? ChunkingConfigId { get; set; }

    public string? LlmModel { get; set; }

    public int? TopK { get; set; }

    public bool? UseReranker { get; set; }

    public string? ExtraParams { get; set; }

    public string? Status { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BenchmarkResult> BenchmarkResults { get; set; } = new List<BenchmarkResult>();

    public virtual BenchmarkSummary? BenchmarkSummary { get; set; }

    public virtual ChunkingConfig? ChunkingConfig { get; set; }

    public virtual EmbeddingModel? EmbeddingModel { get; set; }
}
