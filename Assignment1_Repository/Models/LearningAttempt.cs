namespace Assignment1_Repository.Models;

public class LearningAttempt
{
    public int Id { get; set; }
    public int LearningSetId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal Score { get; set; }
    public decimal TotalPoints { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }

    public virtual LearningSet LearningSet { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<LearningAttemptAnswer> Answers { get; set; } = new List<LearningAttemptAnswer>();
}
