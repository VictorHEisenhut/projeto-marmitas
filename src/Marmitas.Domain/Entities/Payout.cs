namespace Marmitas.Domain.Entities;

public class Payout : Entity
{
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = "Restaurant";
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "Available";
    public DateTime? ProcessedAt { get; set; }
}

