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

    // Endereço obrigatório do restaurante
    public string AddressStreet { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string? AddressComplement { get; set; }
    public string AddressNeighborhood { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressState { get; set; } = string.Empty;
    public string AddressZipCode { get; set; } = string.Empty;

    public User Owner { get; set; } = null!;

    public List<ProductCategory> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();

    private Restaurant() { } // EF Core

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
