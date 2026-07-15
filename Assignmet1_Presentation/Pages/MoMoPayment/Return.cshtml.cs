using Assignmet1_Presentation.Constants;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages.MoMoPayment;

public class ReturnModel : PageModel
{
    private readonly IMomoPaymentService _momoPaymentService;

    public ReturnModel(IMomoPaymentService momoPaymentService)
    {
        _momoPaymentService = momoPaymentService;
    }

    public async Task<IActionResult> OnGetAsync(
        string? partnerCode,
        string? orderId,
        string? requestId,
        decimal? amount,
        string? orderInfo,
        string? orderType,
        long? transId,
        int? resultCode,
        string? message,
        string? payType,
        long? responseTime,
        string? extraData,
        string? signature)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var ticket = await _momoPaymentService.GetTicketByOrderIdAsync(orderId);
            if (ticket is not null && ticket.Status == PaymentStatuses.Approved)
            {
                TempData["Success"] = "Thanh toan MoMo thanh cong. Subscription da duoc kich hoat.";
                return RedirectToPage("/Subscription/Index");
            }

            if (ticket is not null && ticket.Status == PaymentStatuses.MomoPending)
            {
                var callback = new MoMoCallbackRequestDto
                {
                    PartnerCode = partnerCode,
                    OrderId = orderId,
                    RequestId = requestId,
                    Amount = amount ?? 0,
                    OrderInfo = orderInfo,
                    OrderType = orderType,
                    TransId = transId,
                    ResultCode = resultCode ?? -1,
                    Message = message,
                    PayType = payType,
                    ResponseTime = responseTime ?? 0,
                    ExtraData = extraData,
                    Signature = signature
                };

                var completed = await _momoPaymentService.HandleIpnAsync(callback);

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
