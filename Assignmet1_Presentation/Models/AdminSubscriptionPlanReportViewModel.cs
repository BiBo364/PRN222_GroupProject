namespace Assignmet1_Presentation.Models;

public class AdminSubscriptionPlanReportViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int PurchaseCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal PurchaseSharePercent { get; set; }
}
