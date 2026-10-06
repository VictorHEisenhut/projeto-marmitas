namespace Marmitas.Application.Orders.Dtos;

public class CreateOrderRequest
{
    public Guid RestaurantId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

