using Assignment1_Service.Models;

namespace Assignment1_Service.Services.Interfaces;

public interface IAccountNotificationService
{
    Task<AccountNotificationResult> SendAccountCreatedEmailAsync(
        string toEmail,
        string fullName,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
