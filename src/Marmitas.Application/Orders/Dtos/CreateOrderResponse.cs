namespace Marmitas.Application.Orders.Dtos;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ComplaintMessage { get; set; }
    public DateTime? ComplaintCreatedAt { get; set; }
    public string? ComplaintReply { get; set; }
    public DateTime? ComplaintRepliedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SubmitOrderComplaintRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ReplyOrderComplaintRequest
{
    public string Reply { get; set; } = string.Empty;
}
