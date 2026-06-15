using Assignmet1_Presentation.Constants;
using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Controllers;

public class MoMoPaymentController : Controller
{
    private readonly IMomoPaymentService _momoPaymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly MoMoPaymentSettings _momoSettings;

    public MoMoPaymentController(
        IMomoPaymentService momoPaymentService,
        ISubscriptionService subscriptionService,
        IOptions<MoMoPaymentSettings> momoSettings)
    {
        _momoPaymentService = momoPaymentService;
        _subscriptionService = subscriptionService;
        _momoSettings = momoSettings.Value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLogin]
    public async Task<IActionResult> Checkout(int planId)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var returnUrl = string.IsNullOrWhiteSpace(_momoSettings.RedirectUrl)
            ? Url.Action(nameof(Return), "MoMoPayment", null, Request.Scheme)!
            : _momoSettings.RedirectUrl;
        var ipnUrl = string.IsNullOrWhiteSpace(_momoSettings.IpnUrl)
            ? Url.Action(nameof(Ipn), "MoMoPayment", null, Request.Scheme)!
            : _momoSettings.IpnUrl;

        var result = await _momoPaymentService.CreateCheckoutAsync(userId, planId, returnUrl, ipnUrl);
        if (!result.Success || string.IsNullOrWhiteSpace(result.PayUrl))
        {
            TempData["Error"] = result.Message ?? "Khong the khoi tao thanh toan MoMo.";
            return RedirectToAction("Index", "Subscription");
        }

        return Redirect(result.PayUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Return(string? orderId, int? resultCode, string? message)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var ticket = await _momoPaymentService.GetTicketByOrderIdAsync(orderId);
            if (ticket is not null && ticket.Status == PaymentStatuses.Approved)
            {
                TempData["Success"] = "Thanh toan MoMo thanh cong. Subscription da duoc kich hoat.";
                return RedirectToAction("Index", "Subscription");
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
                    return RedirectToAction("Index", "Subscription");
                }

                TempData["Error"] = completed.Error ?? "MoMo da bao thanh cong nhung khong the kich hoat goi.";
                return RedirectToAction("Index", "Subscription");
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

        return RedirectToAction("Index", "Subscription");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ipn([FromBody] MoMoCallbackRequestDto callback)
    {
        var (success, error) = await _momoPaymentService.HandleIpnAsync(callback);
        return success ? Ok(new { message = "OK" }) : BadRequest(new { message = error ?? "IPN failed" });
    }
}
