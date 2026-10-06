using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class FinancialController : ControllerBase
{
    [HttpGet("api/admin/financeiro/comissoes")] public IActionResult GetCommissions() => StatusCode(501);
    [HttpGet("api/admin/financeiro/repasses")] public IActionResult GetPayouts() => StatusCode(501);
    [HttpPost("api/admin/financeiro/repasses/{id}/processar")] public IActionResult ProcessPayout(Guid id) => StatusCode(501);
    [HttpGet("api/admin/financeiro/transacoes")] public IActionResult GetTransactions() => StatusCode(501);
    [HttpGet("api/admin/financeiro/conciliacao")] public IActionResult GetReconciliation() => StatusCode(501);
    [HttpGet("api/restaurantes/{id}/financeiro")] public IActionResult GetRestaurantFinancials(Guid id) => StatusCode(501);
    [HttpGet("api/entregadores/{id}/financeiro")] public IActionResult GetCourierFinancials(Guid id) => StatusCode(501);
}
