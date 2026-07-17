namespace Assignment1_Repository.Models;

public class LearningSet
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string ActivityType { get; set; } = LearningActivityTypes.Quiz;
    public int? DurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public string? AiModel { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<LearningSetItem> Items { get; set; } = new List<LearningSetItem>();
    public virtual ICollection<LearningAttempt> Attempts { get; set; } = new List<LearningAttempt>();
    public virtual ICollection<LearningSetVersion> Versions { get; set; } = new List<LearningSetVersion>();
}
