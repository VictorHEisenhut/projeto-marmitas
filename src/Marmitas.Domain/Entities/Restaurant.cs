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

