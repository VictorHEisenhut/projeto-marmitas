using System.Security.Claims;
using Marmitas.Application.Orders;
using Marmitas.Application.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/pedidos")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ICreateOrderService _createOrderService;

    public OrdersController(ICreateOrderService createOrderService)
    {
        _createOrderService = createOrderService;
    }

    [HttpGet]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetMyOrders()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var customerId))
            return Unauthorized();

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
        return Ok(order);
    }
}
