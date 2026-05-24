using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Session
{
    public string Id { get; set; } = null!;

    public int? SubjectId { get; set; }

    public string? Title { get; set; }

    public string? UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsArchived { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual Subject? Subject { get; set; }
}
