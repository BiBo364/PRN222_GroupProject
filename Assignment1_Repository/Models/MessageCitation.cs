using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class MessageCitation
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public int ChunkId { get; set; }

    public double? SimilarityScore { get; set; }

    public int? RankOrder { get; set; }

    public bool? WasUsed { get; set; }

    public virtual Chunk Chunk { get; set; } = null!;

    public virtual Message Message { get; set; } = null!;
}
