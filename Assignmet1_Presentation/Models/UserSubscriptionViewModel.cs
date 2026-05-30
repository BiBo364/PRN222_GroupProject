namespace Assignmet1_Presentation.Models;

public class UserSubscriptionViewModel
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}
