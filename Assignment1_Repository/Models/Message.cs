using System;
using System.Collections.Generic;

namespace Assignment1_Repository.Models;

public partial class Message
{
    public int Id { get; set; }

    public string SessionId { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<MessageCitation> MessageCitations { get; set; } = new List<MessageCitation>();

    public virtual Session Session { get; set; } = null!;
}
