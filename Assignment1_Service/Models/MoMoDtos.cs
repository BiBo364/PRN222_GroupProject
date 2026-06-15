using System.Text.Json.Serialization;

namespace Assignment1_Service.Models;

public class MoMoCheckoutResultDto
{
    public bool Success { get; set; }

    public int ResultCode { get; set; }

    public string? Message { get; set; }

    public string? PayUrl { get; set; }

    public string? OrderId { get; set; }

    public string? RequestId { get; set; }
}

public class MoMoCreateRequestDto
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("partnerName")]
    public string PartnerName { get; set; } = string.Empty;

    [JsonPropertyName("storeId")]
    public string StoreId { get; set; } = string.Empty;

    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderInfo")]
    public string OrderInfo { get; set; } = string.Empty;

    [JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("ipnUrl")]
    public string IpnUrl { get; set; } = string.Empty;

    [JsonPropertyName("extraData")]
    public string ExtraData { get; set; } = string.Empty;

    [JsonPropertyName("requestType")]
    public string RequestType { get; set; } = string.Empty;

    [JsonPropertyName("orderGroupId")]
    public string OrderGroupId { get; set; } = string.Empty;

    [JsonPropertyName("autoCapture")]
    public bool AutoCapture { get; set; } = true;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

public class MoMoCallbackRequestDto
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("orderInfo")]
    public string? OrderInfo { get; set; }

    [JsonPropertyName("orderType")]
    public string? OrderType { get; set; }

    [JsonPropertyName("transId")]
    public string? TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payType")]
    public string? PayType { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class MoMoCreateResponseDto
{
    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payUrl")]
    public string? PayUrl { get; set; }
}
