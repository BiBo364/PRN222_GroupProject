namespace Assignment1_Service.Models;

public class SubscriptionQuotaSettings
{
    public const string SectionName = "SubscriptionQuota";

    public int FreeQuestionLimit { get; set; } = 5;

    public int FreeQuotaWindowHours { get; set; } = 24;
}
