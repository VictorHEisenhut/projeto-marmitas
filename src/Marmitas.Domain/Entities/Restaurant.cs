using Marmitas.Domain.Enums;

namespace Marmitas.Domain.Entities;

public class Restaurant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public RestaurantStatus Status { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime? DeletedAt { get; set; }

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
    public decimal DeliveryFee { get; set; } = 0m;
    public double DeliveryRadiusKm { get; set; } = 12.0;
    public string? BankName { get; set; }
    public string? BankAgency { get; set; }
    public string? BankAccount { get; set; }
    public string? PixKey { get; set; }

    public User Owner { get; set; } = null!;

    public List<ProductCategory> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();

    private Restaurant() { }

    public Restaurant(string name, string description, string document, Guid ownerId)
    {
        Name = name.Trim();
        Description = description.Trim();
        Document = document.Trim();
        OwnerId = ownerId;
        Status = RestaurantStatus.PendingApproval;
    }

    public void Approve() => Status = RestaurantStatus.Approved;
    public void Reject() => Status = RestaurantStatus.Rejected;
}
