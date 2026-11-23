namespace Marmitas.Domain.Entities;

public class RestaurantUser : Entity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "restaurant_staff";
    public bool IsActive { get; set; } = true;
}
