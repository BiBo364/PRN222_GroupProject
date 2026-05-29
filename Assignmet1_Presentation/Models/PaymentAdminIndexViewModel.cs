using Assignment1_Repository.Models;

namespace Assignmet1_Presentation.Models;

public class PaymentAdminIndexViewModel
{
    public string? StatusFilter { get; set; }

    public List<PaymentTicket> Tickets { get; set; } = [];

    public int PendingCount { get; set; }
}
