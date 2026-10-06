using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    [HttpGet("api/pedidos/{id}/pagamento")] public IActionResult GetOrderPayment(Guid id) => StatusCode(501);
    [HttpPost("api/pedidos/{id}/pagamento")] public IActionResult ProcessOrderPayment(Guid id) => StatusCode(501);
    [HttpPost("api/pagamentos/{id}/estornar")] public IActionResult Refund(Guid id) => StatusCode(501);
    [HttpGet("api/pagamentos/{id}")] public IActionResult GetPaymentDetails(Guid id) => StatusCode(501);
    [HttpPost("api/pagamentos/webhook")] public IActionResult Webhook() => StatusCode(501);
}
