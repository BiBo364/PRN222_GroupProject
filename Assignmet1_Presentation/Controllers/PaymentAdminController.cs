using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
[RequireAdmin]
public class PaymentAdminController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public PaymentAdminController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var tickets = await _subscriptionService.GetAllTicketsAsync(status);
        var pendingCount = await _subscriptionService.GetPendingTicketCountAsync();

        return View(new PaymentAdminIndexViewModel
        {
            StatusFilter = status,
            Tickets = tickets.Select(ViewModelMapper.ToViewModel).ToList(),
            PendingCount = pendingCount
        });
    }
}
