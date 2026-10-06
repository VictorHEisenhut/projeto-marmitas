using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    [HttpGet("relatorios/vendas")] public IActionResult GetSalesReports() => StatusCode(501);
    [HttpGet("relatorios/assinaturas")] public IActionResult GetSubscriptionsReports() => StatusCode(501);
    [HttpGet("relatorios/entregas")] public IActionResult GetDeliveriesReports() => StatusCode(501);
    [HttpGet("dashboard")] public IActionResult GetDashboardMetrics() => StatusCode(501);
    [HttpGet("auditoria")] public IActionResult GetAuditLogs() => StatusCode(501);
    [HttpGet("auditoria/{id}")] public IActionResult GetAuditLog(Guid id) => StatusCode(501);
    [HttpGet("roles")] public IActionResult GetRoles() => StatusCode(501);
    [HttpPost("roles")] public IActionResult CreateRole() => StatusCode(501);
    [HttpPut("roles/{id}")] public IActionResult UpdateRole(Guid id) => StatusCode(501);
    [HttpGet("permissoes")] public IActionResult GetPermissions() => StatusCode(501);

    [HttpGet("pedidos")] public IActionResult GetAllOrders() => StatusCode(501);
}
