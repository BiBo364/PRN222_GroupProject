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

    public async Task<IActionResult> OnPostAsync([FromBody] MoMoCallbackRequestDto callback)
    {
        var (success, error) = await _momoPaymentService.HandleIpnAsync(callback);
        return success
            ? new OkObjectResult(new { message = "OK" })
            : new BadRequestObjectResult(new { message = error ?? "IPN failed" });
    }
}
