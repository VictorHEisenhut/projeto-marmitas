using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly ICreateOrderService _orders;

    public DeliveriesController(IDeliveryRepository deliveries, ICreateOrderService orders)
    {
        _deliveries = deliveries;
        _orders = orders;
    }

    [HttpGet("api/entregas")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetDeliveries()
    {
        var allOrders = await _orders.GetAllOrdersAsync();
        var existing = await _deliveries.GetAllDeliveriesAsync();
        var changed = false;

        foreach (var order in allOrders)
        {
            if (!existing.Any(d => d.OrderId == order.Id))
            {
                await _deliveries.AddDeliveryAsync(new Delivery
                {
                    OrderId = order.Id,
                    Status = order.Status
                });
                changed = true;
            }
        }

        if (changed)
            await _deliveries.SaveChangesAsync();

        var updatedList = await _deliveries.GetAllDeliveriesAsync();
        return Ok(updatedList);
    }

    [HttpGet("api/entregas/{id}")]
    [Authorize(Roles = "customer,driver,admin,super_admin")]
    public async Task<IActionResult> GetDelivery(Guid id)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery
            {
                OrderId = order.Id,
                Status = order.Status
            };
            await _deliveries.AddDeliveryAsync(delivery);
            await _deliveries.SaveChangesAsync();
        }

        return Ok(delivery);
    }

    [HttpPatch("api/entregas/{id}/aceitar")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> AcceptDelivery(Guid id)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var courierId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery { OrderId = order.Id };
            await _deliveries.AddDeliveryAsync(delivery);
        }

        delivery.CourierId = courierId;
        delivery.Status = "DriverAssigned";
        await _deliveries.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpPatch("api/entregas/{id}/status")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        if (delivery == null)
        {
            var order = await _orders.GetByIdAsync(id);
            delivery = new Delivery { OrderId = order.Id };
            await _deliveries.AddDeliveryAsync(delivery);
        }

        delivery.Status = request.Status;
        await _deliveries.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpGet("api/entregas/{id}/rastreamento")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> TrackDelivery(Guid id)
    {
        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(id);
        Courier? courier = null;
        if (delivery?.CourierId != null)
            courier = await _deliveries.GetCourierByUserIdAsync(delivery.CourierId.Value);

        return Ok(new
        {
            deliveryId = delivery?.Id ?? id,
            orderId = delivery?.OrderId ?? id,
            status = delivery?.Status ?? "InTransit",
            latitude = courier?.Latitude,
            longitude = courier?.Longitude,
            updatedAt = delivery?.UpdatedAt ?? DateTime.UtcNow
        });
    }

    [HttpPatch("api/entregadores/me/disponibilidade")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> SetAvailability([FromBody] UpdateCourierAvailabilityRequest request)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId))
        {
            var courier = await _deliveries.GetCourierByUserIdAsync(userId);
            if (courier != null)
            {
                courier.IsAvailable = request.IsAvailable;
                await _deliveries.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            isAvailable = request.IsAvailable,
            updatedAt = DateTime.UtcNow
        });
    }
}

public class UpdateDeliveryStatusRequest
{
    public string Status { get; set; } = "Delivered";
}

public class UpdateCourierAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}
