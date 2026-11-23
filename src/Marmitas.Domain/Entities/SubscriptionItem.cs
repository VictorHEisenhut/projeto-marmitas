namespace Marmitas.Domain.Entities;

public class SubscriptionItem : Entity
{
    public Guid SubscriptionId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int DayOfWeek { get; set; } = 1;
}
