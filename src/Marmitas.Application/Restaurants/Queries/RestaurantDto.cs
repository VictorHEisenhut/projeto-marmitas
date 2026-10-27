namespace Marmitas.Application.Restaurants.Queries;

public class RestaurantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
}

public class CreateRestaurantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateRestaurantStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
