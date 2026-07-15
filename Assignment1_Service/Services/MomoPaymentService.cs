using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Assignment1_Repository.Models;
using Assignment1_Repository.Repositories.Interfaces;
using Assignment1_Service.Helpers;
using Assignment1_Service.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Assignment1_Service.Services;

public class MomoPaymentService : IMomoPaymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private const string DefaultOrderInfo = "Thanh toan subscription plan";

    private readonly HttpClient _httpClient;
    private readonly ISubscriptionRepository _repository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly MoMoPaymentSettings _settings;

    public MomoPaymentService(
        HttpClient httpClient,
        ISubscriptionRepository repository,
        ISubscriptionService subscriptionService,
        IOptions<MoMoPaymentSettings> settings)
    {
        _httpClient = httpClient;
        _repository = repository;
        _subscriptionService = subscriptionService;
        _settings = settings.Value;
    }

    public async Task<MoMoCheckoutResultDto> CreateCheckoutAsync(int userId, int planId, string returnUrl, string ipnUrl)
    {
        var accessKey = SanitizeSetting(_settings.AccessKey);
        var secretKey = SanitizeSetting(_settings.SecretKey);
        var partnerCode = SanitizeSetting(_settings.PartnerCode);
        var requestType = SanitizeSetting(_settings.RequestType, "payWithMethod");
        var partnerName = SanitizeSetting(_settings.PartnerName, "RAG EDU");
        var storeId = SanitizeSetting(_settings.StoreId, "RagEduStore");
        var orderGroupId = SanitizeSetting(_settings.OrderGroupId);
        var lang = SanitizeSetting(_settings.Lang, "vi");
        var endpoint = SanitizeSetting(_settings.Endpoint, "https://test-payment.momo.vn/v2/gateway/api/create");
        var effectiveReturnUrl = SanitizeSetting(returnUrl);
        var effectiveIpnUrl = SanitizeSetting(ipnUrl);

        if (string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(partnerCode))
        {
            return new MoMoCheckoutResultDto
            {
                Success = false,
                ResultCode = -1,
                Message = "Thieu cau hinh MoMo."
            };
        }

        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan is null)
        {
            return new MoMoCheckoutResultDto
            {
                Success = false,
                ResultCode = -1,
                Message = "Goi dang ky khong hop le."
            };
        }

        var pendingExpirationMinutes = Math.Max(_settings.PendingExpirationMinutes, 1);
        var blockingPendingCutoff = DateTime.UtcNow.AddMinutes(-pendingExpirationMinutes);

        if (await _repository.HasPendingTicketAsync(userId, planId, blockingPendingCutoff))
        {
            return new MoMoCheckoutResultDto
            {
                Success = false,
                ResultCode = -1,
                Message = $"Ban da co giao dich dang cho xu ly cho goi nay. Neu chua thanh toan, vui long doi {pendingExpirationMinutes} phut roi tao lai."
            };
        }

        var requestId = Guid.NewGuid().ToString("N");
        var orderId = $"RAG-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N")[..8]}";
        var amount = DecimalToInteger(plan.Price);
        var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            userId,
            planId
        }, JsonOptions)));

        var ticket = await _repository.AddTicketAsync(new PaymentTicket
        {
            UserId = userId,
            PlanId = plan.Id,
            Amount = plan.Price,
            TransferReference = orderId,
            PaymentMethod = "momo",
            MomoOrderId = orderId,
            MomoRequestId = requestId,
            Status = PaymentTicketStatus.MomoPending
        });

        var requestPayload = new MoMoCreateRequestDto
        {
            PartnerCode = partnerCode,
            PartnerName = partnerName,
            StoreId = storeId,
            AccessKey = accessKey,
            RequestId = requestId,
            Amount = amount.ToString(CultureInfo.InvariantCulture),
            OrderId = orderId,
            OrderInfo = DefaultOrderInfo,
            RedirectUrl = effectiveReturnUrl,
            IpnUrl = effectiveIpnUrl,
            ExtraData = extraData,
            RequestType = requestType,
            OrderGroupId = orderGroupId,
            AutoCapture = _settings.AutoCapture,
            Lang = lang
        };

        requestPayload.Signature = BuildCreateSignature(
            secretKey,
            requestPayload.AccessKey,
            requestPayload.Amount,
            requestPayload.ExtraData,
            requestPayload.IpnUrl,
            requestPayload.OrderId,
            requestPayload.OrderInfo,
            requestPayload.PartnerCode,
            requestPayload.RedirectUrl,
            requestPayload.RequestId,
            requestPayload.RequestType);

        string responseJson;
        MoMoCreateResponseDto? momoResponse;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, requestPayload, JsonOptions);
            responseJson = await response.Content.ReadAsStringAsync();
            momoResponse = JsonSerializer.Deserialize<MoMoCreateResponseDto>(responseJson, JsonOptions);
        }
        catch (Exception ex)
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = $"MoMo checkout failed: {ex.Message}";
            ticket.MomoResponseJson = JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
            await _repository.UpdateTicketAsync(ticket);

            return new MoMoCheckoutResultDto
            {
                Success = false,
                ResultCode = -1,
                Message = "Khong the khoi tao thanh toan MoMo."
            };
        }

        ticket.MomoResponseJson = responseJson;
        ticket.MomoResultCode = momoResponse?.ResultCode;
        ticket.MomoPayUrl = momoResponse?.PayUrl;

        if (momoResponse is null || momoResponse.ResultCode != 0 || string.IsNullOrWhiteSpace(momoResponse.PayUrl))
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = momoResponse?.Message ?? "MoMo checkout failed.";
            await _repository.UpdateTicketAsync(ticket);

            return new MoMoCheckoutResultDto
            {
                Success = false,
                ResultCode = momoResponse?.ResultCode ?? -1,
                Message = momoResponse?.Message ?? "Khong the khoi tao thanh toan MoMo."
            };
        }

        await _repository.UpdateTicketAsync(ticket);

        return new MoMoCheckoutResultDto
        {
            Success = true,
            ResultCode = momoResponse.ResultCode,
            Message = momoResponse.Message,
            PayUrl = momoResponse.PayUrl,
            OrderId = orderId,
            RequestId = requestId
        };
    }

    public async Task<(bool Success, string? Error)> HandleIpnAsync(MoMoCallbackRequestDto callback)
    {
        if (string.IsNullOrWhiteSpace(callback.OrderId))
            return (false, "Thieu orderId.");

        var ticket = await _repository.GetTicketByOrderIdAsync(callback.OrderId);
        if (ticket is null)
            return (false, "Khong tim thay giao dich MoMo.");

        ticket.MomoIpnJson = JsonSerializer.Serialize(callback, JsonOptions);
        ticket.MomoTransId = callback.TransId?.ToString(CultureInfo.InvariantCulture);
        ticket.MomoResultCode = callback.ResultCode;

        if (ticket.Status == PaymentTicketStatus.Approved)
        {
            await _repository.UpdateTicketAsync(ticket);
            return (true, null);
        }

        if (DecimalToInteger(ticket.Amount) != DecimalToInteger(callback.Amount))
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = "MoMo amount mismatch.";
            await _repository.UpdateTicketAsync(ticket);
            return (false, "So tien thanh toan khong khop.");
        }

        if (!string.Equals(callback.PartnerCode, SanitizeSetting(_settings.PartnerCode), StringComparison.Ordinal))
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = "MoMo partner code mismatch.";
            await _repository.UpdateTicketAsync(ticket);
            return (false, "Giao dich MoMo khong hop le.");
        }

        if (!string.IsNullOrWhiteSpace(callback.RequestId) &&
            !string.Equals(callback.RequestId, ticket.MomoRequestId, StringComparison.Ordinal))
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = "MoMo request id mismatch.";
            await _repository.UpdateTicketAsync(ticket);
            return (false, "Giao dich MoMo khong hop le.");
        }

        if (!VerifyCallbackSignature(callback))
        {
            ticket.Status = PaymentTicketStatus.Rejected;
            ticket.AdminNote = "MoMo signature invalid.";
            await _repository.UpdateTicketAsync(ticket);
            return (false, "Chu ky MoMo khong hop le.");
        }

        if (callback.ResultCode == 0)
        {
            await _repository.UpdateTicketAsync(ticket);
            var completed = await _subscriptionService.CompleteTicketAsync(ticket.Id, "MoMo IPN success");
            return completed.Success
                ? (true, null)
                : (false, completed.Error);
        }

        ticket.Status = PaymentTicketStatus.Rejected;
        ticket.AdminNote = callback.Message ?? "MoMo payment failed.";
        await _repository.UpdateTicketAsync(ticket);

        return (false, callback.Message ?? "Thanh toan MoMo that bai.");
    }

    public async Task<PaymentTicketDto?> GetTicketByOrderIdAsync(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return null;

        var ticket = await _repository.GetTicketByOrderIdAsync(orderId);
        return ticket is null ? null : DtoMapper.ToDto(ticket);
    }

    public async Task<(bool Success, string? Error)> ReconcilePendingTicketAsync(int ticketId)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);
        if (ticket is null)
            return (false, "Khong tim thay giao dich MoMo.");

        if (ticket.Status == PaymentTicketStatus.Approved)
            return (true, null);

        if (ticket.Status != PaymentTicketStatus.MomoPending)
            return (false, "Giao dich nay khong con cho MoMo xac nhan.");

        var accessKey = SanitizeSetting(_settings.AccessKey);
        var secretKey = SanitizeSetting(_settings.SecretKey);
        var partnerCode = SanitizeSetting(_settings.PartnerCode);
        if (string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(partnerCode) ||
            string.IsNullOrWhiteSpace(ticket.MomoOrderId))
        {
            return (false, "Thieu cau hinh hoac ma don MoMo.");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var query = new MoMoQueryRequestDto
        {
            PartnerCode = partnerCode,
            RequestId = requestId,
            OrderId = ticket.MomoOrderId,
            Lang = SanitizeSetting(_settings.Lang, "vi")
        };
        query.Signature = BuildQuerySignature(secretKey, accessKey, query.OrderId, partnerCode, requestId);

        MoMoQueryResponseDto? response;
        try
        {
            var endpoint = GetQueryEndpoint();
            using var httpResponse = await _httpClient.PostAsJsonAsync(endpoint, query, JsonOptions);
            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<MoMoQueryResponseDto>(responseJson, JsonOptions);
        }
        catch (Exception ex)
        {
            return (false, $"Khong the truy van trang thai MoMo: {ex.Message}");
        }

        if (response is null ||
            !string.Equals(response.PartnerCode, partnerCode, StringComparison.Ordinal) ||
            !string.Equals(response.RequestId, requestId, StringComparison.Ordinal) ||
            !string.Equals(response.OrderId, ticket.MomoOrderId, StringComparison.Ordinal) ||
            DecimalToInteger(response.Amount) != DecimalToInteger(ticket.Amount))
        {
            return (false, "Phan hoi truy van MoMo khong hop le.");
        }

        ticket.MomoResultCode = response.ResultCode;
        ticket.MomoTransId = response.TransId?.ToString(CultureInfo.InvariantCulture);

        if (response.ResultCode != 0)
        {
            ticket.AdminNote = $"MoMo query: {response.Message ?? "Chua co ket qua thanh toan."}";
            await _repository.UpdateTicketAsync(ticket);
            return (false, ticket.AdminNote);
        }

        await _repository.UpdateTicketAsync(ticket);
        return await _subscriptionService.CompleteTicketAsync(
            ticket.Id,
            "MoMo query success fallback (IPN unavailable)");
    }

    private bool VerifyCallbackSignature(MoMoCallbackRequestDto callback)
    {
        if (string.IsNullOrWhiteSpace(callback.Signature))
            return false;

        var rawSignature = BuildCallbackSignature(callback);
        var expectedSignature = ComputeHmacSha256(SanitizeSetting(_settings.SecretKey), rawSignature);
        return string.Equals(expectedSignature, callback.Signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCreateSignature(
        string secretKey,
        string accessKey,
        string amount,
        string extraData,
        string ipnUrl,
        string orderId,
        string orderInfo,
        string partnerCode,
        string redirectUrl,
        string requestId,
        string requestType)
    {
        var raw = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
        return ComputeHmacSha256(secretKey, raw);
    }

    private static string BuildQuerySignature(
        string secretKey,
        string accessKey,
        string orderId,
        string partnerCode,
        string requestId)
    {
        var raw = $"accessKey={accessKey}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}";
        return ComputeHmacSha256(secretKey, raw);
    }

    private string BuildCallbackSignature(MoMoCallbackRequestDto callback)
    {
        return $"accessKey={SanitizeSetting(_settings.AccessKey)}&amount={DecimalToInteger(callback.Amount)}&extraData={callback.ExtraData ?? string.Empty}&message={callback.Message ?? string.Empty}&orderId={callback.OrderId ?? string.Empty}&orderInfo={callback.OrderInfo ?? string.Empty}&orderType={callback.OrderType ?? string.Empty}&partnerCode={callback.PartnerCode ?? string.Empty}&payType={callback.PayType ?? string.Empty}&requestId={callback.RequestId ?? string.Empty}&responseTime={callback.ResponseTime}&resultCode={callback.ResultCode}&transId={callback.TransId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}";
    }

    private static string ComputeHmacSha256(string secretKey, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string SanitizeSetting(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().Normalize(NormalizationForm.FormC);
    }

    private string GetQueryEndpoint()
    {
        var createEndpoint = SanitizeSetting(
            _settings.Endpoint,
            "https://test-payment.momo.vn/v2/gateway/api/create");
        const string createSuffix = "/create";

        return createEndpoint.EndsWith(createSuffix, StringComparison.OrdinalIgnoreCase)
            ? createEndpoint[..^createSuffix.Length] + "/query"
            : "https://test-payment.momo.vn/v2/gateway/api/query";
    }

    private static long DecimalToInteger(decimal amount)
    {
        return Convert.ToInt64(Math.Round(amount, MidpointRounding.AwayFromZero));
    }
}
