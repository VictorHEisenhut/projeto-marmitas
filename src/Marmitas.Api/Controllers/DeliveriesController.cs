using System.Security.Claims;
using Marmitas.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
[Authorize]
public class DeliveriesController : ControllerBase
{
    public static readonly List<DeliveryDto> Deliveries = new();
    private readonly ICreateOrderService _orders;

    public DeliveriesController(ICreateOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("api/entregas")]
    public async Task<IActionResult> GetDeliveries()
    {
        var allOrders = await _orders.GetAllOrdersAsync();
        foreach (var order in allOrders)
        {
            if (!Deliveries.Any(d => d.OrderId == order.Id))
            {
                Deliveries.Add(new DeliveryDto
                {
                    Id = order.Id,
                    OrderId = order.Id,
                    Status = order.Status,
                    UpdatedAt = order.UpdatedAt
                });
            }
        }

        return Ok(Deliveries);
    }

    [HttpGet("api/entregas/{id}")]
    public IActionResult GetDelivery(Guid id)
    {
        var delivery = Deliveries.FirstOrDefault(d => d.Id == id || d.OrderId == id);
        if (delivery == null)
        {
            delivery = new DeliveryDto
            {
                Id = id,
                OrderId = id,
                Status = "ReadyForPickup",
                UpdatedAt = DateTime.UtcNow
            };
            Deliveries.Add(delivery);
        }

        return Ok(delivery);
    }

    [HttpPatch("api/entregas/{id}/aceitar")]
    public IActionResult AcceptDelivery(Guid id)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var courierId = Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;

        var delivery = Deliveries.FirstOrDefault(d => d.Id == id || d.OrderId == id);
        if (delivery == null)
        {
            delivery = new DeliveryDto { Id = id, OrderId = id };
            Deliveries.Add(delivery);
        }

        delivery.CourierId = courierId;
        delivery.Status = "DriverAssigned";
        delivery.UpdatedAt = DateTime.UtcNow;

        return Ok(delivery);
    }

    [HttpPatch("api/entregas/{id}/status")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var delivery = Deliveries.FirstOrDefault(d => d.Id == id || d.OrderId == id);
        if (delivery == null)
        {
            delivery = new DeliveryDto { Id = id, OrderId = id };
            Deliveries.Add(delivery);
        }

        delivery.Status = request.Status;
        delivery.UpdatedAt = DateTime.UtcNow;

        return Ok(delivery);
    }

    [HttpGet("api/entregas/{id}/rastreamento")]
    public IActionResult TrackDelivery(Guid id)
    {
        var delivery = Deliveries.FirstOrDefault(d => d.Id == id || d.OrderId == id);
        return Ok(new
        {
            deliveryId = id,
            status = delivery?.Status ?? "InTransit",
            updatedAt = DateTime.UtcNow
        });
    }

    [HttpPatch("api/entregadores/me/disponibilidade")]
    public IActionResult SetAvailability([FromBody] UpdateCourierAvailabilityRequest request)
    {
        return Ok(new
        {
            isAvailable = request.IsAvailable,
            updatedAt = DateTime.UtcNow
        });
    }
}

public class DeliveryDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? CourierId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public string Status { get; set; } = "Delivered";
}

public class UpdateCourierAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}
