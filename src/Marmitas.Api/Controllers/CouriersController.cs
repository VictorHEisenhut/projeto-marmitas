using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/entregadores")]
[ApiController]
public class CouriersController : ControllerBase
{
    [HttpPost("registro")] public IActionResult Register() => StatusCode(501);
    [HttpGet] public IActionResult GetAll() => StatusCode(501);
    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPatch("{id}/aprovar")] public IActionResult Approve(Guid id) => StatusCode(501);
    [HttpPatch("{id}/status")] public IActionResult ChangeStatus(Guid id) => StatusCode(501);
    [HttpGet("me/documentos")] public IActionResult GetMyDocuments() => StatusCode(501);
    [HttpPost("me/documentos")] public IActionResult UploadDocument() => StatusCode(501);
    [HttpGet("me/veiculos")] public IActionResult GetMyVehicles() => StatusCode(501);
    [HttpPost("me/veiculos")] public IActionResult AddVehicle() => StatusCode(501);
    [HttpPut("me/veiculos/{id}")] public IActionResult UpdateVehicle(Guid id) => StatusCode(501);
    [HttpPatch("me/localizacao")] public IActionResult UpdateLocation() => StatusCode(501);
    [HttpGet("me/entregas")] public IActionResult GetMyDeliveries() => StatusCode(501);
    [HttpGet("{id}/historico")] public IActionResult GetDeliveryHistory(Guid id) => StatusCode(501);
    [HttpGet("me/ganhos")] public IActionResult GetMyEarnings() => StatusCode(501);
}
