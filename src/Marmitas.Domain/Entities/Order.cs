using Marmitas.Domain.Enums;

namespace Marmitas.Domain.Entities;

public class Order : Entity
{
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

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

    private void CalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.TotalPrice);
    }
}

