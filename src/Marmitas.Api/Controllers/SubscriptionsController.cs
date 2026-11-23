using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Platform.Dtos;
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
    private readonly ICustomerProfileRepository _profiles;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentGatewayFactory _gatewayFactory;

    public SubscriptionsController(
        ISubscriptionRepository subscriptions,
        ICreateOrderService orders,
        ICustomerProfileRepository profiles,
        IPaymentRepository payments,
        IPaymentGatewayFactory gatewayFactory)
    {
        _subscriptions = subscriptions;
        _orders = orders;
        _profiles = profiles;
        _payments = payments;
        _gatewayFactory = gatewayFactory;
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

        var userId = GetUserId();
        var existingSubs = await _subscriptions.GetSubscriptionsByUserAsync(userId);
        if (existingSubs.Any(s => s.Status == "Active" && s.PlanId == plan.Id))
            throw new Exception("Você já possui uma assinatura ativa neste plano.");

        var mealsInCycle = plan.MealsPerMonth > 0 ? plan.MealsPerMonth : plan.MealsPerWeek * 4;
        var gateway = _gatewayFactory.GetGateway();
        var charge = await gateway.ChargeAsync(new PaymentChargeRequest(
            plan.Id,
            plan.MonthlyPrice,
            request.PaymentMethod ?? "PIX",
            gateway.ProviderName,
            request.CardToken));

        var subscription = new Subscription
        {
            UserId = userId,
            PlanId = plan.Id,
            PlanName = plan.Name,
            MonthlyPrice = plan.MonthlyPrice,
            Status = charge.Approved ? "Active" : "GracePeriod",
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            TotalMealsInCycle = mealsInCycle,
            UsedMealsInCycle = 0,
            PreferredRestaurantId = request.PreferredRestaurantId,
            DefaultAddressId = request.DefaultAddressId
        };

        if (!charge.Approved)
        {
            subscription.RegisterPaymentFailure();
        }

        await _subscriptions.AddSubscriptionAsync(subscription);
        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            SubscriptionId = subscription.Id,
            TargetType = "Platform",
            TargetId = userId,
            EntryType = "Credit",
            Category = "SubscriptionRevenue",
            Amount = plan.MonthlyPrice,
            Description = $"Contratação do plano de assinatura '{plan.Name}'"
        });

        await _profiles.AddNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "Assinatura ativada",
            Message = $"Sua assinatura do plano '{plan.Name}' está ativa com franquia de {mealsInCycle} refeições no ciclo.",
            IsRead = false
        });

        await _subscriptions.SaveChangesAsync();

        return StatusCode(201, subscription);
    }

    [HttpPatch("{id}/pausar")]
    public async Task<IActionResult> Pause(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Pause();
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/reativar")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Reactivate();
        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/renovar")]
    public async Task<IActionResult> Renew(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        var plan = await _subscriptions.GetPlanByIdAsync(sub.PlanId);
        var meals = plan != null
            ? (plan.MealsPerMonth > 0 ? plan.MealsPerMonth : plan.MealsPerWeek * 4)
            : sub.TotalMealsInCycle;

        sub.Renew(meals);

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            SubscriptionId = sub.Id,
            TargetType = "Platform",
            TargetId = sub.UserId,
            EntryType = "Credit",
            Category = "SubscriptionRenewal",
            Amount = sub.MonthlyPrice,
            Description = $"Renovação mensal da assinatura '{sub.PlanName}'"
        });

        await _profiles.AddNotificationAsync(new Notification
        {
            UserId = sub.UserId,
            Title = "Assinatura renovada",
            Message = $"Sua assinatura do plano '{sub.PlanName}' foi renovada para o novo ciclo.",
            IsRead = false
        });

        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/cancelar")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var sub = await GetRequiredAsync(id);
        sub.Cancel();

        await _profiles.AddNotificationAsync(new Notification
        {
            UserId = sub.UserId,
            Title = "Assinatura cancelada",
            Message = $"Sua assinatura do plano '{sub.PlanName}' foi cancelada.",
            IsRead = false
        });

        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPatch("{id}/alterar-plano")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangeSubscriptionPlanRequest request)
    {
        var sub = await GetRequiredAsync(id);
        var newPlan = await _subscriptions.GetPlanByIdAsync(request.NewPlanId);
        if (newPlan == null || !newPlan.IsActive)
            throw new Exception("O novo plano selecionado não foi encontrado ou está inativo.");

        var proratedDiff = sub.ChangePlan(newPlan);
        if (proratedDiff != 0)
        {
            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                SubscriptionId = sub.Id,
                TargetType = "Platform",
                TargetId = sub.UserId,
                EntryType = proratedDiff > 0 ? "Credit" : "Debit",
                Category = "SubscriptionProrata",
                Amount = Math.Abs(proratedDiff),
                Description = $"Ajuste prorrata de alteração de plano para '{newPlan.Name}'"
            });
        }

        await _subscriptions.SaveChangesAsync();
        return Ok(new
        {
            subscription = sub,
            proratedAdjustment = proratedDiff
        });
    }

    [HttpPatch("{id}/configuracoes")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] UpdateSubscriptionSettingsRequest request)
    {
        var sub = await GetRequiredAsync(id);
        if (request.DefaultAddressId.HasValue)
            sub.DefaultAddressId = request.DefaultAddressId;
        if (request.PreferredRestaurantId.HasValue)
            sub.PreferredRestaurantId = request.PreferredRestaurantId;

        await _subscriptions.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpGet("{id}/agendamentos")]
    public async Task<IActionResult> GetScheduledDeliveries(Guid id)
    {
        await GetRequiredAsync(id);
        var list = await _subscriptions.GetSubscriptionDeliveriesAsync(id);
        return Ok(list);
    }

    [HttpPost("{id}/agendamentos")]
    public async Task<IActionResult> ScheduleDelivery(Guid id, [FromBody] ScheduleSubscriptionDeliveryRequest request)
    {
        var sub = await GetRequiredAsync(id);
        if (sub.Status != "Active")
            throw new Exception("Apenas assinaturas ativas permitem agendar entregas.");

        var scheduled = new SubscriptionDelivery
        {
            SubscriptionId = sub.Id,
            RestaurantId = request.RestaurantId ?? sub.PreferredRestaurantId,
            AddressId = request.AddressId ?? sub.DefaultAddressId,
            ScheduledDate = request.ScheduledDate == default ? DateTime.UtcNow.AddDays(1) : request.ScheduledDate,
            DeliveryWindow = string.IsNullOrWhiteSpace(request.DeliveryWindow) ? "11:30 - 13:30" : request.DeliveryWindow.Trim(),
            MealsCount = Math.Max(1, request.MealsCount),
            Status = "Scheduled"
        };

        await _subscriptions.AddSubscriptionDeliveryAsync(scheduled);
        await _subscriptions.SaveChangesAsync();
        return StatusCode(201, scheduled);
    }

    [HttpDelete("{id}/agendamentos/{deliveryId}")]
    public async Task<IActionResult> CancelScheduledDelivery(Guid id, Guid deliveryId)
    {
        await GetRequiredAsync(id);
        var item = await _subscriptions.GetSubscriptionDeliveryByIdAsync(deliveryId);
        if (item == null || item.SubscriptionId != id)
            throw new Exception("Agendamento não encontrado.");

        _subscriptions.RemoveSubscriptionDelivery(item);
        await _subscriptions.SaveChangesAsync();
        return NoContent();
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
                paidAt = sub.StartDate,
                nextBillingDate = sub.NextBillingDate
            }
        });
    }

    private async Task<Subscription> GetRequiredAsync(Guid id)
    {
        var sub = await _subscriptions.GetSubscriptionByIdAsync(id);
        if (sub == null)
            throw new Exception("Assinatura não encontrada.");

        if (!User.IsInRole("admin") && !User.IsInRole("super_admin") && sub.UserId != GetUserId())
            throw new Exception("Você não tem permissão para acessar esta assinatura.");

        return sub;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
