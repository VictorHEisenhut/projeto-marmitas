using Marmitas.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Marmitas.Api.Hubs;

public sealed class OrderTrackingHub : Hub
{
    public Task JoinOrderGroup(string orderId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");

    public Task JoinRestaurantGroup(string restaurantId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant:{restaurantId}");

    public Task JoinUserGroup(string userId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
}

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<OrderTrackingHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<OrderTrackingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangedAsync(Guid orderId, Guid customerId, Guid restaurantId, string status)
    {
        var payload = new
        {
            orderId,
            customerId,
            restaurantId,
            status,
            updatedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"order:{orderId}").SendAsync("OrderStatusUpdated", payload);
        await _hubContext.Clients.Group($"user:{customerId}").SendAsync("OrderStatusUpdated", payload);
        await _hubContext.Clients.Group($"restaurant:{restaurantId}").SendAsync("OrderStatusUpdated", payload);
    }

    public Task NotifyDeliveryQueueChangedAsync(Guid deliveryId, Guid orderId, string status)
    {
        return _hubContext.Clients.All.SendAsync("DeliveryUpdated", new
        {
            deliveryId,
            orderId,
            status,
            updatedAt = DateTime.UtcNow
        });
    }

    public Task NotifyDriverLocationAsync(Guid orderId, Guid courierId, double latitude, double longitude)
    {
        return _hubContext.Clients.Group($"order:{orderId}").SendAsync("DriverLocationUpdated", new
        {
            orderId,
            courierId,
            latitude,
            longitude,
            updatedAt = DateTime.UtcNow
        });
    }

    public Task NotifyUserAsync(Guid userId, string title, string message)
    {
        return _hubContext.Clients.Group($"user:{userId}").SendAsync("NotificationReceived", new
        {
            userId,
            title,
            message,
            createdAt = DateTime.UtcNow
        });
    }
}
