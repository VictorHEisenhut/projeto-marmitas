using System.Text.Json;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Financial;
using Marmitas.Application.Integrations;
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
    private readonly IUserRepository _users;
    private readonly IRestaurantIntegrationService _integrations;
    private readonly IOrderPaymentSettlement _settlement;
    private readonly IMercadoPagoApi _mercadoPago;

    public PaymentsController(
        IPaymentRepository payments,
        ICreateOrderService orders,
        IPaymentGatewayFactory gatewayFactory,
        IFinancialLedgerService ledger,
        IUserRepository users,
        IRestaurantIntegrationService integrations,
        IOrderPaymentSettlement settlement,
        IMercadoPagoApi mercadoPago)
    {
        _payments = payments;
        _orders = orders;
        _gatewayFactory = gatewayFactory;
        _ledger = ledger;
        _users = users;
        _integrations = integrations;
        _settlement = settlement;
        _mercadoPago = mercadoPago;
    }

    [HttpGet("api/pedidos/{id}/pagamento")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetOrderPayment(Guid id, CancellationToken cancellationToken)
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

        if (string.Equals(payment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            payment = await _settlement.RefreshAsync(payment, cancellationToken) ?? payment;

        return Ok(payment);
    }

    [HttpPost("api/pedidos/{id}/pagamento")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> ProcessOrderPayment(Guid id, [FromBody] ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var order = await _orders.GetByIdAsync(id);
        var method = string.IsNullOrWhiteSpace(request.Method) ? "PIX" : request.Method.Trim();
        var existing = await _payments.GetPaymentByOrderIdAsync(id);

        if (existing != null && string.Equals(existing.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            return StatusCode(201, existing);

        if (existing != null &&
            string.Equals(existing.Status, "Pending", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Method, method, StringComparison.OrdinalIgnoreCase))
        {
            var refreshed = await _settlement.RefreshAsync(existing, cancellationToken) ?? existing;
            return StatusCode(201, refreshed);
        }

        var accessToken = await _integrations.GetAccessTokenAsync(order.RestaurantId, cancellationToken);
        var useLive = !string.IsNullOrWhiteSpace(accessToken);
        if (useLive && IsCard(method) && order.TotalAmount > 0 && string.IsNullOrWhiteSpace(request.CardToken))
        {
            throw DomainException.Validation("Informe os dados do cartão para pagar com o Mercado Pago da loja.");
        }

        var payer = await _users.GetByIdAsync(order.CustomerId);
        var gateway = _gatewayFactory.GetGateway(useLive ? "MercadoPago" : request.Gateway);
        var chargeResult = await gateway.ChargeAsync(new PaymentChargeRequest(
            order.Id,
            order.TotalAmount,
            method,
            gateway.ProviderName,
            request.CardToken,
            string.IsNullOrWhiteSpace(request.PayerEmail) ? payer?.Email : request.PayerEmail,
            useLive ? accessToken : null,
            request.PaymentMethodId,
            request.IssuerId,
            request.Installments < 1 ? 1 : request.Installments,
            string.IsNullOrWhiteSpace(request.PayerIdentificationType) ? "CPF" : request.PayerIdentificationType,
            request.PayerIdentificationNumber,
            $"Pedido {order.RestaurantName} #{order.Id.ToString()[..8]}",
            idempotencyKey), cancellationToken);

        var payment = await _settlement.RecordChargeAsync(order, existing, method, chargeResult, idempotencyKey, cancellationToken);

        if (!chargeResult.Approved && !chargeResult.Pending)
        {
            throw new DomainException(
                chargeResult.FailureReason ?? "Pagamento recusado.",
                ErrorCodes.PaymentDeclined,
                ErrorKind.Validation);
        }

        return StatusCode(201, payment);
    }

    [HttpPost("api/pagamentos/{id}/estornar")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest? request = null, CancellationToken cancellationToken = default)
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

        var order = await _orders.GetByIdAsync(payment.OrderId);
        var token = await _integrations.GetAccessTokenAsync(order.RestaurantId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(token) && MercadoPagoPaymentIds.IsLive(payment.ExternalTransactionId))
        {
            var refund = await _mercadoPago.RefundAsync(token, payment.ExternalTransactionId!, payment.Amount, cancellationToken);
            if (!refund.Approved)
            {
                throw new DomainException(
                    refund.FailureReason ?? "Estorno recusado pelo Mercado Pago.",
                    ErrorCodes.PaymentDeclined,
                    ErrorKind.Validation);
            }
        }
        else
        {
            var gateway = _gatewayFactory.GetGateway(payment.Gateway);
            await gateway.RefundAsync(payment.ExternalTransactionId ?? payment.Id.ToString(), payment.Amount, cancellationToken);
        }

        payment.Status = "Refunded";
        payment.RefundedAt = DateTime.UtcNow;

        await _orders.UpdateStatusAsync(payment.OrderId, "Refunded", null, "admin", request?.Reason ?? "Estorno processado");
        await _ledger.RecordOrderRefundAsync(order, payment, payment.Amount, request?.Reason ?? "Estorno administrativo");

        await _payments.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpGet("api/pagamentos/{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetPaymentDetails(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetPaymentByIdAsync(id) ?? await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        if (string.Equals(payment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            payment = await _settlement.RefreshAsync(payment, cancellationToken) ?? payment;

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

    [HttpPost("api/pagamentos/webhook/mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> MercadoPagoWebhook(CancellationToken cancellationToken)
    {
        var paymentId = Request.Query["data.id"].FirstOrDefault()
                        ?? Request.Query["id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var raw = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("data", out var data) &&
                        data.ValueKind == JsonValueKind.Object &&
                        data.TryGetProperty("id", out var idElement))
                    {
                        paymentId = idElement.ValueKind == JsonValueKind.Number
                            ? idElement.GetRawText()
                            : idElement.GetString();
                    }
                }
                catch (JsonException)
                {
                    paymentId = null;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(paymentId))
            await _settlement.RefreshByExternalIdAsync(paymentId, cancellationToken);

        return Ok(new { received = true, processedAt = DateTime.UtcNow });
    }

    private static bool IsCard(string method)
    {
        var normalized = method.Replace("_", string.Empty, StringComparison.Ordinal).Trim();
        return normalized.Equals("CreditCard", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("DebitCard", StringComparison.OrdinalIgnoreCase);
    }
}
