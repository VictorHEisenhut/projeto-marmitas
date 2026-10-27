using Marmitas.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
[Authorize]
public class FinancialController : ControllerBase
{
    private readonly ICreateOrderService _orders;

    public FinancialController(ICreateOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("api/admin/financeiro/comissoes")]
    public async Task<IActionResult> GetCommissions()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var validOrders = orders.Where(o => o.Status != "Cancelled").ToList();
        var gross = validOrders.Sum(o => o.TotalAmount);
        return Ok(new
        {
            commissionRatePercentage = 10,
            grossVolume = gross,
            totalCommission = Math.Round(gross * 0.10m, 2)
        });
    }

    [HttpGet("api/admin/financeiro/repasses")]
    public async Task<IActionResult> GetPayouts()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var payouts = orders
            .Where(o => o.Status != "Cancelled")
            .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
            .Select(g => new
            {
                id = g.Key.RestaurantId,
                restaurantId = g.Key.RestaurantId,
                restaurantName = g.Key.RestaurantName,
                grossAmount = g.Sum(x => x.TotalAmount),
                netPayoutAmount = Math.Round(g.Sum(x => x.TotalAmount) * 0.90m, 2),
                status = "Available"
            });

        return Ok(payouts);
    }

    [HttpPost("api/admin/financeiro/repasses/{id}/processar")]
    public IActionResult ProcessPayout(Guid id)
    {
        return Ok(new
        {
            payoutId = id,
            status = "Processed",
            processedAt = DateTime.UtcNow
        });
    }

    [HttpGet("api/admin/financeiro/transacoes")]
    public async Task<IActionResult> GetTransactions()
    {
        var orders = await _orders.GetAllOrdersAsync();
        var transactions = orders.Select(o => new
        {
            transactionId = o.Id,
            orderId = o.Id,
            restaurantName = o.RestaurantName,
            amount = o.TotalAmount,
            status = o.Status,
            createdAt = o.CreatedAt
        });

        return Ok(transactions);
    }

    [HttpGet("api/admin/financeiro/conciliacao")]
    public async Task<IActionResult> GetReconciliation()
    {
        var orders = await _orders.GetAllOrdersAsync();
        return Ok(new
        {
            totalOrdersChecked = orders.Count,
            discrepanciesFound = 0,
            status = "Reconciled",
            checkedAt = DateTime.UtcNow
        });
    }

    [HttpGet("api/restaurantes/{id}/financeiro")]
    public async Task<IActionResult> GetRestaurantFinancials(Guid id)
    {
        var orders = await _orders.GetAllOrdersAsync();
        var restOrders = orders.Where(o => o.RestaurantId == id && o.Status != "Cancelled").ToList();
        var gross = restOrders.Sum(o => o.TotalAmount);

        return Ok(new
        {
            restaurantId = id,
            totalOrders = restOrders.Count,
            grossRevenue = gross,
            platformCommission = Math.Round(gross * 0.10m, 2),
            netRevenue = Math.Round(gross * 0.90m, 2)
        });
    }

    [HttpGet("api/entregadores/{id}/financeiro")]
    public IActionResult GetCourierFinancials(Guid id)
    {
        var completed = DeliveriesController.Deliveries.Count(d => d.CourierId == id && d.Status == "Delivered");
        return Ok(new
        {
            courierId = id,
            completedDeliveries = completed,
            feePerDelivery = 8.50m,
            totalBalance = completed * 8.50m
        });
    }
}
