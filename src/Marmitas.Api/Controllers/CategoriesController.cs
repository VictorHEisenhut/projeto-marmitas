using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/categorias")]
[ApiController]
public class CategoriesController : ControllerBase
{
    [HttpGet] public IActionResult GetAll() => StatusCode(501);
    [HttpPost] public IActionResult Create() => StatusCode(501);
    [HttpPut("{id}")] public IActionResult Update(Guid id) => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult Delete(Guid id) => StatusCode(501);
}
