namespace Marmitas.Domain.Entities;

public class ProductOption : Entity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public int MaxQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
