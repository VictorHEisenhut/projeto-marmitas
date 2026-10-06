using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class MarmitasController : ControllerBase
{
    [HttpGet("api/marmitas")] public IActionResult GetAll() => StatusCode(501);
    [HttpGet("api/marmitas/{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPost("api/restaurantes/{id}/marmitas")] public IActionResult AddToMenu(Guid id) => StatusCode(501);
    [HttpPut("api/marmitas/{id}")] public IActionResult Update(Guid id) => StatusCode(501);
    [HttpPatch("api/marmitas/{id}/disponibilidade")] public IActionResult ChangeAvailability(Guid id) => StatusCode(501);
    [HttpDelete("api/marmitas/{id}")] public IActionResult Delete(Guid id) => StatusCode(501);

    [HttpGet("api/marmitas/{id}/opcoes")] public IActionResult GetOptions(Guid id) => StatusCode(501);
    [HttpPost("api/marmitas/{id}/opcoes")] public IActionResult AddOption(Guid id) => StatusCode(501);
    [HttpPut("api/marmitas/opcoes/{id}")] public IActionResult UpdateOption(Guid id) => StatusCode(501);
    [HttpDelete("api/marmitas/opcoes/{id}")] public IActionResult DeleteOption(Guid id) => StatusCode(501);
}
