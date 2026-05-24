using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class EmbeddingModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string ModelId { get; set; } = null!;

    public int Dimension { get; set; }

    public string? Language { get; set; }

    public bool? IsFree { get; set; }

    public string? ApiKeyEnv { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BenchmarkRun> BenchmarkRuns { get; set; } = new List<BenchmarkRun>();

    public virtual ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();
}
