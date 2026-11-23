using Marmitas.Domain.Enums;
using Marmitas.Domain.Errors;
using Marmitas.Domain.Events;
using Marmitas.Domain.Exceptions;

namespace Marmitas.Domain.Entities;

public class Order : Entity
{
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    public string? DeliveryAddress { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string ConfirmationPin { get; set; } = string.Empty;

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
        ConfirmationPin = Random.Shared.Next(1000, 9999).ToString();
    }

    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        CalculateTotal();
    }

    public void SetDeliveryFee(decimal deliveryFee)
    {
        DeliveryFee = Math.Max(0m, Math.Round(deliveryFee, 2));
        CalculateTotal();
    }

    public void ApplyCoupon(string code, decimal discountPercentage)
    {
        Subtotal = Items.Sum(i => i.TotalPrice);
        CouponCode = code.Trim().ToUpperInvariant();
        DiscountAmount = Math.Round(Subtotal * (discountPercentage / 100m), 2);
        TotalAmount = Math.Max(0m, Subtotal - DiscountAmount) + DeliveryFee;
    }

    public bool CanTransitionTo(OrderStatus nextStatus)
    {
        if (Status == nextStatus) return true;

        return Status switch
        {
            OrderStatus.Created => nextStatus is OrderStatus.PaymentPending or OrderStatus.Paid or OrderStatus.Confirmed
                or OrderStatus.Preparing or OrderStatus.ReadyForPickup or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.PaymentPending => nextStatus is OrderStatus.Paid or OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.Paid => nextStatus is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup
                or OrderStatus.DriverAssigned or OrderStatus.Cancelled or OrderStatus.Refunded,
            OrderStatus.Confirmed => nextStatus is OrderStatus.Preparing or OrderStatus.ReadyForPickup
                or OrderStatus.DriverAssigned or OrderStatus.Cancelled or OrderStatus.Refunded,
            OrderStatus.Preparing => nextStatus is OrderStatus.ReadyForPickup or OrderStatus.DriverAssigned or OrderStatus.Cancelled,
            OrderStatus.ReadyForPickup => nextStatus is OrderStatus.DriverAssigned or OrderStatus.PickedUp
                or OrderStatus.InTransit or OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.DriverAssigned => nextStatus is OrderStatus.PickedUp or OrderStatus.InTransit
                or OrderStatus.Delivered or OrderStatus.ReadyForPickup or OrderStatus.Cancelled,
            OrderStatus.PickedUp => nextStatus is OrderStatus.InTransit or OrderStatus.Delivered,
            OrderStatus.InTransit => nextStatus is OrderStatus.Delivered,
            OrderStatus.Delivered => nextStatus is OrderStatus.Refunded,
            OrderStatus.Cancelled => nextStatus is OrderStatus.Refunded,
            OrderStatus.Refunded => false,
            OrderStatus.Failed => nextStatus is OrderStatus.PaymentPending or OrderStatus.Cancelled,
            _ => false
        };
    }

    public void TransitionTo(OrderStatus nextStatus)
    {
        if (!CanTransitionTo(nextStatus))
        {
            throw new DomainException(
                $"Transição de status inválida de '{Status}' para '{nextStatus}'.",
                ErrorCodes.OrderInvalidStatusTransition,
                ErrorKind.Conflict);
        }

        var previous = Status;
        Status = nextStatus;
        if (previous != nextStatus)
        {
            AddDomainEvent(new OrderStatusChangedDomainEvent(
                Id,
                CustomerId,
                RestaurantId,
                previous.ToString(),
                nextStatus.ToString(),
                DateTime.UtcNow));
        }
    }

    public bool CanBeCancelledByCustomer()
    {
        return Status is OrderStatus.Created or OrderStatus.PaymentPending or OrderStatus.Paid or OrderStatus.Confirmed;
    }

    private void CalculateTotal()
    {
        Subtotal = Items.Sum(i => i.TotalPrice);
        TotalAmount = Math.Max(0m, Subtotal - DiscountAmount) + DeliveryFee;
    }
}
