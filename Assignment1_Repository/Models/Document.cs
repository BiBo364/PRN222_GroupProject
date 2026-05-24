using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Document
{
    public int Id { get; set; }

    public int? SubjectId { get; set; }

    public int? ChapterId { get; set; }

    public string Filename { get; set; } = null!;

    public string OriginalName { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public long? FileSize { get; set; }

    public string? FileHash { get; set; }

    public string StoragePath { get; set; } = null!;

    public int? PageCount { get; set; }

    public string? Status { get; set; }

    public string? ErrorMsg { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? IndexedAt { get; set; }

    public virtual Chapter? Chapter { get; set; }

    public virtual ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();

    public virtual Subject? Subject { get; set; }

    public virtual User? UploadedByNavigation { get; set; }
}
