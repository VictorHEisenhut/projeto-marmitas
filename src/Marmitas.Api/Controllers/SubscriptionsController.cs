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
    private readonly IRestaurantRepository _restaurants;

    public SubscriptionsController(
        ISubscriptionRepository subscriptions,
        ICreateOrderService orders,
        ICustomerProfileRepository profiles,
        IPaymentRepository payments,
        IPaymentGatewayFactory gatewayFactory,
        IRestaurantRepository restaurants)
    {
        _subscriptions = subscriptions;
        _orders = orders;
        _profiles = profiles;
        _payments = payments;
        _gatewayFactory = gatewayFactory;
        _restaurants = restaurants;
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
        if (plan.RestaurantId == Guid.Empty)
            throw new Exception("Este plano não está vinculado a um restaurante válido.");

        var restaurant = await _restaurants.GetByIdAsync(plan.RestaurantId);
        var restaurantName = !string.IsNullOrWhiteSpace(plan.RestaurantName)
            ? plan.RestaurantName
            : (restaurant?.Name ?? string.Empty);

        var userId = GetUserId();
        var existingSubs = await _subscriptions.GetSubscriptionsByUserAsync(userId);
        if (existingSubs.Any(s => s.Status == "Active" && s.RestaurantId == plan.RestaurantId))
            throw new Exception("Você já possui uma assinatura ativa neste restaurante. Use a opção de alterar plano em Minha Conta.");

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
            RestaurantId = plan.RestaurantId,
            RestaurantName = restaurantName,
            MonthlyPrice = plan.MonthlyPrice,
            Status = charge.Approved ? "Active" : "GracePeriod",
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            TotalMealsInCycle = mealsInCycle,
            UsedMealsInCycle = 0,
            PreferredRestaurantId = plan.RestaurantId,
            DefaultAddressId = request.DefaultAddressId
        };

        if (!charge.Approved)
        {
            subscription.RegisterPaymentFailure();
        }

        await _subscriptions.AddSubscriptionAsync(subscription);

        if (charge.Approved)
        {
            var commissionAmount = Math.Round(plan.MonthlyPrice * 0.10m, 2);
            var restaurantNet = Math.Max(0m, plan.MonthlyPrice - commissionAmount);

            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                SubscriptionId = subscription.Id,
                TargetType = "Restaurant",
                TargetId = plan.RestaurantId,
                EntryType = "Credit",
                Category = "SubscriptionRevenue",
                Amount = restaurantNet,
                Description = $"Assinatura '{plan.Name}' em {restaurantName} (líquido restaurante)"
            });

            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                SubscriptionId = subscription.Id,
                TargetType = "Platform",
                TargetId = plan.RestaurantId,
                EntryType = "Credit",
                Category = "SubscriptionCommission",
                Amount = commissionAmount,
                Description = $"Comissão da plataforma (10%) sobre assinatura '{plan.Name}'"
            });
        }

        await _profiles.AddNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "Assinatura ativada",
            Message = $"Sua assinatura do plano '{plan.Name}' ({restaurantName}) está ativa com franquia de {mealsInCycle} refeições no ciclo.",
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

        if (sub.RestaurantId != Guid.Empty)
        {
            var commissionAmount = Math.Round(sub.MonthlyPrice * 0.10m, 2);
            var restaurantNet = Math.Max(0m, sub.MonthlyPrice - commissionAmount);

            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                SubscriptionId = sub.Id,
                TargetType = "Restaurant",
                TargetId = sub.RestaurantId,
                EntryType = "Credit",
                Category = "SubscriptionRenewal",
                Amount = restaurantNet,
                Description = $"Renovação mensal '{sub.PlanName}' (líquido restaurante)"
            });

            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                SubscriptionId = sub.Id,
                TargetType = "Platform",
                TargetId = sub.RestaurantId,
                EntryType = "Credit",
                Category = "SubscriptionCommission",
                Amount = commissionAmount,
                Description = $"Comissão da plataforma (10%) sobre renovação '{sub.PlanName}'"
            });
        }
        else
        {
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
        }

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
            var entryType = proratedDiff > 0 ? "Credit" : "Debit";
            var absDiff = Math.Abs(proratedDiff);
            if (sub.RestaurantId != Guid.Empty)
            {
                var commissionDiff = Math.Round(absDiff * 0.10m, 2);
                var restaurantDiff = Math.Max(0m, absDiff - commissionDiff);

                await _payments.AddFinancialTransactionAsync(new FinancialTransaction
                {
                    SubscriptionId = sub.Id,
                    TargetType = "Restaurant",
                    TargetId = sub.RestaurantId,
                    EntryType = entryType,
                    Category = "SubscriptionProrata",
                    Amount = restaurantDiff,
                    Description = $"Ajuste prorrata de alteração para '{newPlan.Name}' (restaurante)"
                });

                await _payments.AddFinancialTransactionAsync(new FinancialTransaction
                {
                    SubscriptionId = sub.Id,
                    TargetType = "Platform",
                    TargetId = sub.RestaurantId,
                    EntryType = entryType,
                    Category = "SubscriptionProrata",
                    Amount = commissionDiff,
                    Description = $"Ajuste prorrata de comissão para '{newPlan.Name}'"
                });
            }
            else
            {
                await _payments.AddFinancialTransactionAsync(new FinancialTransaction
                {
                    SubscriptionId = sub.Id,
                    TargetType = "Platform",
                    TargetId = sub.UserId,
                    EntryType = entryType,
                    Category = "SubscriptionProrata",
                    Amount = absDiff,
                    Description = $"Ajuste prorrata de alteração de plano para '{newPlan.Name}'"
                });
            }
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
        {
            if (sub.RestaurantId != Guid.Empty && request.PreferredRestaurantId.Value != sub.RestaurantId)
                throw new Exception("A assinatura pertence exclusivamente ao restaurante contratado.");
            sub.PreferredRestaurantId = request.PreferredRestaurantId;
        }

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

        if (request.RestaurantId.HasValue && sub.RestaurantId != Guid.Empty && request.RestaurantId.Value != sub.RestaurantId)
            throw new Exception("Os agendamentos desta assinatura devem ser feitos para o restaurante contratado.");

        var scheduled = new SubscriptionDelivery
        {
            SubscriptionId = sub.Id,
            RestaurantId = sub.RestaurantId != Guid.Empty ? sub.RestaurantId : (request.RestaurantId ?? sub.PreferredRestaurantId),
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
        var filteredOrders = userOrders
            .Where(o => o.SubscriptionId == sub.Id || (sub.RestaurantId != Guid.Empty && o.RestaurantId == sub.RestaurantId))
            .ToList();
        return Ok(new { subscriptionId = sub.Id, orders = filteredOrders });
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
