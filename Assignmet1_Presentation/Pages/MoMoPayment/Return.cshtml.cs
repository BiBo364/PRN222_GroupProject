using Assignmet1_Presentation.Constants;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.MoMoPayment;

public class ReturnModel : PageModel
{
    private readonly IMomoPaymentService _momoPaymentService;
    private readonly ISubscriptionService _subscriptionService;

    public ReturnModel(
        IMomoPaymentService momoPaymentService,
        ISubscriptionService subscriptionService)
    {
        _momoPaymentService = momoPaymentService;
        _subscriptionService = subscriptionService;
    }

    public async Task<IActionResult> OnGetAsync(string? orderId, int? resultCode, string? message)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var ticket = await _momoPaymentService.GetTicketByOrderIdAsync(orderId);
            if (ticket is not null && ticket.Status == PaymentStatuses.Approved)
            {
                TempData["Success"] = "Thanh toan MoMo thanh cong. Subscription da duoc kich hoat.";
                return RedirectToPage("/Subscription/Index");
            }

            if (ticket is not null &&
                resultCode == 0 &&
                ticket.Status == PaymentStatuses.MomoPending)
            {
                var completed = await _subscriptionService.CompleteTicketAsync(
                    ticket.Id,
                    "MoMo return success fallback (local test without IPN)");

                if (completed.Success)
                {
                    TempData["Success"] = "Thanh toan MoMo thanh cong. Subscription da duoc kich hoat.";
                    return RedirectToPage("/Subscription/Index");
                }

                TempData["Error"] = completed.Error ?? "MoMo da bao thanh cong nhung khong the kich hoat goi.";
                return RedirectToPage("/Subscription/Index");
            }
        }

        if (resultCode == 0)
        {
            TempData["Info"] = "MoMo da ghi nhan giao dich. He thong dang cho xac nhan callback.";
        }
        else
        {
            TempData["Error"] = message ?? "Thanh toan MoMo chua hoan tat.";
        }

        return RedirectToPage("/Subscription/Index");
    }
}
