using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/cupons")]
[ApiController]
public class CouponsController : ControllerBase
{
    [HttpGet] public IActionResult GetAll() => StatusCode(501);
    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPost] public IActionResult Create() => StatusCode(501);
    [HttpPut("{id}")] public IActionResult Update(Guid id) => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult Delete(Guid id) => StatusCode(501);
    [HttpPost("validar")] public IActionResult ValidateCoupon() => StatusCode(501);
    [HttpGet("{id}/usos")] public IActionResult GetUsages(Guid id) => StatusCode(501);
}
