using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Chunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int? ChunkingConfigId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public int? PageNumber { get; set; }

    public int? CharStart { get; set; }

    public int? CharEnd { get; set; }

    public int? TokenCount { get; set; }

    public string? Metadata { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ChunkingConfig? ChunkingConfig { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();

    public virtual ICollection<MessageCitation> MessageCitations { get; set; } = new List<MessageCitation>();
}
