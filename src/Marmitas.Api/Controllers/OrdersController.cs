using System.Security.Claims;
using System.Text.Json;
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
    private readonly IRestaurantRepository _restaurants;
    private readonly IPaymentRepository _payments;

    public OrdersController(
        ICreateOrderService createOrderService,
        IDeliveryRepository deliveries,
        IRestaurantRepository restaurants,
        IPaymentRepository payments)
    {
        _createOrderService = createOrderService;
        _deliveries = deliveries;
        _restaurants = restaurants;
        _payments = payments;
    }

    [HttpGet]
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var userId))
            return Unauthorized();

        List<OrderDto> list;
        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
            {
                var authorized = await _restaurants.IsUserAuthorizedAsync(restaurantId.Value, userId);
                if (!authorized)
                    return Forbid();
            }

            list = await _createOrderService.GetByRestaurantIdAsync(restaurantId.Value);
        }
        else
        {
            list = await _createOrderService.GetMyOrdersAsync(userId);
        }

        Response.Headers["X-Total-Count"] = list.Count.ToString();
        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            list = list.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
        }

        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _createOrderService.GetByIdAsync(id);
        if (!await CanAccessOrderAsync(order))
            return Forbid();

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
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var existing = await _createOrderService.GetByIdAsync(id);
        if (!await CanAccessOrderAsync(existing))
            return Forbid();

        var isStaffOrAdmin = User.IsInRole("admin") ||
                             User.IsInRole("super_admin") ||
                             User.IsInRole("restaurant_owner") ||
                             User.IsInRole("restaurant_staff");

        var order = await _createOrderService.CancelAsync(id, isStaffOrAdmin);
        return Ok(order);
    }

    [HttpGet("{id}/itens")]
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetItems(Guid id)
    {
        var existing = await _createOrderService.GetByIdAsync(id);
        if (!await CanAccessOrderAsync(existing))
            return Forbid();

        var items = await _createOrderService.GetItemsAsync(id);
        return Ok(items);
    }

    [HttpGet("{id}/historico-status")]
    [Authorize(Roles = "customer,restaurant_owner,restaurant_staff,driver,admin,super_admin")]
    public async Task<IActionResult> GetStatusHistory(Guid id)
    {
        var order = await _createOrderService.GetByIdAsync(id);
        var history = await _createOrderService.GetStatusHistoryAsync(id);
        return Ok(new
        {
            orderId = order.Id,
            currentStatus = order.Status,
            createdAt = order.CreatedAt,
            updatedAt = order.UpdatedAt,
            history
        });
    }

    [HttpPost("{id}/historico-status")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff,driver")]
    public async Task<IActionResult> AddStatusHistory(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        Guid.TryParse(subClaim, out var actorUserId);

        var existing = await _createOrderService.GetByIdAsync(id);
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin") && !User.IsInRole("driver"))
        {
            var authorized = await _restaurants.IsUserAuthorizedAsync(existing.RestaurantId, actorUserId);
            if (!authorized)
                return Forbid();
        }

        var isAdmin = User.IsInRole("admin") || User.IsInRole("super_admin");
        if (string.Equals(request.Status, "Delivered", StringComparison.OrdinalIgnoreCase) && !isAdmin)
        {
            throw new Exception("A confirmação de entrega exige o código PIN de 4 dígitos do cliente pelo painel do entregador (ou liberação excepcional pelo Administrador).");
        }

        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "staff";
        var order = await _createOrderService.UpdateStatusAsync(id, request.Status, actorUserId, actorRole, request.Notes);

        var delivery = await _deliveries.GetDeliveryByIdOrOrderAsync(order.Id);
        if (delivery == null)
        {
            delivery = new Delivery
            {
                OrderId = order.Id,
                Status = order.Status,
                ConfirmationPin = order.ConfirmationPin,
                DeliveryFee = order.DeliveryFee > 0 ? order.DeliveryFee : 8.50m
            };
            await _deliveries.AddDeliveryAsync(delivery);
        }
        else
        {
            delivery.Status = order.Status;
            if (string.IsNullOrWhiteSpace(delivery.ConfirmationPin))
                delivery.ConfirmationPin = order.ConfirmationPin;
        }

        await _deliveries.SaveChangesAsync();
        return Ok(order);
    }

    [HttpPost("{id}/reclamacao")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> SubmitComplaint(Guid id, [FromBody] SubmitOrderComplaintRequest request)
    {
        var existing = await _createOrderService.GetByIdAsync(id);
        if (!IsOwnerOrAdmin(existing.CustomerId))
            return Forbid();

        var order = await _createOrderService.SubmitComplaintAsync(id, request.Message);
        return Ok(order);
    }

    [HttpPost("{id}/reclamacao/resposta")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> ReplyComplaint(Guid id, [FromBody] ReplyOrderComplaintRequest request)
    {
        var existing = await _createOrderService.GetByIdAsync(id);
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(subClaim, out var userId) ||
                !await _restaurants.IsUserAuthorizedAsync(existing.RestaurantId, userId))
            {
                return Forbid();
            }
        }

        var order = await _createOrderService.ReplyComplaintAsync(id, request.Reply);
        return Ok(order);
    }

    private async Task<bool> CanAccessOrderAsync(OrderDto order)
    {
        if (IsOwnerOrAdmin(order.CustomerId))
            return true;

        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var currentUserId))
            return false;

        return await _restaurants.IsUserAuthorizedAsync(order.RestaurantId, currentUserId);
    }

    private bool IsOwnerOrAdmin(Guid customerId)
    {
        if (User.IsInRole("admin") || User.IsInRole("super_admin"))
            return true;

        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subClaim, out var currentUserId) && currentUserId == customerId;
    }
}
