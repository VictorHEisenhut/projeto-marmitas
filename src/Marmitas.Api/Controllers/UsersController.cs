using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("me")] public IActionResult GetMe() => StatusCode(501);
    [HttpPut("me")] public IActionResult UpdateMe() => StatusCode(501);
    [HttpDelete("me")] public IActionResult DeleteMe() => StatusCode(501);

    [HttpGet] public IActionResult GetAll() => StatusCode(501);
    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPut("{id}")] public IActionResult Update(Guid id) => StatusCode(501);
    [HttpPatch("{id}/status")] public IActionResult ChangeStatus(Guid id) => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult Delete(Guid id) => StatusCode(501);
    [HttpPatch("{id}/roles")] public IActionResult AssignRoles(Guid id) => StatusCode(501);
}
