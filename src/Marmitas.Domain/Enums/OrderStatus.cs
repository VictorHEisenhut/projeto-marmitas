namespace Marmitas.Domain.Enums;

public enum OrderStatus
{
    Created = 0,
    PaymentPending = 1,
    Paid = 2,
    Confirmed = 3,
    Preparing = 4,
    ReadyForPickup = 5,
    DriverAssigned = 6,
    PickedUp = 7,
    InTransit = 8,
    Delivered = 9,
    Cancelled = 10,
    Refunded = 11,
    Failed = 12
}

