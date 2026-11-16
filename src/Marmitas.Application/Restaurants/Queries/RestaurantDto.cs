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

    public string AddressStreet { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string? AddressComplement { get; set; }
    public string AddressNeighborhood { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressState { get; set; } = string.Empty;
    public string AddressZipCode { get; set; } = string.Empty;
}

public class CreateRestaurantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? OwnerId { get; set; }

    public string AddressStreet { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string? AddressComplement { get; set; }
    public string AddressNeighborhood { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressState { get; set; } = string.Empty;
    public string AddressZipCode { get; set; } = string.Empty;
}

public class UpdateRestaurantStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
