namespace Assignment1_Service.Models;

public class PaymentTicketDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransferReference { get; set; }
    public string Status { get; set; } = PaymentTicketStatuses.Pending;
    public string? AdminNote { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
