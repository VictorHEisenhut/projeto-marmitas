using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/pedidos")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ICreateOrderService _createOrderService;
    private readonly IDeliveryRepository _deliveries;

    public OrdersController(ICreateOrderService createOrderService, IDeliveryRepository deliveries)
    {
        _createOrderService = createOrderService;
        _deliveries = deliveries;
    }

    [HttpGet]
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetMyOrders([FromQuery] Guid? restaurantId = null)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var customerId))
            return Unauthorized();

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            var allOrders = await _createOrderService.GetAllOrdersAsync();
            var restaurantOrders = allOrders.Where(o => o.RestaurantId == restaurantId.Value).ToList();
            return Ok(restaurantOrders);
        }

        var orders = await _createOrderService.GetMyOrdersAsync(customerId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _createOrderService.GetByIdAsync(id);
        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var customerId))
            return Unauthorized();

        var result = await _createOrderService.ExecuteAsync(customerId, request);
        return StatusCode(201, result);
    }

    [HttpPatch("{id}/cancelar")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var order = await _createOrderService.CancelAsync(id);
        return Ok(order);
    }

    [HttpGet("{id}/itens")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetItems(Guid id)
    {
        var items = await _createOrderService.GetItemsAsync(id);
        return Ok(items);
    }

    [HttpGet("{id}/historico-status")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetStatusHistory(Guid id)
    {
        var order = await _createOrderService.GetByIdAsync(id);
        return Ok(new
        {
            orderId = order.Id,
            currentStatus = order.Status,
            createdAt = order.CreatedAt,
            updatedAt = order.UpdatedAt
        });
    }

    [HttpPost("{id}/historico-status")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff,driver")]
    public async Task<IActionResult> AddStatusHistory(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _createOrderService.UpdateStatusAsync(id, request.Status);

        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(order.Id);
        if (delivery == null)
        {
            delivery = new Delivery
            {
                OrderId = order.Id,
                Status = order.Status
            };
            await _deliveries.AddDeliveryAsync(delivery);
        }
        else
        {
            delivery.Status = order.Status;
        }

        await _deliveries.SaveChangesAsync();
        return Ok(order);
    }

    [HttpPost("{id}/reclamacao")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> SubmitComplaint(Guid id, [FromBody] SubmitOrderComplaintRequest request)
    {
        var order = await _createOrderService.SubmitComplaintAsync(id, request.Message);
        return Ok(order);
    }

    [HttpPost("{id}/reclamacao/resposta")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> ReplyComplaint(Guid id, [FromBody] ReplyOrderComplaintRequest request)
    {
        var order = await _createOrderService.ReplyComplaintAsync(id, request.Reply);
        return Ok(order);
    }
}
