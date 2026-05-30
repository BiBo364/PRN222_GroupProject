namespace Assignment1_Service.Models;

public class UserSubscriptionDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}
