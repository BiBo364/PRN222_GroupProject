namespace Assignmet1_Presentation.Models;

public class PaymentAdminIndexViewModel
{
    public string? StatusFilter { get; set; }
    public List<PaymentTicketViewModel> Tickets { get; set; } = [];
    public int PendingCount { get; set; }
}
