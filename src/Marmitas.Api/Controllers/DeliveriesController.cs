using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class DeliveriesController : ControllerBase
{
    [HttpGet("api/entregas")] public IActionResult GetDeliveries() => StatusCode(501);
    [HttpGet("api/entregas/{id}")] public IActionResult GetDelivery(Guid id) => StatusCode(501);
    [HttpPatch("api/entregas/{id}/aceitar")] public IActionResult AcceptDelivery(Guid id) => StatusCode(501);
    [HttpPatch("api/entregas/{id}/status")] public IActionResult UpdateStatus(Guid id) => StatusCode(501);
    [HttpGet("api/entregas/{id}/rastreamento")] public IActionResult TrackDelivery(Guid id) => StatusCode(501);
    [HttpPatch("api/entregadores/me/disponibilidade")] public IActionResult SetAvailability() => StatusCode(501);
}
