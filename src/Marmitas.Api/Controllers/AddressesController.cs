using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/enderecos")]
[ApiController]
public class AddressesController : ControllerBase
{
    [HttpGet] public IActionResult GetMyAddresses() => StatusCode(501);
    [HttpPost] public IActionResult CreateAddress() => StatusCode(501);
    [HttpPut("{id}")] public IActionResult UpdateAddress(Guid id) => StatusCode(501);
    [HttpPatch("{id}/principal")] public IActionResult SetMainAddress(Guid id) => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult DeleteAddress(Guid id) => StatusCode(501);
}
