namespace Marmitas.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyOrderStatusChangedAsync(Guid orderId, Guid customerId, Guid restaurantId, string status);
    Task NotifyDeliveryQueueChangedAsync(Guid deliveryId, Guid orderId, string status);
    Task NotifyDriverLocationAsync(Guid orderId, Guid courierId, double latitude, double longitude);
    Task NotifyUserAsync(Guid userId, string title, string message);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
}

public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
