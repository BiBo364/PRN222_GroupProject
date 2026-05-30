using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class SubscriptionIndexViewModel
{
    public List<SubscriptionPlanViewModel> Plans { get; set; } = [];
    public UserSubscriptionViewModel? ActiveSubscription { get; set; }
    public List<PaymentTicketViewModel> Tickets { get; set; } = [];
    public PaymentSettings PaymentInfo { get; set; } = new();
    public CreateTicketViewModel CreateTicket { get; set; } = new();
}

public class CreateTicketViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn gói.")]
    [Display(Name = "Gói đăng ký")]
    public int PlanId { get; set; }
}
