using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/favoritos")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private static readonly List<FavoriteDto> Favorites = new();

    [HttpGet]
    public IActionResult GetMyFavorites()
    {
        var userId = GetUserId();
        return Ok(Favorites.Where(f => f.UserId == userId));
    }

    [HttpPost]
    public IActionResult AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var fav = new FavoriteDto
        {
            Id = Guid.NewGuid(),
            UserId = GetUserId(),
            RestaurantId = request.RestaurantId,
            ProductId = request.ProductId,
            CreatedAt = DateTime.UtcNow
        };

        Favorites.Add(fav);
        return StatusCode(201, fav);
    }

    [HttpDelete("{id}")]
    public IActionResult RemoveFavorite(Guid id)
    {
        var fav = Favorites.FirstOrDefault(f => f.Id == id);
        if (fav == null)
            throw new Exception("Favorito não encontrado.");

        Favorites.Remove(fav);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? ProductId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddFavoriteRequest
{
    public Guid? RestaurantId { get; set; }
    public Guid? ProductId { get; set; }
}
