namespace Marmitas.Domain.Entities;

public class Review : Entity
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string TargetType { get; set; } = "Customer";
    public string AuthorName { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
}
