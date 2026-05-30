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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? adminNote)
    {
        var adminId = HttpContext.Session.GetInt32("UserId")!.Value;
        var (success, error) = await _subscriptionService.ApproveTicketAsync(id, adminId, adminNote);

        TempData[success ? "Success" : "Error"] = success
            ? "Đã chấp nhận thanh toán. Người dùng đã được cấp subscription."
            : error ?? "Không thể duyệt ticket.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNote)
    {
        var adminId = HttpContext.Session.GetInt32("UserId")!.Value;
        var (success, error) = await _subscriptionService.RejectTicketAsync(id, adminId, adminNote);

        TempData[success ? "Success" : "Error"] = success
            ? "Đã từ chối ticket thanh toán."
            : error ?? "Không thể từ chối ticket.";

        return RedirectToAction(nameof(Index));
    }
}
