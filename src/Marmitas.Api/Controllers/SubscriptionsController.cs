using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/assinaturas")]
[ApiController]
public class SubscriptionsController : ControllerBase
{
    [HttpGet] public IActionResult GetMySubscriptions() => StatusCode(501);
    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPost] public IActionResult Subscribe() => StatusCode(501);
    [HttpPatch("{id}/pausar")] public IActionResult Pause(Guid id) => StatusCode(501);
    [HttpPatch("{id}/reativar")] public IActionResult Reactivate(Guid id) => StatusCode(501);
    [HttpPatch("{id}/renovar")] public IActionResult Renew(Guid id) => StatusCode(501);
    [HttpPatch("{id}/cancelar")] public IActionResult Cancel(Guid id) => StatusCode(501);
    [HttpGet("{id}/pedidos")] public IActionResult GetSubscriptionOrders(Guid id) => StatusCode(501);
    [HttpGet("{id}/pagamentos")] public IActionResult GetSubscriptionPayments(Guid id) => StatusCode(501);
}
