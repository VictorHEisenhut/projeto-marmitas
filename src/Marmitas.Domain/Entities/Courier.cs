namespace Marmitas.Domain.Entities;

public class Courier : Entity
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved";
    public bool IsAvailable { get; set; } = true;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

