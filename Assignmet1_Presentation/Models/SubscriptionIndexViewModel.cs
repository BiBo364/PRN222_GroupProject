using Assignment1_Repository.Models;
using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class SubscriptionIndexViewModel
{
    public List<SubscriptionPlan> Plans { get; set; } = [];

    public UserSubscription? ActiveSubscription { get; set; }

    public List<PaymentTicket> Tickets { get; set; } = [];

    public PaymentSettings PaymentInfo { get; set; } = new();

    public CreateTicketInput CreateTicket { get; set; } = new();
}

public class CreateTicketInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn gói.")]
    [Display(Name = "Gói đăng ký")]
    public int PlanId { get; set; }
}
