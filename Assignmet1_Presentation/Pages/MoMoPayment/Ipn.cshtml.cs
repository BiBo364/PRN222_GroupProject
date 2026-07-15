using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.MoMoPayment;

[IgnoreAntiforgeryToken]
public class IpnModel : PageModel
{
    private readonly IMomoPaymentService _momoPaymentService;

    public IpnModel(IMomoPaymentService momoPaymentService)
    {
        _momoPaymentService = momoPaymentService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var callback = await Request.ReadFromJsonAsync<MoMoCallbackRequestDto>();
        if (callback is null)
            return BadRequest(new { message = "IPN body is required." });

        var (success, error) = await _momoPaymentService.HandleIpnAsync(callback);
        // MoMo expects HTTP 204 after the notification is processed.
        return success
            ? new NoContentResult()
            : new BadRequestObjectResult(new { message = error ?? "IPN failed" });
    }
}
