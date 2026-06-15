namespace Assignment1_Service.Models;

public class MoMoPaymentSettings
{
    public const string SectionName = "MoMo";

    public string PartnerCode { get; set; } = "MOMO";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    public string IpnUrl { get; set; } = string.Empty;

    public string RedirectUrl { get; set; } = string.Empty;

    public string RequestType { get; set; } = "payWithMethod";

    public string PartnerName { get; set; } = "RAG EDU";

    public string StoreId { get; set; } = "RagEduStore";

    public string OrderGroupId { get; set; } = string.Empty;

    public bool AutoCapture { get; set; } = true;

    public string Lang { get; set; } = "vi";

    public int PendingExpirationMinutes { get; set; } = 30;
}
