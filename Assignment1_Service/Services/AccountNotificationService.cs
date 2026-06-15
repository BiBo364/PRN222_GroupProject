using System.Net;
using System.Net.Mail;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class AccountNotificationService : IAccountNotificationService
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<AccountNotificationService> _logger;

    public AccountNotificationService(
        IOptions<SmtpOptions> smtpOptions,
        ILogger<AccountNotificationService> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task<AccountNotificationResult> SendAccountCreatedEmailAsync(
        string toEmail,
        string fullName,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        if (!_smtpOptions.Enabled)
        {
            return new AccountNotificationResult
            {
                IsSuccess = false,
                Message = "SMTP chua duoc bat trong cau hinh."
            };
        }

        if (!IsConfigured())
        {
            return new AccountNotificationResult
            {
                IsSuccess = false,
                Message = "SMTP chua duoc cau hinh day du."
            };
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
                Subject = "Tai khoan RAG EDU da duoc tao",
                Body = BuildHtmlBody(fullName, username, temporaryPassword),
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
            };

            using var registration = cancellationToken.Register(client.SendAsyncCancel);
            await client.SendMailAsync(message, cancellationToken);

            return new AccountNotificationResult
            {
                IsSuccess = true,
                Message = "Da gui email thong bao."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account creation email to {Email}.", toEmail);
            return new AccountNotificationResult
            {
                IsSuccess = false,
                Message = $"Gui email that bai: {ex.Message}"
            };
        }
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_smtpOptions.Host)
            && _smtpOptions.Port > 0
            && !string.IsNullOrWhiteSpace(_smtpOptions.Username)
            && !string.IsNullOrWhiteSpace(_smtpOptions.Password)
            && !string.IsNullOrWhiteSpace(_smtpOptions.FromEmail);
    }

    private string BuildHtmlBody(string fullName, string username, string temporaryPassword)
    {
        var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? username : fullName);
        var safeUsername = WebUtility.HtmlEncode(username);
        var safePassword = WebUtility.HtmlEncode(temporaryPassword);
        var loginUrl = string.IsNullOrWhiteSpace(_smtpOptions.LoginUrl)
            ? string.Empty
            : $"<p><a href=\"{WebUtility.HtmlEncode(_smtpOptions.LoginUrl)}\">Dang nhap he thong</a></p>";

        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.6;color:#1f2937">
                <p>Xin chao {displayName},</p>
                <p>Tai khoan hoc tap cua ban tren he thong RAG EDU da duoc tao.</p>
                <p><strong>Username:</strong> {safeUsername}<br />
                <strong>Mat khau tam thoi:</strong> {safePassword}</p>
                <p>Vui long dang nhap va doi mat khau ngay o lan dang nhap dau tien de bao mat tai khoan.</p>
                {loginUrl}
                <p>Neu ban khong nhan yeu cau nay, vui long lien he admin hoac giang vien phu trach.</p>
            </div>
            """;
    }
}
