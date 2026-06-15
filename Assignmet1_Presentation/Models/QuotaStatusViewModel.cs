namespace Assignmet1_Presentation.Models;

public class QuotaStatusViewModel
{
    public int SubjectId { get; set; }

    public string? SubjectCode { get; set; }

    public string? SubjectName { get; set; }

    public bool IsPlus { get; set; }

    public bool IsAllowed { get; set; }

    public int QuestionLimit { get; set; }

    public int QuestionsUsed { get; set; }

    public int QuestionsRemaining { get; set; }

    public DateTime WindowStartAt { get; set; }

    public DateTime WindowEndAt { get; set; }

    public string CurrentPlanName { get; set; } = "Free";

    public string? CurrentPackageName { get; set; }

    public string? Message { get; set; }
}
