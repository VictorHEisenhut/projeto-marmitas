namespace Marmitas.Domain.Entities;

public class CourierVehicle : Entity
{
    public Guid CourierId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}

