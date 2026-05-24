using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Embedding
{
    public int Id { get; set; }

    public int ChunkId { get; set; }

    public int EmbeddingModelId { get; set; }

    public string Vector { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Chunk Chunk { get; set; } = null!;

    public virtual EmbeddingModel EmbeddingModel { get; set; } = null!;
}
