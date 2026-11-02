namespace Marmitas.Domain.Entities;

public class Favorite : Entity
{
    public Guid UserId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? ProductId { get; set; }
}

