using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.PaymentAdmin;

// @page "/PaymentAdmin/Index"
[RequireLogin]
[RequireAdmin]
public class IndexModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;

    public IndexModel(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public PaymentAdminIndexViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? status)
    {
        var tickets = await _subscriptionService.GetAllTicketsAsync(status);
        var pendingCount = await _subscriptionService.GetPendingTicketCountAsync();

        ViewModel = new PaymentAdminIndexViewModel
        {
            StatusFilter = status,
            Tickets = tickets.Select(ViewModelMapper.ToViewModel).ToList(),
            PendingCount = pendingCount
        };

        return Page();
    }
}
