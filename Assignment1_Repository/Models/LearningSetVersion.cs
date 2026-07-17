namespace Assignment1_Repository.Models;

public class LearningSetVersion
{
    public int Id { get; set; }
    public int LearningSetId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual LearningSet LearningSet { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}
