namespace Marmitas.Application.Restaurants.Queries;

public class CategoryDto
{
    public Guid Id { get; set; }
    public Guid? RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class CreateCategoryRequest
{
    public Guid? RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class ProductAddonDto
{
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public int MaxQuantity { get; set; } = 1;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public bool AvailableInPlans { get; set; } = true;
    public bool AllowsCutlery { get; set; }
    public bool CutleryHasExtraCost { get; set; }
    public decimal CutleryPrice { get; set; }
    public List<ProductAddonDto> Addons { get; set; } = new();
}

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool AvailableInPlans { get; set; } = true;
    public bool AllowsCutlery { get; set; }
    public bool CutleryHasExtraCost { get; set; }
    public decimal CutleryPrice { get; set; }
    public List<ProductAddonDto> Addons { get; set; } = new();
}

public class UpdateProductAvailabilityRequest
{
    public bool IsActive { get; set; }
}

public class ProductOptionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}

public class SaveProductOptionRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}
