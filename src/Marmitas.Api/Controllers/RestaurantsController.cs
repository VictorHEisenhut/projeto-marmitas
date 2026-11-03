using System.Security.Claims;
using Marmitas.Application.Restaurants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/restaurantes")]
[ApiController]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public RestaurantsController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var restaurants = await _restaurantQueryService.GetAllPublicAsync();
        return Ok(restaurants);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await _restaurantQueryService.GetByIdAsync(id);
        return Ok(restaurant);
    }

    [HttpPost]
    [Authorize(Roles = "restaurant_owner,admin,super_admin")]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var ownerId))
            return Unauthorized();

        var result = await _restaurantQueryService.CreateAsync(ownerId, request);
        return StatusCode(201, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "restaurant_owner,admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRestaurantRequest request)
    {
        var result = await _restaurantQueryService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] UpdateRestaurantStatusRequest request)
    {
        var result = await _restaurantQueryService.ChangeStatusAsync(id, request.Status);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _restaurantQueryService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/cardapio")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(Guid id)
    {
        var products = await _restaurantQueryService.GetRestaurantProductsAsync(id);
        return Ok(products);
    }
}
