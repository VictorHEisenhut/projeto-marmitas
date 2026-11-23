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
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string OpeningHours { get; set; } = "Seg a Sáb • 10:30 às 15:30";
    public decimal DeliveryFee { get; set; }
    public double DeliveryRadiusKm { get; set; } = 12.0;
    public string? BankName { get; set; }
    public string? BankAgency { get; set; }
    public string? BankAccount { get; set; }
    public string? PixKey { get; set; }
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
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? OpeningHours { get; set; }
    public decimal? DeliveryFee { get; set; }
    public double? DeliveryRadiusKm { get; set; }
    public string? BankName { get; set; }
    public string? BankAgency { get; set; }
    public string? BankAccount { get; set; }
    public string? PixKey { get; set; }
}

public class UpdateRestaurantStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class RestaurantHourDto
{
    public int DayOfWeek { get; set; }
    public string OpenTime { get; set; } = "10:30";
    public string CloseTime { get; set; } = "15:30";
    public bool IsOpen { get; set; } = true;
}

public class DeliveryZoneDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = "São Paulo";
    public double RadiusKm { get; set; } = 8.0;
    public decimal DeliveryFee { get; set; } = 6.90m;
    public int EstimatedMinutes { get; set; } = 35;
    public bool IsActive { get; set; } = true;
}

public class SaveDeliveryZoneRequest
{
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = "São Paulo";
    public double RadiusKm { get; set; } = 8.0;
    public decimal DeliveryFee { get; set; } = 6.90m;
    public int EstimatedMinutes { get; set; } = 35;
}
