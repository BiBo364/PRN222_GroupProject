namespace Assignment1_Repository.Models;

public class LearningAttemptAnswer
{
    public int Id { get; set; }
    public int LearningAttemptId { get; set; }
    public int QuestionBankItemId { get; set; }
    public string? SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public decimal AwardedPoints { get; set; }
    public DateTime AnsweredAt { get; set; }

    public virtual LearningAttempt LearningAttempt { get; set; } = null!;
    public virtual QuestionBankItem QuestionBankItem { get; set; } = null!;
}
