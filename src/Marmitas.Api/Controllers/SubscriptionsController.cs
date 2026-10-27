using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/assinaturas")]
[ApiController]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private static readonly List<SubscriptionDto> Subscriptions = new();

    [HttpGet]
    public IActionResult GetMySubscriptions()
    {
        var userId = GetUserId();
        return Ok(Subscriptions.Where(s => s.UserId == userId));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var sub = Subscriptions.FirstOrDefault(s => s.Id == id);
        if (sub == null)
            throw new Exception("Assinatura não encontrada.");

        return Ok(sub);
    }

    [HttpPost]
    public IActionResult Subscribe([FromBody] CreateSubscriptionRequest request)
    {
        var plan = PlansController.Plans.FirstOrDefault(p => p.Id == request.PlanId);
        if (plan == null)
            throw new Exception("Plano informado não encontrado.");

        var subscription = new SubscriptionDto
        {
            Id = Guid.NewGuid(),
            UserId = GetUserId(),
            PlanId = plan.Id,
            PlanName = plan.Name,
            MonthlyPrice = plan.MonthlyPrice,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1)
        };

        Subscriptions.Add(subscription);
        return StatusCode(201, subscription);
    }

    [HttpPatch("{id}/pausar")]
    public IActionResult Pause(Guid id)
    {
        var sub = GetRequired(id);
        sub.Status = "Paused";
        return Ok(sub);
    }

    [HttpPatch("{id}/reativar")]
    public IActionResult Reactivate(Guid id)
    {
        var sub = GetRequired(id);
        sub.Status = "Active";
        return Ok(sub);
    }

    [HttpPatch("{id}/renovar")]
    public IActionResult Renew(Guid id)
    {
        var sub = GetRequired(id);
        sub.Status = "Active";
        sub.NextBillingDate = DateTime.UtcNow.AddMonths(1);
        return Ok(sub);
    }

    [HttpPatch("{id}/cancelar")]
    public IActionResult Cancel(Guid id)
    {
        var sub = GetRequired(id);
        sub.Status = "Cancelled";
        return Ok(sub);
    }

    [HttpGet("{id}/pedidos")]
    public IActionResult GetSubscriptionOrders(Guid id)
    {
        var sub = GetRequired(id);
        return Ok(new { subscriptionId = sub.Id, orders = Array.Empty<object>() });
    }

    [HttpGet("{id}/pagamentos")]
    public IActionResult GetSubscriptionPayments(Guid id)
    {
        var sub = GetRequired(id);
        return Ok(new[]
        {
            new
            {
                subscriptionId = sub.Id,
                amount = sub.MonthlyPrice,
                status = "Paid",
                paidAt = sub.StartDate
            }
        });
    }

    private static SubscriptionDto GetRequired(Guid id)
    {
        var sub = Subscriptions.FirstOrDefault(s => s.Id == id);
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

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime NextBillingDate { get; set; }
}

public class CreateSubscriptionRequest
{
    public Guid PlanId { get; set; }
}
