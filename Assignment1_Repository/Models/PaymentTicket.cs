using System;

namespace Assignment1_Repository.Models;

public partial class PaymentTicket
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public decimal Amount { get; set; }

    public string? TransferReference { get; set; }

    public string Status { get; set; } = PaymentTicketStatus.Pending;

    public string? AdminNote { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual UserSubscription? UserSubscription { get; set; }
}
