namespace Marmitas.Domain.Entities;

public class CourierDocument : Entity
{
    public Guid CourierId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = "Verified";
}

