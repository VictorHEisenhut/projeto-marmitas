namespace Marmitas.Domain.Entities;

public class DriverLocation : Entity
{
    public Guid CourierId { get; set; }
    public Guid? DeliveryId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
