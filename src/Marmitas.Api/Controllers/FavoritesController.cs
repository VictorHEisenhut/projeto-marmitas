using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/usuarios/me/favoritos")]
[ApiController]
[Authorize(Roles = "customer,admin,super_admin")]
public class FavoritesController : ControllerBase
{
    private readonly ICustomerProfileRepository _profiles;

    public FavoritesController(ICustomerProfileRepository profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = GetUserId();
        var list = await _profiles.GetFavoritesByUserAsync(userId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var fav = new Favorite
        {
            UserId = GetUserId(),
            RestaurantId = request.RestaurantId,
            ProductId = request.ProductId
        };

        await _profiles.AddFavoriteAsync(fav);
        await _profiles.SaveChangesAsync();

        return StatusCode(201, fav);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFavorite(Guid id)
    {
        var fav = await _profiles.GetFavoriteByIdAsync(id, GetUserId());
        if (fav == null)
            throw new Exception("Favorito não encontrado.");

        _profiles.RemoveFavorite(fav);
        await _profiles.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
