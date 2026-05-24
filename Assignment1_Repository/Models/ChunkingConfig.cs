using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class ChunkingConfig
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Strategy { get; set; } = null!;

    public int ChunkSize { get; set; }

    public int ChunkOverlap { get; set; }

    public string? Params { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BenchmarkRun> BenchmarkRuns { get; set; } = new List<BenchmarkRun>();

    public virtual ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();
}
