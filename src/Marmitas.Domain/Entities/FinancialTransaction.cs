namespace Marmitas.Domain.Entities;

public class FinancialTransaction : Entity
{
    public Guid? OrderId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? PayoutId { get; set; }
    public string TargetType { get; set; } = "Platform";
    public Guid? TargetId { get; set; }
    public string EntryType { get; set; } = "Credit";
    public string Category { get; set; } = "Revenue";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
