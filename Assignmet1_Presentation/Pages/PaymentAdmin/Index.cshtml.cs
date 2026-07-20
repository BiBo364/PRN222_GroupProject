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
    public PaginationSlice<PaymentTicketViewModel> TicketsPagination { get; private set; } =
        PaginationHelper.Paginate<PaymentTicketViewModel>([], 1, 15);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(string? status)
    {
        var tickets = await _subscriptionService.GetAllTicketsAsync(status);
        var pendingCount = await _subscriptionService.GetPendingTicketCountAsync();
        TicketsPagination = PaginationHelper.Paginate(
            tickets.Select(ViewModelMapper.ToViewModel),
            PageNumber,
            15);
        PageNumber = TicketsPagination.CurrentPage;

        ViewModel = new PaymentAdminIndexViewModel
        {
            StatusFilter = status,
            Tickets = TicketsPagination.Items.ToList(),
            PendingCount = pendingCount
        };

        return Page();
    }
}
