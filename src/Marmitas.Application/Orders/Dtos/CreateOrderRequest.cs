namespace Marmitas.Application.Orders.Dtos;

public class CreateOrderRequest
{
    public Guid RestaurantId { get; set; }
    public bool IsPlanRedemption { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public bool WantsCutlery { get; set; }
    public List<OrderItemAddonRequest> SelectedAddons { get; set; } = new();
}

public class OrderItemAddonRequest
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

