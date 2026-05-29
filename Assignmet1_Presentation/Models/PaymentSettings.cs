namespace Assignmet1_Presentation.Models;

public class PaymentSettings
{
    public const string SectionName = "Payment";

    public string BankName { get; set; } = "Vietcombank";

    public string AccountNumber { get; set; } = "0123456789";

    public string AccountHolder { get; set; } = "ADMIN RAG EDU";

    public string TransferContentHint { get; set; } = "RAG <username>";
}
