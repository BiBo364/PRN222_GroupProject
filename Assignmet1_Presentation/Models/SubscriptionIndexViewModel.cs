namespace Assignmet1_Presentation.Models;

public class SubscriptionIndexViewModel
{
    public bool IsAdminView { get; set; }
    public List<SubscriptionPlanViewModel> Plans { get; set; } = [];
    public bool IsPlus { get; set; }
    public string CurrentPlanName { get; set; } = "Free";
    public string? CurrentPackageName { get; set; }
    public int FreeQuestionLimit { get; set; }
    public int FreeQuotaWindowHours { get; set; }
    public UserSubscriptionViewModel? ActiveSubscription { get; set; }
    public List<QuotaStatusViewModel> SubjectQuotas { get; set; } = [];
    public List<PaymentTicketViewModel> Tickets { get; set; } = [];
    public int SubjectCount { get; set; }
    public int SubjectsOutOfQuotaCount { get; set; }
    public int TotalQuestionsRemaining { get; set; }
    public int LowestQuestionsRemaining { get; set; }
    public DateTime? NextResetAt { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
    public decimal AdminTotalRevenue { get; set; }
    public int AdminSuccessfulOrders { get; set; }
    public string AdminTopPlanName { get; set; } = "Chưa có";
    public int AdminTopPlanCount { get; set; }
    public decimal AdminAverageOrderValue { get; set; }
    public List<AdminSubscriptionPlanReportViewModel> AdminPlanReports { get; set; } = [];
}
