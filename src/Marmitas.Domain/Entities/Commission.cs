namespace Marmitas.Domain.Entities;

public class Commission : Entity
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal RatePercentage { get; set; } = 10m;
    public decimal CommissionAmount { get; set; }
}
