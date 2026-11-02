namespace Marmitas.Domain.Entities;

public class Delivery : Entity
{
    public Guid OrderId { get; set; }
    public Guid? CourierId { get; set; }
    public string Status { get; set; } = "ReadyForPickup";
}

