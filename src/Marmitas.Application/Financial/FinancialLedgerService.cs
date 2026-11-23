using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Domain.Entities;

namespace Marmitas.Application.Financial;

public interface IFinancialLedgerService
{
    Task RecordOrderPaymentAsync(OrderDto order, Payment payment);
    Task RecordOrderRefundAsync(OrderDto order, Payment payment, decimal refundAmount, string reason);
    Task RecordDeliveryCompletedAsync(Delivery delivery, Guid courierId);
    Task<object> ReconcileAsync(List<OrderDto> orders, List<Payout> payouts);
}

public class FinancialLedgerService : IFinancialLedgerService
{
    private const decimal PlatformCommissionRate = 0.10m;
    private const decimal PixGatewayRate = 0.0099m;
    private const decimal CardGatewayRate = 0.0299m;

    private readonly IPaymentRepository _payments;

    public FinancialLedgerService(IPaymentRepository payments)
    {
        _payments = payments;
    }

    public async Task RecordOrderPaymentAsync(OrderDto order, Payment payment)
    {
        var gross = order.TotalAmount;
        var commissionAmount = Math.Round(gross * PlatformCommissionRate, 2);
        var gatewayRate = string.Equals(payment.Method, "PIX", StringComparison.OrdinalIgnoreCase)
            ? PixGatewayRate
            : CardGatewayRate;
        var gatewayFee = Math.Round(gross * gatewayRate, 2);
        var deliveryFee = order.DeliveryFee;
        var restaurantNet = Math.Max(0m, gross - commissionAmount - gatewayFee - deliveryFee);

        await _payments.AddCommissionAsync(new Commission
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            OrderAmount = gross,
            RatePercentage = PlatformCommissionRate * 100m,
            CommissionAmount = commissionAmount
        });

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = order.Id,
            TargetType = "Platform",
            TargetId = order.RestaurantId,
            EntryType = "Credit",
            Category = "Revenue",
            Amount = gross,
            Description = $"Receita bruta do pedido #{order.Id.ToString()[..8]} ({payment.Method})"
        });

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = order.Id,
            TargetType = "Platform",
            TargetId = order.RestaurantId,
            EntryType = "Credit",
            Category = "Commission",
            Amount = commissionAmount,
            Description = $"Comissão da plataforma (10%) sobre pedido #{order.Id.ToString()[..8]}"
        });

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = order.Id,
            TargetType = "Gateway",
            TargetId = payment.Id,
            EntryType = "Debit",
            Category = "GatewayFee",
            Amount = gatewayFee,
            Description = $"Taxa de processamento do gateway ({payment.Gateway})"
        });

        if (order.DiscountAmount > 0)
        {
            await _payments.AddFinancialTransactionAsync(new FinancialTransaction
            {
                OrderId = order.Id,
                TargetType = "Platform",
                TargetId = order.RestaurantId,
                EntryType = "Debit",
                Category = "Discount",
                Amount = order.DiscountAmount,
                Description = $"Desconto de cupom ({order.CouponCode ?? "PROMO"})"
            });
        }

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = order.Id,
            TargetType = "Restaurant",
            TargetId = order.RestaurantId,
            EntryType = "Credit",
            Category = "RestaurantNet",
            Amount = restaurantNet,
            Description = $"Valor líquido do restaurante no pedido #{order.Id.ToString()[..8]}"
        });
    }

    public async Task RecordOrderRefundAsync(OrderDto order, Payment payment, decimal refundAmount, string reason)
    {
        await _payments.AddRefundAsync(new Refund
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Amount = refundAmount,
            Reason = reason,
            Status = "Processed",
            ExternalRefundId = $"ref_{Guid.NewGuid():N}"[..16],
            ProcessedAt = DateTime.UtcNow
        });

        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = order.Id,
            TargetType = "Platform",
            TargetId = order.RestaurantId,
            EntryType = "Debit",
            Category = "Refund",
            Amount = refundAmount,
            Description = $"Estorno do pedido #{order.Id.ToString()[..8]}: {reason}"
        });
    }

    public async Task RecordDeliveryCompletedAsync(Delivery delivery, Guid courierId)
    {
        var fee = delivery.DeliveryFee > 0 ? delivery.DeliveryFee : 8.50m;
        await _payments.AddFinancialTransactionAsync(new FinancialTransaction
        {
            OrderId = delivery.OrderId,
            TargetType = "Driver",
            TargetId = courierId,
            EntryType = "Credit",
            Category = "DeliveryFee",
            Amount = fee,
            Description = $"Repasse de entrega concluída do pedido #{delivery.OrderId.ToString()[..8]}"
        });
    }

    public async Task<object> ReconcileAsync(List<OrderDto> orders, List<Payout> payouts)
    {
        var ledgerEntries = await _payments.GetFinancialTransactionsAsync();
        var payments = await _payments.GetAllPaymentsAsync();

        var validOrders = orders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").ToList();
        var expectedGross = validOrders.Sum(o => o.TotalAmount);
        var expectedNetRestaurants = Math.Round(expectedGross * (1m - PlatformCommissionRate), 2);
        var recordedPayoutsNet = payouts
            .Where(p => p.TargetType == "Restaurant")
            .Sum(p => p.NetAmount);

        var difference = Math.Abs(expectedNetRestaurants - recordedPayoutsNet);
        var discrepancies = difference > 0.05m && payouts.Count > 0 ? 1 : 0;

        return new
        {
            totalOrdersChecked = orders.Count,
            totalPaymentsRecorded = payments.Count,
            totalLedgerEntries = ledgerEntries.Count,
            totalPayoutsRecorded = payouts.Count,
            expectedGrossVolume = expectedGross,
            expectedRestaurantNet = expectedNetRestaurants,
            recordedPayoutsNet,
            discrepanciesFound = discrepancies,
            status = discrepancies == 0 ? "Reconciled" : "ReviewRequired",
            checkedAt = DateTime.UtcNow
        };
    }
}
