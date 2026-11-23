namespace Marmitas.Domain.Entities;

public class Refund : Entity
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "Estorno solicitado";
    public string Status { get; set; } = "Processed";
    public string? ExternalRefundId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
