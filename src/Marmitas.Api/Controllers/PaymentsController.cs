using System.Text.Json;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Financial;
using Marmitas.Application.Orders;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Domain.Errors;
using Marmitas.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _payments;
    private readonly ICreateOrderService _orders;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IFinancialLedgerService _ledger;

    public PaymentsController(
        IPaymentRepository payments,
        ICreateOrderService orders,
        IPaymentGatewayFactory gatewayFactory,
        IFinancialLedgerService ledger)
    {
        _payments = payments;
        _orders = orders;
        _gatewayFactory = gatewayFactory;
        _ledger = ledger;
    }

    [HttpGet("api/pedidos/{id}/pagamento")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetOrderPayment(Guid id)
    {
        var order = await _orders.GetByIdAsync(id);
        var payment = await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
        {
            return Ok(new
            {
                orderId = order.Id,
                amount = order.TotalAmount,
                method = "PIX",
                gateway = "MercadoPago",
                status = order.Status == "Paid" ? "Approved" : "Pending",
                createdAt = order.CreatedAt
            });
        }

        return Ok(payment);
    }

    [HttpPost("api/pedidos/{id}/pagamento")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> ProcessOrderPayment(Guid id, [FromBody] ProcessPaymentRequest request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var order = await _orders.GetByIdAsync(id);
        var method = string.IsNullOrWhiteSpace(request.Method) ? "PIX" : request.Method.Trim();
        var gateway = _gatewayFactory.GetGateway(request.Gateway);

        var chargeResult = await gateway.ChargeAsync(new PaymentChargeRequest(
            order.Id,
            order.TotalAmount,
            method,
            gateway.ProviderName,
            request.CardToken));

        var payment = await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
        {
            payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Method = method,
                Gateway = chargeResult.GatewayName,
                ExternalTransactionId = chargeResult.ExternalTransactionId,
                PixQrCode = chargeResult.PixQrCode,
                PixCopyPaste = chargeResult.PixCopyPaste,
                IdempotencyKey = idempotencyKey?.Trim(),
                Status = chargeResult.Status,
                PaidAt = chargeResult.Approved ? DateTime.UtcNow : null
            };
            await _payments.AddPaymentAsync(payment);
        }
        else
        {
            payment.Amount = order.TotalAmount;
            payment.Method = method;
            payment.Gateway = chargeResult.GatewayName;
            payment.ExternalTransactionId = chargeResult.ExternalTransactionId;
            payment.PixQrCode = chargeResult.PixQrCode;
            payment.PixCopyPaste = chargeResult.PixCopyPaste;
            payment.IdempotencyKey = idempotencyKey?.Trim();
            payment.Status = chargeResult.Status;
            if (chargeResult.Approved)
                payment.PaidAt = DateTime.UtcNow;
        }

        await _payments.AddPaymentTransactionAsync(new PaymentTransaction
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Gateway = chargeResult.GatewayName,
            ExternalTransactionId = chargeResult.ExternalTransactionId,
            Amount = order.TotalAmount,
            Status = chargeResult.Status
        });

        if (!chargeResult.Approved)
        {
            await _orders.UpdateStatusAsync(id, "Failed", null, "gateway", chargeResult.FailureReason);
            await _payments.SaveChangesAsync();
            throw new DomainException(
                chargeResult.FailureReason ?? "Pagamento recusado.",
                ErrorCodes.PaymentDeclined,
                ErrorKind.Validation);
        }

        await _orders.UpdateStatusAsync(id, "Paid", null, "gateway", $"Pago via {chargeResult.GatewayName} ({method})");
        await _ledger.RecordOrderPaymentAsync(order, payment);

        await _payments.SaveChangesAsync();
        return StatusCode(201, payment);
    }

    [HttpPost("api/pagamentos/{id}/estornar")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest? request = null)
    {
        var payment = await _payments.GetPaymentByIdAsync(id) ?? await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
        {
            var existingOrder = await _orders.GetByIdAsync(id);
            payment = new Payment
            {
                OrderId = existingOrder.Id,
                Amount = existingOrder.TotalAmount,
                Method = "PIX",
                Gateway = "MercadoPago",
                ExternalTransactionId = $"mp_seed_{existingOrder.Id:N}"[..24],
                Status = "Approved",
                PaidAt = existingOrder.CreatedAt
            };
            await _payments.AddPaymentAsync(payment);
        }

        if (payment.Status == "Refunded")
            return Ok(payment);

        var gateway = _gatewayFactory.GetGateway(payment.Gateway);
        await gateway.RefundAsync(payment.ExternalTransactionId ?? payment.Id.ToString(), payment.Amount);

        payment.Status = "Refunded";
        payment.RefundedAt = DateTime.UtcNow;

        var order = await _orders.GetByIdAsync(payment.OrderId);
        await _orders.UpdateStatusAsync(payment.OrderId, "Refunded", null, "admin", request?.Reason ?? "Estorno processado");
        await _ledger.RecordOrderRefundAsync(order, payment, payment.Amount, request?.Reason ?? "Estorno administrativo");

        await _payments.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpGet("api/pagamentos/{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetPaymentDetails(Guid id)
    {
        var payment = await _payments.GetPaymentByIdAsync(id) ?? await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        return Ok(payment);
    }

    [HttpPost("api/pagamentos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest? request)
    {
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        var gateway = _gatewayFactory.GetGateway(request?.Gateway);
        var rawJson = request != null ? JsonSerializer.Serialize(request) : string.Empty;

        if (!gateway.ValidateWebhookSignature(rawJson, signature))
        {
            throw new DomainException(
                "Assinatura criptográfica do webhook inválida.",
                ErrorCodes.PaymentWebhookSignatureInvalid,
                ErrorKind.Unauthorized);
        }

        if (request?.PaymentId != null)
        {
            var payment = await _payments.GetPaymentByIdAsync(request.PaymentId.Value);
            if (payment != null && !string.IsNullOrWhiteSpace(request.Status))
            {
                payment.Status = request.Status.Trim();
                await _payments.AddPaymentTransactionAsync(new PaymentTransaction
                {
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    Gateway = gateway.ProviderName,
                    ExternalTransactionId = request.ExternalTransactionId ?? $"wh_{Guid.NewGuid():N}"[..18],
                    Amount = payment.Amount,
                    Status = payment.Status,
                    RawPayload = rawJson
                });
                await _payments.SaveChangesAsync();
            }
        }

        return Ok(new { received = true, processedAt = DateTime.UtcNow });
    }
}
