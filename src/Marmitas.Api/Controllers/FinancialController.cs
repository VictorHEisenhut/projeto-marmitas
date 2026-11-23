using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Financial;
using Marmitas.Application.Orders;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class FinancialController : ControllerBase
{
    private readonly ICreateOrderService _orders;
    private readonly IPaymentRepository _payments;
    private readonly IDeliveryRepository _deliveries;
    private readonly IRestaurantRepository _restaurants;
    private readonly IFinancialLedgerService _ledger;

    public FinancialController(
        ICreateOrderService orders,
        IPaymentRepository payments,
        IDeliveryRepository deliveries,
        IRestaurantRepository restaurants,
        IFinancialLedgerService ledger)
    {
        _orders = orders;
        _payments = payments;
        _deliveries = deliveries;
        _restaurants = restaurants;
        _ledger = ledger;
    }

    [HttpGet("api/admin/financeiro/comissoes")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetCommissions()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var validOrders = orders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").ToList();
        var gross = validOrders.Sum(o => o.TotalAmount);
        var recordedCommissions = await _payments.GetAllCommissionsAsync();

        return Ok(new
        {
            commissionRatePercentage = 10,
            grossVolume = gross,
            totalCommission = Math.Round(gross * 0.10m, 2),
            items = recordedCommissions
        });
    }

    [HttpGet("api/admin/financeiro/repasses")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetPayouts()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var existingPayouts = await _payments.GetAllPayoutsAsync();
        var changed = false;

        var grouped = orders
            .Where(o => o.Status != "Cancelled" && o.Status != "Refunded")
            .GroupBy(o => o.RestaurantId);

        foreach (var group in grouped)
        {
            var gross = group.Sum(x => x.TotalAmount);
            var net = Math.Round(gross * 0.90m, 2);
            var payout = existingPayouts.FirstOrDefault(p => p.TargetId == group.Key && p.TargetType == "Restaurant");
            if (payout == null)
            {
                await _payments.AddPayoutAsync(new Payout
                {
                    TargetId = group.Key,
                    TargetType = "Restaurant",
                    GrossAmount = gross,
                    NetAmount = net,
                    Status = "Available"
                });
                changed = true;
            }
            else if (payout.Status == "Available")
            {
                payout.GrossAmount = gross;
                payout.NetAmount = net;
                changed = true;
            }
        }

        var deliveries = await _deliveries.GetAllDeliveriesAsync();
        var driverGroups = deliveries
            .Where(d => d.Status == "Delivered" && d.CourierId.HasValue)
            .GroupBy(d => d.CourierId!.Value);

        foreach (var dGroup in driverGroups)
        {
            var driverTotal = dGroup.Sum(d => d.DeliveryFee > 0 ? d.DeliveryFee : 8.50m);
            var payout = existingPayouts.FirstOrDefault(p => p.TargetId == dGroup.Key && p.TargetType == "Driver");
            if (payout == null)
            {
                await _payments.AddPayoutAsync(new Payout
                {
                    TargetId = dGroup.Key,
                    TargetType = "Driver",
                    GrossAmount = driverTotal,
                    NetAmount = driverTotal,
                    Status = "Available"
                });
                changed = true;
            }
            else if (payout.Status == "Available")
            {
                payout.GrossAmount = driverTotal;
                payout.NetAmount = driverTotal;
                changed = true;
            }
        }

        if (changed)
            await _payments.SaveChangesAsync();

        var list = await _payments.GetAllPayoutsAsync();
        return Ok(list);
    }

    [HttpPost("api/admin/financeiro/repasses/{id}/processar")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> ProcessPayout(Guid id)
    {
        var payout = await _payments.GetPayoutByIdAsync(id);
        if (payout == null)
        {
            payout = new Payout
            {
                TargetId = id,
                TargetType = "Restaurant",
                Status = "Processed",
                ProcessedAt = DateTime.UtcNow
            };
            await _payments.AddPayoutAsync(payout);
        }
        else if (payout.Status != "Processed")
        {
            payout.Status = "Processed";
            payout.ProcessedAt = DateTime.UtcNow;
        }

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            PayoutId = payout.Id,
            TargetType = payout.TargetType,
            TargetId = payout.TargetId,
            EntryType = "Debit",
            Category = "Payout",
            Amount = payout.NetAmount,
            Description = $"Repasse financeiro liquidado ({payout.TargetType})"
        });

        await _payments.SaveChangesAsync();
        return Ok(payout);
    }

    [HttpGet("api/admin/financeiro/transacoes")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetTransactions()
    {
        var ledger = await _payments.GetFinancialTransactionsAsync();
        if (ledger.Count > 0)
        {
            return Ok(ledger.Select(t => new
            {
                transactionId = t.Id,
                orderId = t.OrderId,
                targetType = t.TargetType,
                entryType = t.EntryType,
                category = t.Category,
                amount = t.Amount,
                description = t.Description,
                createdAt = t.CreatedAt
            }));
        }

        var orders = await _orders.GetAllOrdersAsync();
        var transactions = orders.Select(o => new
        {
            transactionId = o.Id,
            orderId = o.Id,
            restaurantName = o.RestaurantName,
            entryType = o.Status == "Cancelled" ? "Debit" : "Credit",
            category = "OrderRevenue",
            amount = o.TotalAmount,
            status = o.Status,
            createdAt = o.CreatedAt
        });

        return Ok(transactions);
    }

    [HttpPost("api/admin/financeiro/ajustes")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreateFinancialAdjustmentRequest request)
    {
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Informe valor positivo e motivo para o lançamento de ajuste.");

        var tx = new FinancialTransaction
        {
            TargetType = string.IsNullOrWhiteSpace(request.TargetType) ? "Restaurant" : request.TargetType.Trim(),
            TargetId = request.TargetId,
            EntryType = string.Equals(request.EntryType, "Debit", StringComparison.OrdinalIgnoreCase) ? "Debit" : "Credit",
            Category = "Adjustment",
            Amount = request.Amount,
            Description = request.Description.Trim()
        };

        await _payments.AddFinancialTransactionAsync(tx);
        await _payments.SaveChangesAsync();
        return StatusCode(201, tx);
    }

    [HttpGet("api/admin/financeiro/conciliacao")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetReconciliation()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var payouts = await _payments.GetAllPayoutsAsync();
        var result = await _ledger.ReconcileAsync(orders, payouts);
        return Ok(result);
    }

    [HttpGet("api/restaurantes/{id}/financeiro")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> GetRestaurantFinancials(Guid id)
    {
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || !await _restaurants.IsUserAuthorizedAsync(id, userId))
                return Forbid();
        }

        var restOrders = await _orders.GetByRestaurantIdAsync(id);
        var validOrders = restOrders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").ToList();
        var gross = validOrders.Sum(o => o.TotalAmount);
        var ledger = await _payments.GetFinancialTransactionsAsync(id, "Restaurant");

        return Ok(new
        {
            restaurantId = id,
            totalOrders = validOrders.Count,
            grossRevenue = gross,
            platformCommission = Math.Round(gross * 0.10m, 2),
            netRevenue = Math.Round(gross * 0.90m, 2),
            ledgerEntries = ledger
        });
    }

    [HttpGet("api/entregadores/{id}/financeiro")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetCourierFinancials(Guid id)
    {
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var courier = Guid.TryParse(sub, out var userId) ? await _deliveries.GetCourierByUserIdAsync(userId) : null;
            if (courier == null || (courier.Id != id && courier.UserId != id && userId != id))
                return Forbid();
        }

        var deliveries = await _deliveries.GetDeliveriesByCourierAsync(id);
        var completed = deliveries.Where(d => d.Status == "Delivered").ToList();
        var totalBalance = completed.Sum(d => d.DeliveryFee > 0 ? d.DeliveryFee : 8.50m);

        return Ok(new
        {
            courierId = id,
            completedDeliveries = completed.Count,
            feePerDelivery = 8.50m,
            totalBalance
        });
    }
}
