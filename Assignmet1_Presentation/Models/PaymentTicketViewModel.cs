namespace Assignmet1_Presentation.Models;

public class PaymentTicketViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransferReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
