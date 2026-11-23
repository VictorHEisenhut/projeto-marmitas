using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Enums;
using Marmitas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Api.Workers;

public sealed class PlatformBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlatformBackgroundWorker> _logger;

    public PlatformBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PlatformBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunScheduledMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha na execução do ciclo de jobs agendados.");
            }
        }
    }

    public async Task RunScheduledMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var gatewayFactory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();
        var now = DateTime.UtcNow;

        var dueSubscriptions = await subscriptions.GetDueForRenewalAsync(now);
        var gateway = gatewayFactory.GetGateway();

        foreach (var sub in dueSubscriptions)
        {
            var charge = await gateway.ChargeAsync(
                new PaymentChargeRequest(sub.Id, sub.MonthlyPrice, "PIX", gateway.ProviderName),
                cancellationToken);

            if (charge.Approved)
            {
                var plan = await subscriptions.GetPlanByIdAsync(sub.PlanId);
                var meals = plan != null
                    ? (plan.MealsPerMonth > 0 ? plan.MealsPerMonth : plan.MealsPerWeek * 4)
                    : 20;
                sub.Renew(meals);
            }
            else
            {
                sub.RegisterPaymentFailure();
            }
        }

        var abandonedCutoff = now.AddMinutes(-30);
        var abandonedOrders = await db.Orders
            .Where(o => (o.Status == OrderStatus.Created || o.Status == OrderStatus.PaymentPending) &&
                        o.CreatedAt < abandonedCutoff)
            .ToListAsync(cancellationToken);

        foreach (var order in abandonedOrders)
        {
            order.TransitionTo(OrderStatus.Cancelled);
        }

        var expiredIdempotency = await db.IdempotencyRecords
            .Where(r => r.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredIdempotency.Count > 0)
        {
            db.IdempotencyRecords.RemoveRange(expiredIdempotency);
        }

        var expiredResetTokens = await db.PasswordResetTokens
            .Where(t => t.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredResetTokens.Count > 0)
        {
            db.PasswordResetTokens.RemoveRange(expiredResetTokens);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
