namespace Marmitas.Domain.Entities;

public class ProductCategory : Entity
{
    public Guid? RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Restaurant? Restaurant { get; set; }
    public List<Product> Products { get; set; } = new();

    private ProductCategory() { } // EF Core

    public ProductCategory(Guid? restaurantId, string name, int displayOrder)
    {
        RestaurantId = restaurantId;
        Name = name.Trim();
        DisplayOrder = displayOrder;
    }
}
