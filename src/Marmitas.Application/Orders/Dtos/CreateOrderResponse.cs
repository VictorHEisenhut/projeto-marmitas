namespace Marmitas.Application.Orders.Dtos;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? DeliveryAddress { get; set; }
    public string ConfirmationPin { get; set; } = string.Empty;
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
    public Guid? SubscriptionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? DeliveryAddress { get; set; }
    public string ConfirmationPin { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ComplaintMessage { get; set; }
    public DateTime? ComplaintCreatedAt { get; set; }
    public string? ComplaintReply { get; set; }
    public DateTime? ComplaintRepliedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByRole { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SubmitOrderComplaintRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ReplyOrderComplaintRequest
{
    public string Reply { get; set; } = string.Empty;
}
