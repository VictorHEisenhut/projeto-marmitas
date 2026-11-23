namespace Marmitas.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Approved = 2,
    Declined = 3,
    Refunded = 4,
    ChargedBack = 5,
    Cancelled = 6
}

public enum SubscriptionStatus
{
    PendingPayment = 0,
    Active = 1,
    Paused = 2,
    GracePeriod = 3,
    Suspended = 4,
    Cancelled = 5,
    Expired = 6
}

public enum DeliveryStatus
{
    ReadyForPickup = 0,
    DriverAssigned = 1,
    PickedUp = 2,
    InTransit = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum CourierStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3
}

public enum PayoutStatus
{
    Available = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}
