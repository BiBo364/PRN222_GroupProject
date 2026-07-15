using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IMomoPaymentService
{
    Task<MoMoCheckoutResultDto> CreateCheckoutAsync(int userId, int planId, string returnUrl, string ipnUrl);

    Task<(bool Success, string? Error)> HandleIpnAsync(MoMoCallbackRequestDto callback);

    Task<(bool Success, string? Error)> ReconcilePendingTicketAsync(int ticketId);

    Task<PaymentTicketDto?> GetTicketByOrderIdAsync(string orderId);
}
