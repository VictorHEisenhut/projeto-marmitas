using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/favoritos")]
[ApiController]
public class FavoritesController : ControllerBase
{
    [HttpGet] public IActionResult GetMyFavorites() => StatusCode(501);
    [HttpPost] public IActionResult AddFavorite() => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult RemoveFavorite(Guid id) => StatusCode(501);
}
