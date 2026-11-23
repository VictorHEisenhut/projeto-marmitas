namespace Marmitas.Domain.Entities;

public class Payment : Entity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "PIX";
    public string Status { get; set; } = "Pending";
    public string Gateway { get; set; } = "MercadoPago";
    public string? ExternalTransactionId { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixCopyPaste { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
