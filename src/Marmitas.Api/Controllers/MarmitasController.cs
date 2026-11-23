using System.Security.Claims;
using Marmitas.Application.Restaurants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class MarmitasController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public MarmitasController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet("api/marmitas")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
    {
        var products = await _restaurantQueryService.GetAllProductsAsync();
        Response.Headers["X-Total-Count"] = products.Count.ToString();
        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            products = products.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
        }
        return Ok(products);
    }

    [HttpGet("api/marmitas/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        return Ok(product);
    }

    [HttpPost("api/restaurantes/{id}/marmitas")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> AddToMenu(Guid id, [FromBody] CreateProductRequest request)
    {
        if (!await CanManageRestaurantAsync(id))
            return Forbid();

        var result = await _restaurantQueryService.CreateProductAsync(id, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/marmitas/{id}")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProductRequest request)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        if (!await CanManageRestaurantAsync(product.RestaurantId))
            return Forbid();

        var result = await _restaurantQueryService.UpdateProductAsync(id, request);
        return Ok(result);
    }

    [HttpPatch("api/marmitas/{id}/disponibilidade")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> ChangeAvailability(Guid id, [FromBody] UpdateProductAvailabilityRequest request)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        if (!await CanManageRestaurantAsync(product.RestaurantId))
            return Forbid();

        var result = await _restaurantQueryService.ChangeProductAvailabilityAsync(id, request.IsActive);
        return Ok(result);
    }

    [HttpDelete("api/marmitas/{id}")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        if (!await CanManageRestaurantAsync(product.RestaurantId))
            return Forbid();

        await _restaurantQueryService.DeleteProductAsync(id);
        return NoContent();
    }

    [HttpGet("api/marmitas/{id}/opcoes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOptions(Guid id)
    {
        var list = await _restaurantQueryService.GetProductOptionsAsync(id);
        return Ok(list);
    }

    [HttpPost("api/marmitas/{id}/opcoes")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> AddOption(Guid id, [FromBody] SaveProductOptionRequest request)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        if (!await CanManageRestaurantAsync(product.RestaurantId))
            return Forbid();

        var option = await _restaurantQueryService.AddProductOptionAsync(id, request);
        return StatusCode(201, option);
    }

    [HttpPut("api/marmitas/opcoes/{id}")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> UpdateOption(Guid id, [FromBody] SaveProductOptionRequest request)
    {
        var option = await _restaurantQueryService.UpdateProductOptionAsync(id, request);
        return Ok(option);
    }

    [HttpDelete("api/marmitas/opcoes/{id}")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> DeleteOption(Guid id)
    {
        await _restaurantQueryService.DeleteProductOptionAsync(id);
        return NoContent();
    }

    private async Task<bool> CanManageRestaurantAsync(Guid restaurantId)
    {
        if (User.IsInRole("admin") || User.IsInRole("super_admin"))
            return true;

        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var userId))
            return false;

        return await _restaurantQueryService.IsUserAuthorizedAsync(restaurantId, userId, false);
    }
}
