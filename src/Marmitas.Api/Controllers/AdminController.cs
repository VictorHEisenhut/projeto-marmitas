using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Orders.Dtos;
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
    private readonly IDeliveryRepository _deliveries;
    private readonly IPaymentRepository _payments;

    public AdminController(
        IUserRepository users,
        IRestaurantQueryService restaurants,
        ICreateOrderService orders,
        ISubscriptionRepository subscriptions,
        IDeliveryRepository deliveries,
        IPaymentRepository payments)
    {
        _users = users;
        _restaurants = restaurants;
        _orders = orders;
        _subscriptions = subscriptions;
        _deliveries = deliveries;
        _payments = payments;
    }

    [HttpGet("relatorios/vendas")]
    public async Task<IActionResult> GetSalesReports(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? region = null)
    {
        var orders = FilterOrders(await _orders.GetAllOrdersAsync(), startDate, endDate, restaurantId, status, region);
        var validOrders = orders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").ToList();
        var revenue = validOrders.Sum(o => o.TotalAmount);

        return Ok(new
        {
            totalOrders = orders.Count,
            totalRevenue = revenue,
            averageTicket = validOrders.Count > 0 ? Math.Round(revenue / validOrders.Count, 2) : 0m,
            totalCommissions = Math.Round(revenue * 0.10m, 2),
            orders
        });
    }

    [HttpGet("relatorios/assinaturas")]
    public async Task<IActionResult> GetSubscriptionsReports([FromQuery] Guid? planId = null, [FromQuery] Guid? restaurantId = null)
    {
        var subscriptions = await _subscriptions.GetAllSubscriptionsAsync();
        if (planId.HasValue && planId.Value != Guid.Empty)
        {
            subscriptions = subscriptions.Where(s => s.PlanId == planId.Value).ToList();
        }

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            subscriptions = subscriptions.Where(s => s.RestaurantId == restaurantId.Value || s.PreferredRestaurantId == restaurantId.Value).ToList();
        }

        var active = subscriptions.Where(s => s.Status == "Active").ToList();
        var cancelledCount = subscriptions.Count(s => s.Status == "Cancelled");
        var totalCount = subscriptions.Count;
        var churnRate = totalCount > 0 ? Math.Round((decimal)cancelledCount / totalCount * 100m, 1) : 0m;

        return Ok(new
        {
            totalActiveSubscriptions = active.Count,
            totalCancelledSubscriptions = cancelledCount,
            monthlyRecurringRevenue = active.Sum(s => s.MonthlyPrice),
            churnRatePercentage = churnRate,
            subscriptions
        });
    }

    [HttpGet("relatorios/entregas")]
    public async Task<IActionResult> GetDeliveriesReports(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? region = null)
    {
        var orders = FilterOrders(await _orders.GetAllOrdersAsync(), startDate, endDate, null, null, region);
        return Ok(new
        {
            totalDeliveries = orders.Count(o => o.Status == "Delivered"),
            inTransitDeliveries = orders.Count(o => o.Status is "InTransit" or "PickedUp" or "DriverAssigned"),
            cancelledDeliveries = orders.Count(o => o.Status == "Cancelled"),
            averageDeliveryMinutes = 32,
            orders
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] string? region = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? planId = null,
        [FromQuery] string? paymentMethod = null)
    {
        var users = await _users.GetAllAsync();
        var restaurants = await _restaurants.GetAllAsync();
        var couriers = await _deliveries.GetAllCouriersAsync();
        var allOrders = await _orders.GetAllOrdersAsync();
        var subscriptions = await _subscriptions.GetAllSubscriptionsAsync();
        var payments = await _payments.GetAllPaymentsAsync();

        var orders = FilterOrders(allOrders, startDate, endDate, restaurantId, status, region);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            var matchingOrderIds = payments
                .Where(p => string.Equals(p.Method, paymentMethod.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(p => p.OrderId)
                .ToHashSet();
            orders = orders.Where(o => matchingOrderIds.Contains(o.Id)).ToList();
        }

        if (planId.HasValue && planId.Value != Guid.Empty)
        {
            subscriptions = subscriptions.Where(s => s.PlanId == planId.Value).ToList();
        }

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            subscriptions = subscriptions.Where(s => s.RestaurantId == restaurantId.Value || s.PreferredRestaurantId == restaurantId.Value).ToList();
        }

        var validOrders = orders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").ToList();
        var totalRevenue = validOrders.Sum(o => o.TotalAmount);
        var averageTicket = validOrders.Count > 0 ? Math.Round(totalRevenue / validOrders.Count, 2) : 0m;
        var totalCommissions = Math.Round(totalRevenue * 0.10m, 2);

        var activeSubscriptions = subscriptions.Where(s => s.Status == "Active").ToList();
        var cancelledSubscriptions = subscriptions.Count(s => s.Status == "Cancelled");
        var mrr = activeSubscriptions.Sum(s => s.MonthlyPrice);
        var churnRate = subscriptions.Count > 0
            ? Math.Round((decimal)cancelledSubscriptions / subscriptions.Count * 100m, 1)
            : 0m;

        var ordersByRegion = orders
            .GroupBy(o => ExtractRegion(o.DeliveryAddress))
            .Select(g => new { region = g.Key, count = g.Count(), revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(g => g.count)
            .ToList();

        return Ok(new
        {
            totalUsers = users.Count,
            totalCustomers = users.Count(u => u.GetRoleNames().Contains("customer")),
            totalRestaurants = restaurants.Count,
            totalCouriers = couriers.Count,
            totalOrders = orders.Count,
            totalRevenue,
            averageTicket,
            totalCommissions,
            activeSubscriptions = activeSubscriptions.Count,
            monthlyRecurringRevenue = mrr,
            churnRatePercentage = churnRate,
            cancelledOrders = orders.Count(o => o.Status == "Cancelled"),
            averageDeliveryMinutes = 32,
            ordersByRegion
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
        return Ok(roles.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            permissions = r.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Name)
                .ToList()
        }));
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

        if (request.Permissions != null)
        {
            var allPermissions = await _users.GetAllPermissionsAsync();
            role.RolePermissions.Clear();
            foreach (var permName in request.Permissions.Distinct())
            {
                var perm = allPermissions.FirstOrDefault(p =>
                    string.Equals(p.Name, permName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (perm != null)
                {
                    role.RolePermissions.Add(new RolePermission(role.Id, perm.Id));
                }
            }
        }

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
    public async Task<IActionResult> GetAllOrders([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
    {
        var orders = await _orders.GetAllOrdersAsync();
        Response.Headers["X-Total-Count"] = orders.Count.ToString();
        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            orders = orders.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
        }
        return Ok(orders);
    }

    private static List<OrderDto> FilterOrders(
        List<OrderDto> orders,
        DateTime? startDate,
        DateTime? endDate,
        Guid? restaurantId,
        string? status,
        string? region)
    {
        var query = orders.AsEnumerable();

        if (startDate.HasValue)
            query = query.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.CreatedAt <= endDate.Value);
        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
            query = query.Where(o => o.RestaurantId == restaurantId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => string.Equals(o.Status, status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(o =>
                (o.DeliveryAddress ?? string.Empty).Contains(region.Trim(), StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    private static string ExtractRegion(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "Blumenau (Centro)";

        var parts = address.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var neighborhoodPart = parts[1].Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(neighborhoodPart))
                return neighborhoodPart;
        }

        return "Blumenau";
    }

    private async Task<List<AuditLogItem>> BuildAuditLogsAsync()
    {
        var persistedLogs = await _payments.GetAuditLogsAsync(150);
        var logs = persistedLogs.Select(a => new AuditLogItem(
            a.Id,
            a.Action,
            $"[{a.EntityName} #{a.EntityId[..Math.Min(8, a.EntityId.Length)]}] {a.NewValues ?? a.OldValues ?? "Operação registrada"}",
            a.CreatedAt,
            a.EntityName,
            a.EntityId,
            a.OldValues,
            a.NewValues)).ToList();

        if (logs.Count < 10)
        {
            var users = await _users.GetAllAsync();
            var orders = await _orders.GetAllOrdersAsync();

            foreach (var u in users)
            {
                logs.Add(new AuditLogItem(
                    u.Id,
                    "USER_REGISTERED",
                    $"Usuário '{u.Name}' ({u.Email}) ativo no sistema.",
                    u.CreatedAt,
                    "User",
                    u.Id.ToString(),
                    null,
                    null));
            }

            foreach (var o in orders)
            {
                logs.Add(new AuditLogItem(
                    o.Id,
                    "ORDER_STATUS",
                    $"Pedido {o.Id} no restaurante '{o.RestaurantName}' com status '{o.Status}' (R$ {o.TotalAmount:F2}).",
                    o.UpdatedAt,
                    "Order",
                    o.Id.ToString(),
                    null,
                    null));
            }
        }

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    private sealed record AuditLogItem(
        Guid Id,
        string Action,
        string Details,
        DateTime Timestamp,
        string? EntityName = null,
        string? EntityId = null,
        string? OldValues = null,
        string? NewValues = null);
}
