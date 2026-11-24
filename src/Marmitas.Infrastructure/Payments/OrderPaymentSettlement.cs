using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Financial;
using Marmitas.Application.Integrations;
using Marmitas.Application.Orders;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Marmitas.Infrastructure.Payments;

public sealed class OrderPaymentSettlement : IOrderPaymentSettlement
{
    private readonly IPaymentRepository _payments;
    private readonly ICreateOrderService _orders;
    private readonly IFinancialLedgerService _ledger;
    private readonly IRestaurantIntegrationService _integrations;
    private readonly IMercadoPagoApi _mercadoPago;
    private readonly ILogger<OrderPaymentSettlement> _logger;

    public OrderPaymentSettlement(
        IPaymentRepository payments,
        ICreateOrderService orders,
        IFinancialLedgerService ledger,
        IRestaurantIntegrationService integrations,
        IMercadoPagoApi mercadoPago,
        ILogger<OrderPaymentSettlement> logger)
    {
        _payments = payments;
        _orders = orders;
        _ledger = ledger;
        _integrations = integrations;
        _mercadoPago = mercadoPago;
        _logger = logger;
    }

    public async Task<Payment> RecordChargeAsync(
        OrderDto order,
        Payment? existing,
        string method,
        PaymentGatewayResult result,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var payment = existing ?? new Payment { OrderId = order.Id };
        var isNew = existing == null;

        payment.Amount = order.TotalAmount;
        payment.Method = method;
        payment.Gateway = result.GatewayName;
        payment.ExternalTransactionId = result.ExternalTransactionId;
        payment.PixQrCode = result.PixQrCode;
        payment.PixCopyPaste = result.PixCopyPaste;
        payment.IdempotencyKey = idempotencyKey?.Trim();
        payment.Status = result.Status;
        if (result.Approved && !result.Pending)
            payment.PaidAt = DateTime.UtcNow;

        if (isNew)
            await _payments.AddPaymentAsync(payment);

        await _payments.AddPaymentTransactionAsync(new PaymentTransaction
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Gateway = result.GatewayName,
            ExternalTransactionId = result.ExternalTransactionId,
            Amount = order.TotalAmount,
            Status = result.Status
        });

        await ApplyOrderStatusAsync(order, payment, result.Approved && !result.Pending, result.Pending);
        await _payments.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment?> RefreshAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(payment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return payment;

        if (!MercadoPagoPaymentIds.IsLive(payment.ExternalTransactionId))
            return payment;

        var order = await _orders.GetByIdAsync(payment.OrderId);
        var token = await _integrations.GetAccessTokenAsync(order.RestaurantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return payment;

        PaymentGatewayResult remote;
        try
        {
            remote = await _mercadoPago.GetPaymentAsync(token, payment.ExternalTransactionId!, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao consultar o pagamento {PaymentId} no Mercado Pago", payment.ExternalTransactionId);
            return payment;
        }

        if (string.Equals(remote.Status, payment.Status, StringComparison.OrdinalIgnoreCase))
            return payment;

        payment.Status = remote.Status;
        if (!string.IsNullOrWhiteSpace(remote.PixCopyPaste))
            payment.PixCopyPaste = remote.PixCopyPaste;
        if (!string.IsNullOrWhiteSpace(remote.PixQrCode))
            payment.PixQrCode = remote.PixQrCode;
        if (remote.Approved && !remote.Pending)
            payment.PaidAt ??= DateTime.UtcNow;

        await _payments.AddPaymentTransactionAsync(new PaymentTransaction
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Gateway = "MercadoPago",
            ExternalTransactionId = payment.ExternalTransactionId ?? remote.ExternalTransactionId,
            Amount = payment.Amount,
            Status = remote.Status
        });

        await ApplyOrderStatusAsync(order, payment, remote.Approved && !remote.Pending, remote.Pending);
        await _payments.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment?> RefreshByExternalIdAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        if (!MercadoPagoPaymentIds.IsLive(externalPaymentId))
            return null;

        var payment = await _payments.GetPaymentByExternalTransactionIdAsync(externalPaymentId.Trim());
        if (payment == null)
            return null;

        return await RefreshAsync(payment, cancellationToken);
    }

    public async Task RefreshPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _payments.GetPendingMercadoPagoPaymentsAsync();
        foreach (var payment in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await RefreshAsync(payment, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Falha ao sincronizar pagamento pendente {PaymentId}", payment.Id);
            }
        }
    }

    private async Task ApplyOrderStatusAsync(OrderDto order, Payment payment, bool approved, bool pending)
    {
        try
        {
            if (approved)
            {
                if (CanMove(order.Status, "Paid"))
                {
                    order = await _orders.UpdateStatusAsync(
                        order.Id,
                        "Paid",
                        null,
                        "gateway",
                        $"Pago via {payment.Gateway} ({payment.Method})");
                }

                if (!await _payments.HasRestaurantNetEntryAsync(order.Id))
                    await _ledger.RecordOrderPaymentAsync(order, payment);

                return;
            }

            if (pending)
            {
                if (CanMove(order.Status, "PaymentPending"))
                {
                    await _orders.UpdateStatusAsync(
                        order.Id,
                        "PaymentPending",
                        null,
                        "gateway",
                        "Aguardando confirmação do Mercado Pago");
                }

                return;
            }

            if (CanMove(order.Status, "Failed"))
            {
                await _orders.UpdateStatusAsync(
                    order.Id,
                    "Failed",
                    null,
                    "gateway",
                    "Pagamento recusado");
            }
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Status do pedido {OrderId} não foi alterado após o pagamento", order.Id);
        }
    }

    private static bool CanMove(string current, string target)
    {
        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            return false;

        return target switch
        {
            "Paid" => current is "Created" or "PaymentPending" or "Failed",
            "PaymentPending" => current is "Created" or "Failed",
            "Failed" => current is "Created" or "PaymentPending",
            _ => false
        };
    }
}
