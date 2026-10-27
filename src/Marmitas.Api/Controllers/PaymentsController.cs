using Marmitas.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private static readonly List<PaymentDto> Payments = new();
    private readonly ICreateOrderService _orders;

    public PaymentsController(ICreateOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("api/pedidos/{id}/pagamento")]
    [Authorize]
    public async Task<IActionResult> GetOrderPayment(Guid id)
    {
        var order = await _orders.GetByIdAsync(id);
        var payment = Payments.FirstOrDefault(p => p.OrderId == id) ?? new PaymentDto
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = "PIX",
            Status = order.Status == "Paid" ? "Approved" : "Pending",
            CreatedAt = order.CreatedAt
        };

        return Ok(payment);
    }

    [HttpPost("api/pedidos/{id}/pagamento")]
    [Authorize]
    public async Task<IActionResult> ProcessOrderPayment(Guid id, [FromBody] ProcessPaymentRequest request)
    {
        var order = await _orders.GetByIdAsync(id);
        await _orders.UpdateStatusAsync(id, "Paid");

        var existing = Payments.FirstOrDefault(p => p.OrderId == id);
        if (existing != null)
            Payments.Remove(existing);

        var payment = new PaymentDto
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = string.IsNullOrWhiteSpace(request.Method) ? "PIX" : request.Method.Trim(),
            Status = "Approved",
            CreatedAt = DateTime.UtcNow
        };

        Payments.Add(payment);
        return StatusCode(201, payment);
    }

    [HttpPost("api/pagamentos/{id}/estornar")]
    [Authorize]
    public async Task<IActionResult> Refund(Guid id)
    {
        var payment = Payments.FirstOrDefault(p => p.Id == id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        payment.Status = "Refunded";
        await _orders.UpdateStatusAsync(payment.OrderId, "Refunded");

        return Ok(payment);
    }

    [HttpGet("api/pagamentos/{id}")]
    [Authorize]
    public IActionResult GetPaymentDetails(Guid id)
    {
        var payment = Payments.FirstOrDefault(p => p.Id == id);
        if (payment == null)
            throw new Exception("Pagamento não encontrado.");

        return Ok(payment);
    }

    [HttpPost("api/pagamentos/webhook")]
    [AllowAnonymous]
    public IActionResult Webhook([FromBody] PaymentWebhookRequest? request)
    {
        if (request?.PaymentId != null)
        {
            var payment = Payments.FirstOrDefault(p => p.Id == request.PaymentId.Value);
            if (payment != null && !string.IsNullOrWhiteSpace(request.Status))
                payment.Status = request.Status;
        }

        return Ok(new { received = true, processedAt = DateTime.UtcNow });
    }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
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
