namespace Assignment1_Repository.Models;

public class LearningSetItem
{
    public int Id { get; set; }
    public int LearningSetId { get; set; }
    public int QuestionBankItemId { get; set; }
    public int OrderIndex { get; set; }
    public decimal Points { get; set; } = 1m;

    public virtual LearningSet LearningSet { get; set; } = null!;
    public virtual QuestionBankItem QuestionBankItem { get; set; } = null!;
}
