namespace Marmitas.Domain.Entities;

public class PaymentTransaction : Entity
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string Gateway { get; set; } = "MercadoPago";
    public string ExternalTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Approved";
    public string? RawPayload { get; set; }
}
