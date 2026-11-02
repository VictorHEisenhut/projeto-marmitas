using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Restaurants.Queries;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "admin,super_admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRestaurantQueryService _restaurants;
    private readonly ICreateOrderService _orders;
    private readonly ISubscriptionRepository _subscriptions;

    public AdminController(
        IUserRepository users,
        IRestaurantQueryService restaurants,
        ICreateOrderService orders,
        ISubscriptionRepository subscriptions)
    {
        _users = users;
        _restaurants = restaurants;
        _orders = orders;
        _subscriptions = subscriptions;
    }

    [HttpGet("relatorios/vendas")]
    public async Task<IActionResult> GetSalesReports()
    {
        var orders = await _orders.GetAllOrdersAsync();
        return Ok(new
        {
            totalOrders = orders.Count,
            totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
            orders
        });
    }

    [HttpGet("relatorios/assinaturas")]
    public async Task<IActionResult> GetSubscriptionsReports()
    {
        var subscriptions = await _subscriptions.GetAllSubscriptionsAsync();
        var active = subscriptions.Where(s => s.Status == "Active").ToList();

        return Ok(new
        {
            totalActiveSubscriptions = active.Count,
            totalCancelledSubscriptions = subscriptions.Count(s => s.Status == "Cancelled"),
            monthlyRecurringRevenue = active.Sum(s => s.MonthlyPrice),
            subscriptions
        });
    }

    [HttpGet("relatorios/entregas")]
    public async Task<IActionResult> GetDeliveriesReports()
    {
        var orders = await _orders.GetAllOrdersAsync();
        return Ok(new
        {
            totalDeliveries = orders.Count(o => o.Status == "Delivered"),
            inTransitDeliveries = orders.Count(o => o.Status == "InTransit"),
            cancelledDeliveries = orders.Count(o => o.Status == "Cancelled"),
            orders
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var users = await _users.GetAllAsync();
        var restaurants = await _restaurants.GetAllPublicAsync();
        var orders = await _orders.GetAllOrdersAsync();

        return Ok(new
        {
            totalUsers = users.Count,
            totalRestaurants = restaurants.Count,
            totalOrders = orders.Count,
            totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount)
        });
    }

    [HttpGet("auditoria")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await BuildAuditLogsAsync();
        return Ok(logs);
    }

    [HttpGet("auditoria/{id}")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        var logs = await BuildAuditLogsAsync();
        var log = logs.FirstOrDefault(l => l.Id == id);
        if (log == null)
            throw new Exception("Registro de auditoria não encontrado.");

        return Ok(log);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _users.GetAllRolesAsync();
        return Ok(roles.Select(r => new { r.Id, r.Name, r.Description }));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome da role é obrigatório.");

        var role = new Role(request.Name.Trim().ToLower(), request.Description ?? string.Empty);
        await _users.AddRoleAsync(role);
        await _users.SaveChangesAsync();

        return StatusCode(201, new { role.Id, role.Name, role.Description });
    }

    [HttpPut("roles/{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] CreateRoleRequest request)
    {
        var role = await _users.GetRoleByIdAsync(id);
        if (role == null)
            throw new Exception("Role não encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            role.Name = request.Name.Trim().ToLower();
        role.Description = request.Description ?? string.Empty;

        await _users.SaveChangesAsync();
        return Ok(new { role.Id, role.Name, role.Description });
    }

    [HttpGet("permissoes")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _users.GetAllPermissionsAsync();
        return Ok(permissions.Select(p => new { p.Id, p.Name, p.Description }));
    }

    [HttpGet("pedidos")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orders.GetAllOrdersAsync();
        return Ok(orders);
    }

    private async Task<List<AuditLogItem>> BuildAuditLogsAsync()
    {
        var users = await _users.GetAllAsync();
        var orders = await _orders.GetAllOrdersAsync();

        var logs = new List<AuditLogItem>();

        foreach (var u in users)
        {
            logs.Add(new AuditLogItem(
                u.Id,
                "USER_REGISTERED",
                $"Usuário '{u.Name}' ({u.Email}) ativo no sistema.",
                u.CreatedAt));
        }

        foreach (var o in orders)
        {
            logs.Add(new AuditLogItem(
                o.Id,
                "ORDER_STATUS",
                $"Pedido {o.Id} no restaurante '{o.RestaurantName}' com status '{o.Status}' (R$ {o.TotalAmount:F2}).",
                o.UpdatedAt));
        }

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    private sealed record AuditLogItem(Guid Id, string Action, string Details, DateTime Timestamp);
}
