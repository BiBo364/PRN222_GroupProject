using Assignmet1_Presentation.Filters;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Assignmet1_Presentation.Pages.MoMoPayment;

[RequireLogin]
public class CheckoutModel : PageModel
{
    private readonly IMomoPaymentService _momoPaymentService;
    private readonly MoMoPaymentSettings _momoSettings;

    public CheckoutModel(
        IMomoPaymentService momoPaymentService,
        IOptions<MoMoPaymentSettings> momoSettings)
    {
        _momoPaymentService = momoPaymentService;
        _momoSettings = momoSettings.Value;
    }

    public async Task<IActionResult> OnPostAsync(int planId)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var returnUrl = string.IsNullOrWhiteSpace(_momoSettings.RedirectUrl)
            ? Url.Page("/MoMoPayment/Return", null, null, Request.Scheme)!
            : _momoSettings.RedirectUrl;
        var ipnUrl = string.IsNullOrWhiteSpace(_momoSettings.IpnUrl)
            ? Url.Page("/MoMoPayment/Ipn", null, null, Request.Scheme)!
            : _momoSettings.IpnUrl;

        var result = await _momoPaymentService.CreateCheckoutAsync(userId, planId, returnUrl, ipnUrl);
        if (!result.Success || string.IsNullOrWhiteSpace(result.PayUrl))
        {
            TempData["Error"] = result.Message ?? "Khong the khoi tao thanh toan MoMo.";
            return RedirectToPage("/Subscription/Index");
        }

        return Redirect(result.PayUrl);
    }
}
