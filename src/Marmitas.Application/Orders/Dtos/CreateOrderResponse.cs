namespace Marmitas.Application.Orders.Dtos;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

