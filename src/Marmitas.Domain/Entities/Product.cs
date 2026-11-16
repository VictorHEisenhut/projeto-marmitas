namespace Marmitas.Domain.Entities;

public class Product : Entity
{
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public bool AvailableInPlans { get; set; } = true;
    public bool AllowsCutlery { get; set; }
    public bool CutleryHasExtraCost { get; set; }
    public decimal CutleryPrice { get; set; }
    public string AddonsJson { get; set; } = "[]";
    public DateTime? DeletedAt { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
    public ProductCategory Category { get; set; } = null!;

    private Product() { }

    public Product(Guid restaurantId, Guid categoryId, string name, string description, decimal price, string? imageUrl)
    {
        RestaurantId = restaurantId;
        CategoryId = categoryId;
        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        IsActive = true;
        ImageUrl = imageUrl;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

