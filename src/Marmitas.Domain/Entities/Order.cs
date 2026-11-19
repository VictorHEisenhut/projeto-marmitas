using Marmitas.Domain.Enums;

namespace Marmitas.Domain.Entities;

public class Order : Entity
{
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    public string? DeliveryAddress { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public string? ComplaintMessage { get; set; }
    public DateTime? ComplaintCreatedAt { get; set; }
    public string? ComplaintReply { get; set; }
    public DateTime? ComplaintRepliedAt { get; set; }

    public User Customer { get; set; } = null!;
    public Restaurant Restaurant { get; set; } = null!;

    public List<OrderItem> Items { get; set; } = new();

    private Order() { }

    public Order(Guid customerId, Guid restaurantId)
    {
        CustomerId = customerId;
        RestaurantId = restaurantId;
        Status = OrderStatus.Created;
    }

    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        CalculateTotal();
    }

    public void ApplyCoupon(string code, decimal discountPercentage)
    {
        var subtotal = Items.Sum(i => i.TotalPrice);
        CouponCode = code.Trim().ToUpperInvariant();
        DiscountAmount = Math.Round(subtotal * (discountPercentage / 100m), 2);
        TotalAmount = Math.Max(0m, subtotal - DiscountAmount);
    }

    private void CalculateTotal()
    {
        var subtotal = Items.Sum(i => i.TotalPrice);
        TotalAmount = Math.Max(0m, subtotal - DiscountAmount);
    }
}
