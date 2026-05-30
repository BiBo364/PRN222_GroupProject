using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly PaymentSettings _paymentSettings;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IOptions<PaymentSettings> paymentSettings)
    {
        _subscriptionService = subscriptionService;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var plans = await _subscriptionService.GetActivePlansAsync();
        var active = await _subscriptionService.GetActiveSubscriptionAsync(userId);
        var tickets = await _subscriptionService.GetUserTicketsAsync(userId);

        return View(new SubscriptionIndexViewModel
        {
            Plans = plans.Select(ViewModelMapper.ToViewModel).ToList(),
            ActiveSubscription = active is null ? null : ViewModelMapper.ToViewModel(active),
            Tickets = tickets.Select(ViewModelMapper.ToViewModel).ToList(),
            PaymentInfo = _paymentSettings,
            CreateTicket = new CreateTicketViewModel()
        });
    }

    [HttpGet]
    public IActionResult CreateTicket() => RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(
        [Bind(Prefix = "CreateTicket")] CreateTicketViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetFirstErrorMessage() ?? "Vui lòng chọn gói đăng ký.";
            return RedirectToAction(nameof(Index));
        }

        var error = await _subscriptionService.CreateTicketAsync(userId, model.PlanId);

        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã tạo ticket thanh toán. Admin sẽ xem và duyệt sau khi nhận tiền.";
        return RedirectToAction(nameof(Index));
    }

    private string? GetFirstErrorMessage()
    {
        foreach (var entry in ModelState.Values)
        {
            var message = entry.Errors.FirstOrDefault()?.ErrorMessage;
            if (!string.IsNullOrWhiteSpace(message))
                return message;
        }

        return null;
    }
}
