using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _payments;
    private readonly ICreateOrderService _orders;

    public PaymentsController(IPaymentRepository payments, ICreateOrderService orders)
    {
        _payments = payments;
        _orders = orders;
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
        var order = await _orders.GetByIdAsync(id);
        await _orders.UpdateStatusAsync(id, "Paid");

        var payment = await _payments.GetPaymentByOrderIdAsync(id);
        if (payment == null)
        {
            payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Method = string.IsNullOrWhiteSpace(request.Method) ? "PIX" : request.Method.Trim(),
                Status = "Approved"
            };
            await _payments.AddPaymentAsync(payment);
        }
        else
        {
            payment.Amount = order.TotalAmount;
            payment.Method = string.IsNullOrWhiteSpace(request.Method) ? "PIX" : request.Method.Trim();
            payment.Status = "Approved";
        }

        await _payments.SaveChangesAsync();
        return StatusCode(201, payment);
    }

    [HttpPost("api/pagamentos/{id}/estornar")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Refund(Guid id)
    {
        var payment = await _payments.GetPaymentByIdAsync(id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        payment.Status = "Refunded";
        await _payments.SaveChangesAsync();
        await _orders.UpdateStatusAsync(payment.OrderId, "Refunded");

        return Ok(payment);
    }

    [HttpGet("api/pagamentos/{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> GetPaymentDetails(Guid id)
    {
        var payment = await _payments.GetPaymentByIdAsync(id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        return Ok(payment);
    }

    [HttpPost("api/pagamentos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest? request)
    {
        if (request?.PaymentId != null)
        {
            var payment = await _payments.GetPaymentByIdAsync(request.PaymentId.Value);
            if (payment != null && !string.IsNullOrWhiteSpace(request.Status))
            {
                payment.Status = request.Status;
                await _payments.SaveChangesAsync();
            }
        }

        return Ok(new { received = true, processedAt = DateTime.UtcNow });
    }
}

public class ProcessPaymentRequest
{
    public string Method { get; set; } = "PIX";
}

public class PaymentWebhookRequest
{
    public Guid? PaymentId { get; set; }
    public string? Status { get; set; }
}
