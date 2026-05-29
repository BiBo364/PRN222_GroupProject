using System;

namespace Assignment1_Repository.Models;

public partial class UserSubscription
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; }

    public int? PaymentTicketId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual PaymentTicket? PaymentTicket { get; set; }
}
