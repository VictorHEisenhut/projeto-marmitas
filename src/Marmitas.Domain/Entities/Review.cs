namespace Marmitas.Domain.Entities;

public class Review : Entity
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

