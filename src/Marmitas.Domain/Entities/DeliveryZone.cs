namespace Marmitas.Domain.Entities;

public class DeliveryZone : Entity
{
    public Guid RestaurantId { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = "São Paulo";
    public double RadiusKm { get; set; } = 8.0;
    public decimal DeliveryFee { get; set; } = 6.90m;
    public int EstimatedMinutes { get; set; } = 35;
    public bool IsActive { get; set; } = true;
}
