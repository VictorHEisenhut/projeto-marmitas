using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/assinaturas")]
[ApiController]
[Authorize(Roles = "customer,admin,super_admin")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ICreateOrderService _orders;

    public SubscriptionsController(ISubscriptionRepository subscriptions, ICreateOrderService orders)
    {
        _subscriptions = subscriptions;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var userId = GetUserId();
        var list = await _subscriptions.GetSubscriptionsByUserAsync(userId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        return Ok(sub);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] CreateSubscriptionRequest request)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(request.PlanId);
        if (plan == null || !plan.IsActive)
            throw new Exception("Plano informado não encontrado.");

        var subscription = new Subscription
        {
            UserId = GetUserId(),
            PlanId = plan.Id,
            PlanName = plan.Name,
            MonthlyPrice = plan.MonthlyPrice,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1)
        };

        await _subscriptions.AddSubscriptionAsync(subscription);
        await _subscriptions.SaveChangesAsync();

        return StatusCode(201, subscription);
    }

    [HttpPatch("{id}/pausar")]
    public async Task<IActionResult> Pause(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Status = "Paused";
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/reativar")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Status = "Active";
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/renovar")]
    public async Task<IActionResult> Renew(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Status = "Active";
        sub.NextBillingDate = DateTime.UtcNow.AddMonths(1);
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/cancelar")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Status = "Cancelled";
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpGet("{id}/pedidos")]
    public async Task<IActionResult> GetSubscriptionOrders(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        var userOrders = await _orders.GetMyOrdersAsync(sub.UserId);
        return Ok(new { subscriptionId = sub.Id, orders = userOrders });
    }

    [HttpGet("{id}/pagamentos")]
    public async Task<IActionResult> GetSubscriptionPayments(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        return Ok(new[]
        {
            new
            {
                subscriptionId = sub.Id,
                amount = sub.MonthlyPrice,
                status = sub.Status == "Cancelled" ? "Cancelled" : "Paid",
                paidAt = sub.StartDate
            }
        });
    }

    private async Task<Subscription> GetRequiredAsync(Guid id)
    {
        var sub = await _subscriptions.GetSubscriptionByIdAsync(id);
        if (sub == null)
            throw new Exception("Assinatura não encontrada.");
        return sub;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public class CreateSubscriptionRequest
{
    public Guid PlanId { get; set; }
}
