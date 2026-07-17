namespace Assignment1_Repository.Models;

public class QuestionBankItem
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ChapterId { get; set; }
    public string QuestionType { get; set; } = LearningQuestionTypes.MultipleChoice;
    public string Prompt { get; set; } = string.Empty;
    public string? OptionsJson { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = LearningDifficultyLevels.Medium;
    public string? Topic { get; set; }
    public string? LearningObjective { get; set; }
    public string? SourceReferencesJson { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsAiGenerated { get; set; } = true;
    public string? AiModel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual Chapter? Chapter { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<LearningSetItem> LearningSetItems { get; set; } = new List<LearningSetItem>();
    public virtual ICollection<LearningAttemptAnswer> AttemptAnswers { get; set; } = new List<LearningAttemptAnswer>();
}
