namespace Assignmet1_Presentation.Models;

public class SubscriptionIndexViewModel
{
    public List<SubscriptionPlanViewModel> Plans { get; set; } = [];
    public bool IsPlus { get; set; }
    public string CurrentPlanName { get; set; } = "Free";
    public string? CurrentPackageName { get; set; }
    public int FreeQuestionLimit { get; set; }
    public int FreeQuotaWindowHours { get; set; }
    public UserSubscriptionViewModel? ActiveSubscription { get; set; }
    public List<QuotaStatusViewModel> SubjectQuotas { get; set; } = [];
    public List<PaymentTicketViewModel> Tickets { get; set; } = [];
}
