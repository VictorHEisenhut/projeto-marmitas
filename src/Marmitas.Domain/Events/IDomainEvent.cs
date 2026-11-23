namespace Marmitas.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid RestaurantId,
    decimal TotalAmount,
    DateTime OccurredOn) : IDomainEvent;

public sealed record OrderStatusChangedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid RestaurantId,
    string PreviousStatus,
    string NewStatus,
    DateTime OccurredOn) : IDomainEvent;

public sealed record PaymentStatusChangedDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status,
    DateTime OccurredOn) : IDomainEvent;

public sealed record SubscriptionStatusChangedDomainEvent(
    Guid SubscriptionId,
    Guid UserId,
    string Status,
    DateTime OccurredOn) : IDomainEvent;
